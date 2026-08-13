using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor.Api
{
    /// <summary>
    /// Replays a previously-prefetched cache. All reads are local: the pipeline
    /// runs unmodified — the trick is that GetImageFillsAsync returns
    /// <c>file://</c> URIs that DownloadBytesAsync resolves to local fill bytes.
    ///
    /// Cache misses throw FileNotFoundException so failures surface early in
    /// DownloadDocumentStep / DownloadSpritesStep rather than later as parsing
    /// or rendering errors.
    /// </summary>
    public class OfflineFigmaApiClient : IFigmaApiClient
    {
        private readonly FigmaCacheStore _store;

        public OfflineFigmaApiClient(FigmaCacheStore store)
        {
            _store = store ?? throw new System.ArgumentNullException(nameof(store));
        }

        public Task<string> GetDocumentJsonAsync(string fileKey, CancellationToken token)
        {
            EnsureFileKey(fileKey);
            return Task.FromResult(_store.ReadDocumentJson());
        }

        /// <summary>
        /// Per-node render endpoint (/v1/images). Returns <c>file://</c> URIs for any
        /// requested node ids that have a cached render on disk (render/&lt;nodeId&gt;.png),
        /// so DownloadSpritesStep's VectorPngFallback can resolve them offline. Misses are
        /// silently skipped — mirrors the live API contract (and GetImageFillsAsync here).
        /// </summary>
        public Task<Dictionary<string, string>> GetImageUrlsAsync(string fileKey,
            string[] nodeIds, float scale, CancellationToken token)
        {
            EnsureFileKey(fileKey);
            var result = new Dictionary<string, string>();
            if (nodeIds != null)
            {
                foreach (var id in nodeIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    if (_store.HasRender(id))
                        result[id] = FigmaCacheStore.ToFileUri(_store.RenderPath(id));
                }
            }
            return Task.FromResult(result);
        }

        public Task<Dictionary<string, string>> GetImageFillsAsync(string fileKey,
            CancellationToken token)
        {
            EnsureFileKey(fileKey);
            var result = new Dictionary<string, string>();
            // Only surface refs we actually have bytes for: matches live API contract
            // (missing keys are silently skipped by DownloadSpritesStep).
            foreach (var kvp in _store.ReadImageFills())
            {
                if (_store.HasFill(kvp.Key))
                    result[kvp.Key] = FigmaCacheStore.ToFileUri(_store.FillPath(kvp.Key));
            }
            return Task.FromResult(result);
        }

        public Task<byte[]> DownloadBytesAsync(string url, CancellationToken token)
        {
            if (!FigmaCacheStore.TryGetLocalPathFromCacheUri(url, out var path))
                throw new System.InvalidOperationException(
                    $"OfflineFigmaApiClient only handles file:// URIs; got '{url}'. " +
                    "Did the cache get out of sync?");
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"F2U cache miss: bytes not found at '{path}'.", path);
            return Task.FromResult(File.ReadAllBytes(path));
        }

        private void EnsureFileKey(string fileKey)
        {
            if (!string.Equals(fileKey, _store.FileKey, System.StringComparison.Ordinal))
                throw new System.InvalidOperationException(
                    $"OfflineFigmaApiClient bound to fileKey '{_store.FileKey}' but pipeline requested '{fileKey}'.");
        }
    }
}
