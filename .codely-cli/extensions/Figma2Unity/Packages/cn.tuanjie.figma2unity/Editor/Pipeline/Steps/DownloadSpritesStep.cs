using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// Step 13: Download sprites for IMAGE Paint nodes (Design.md §附录 D.1).
    /// v1 only handles IMAGE Paint; v2 will append VectorPngFallback.
    ///
    /// Strategy:
    ///   1. Batch /v1/images calls (handled by FigmaApiClient.GetImageUrlsAsync).
    ///   2. Download PNG bytes in parallel (bounded concurrency) — NO AssetDatabase
    ///      calls during downloads, so Unity main thread stays responsive and the
    ///      progress bar keeps updating.
    ///   3. Import all PNGs in one StartAssetEditing/StopAssetEditing batch.
    /// </summary>
    public class DownloadSpritesStep : PipelineStep
    {
        public override string DisplayName => "Download Sprites";
        public override float ProgressWeight => 4f;

        // Conservative concurrency: high enough to mask latency, low enough to avoid
        // Figma rate-limiting / running out of file handles.
        private const int MaxConcurrentDownloads = 6;

        public override async Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.AllNodes == null || ctx.ApiClient == null)
                return StepResult.Ok();

            var imageRefs = new List<(FObject Node, string Ref)>();
            foreach (var n in ctx.AllNodes)
            {
                if (n.Graphic != null && n.Graphic.HasImageFill && n.Fills != null)
                {
                    // Pick the TOPMOST VISIBLE image fill. Figma renders fills bottom→top, so
                    // the LAST visible IMAGE fill is the one actually shown on top; earlier and
                    // hidden (visible:false) fills must be ignored. The previous code took the
                    // first IMAGE fill regardless of visibility — so a common Figma pattern
                    // (keep the original image hidden underneath a processed/filtered copy)
                    // imported the wrong, often fully-opaque image, making the node look
                    // "pasted on" instead of correctly blended with the background.
                    Paint paint = null;
                    foreach (var f in n.Fills)
                    {
                        if (f != null && f.Visible && f.Type == PaintType.IMAGE
                            && !string.IsNullOrEmpty(f.ImageRef))
                            paint = f; // keep last → topmost visible image fill
                    }
                    if (paint != null)
                        imageRefs.Add((n, paint.ImageRef));
                }
            }

            // v2: collect VECTOR / BOOLEAN_OPERATION nodes that need the /v1/images
            // node-render fallback. VectorRenderSelector is the shared source of truth
            // (also used by F2UPrefetchCommand) — it scopes to screen-subtree icons that
            // actually get a GameObject, never the whole flattened tree, so large design
            // systems don't request tens of thousands of vector renders.
            var vectorNodes = new List<FObject>();
            foreach (var id in Rendering.VectorRenderSelector.SelectNodeIds(ctx))
            {
                if (!ctx.NodeMap.TryGetValue(id, out var n) || n == null) continue;
                if (!string.IsNullOrEmpty(ctx.GetSpritePath(n))) continue;
                vectorNodes.Add(n);
            }

            if (imageRefs.Count == 0 && vectorNodes.Count == 0) return StepResult.Ok();

            // imageRef -> CDN URL via /v1/files/:key/images (original uploaded asset, not a node render).
            // Skip when no IMAGE Paint nodes exist — vectors-only documents must not error here.
            Dictionary<string, string> refUrls = new Dictionary<string, string>();
            if (imageRefs.Count > 0)
            {
                try
                {
                    refUrls = await ctx.ApiClient.GetImageFillsAsync(ctx.FileId, token);
                }
                catch (System.Exception ex)
                {
                    return StepResult.Fail("Image fill URLs fetch failed: " + ex.Message);
                }
                UnityEngine.Debug.Log($"[Figma2Unity] DownloadSprites: {refUrls.Count} imageRef URLs returned by Figma.");
            }

            string spriteFolder = ctx.Config != null ? ctx.Config.SpriteFolder : "Assets/Sprites";
            Directory.CreateDirectory(spriteFolder);

            var distinctRefs = imageRefs.Select(x => x.Ref).Distinct().ToList();
            UnityEngine.Debug.Log($"[Figma2Unity] DownloadSprites: {imageRefs.Count} IMAGE Paint nodes, {distinctRefs.Count} distinct imageRefs.");

            // Phase 1: download each distinct imageRef once (NO AssetDatabase calls here).
            var refToPath = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            int completed = 0;
            int total = distinctRefs.Count;
            using (var gate = new SemaphoreSlim(MaxConcurrentDownloads))
            {
                var tasks = new List<Task>();
                foreach (var imageRef in distinctRefs)
                {
                    if (!refUrls.TryGetValue(imageRef, out var url) || string.IsNullOrEmpty(url))
                    {
                        UnityEngine.Debug.LogWarning($"[Figma2Unity] No URL for imageRef '{imageRef}' — skipping.");
                        continue;
                    }
                    var path = $"{spriteFolder}/{SanitizeFileName(imageRef)}.png";
                    tasks.Add(DownloadOne(imageRef, path, url, ctx, gate, refToPath, () =>
                    {
                        int done = Interlocked.Increment(ref completed);
                        ctx.CurrentStepName = $"Download Sprites ({done}/{total})";
                    }, token));
                }
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (System.OperationCanceledException)
                {
                    return StepResult.Fail("Canceled");
                }
            }

            // Phase 2: import each distinct PNG with Sprite importer settings so
            // ApplySpritesStep's LoadAssetAtPath<Sprite> succeeds. Two-step per asset:
            //   1. ImportAsset(ForceSynchronousImport) — Unity creates a default
            //      TextureImporter (Texture2D) and serializes the .meta to disk.
            //   2. Patch the importer to Sprite/Single, mipmaps off; SaveAndReimport.
            // We deliberately skip StartAssetEditing/StopAssetEditing batching here —
            // mixing the batch with ForceSynchronousImport is undocumented territory and
            // the previous batch-only code is exactly why all 45 PNGs in v1 were stranded
            // as Texture2D (Audit Bug 1).
            if (refToPath.Count > 0)
            {
                ConfigureAsSpritesBatch(refToPath.Values);
                foreach (var (node, imageRef) in imageRefs)
                {
                    if (refToPath.TryGetValue(imageRef, out var path))
                        ctx.SetSpritePath(node, path);
                }
            }

            UnityEngine.Debug.Log($"[Figma2Unity] DownloadSprites: {refToPath.Count}/{total} images downloaded + imported.");

            // ---------- v2 Phase 3: VectorPngFallback (/v1/images node renders) ----------
            if (vectorNodes.Count > 0)
            {
                float scale = ctx.Config != null ? UnityEngine.Mathf.Max(1f, ctx.Config.SpriteScale) : 2f;
                string[] vectorIds = vectorNodes.Select(n => n.Id).ToArray();
                Dictionary<string, string> vectorUrls;
                try
                {
                    vectorUrls = await ctx.ApiClient.GetImageUrlsAsync(ctx.FileId, vectorIds, scale, token);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[Figma2Unity] VectorPngFallback URL fetch failed: {ex.Message}");
                    vectorUrls = new Dictionary<string, string>();
                }

                var vectorIdToPath = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
                using (var gate = new SemaphoreSlim(MaxConcurrentDownloads))
                {
                    var tasks = new List<Task>();
                    foreach (var node in vectorNodes)
                    {
                        if (!vectorUrls.TryGetValue(node.Id, out var url) || string.IsNullOrEmpty(url))
                            continue;
                        var path = $"{spriteFolder}/vector_{SanitizeFileName(node.Id)}.png";
                        var capturedId = node.Id;
                        tasks.Add(DownloadOne(capturedId, path, url, ctx, gate, vectorIdToPath, null, token));
                    }
                    try { await Task.WhenAll(tasks); }
                    catch (System.OperationCanceledException) { return StepResult.Fail("Canceled"); }
                }

                if (vectorIdToPath.Count > 0)
                {
                    ConfigureAsSpritesBatch(vectorIdToPath.Values);
                    foreach (var node in vectorNodes)
                    {
                        if (vectorIdToPath.TryGetValue(node.Id, out var path))
                            ctx.SetSpritePath(node, path);
                    }
                }

                UnityEngine.Debug.Log($"[Figma2Unity] DownloadSprites: VectorPngFallback rendered {vectorIdToPath.Count}/{vectorNodes.Count} nodes.");
            }

            return StepResult.Ok();
        }

        private static async Task DownloadOne(string imageRef, string path, string url,
            F2UContext ctx, SemaphoreSlim gate,
            System.Collections.Concurrent.ConcurrentDictionary<string, string> output,
            System.Action onProgress, CancellationToken token)
        {
            await gate.WaitAsync(token);
            try
            {
                byte[] bytes;
                try
                {
                    bytes = await ctx.ApiClient.DownloadBytesAsync(url, token);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[Figma2Unity] Image download failed for ref {imageRef}: {ex.Message}");
                    onProgress?.Invoke();
                    return;
                }
                File.WriteAllBytes(path, bytes);
                output[imageRef] = path;
                onProgress?.Invoke();
            }
            finally
            {
                gate.Release();
            }
        }

        private static string SanitizeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "node";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        /// <summary>
        /// Bulk-import PNGs at <paramref name="assetPaths"/> as Sprite/Single. This replaces the
        /// old per-asset ConfigureAsSprite which ran TWO synchronous reimports per file
        /// (ImportAsset(ForceSynchronousImport) + SaveAndReimport). On large documents (2000+
        /// sprites) that meant thousands of individual Asset Pipeline Refreshes — minutes of a
        /// frozen editor that looked like a hang.
        ///
        /// Strategy that stays correct (avoids the v1 "stranded as Texture2D" bug):
        ///   1. One AssetDatabase.Refresh() imports every new PNG as a default Texture2D and
        ///      creates its importer + .meta — so GetAtPath() below is non-null.
        ///   2. StartAssetEditing/StopAssetEditing brackets a single batched reimport: we only
        ///      patch importers that actually need changing, then SaveAndReimport() is deferred
        ///      and flushed once at StopAssetEditing instead of once per asset.
        /// </summary>
        private static void ConfigureAsSpritesBatch(System.Collections.Generic.IEnumerable<string> assetPaths)
        {
            var paths = assetPaths.Distinct().ToList();
            if (paths.Count == 0) return;

            // Make sure every freshly-written PNG has an importer before we touch settings.
            AssetDatabase.Refresh();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var assetPath in paths)
                {
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null)
                    {
                        // Refresh didn't pick it up (rare) — force a single import so the
                        // importer exists, then patch it.
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                        importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (importer == null) continue;
                    }
                    if (ApplySpriteImporterSettings(importer))
                        importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Force <paramref name="importer"/> into Sprite/Single mode with mipmaps off. Returns
        /// true if anything changed (so callers can skip a no-op reimport, keeping pipeline
        /// re-runs idempotent). Without Sprite/Single, AssetDatabase.LoadAssetAtPath&lt;Sprite&gt;
        /// returns null in ApplySpritesStep.
        /// </summary>
        private static bool ApplySpriteImporterSettings(TextureImporter importer)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            // Mipmaps off for UI sprites: saves memory and avoids blurry sub-pixel edges.
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }
            return dirty;
        }
    }
}
