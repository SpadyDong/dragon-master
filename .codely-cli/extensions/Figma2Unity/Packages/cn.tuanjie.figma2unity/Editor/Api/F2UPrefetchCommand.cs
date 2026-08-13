using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Api;
using Figma2Unity.Editor.Pipeline.Steps;
using Figma2Unity.Editor.PrototypeFlow;
using Figma2Unity.Editor.Rendering;
using Figma2Unity.Editor.Tags;
using Newtonsoft.Json.Linq;

namespace Figma2Unity.Editor
{
    /// <summary>
    /// One-shot downloader that populates a FigmaCacheStore with everything the
    /// pipeline needs to re-run offline:
    ///   1. GET /v1/files/:key         → document.json + file meta
    ///   2. GET /v1/files/:key/images  → imageRef → URL (only refs the doc references)
    ///   3. parallel-download each ref → fills/&lt;imageRef&gt;.bin
    ///   4. GET /v1/images/:key        → VECTOR/BOOLEAN_OPERATION node renders → render/&lt;nodeId&gt;.png
    ///   5. meta.json with version / lastModified / counts.
    ///
    /// IMAGE Paint nodes can share one upload, so we dedup at the imageRef layer
    /// (mirrors DownloadSpritesStep). Node renders are keyed by node id.
    /// </summary>
    public class F2UPrefetchCommand
    {
        public class Progress
        {
            public string Phase;
            public int Current;
            public int Total;
        }

        public class Result
        {
            public string CacheDir;
            public int FillCount;
            public int RenderCount;
            public string Version;
            public string FileName;
            public bool Success;
            public string FailureReason;
            public override string ToString() =>
                Success
                    ? $"OK | fills={FillCount} renders={RenderCount} version={Version} → {CacheDir}"
                    : $"FAIL | {FailureReason}";
        }

        private const int MaxConcurrentDownloads = 6;

        private readonly IFigmaApiClient _client;
        private readonly FigmaCacheStore _store;

        public F2UPrefetchCommand(IFigmaApiClient client, FigmaCacheStore store)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<Result> RunAsync(IProgress<Progress> progress, CancellationToken token)
            => await RunAsync(progress, null, token);

        public async Task<Result> RunAsync(IProgress<Progress> progress, F2UConfig config, CancellationToken token)
        {
            try
            {
                _store.EnsureDirectories();

                // 1) document.json
                Report(progress, "Downloading document", 0, 5);
                var docJson = await _client.GetDocumentJsonAsync(_store.FileKey, token);
                _store.WriteDocumentJson(docJson);
                var (fileName, version, lastModified) = ReadDocMeta(docJson);

                // 2) imageRefs referenced by the doc
                Report(progress, "Scanning IMAGE paints", 1, 5);
                var referenced = CollectImageRefs(docJson);

                Dictionary<string, string> fillUrls = new Dictionary<string, string>();
                if (referenced.Count > 0)
                {
                    Dictionary<string, string> allFills;
                    try { allFills = await _client.GetImageFillsAsync(_store.FileKey, token); }
                    catch (Exception ex)
                    {
                        return new Result { Success = false, FailureReason = "GetImageFillsAsync failed: " + ex.Message };
                    }
                    foreach (var r in referenced)
                    {
                        if (allFills.TryGetValue(r, out var url) && !string.IsNullOrEmpty(url))
                            fillUrls[r] = url;
                    }
                    _store.WriteImageFills(fillUrls);
                }

                // 3) bytes
                int saved = await DownloadFillsAsync(fillUrls, progress, token);

                // 4) VECTOR / BOOLEAN_OPERATION node renders (/v1/images)
                int renders = await DownloadRendersAsync(docJson, config, progress, token);

                // 5) meta.json
                Report(progress, "Writing meta", 4, 5);
                _store.WriteMeta(new FigmaCacheMeta
                {
                    FileKey = _store.FileKey,
                    FileName = fileName,
                    Version = version,
                    LastModified = lastModified,
                    FillCount = saved,
                    RenderCount = renders,
                    PrefetchedAtUtc = DateTime.UtcNow.ToString("o"),
                });

                Report(progress, "Done", 5, 5);
                return new Result
                {
                    Success = true,
                    CacheDir = _store.FileRoot,
                    FillCount = saved,
                    RenderCount = renders,
                    Version = version,
                    FileName = fileName,
                };
            }
            catch (OperationCanceledException)
            {
                return new Result { Success = false, FailureReason = "Canceled" };
            }
            catch (Exception ex)
            {
                return new Result { Success = false, FailureReason = ex.Message };
            }
        }

        private async Task<int> DownloadFillsAsync(Dictionary<string, string> urls,
            IProgress<Progress> progress, CancellationToken token)
        {
            if (urls == null || urls.Count == 0) return 0;
            int total = urls.Count;
            int completed = 0;
            int saved = 0;

            using var gate = new SemaphoreSlim(MaxConcurrentDownloads);
            var tasks = new List<Task>(total);
            foreach (var kvp in urls)
            {
                var imageRef = kvp.Key;
                var url = kvp.Value;
                tasks.Add(DownloadOneAsync(imageRef, url, gate, () =>
                {
                    int done = Interlocked.Increment(ref completed);
                    Report(progress, $"Downloading fills ({done}/{total})", 2, 4);
                }, ok =>
                {
                    if (ok) Interlocked.Increment(ref saved);
                }, token));
            }
            await Task.WhenAll(tasks);
            return saved;
        }

        /// <summary>
        /// Phase 4: render VECTOR / BOOLEAN_OPERATION nodes through /v1/images and cache
        /// the PNG bytes under render/&lt;nodeId&gt;.png so the offline client can replay them.
        /// Reuses the import pipeline's parse → tag → screen-identify path and
        /// <see cref="VectorRenderSelector"/> so the cached set is exactly the icons the
        /// import will request (no white-box drift, and no rendering the whole file's tens
        /// of thousands of vectors).
        /// </summary>
        private async Task<int> DownloadRendersAsync(string docJson, F2UConfig config,
            IProgress<Progress> progress, CancellationToken token)
        {
            if (config != null && !config.EnableVectorPngFallback) return 0;

            var nodeIds = SelectRenderNodeIds(docJson, config);
            if (nodeIds.Count == 0) return 0;

            float scale = config != null ? Math.Max(1f, config.SpriteScale) : 2f;
            var idArray = nodeIds.ToArray();
            int totalIds = idArray.Length;
            Report(progress, $"Fetching node render URLs (0/{totalIds})", 3, 5);

            // Chunk the id list and fetch a chunk at a time so the cancelable progress bar
            // is redrawn between chunks (otherwise its Cancel button is never polled during
            // a long render fetch). Per-chunk 429/500 retry + split lives in FigmaApiClient.
            const int UrlChunkSize = 100;
            var renderUrls = new Dictionary<string, string>();
            for (int offset = 0; offset < totalIds; offset += UrlChunkSize)
            {
                token.ThrowIfCancellationRequested();
                int count = Math.Min(UrlChunkSize, totalIds - offset);
                var chunk = new string[count];
                Array.Copy(idArray, offset, chunk, 0, count);
                try
                {
                    var chunkUrls = await _client.GetImageUrlsAsync(_store.FileKey, chunk, scale, token);
                    foreach (var kvp in chunkUrls) renderUrls[kvp.Key] = kvp.Value;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[Figma2Unity] Prefetch render URL chunk failed: {ex.Message}");
                }
                Report(progress, $"Fetching node render URLs ({Math.Min(offset + count, totalIds)}/{totalIds})", 3, 5);
            }

            if (renderUrls == null || renderUrls.Count == 0) return 0;

            int total = renderUrls.Count;
            int completed = 0;
            int saved = 0;

            using var gate = new SemaphoreSlim(MaxConcurrentDownloads);
            var tasks = new List<Task>(total);
            foreach (var kvp in renderUrls)
            {
                var nodeId = kvp.Key;
                var url = kvp.Value;
                if (string.IsNullOrEmpty(url)) continue;
                tasks.Add(DownloadRenderOneAsync(nodeId, url, gate, () =>
                {
                    int done = Interlocked.Increment(ref completed);
                    Report(progress, $"Downloading renders ({done}/{total})", 3, 5);
                }, ok =>
                {
                    if (ok) Interlocked.Increment(ref saved);
                }, token));
            }
            await Task.WhenAll(tasks);
            return saved;
        }

        private async Task DownloadRenderOneAsync(string nodeId, string url, SemaphoreSlim gate,
            Action onProgress, Action<bool> onResult, CancellationToken token)
        {
            await gate.WaitAsync(token);
            try
            {
                byte[] bytes;
                try { bytes = await _client.DownloadBytesAsync(url, token); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[Figma2Unity] Prefetch render download failed for {nodeId}: {ex.Message}");
                    onResult(false);
                    onProgress();
                    return;
                }
                _store.WriteRender(nodeId, bytes);
                onResult(true);
                onProgress();
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task DownloadOneAsync(string imageRef, string url, SemaphoreSlim gate,
            Action onProgress, Action<bool> onResult, CancellationToken token)
        {
            await gate.WaitAsync(token);
            try
            {
                byte[] bytes;
                try { bytes = await _client.DownloadBytesAsync(url, token); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[Figma2Unity] Prefetch download failed for {imageRef}: {ex.Message}");
                    onResult(false);
                    onProgress();
                    return;
                }
                _store.WriteFill(imageRef, bytes);
                onResult(true);
                onProgress();
            }
            finally
            {
                gate.Release();
            }
        }

        private static (string name, string version, string lastModified) ReadDocMeta(string docJson)
        {
            try
            {
                var obj = JObject.Parse(docJson);
                return (
                    (string)obj["name"] ?? "",
                    (string)obj["version"] ?? "",
                    (string)obj["lastModified"] ?? ""
                );
            }
            catch { return ("", "", ""); }
        }

        /// <summary>
        /// Walks the raw document JSON and collects every distinct imageRef referenced
        /// by an IMAGE Paint. Many nodes share one upload, so dedup happens here.
        /// </summary>
        internal static HashSet<string> CollectImageRefs(string docJson)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(docJson)) return result;
            JObject root;
            try { root = JObject.Parse(docJson); }
            catch { return result; }
            var doc = root["document"];
            if (doc == null) return result;
            Walk(doc, result);
            return result;
        }

        private static void Walk(JToken node, HashSet<string> outRefs)
        {
            if (node == null || node.Type != JTokenType.Object) return;
            var fills = node["fills"] as JArray;
            if (fills != null)
            {
                foreach (var f in fills)
                {
                    if ((string)f?["type"] == "IMAGE")
                    {
                        var imageRef = (string)f?["imageRef"];
                        if (!string.IsNullOrEmpty(imageRef)) outRefs.Add(imageRef);
                    }
                }
            }
            var children = node["children"] as JArray;
            if (children != null)
            {
                foreach (var c in children) Walk(c, outRefs);
            }
        }

        /// <summary>
        /// Reproduces the import pipeline's early stages (parse → flatten → graphics →
        /// tags → screen identify) on the raw document JSON, then asks
        /// <see cref="VectorRenderSelector"/> which node ids need a /v1/images render. This
        /// guarantees the prefetched render set equals the set the offline import requests.
        /// </summary>
        internal static List<string> SelectRenderNodeIds(string docJson, F2UConfig config)
        {
            if (string.IsNullOrEmpty(docJson)) return new List<string>();

            FObject virtualPage;
            try { virtualPage = new FigmaDataParser().ParseJson(docJson, config); }
            catch { return new List<string>(); }
            if (virtualPage == null) return new List<string>();

            var flat = new List<FObject>();
            var map = new Dictionary<string, FObject>();
            FlattenTreeStep.Flatten(virtualPage, flat, map);

            foreach (var fobj in flat)
                fobj.Graphic = ComputeGraphicsStep.Compute(fobj);

            var setter = new TagSetter(config);
            setter.SetTagsByFigmaType(flat);
            setter.SetSmartTags(flat);
            setter.SetIgnoredObjects(flat);

            var ctx = new F2UContext
            {
                Config = config,
                VirtualPage = virtualPage,
                AllNodes = flat,
                NodeMap = map,
            };
            ScreenIdentifier.Identify(ctx);

            return VectorRenderSelector.SelectNodeIds(ctx);
        }

        private static void Report(IProgress<Progress> progress, string phase, int current, int total)
            => progress?.Report(new Progress { Phase = phase, Current = current, Total = total });
    }
}
