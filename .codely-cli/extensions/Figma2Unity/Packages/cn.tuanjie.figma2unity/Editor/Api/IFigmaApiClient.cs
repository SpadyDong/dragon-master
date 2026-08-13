using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor.Api
{
    /// <summary>
    /// Abstraction over Figma data sources so the pipeline can run either live
    /// (FigmaApiClient → REST) or offline (OfflineFigmaApiClient → on-disk cache).
    ///
    /// Methods are async + CancellationToken-aware to keep the pipeline contract
    /// stable across implementations.
    /// </summary>
    public interface IFigmaApiClient
    {
        /// <summary>GET /v1/files/:key — raw document JSON.</summary>
        Task<string> GetDocumentJsonAsync(string fileKey, CancellationToken token);

        /// <summary>
        /// POST/GET /v1/images/:key — node-id → CDN PNG URL map for the requested scale.
        /// Implementations may batch internally.
        /// </summary>
        Task<Dictionary<string, string>> GetImageUrlsAsync(string fileKey,
            string[] nodeIds, float scale, CancellationToken token);

        /// <summary>
        /// GET /v1/files/:key/images — imageRef → original CDN URL map (no re-render).
        /// Returns empty dictionary if the file has no image fills.
        /// </summary>
        Task<Dictionary<string, string>> GetImageFillsAsync(string fileKey, CancellationToken token);

        /// <summary>Download a single binary payload from a (possibly local) URL.</summary>
        Task<byte[]> DownloadBytesAsync(string url, CancellationToken token);
    }
}
