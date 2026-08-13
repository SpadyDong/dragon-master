using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Figma2Unity.Editor.Api
{
    /// <summary>
    /// Minimal HTTP client for Figma REST API: `/v1/files/:key` + `/v1/images/:key`
    /// + `/v1/files/:key/images`. Personal Access Token is read from F2UTokenStorage
    /// by F2UMainWindow before run.
    /// </summary>
    public class FigmaApiClient : IFigmaApiClient
    {
        private const string ApiBase = "https://api.figma.com/v1";

        // Transient-failure handling for /v1/images (500/502/503/504/429): retry the same
        // batch with exponential backoff before falling back to split-and-skip.
        private const int MaxTransientRetries = 3;
        private const int BaseRetryDelayMs = 750;

        // Rate-limit (429) is handled separately: splitting a 429 batch would *multiply*
        // the request count and make throttling worse, so we only back off (honoring
        // Retry-After when present) and retry the same batch, then skip it.
        private const int MaxRateLimitRetries = 6;
        private const int RateLimitBaseDelayMs = 2000;

        // Upper bound on any single backoff. Figma's Retry-After is occasionally a far-future
        // HTTP date (or the local clock is skewed), which made `date - now` compute an absurd
        // multi-day delay and effectively hang the prefetch on Task.Delay. Clamp every computed
        // delay to this ceiling so a bad Retry-After degrades to a normal short retry.
        private const int MaxRetryAfterMs = 60_000;

        // Small pause between successful top-level batches to stay under Figma's rate limit.
        private const int InterBatchDelayMs = 250;

        // Single shared HttpClient for the whole editor session. A new-per-instance client
        // would leak sockets across repeated imports (each socket lingers in TIME_WAIT).
        // The token is NOT set as a default header — Request() attaches X-Figma-Token per
        // request — so one shared client safely serves every FigmaApiClient / token.
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = System.TimeSpan.FromMinutes(5),
        };

        private readonly string _token;

        public FigmaApiClient(string personalAccessToken)
        {
            _token = personalAccessToken ?? "";
        }

        private HttpRequestMessage Request(System.Net.Http.HttpMethod method, string url)
        {
            var req = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(_token))
                req.Headers.Add("X-Figma-Token", _token);
            return req;
        }

        public async Task<string> GetDocumentJsonAsync(string fileKey, CancellationToken token)
        {
            // geometry=paths makes Figma include each node's full `relativeTransform` (2x3 affine).
            // Without it the response carries only the scalar `rotation` field, which CANNOT
            // distinguish a horizontal/vertical flip (negative-determinant matrix) from a true
            // 180° rotation — both report rotation≈π. The matrix is required so flipped nodes
            // (e.g. mirrored car/photo fills) render mirrored instead of upside-down.
            using var req = Request(HttpMethod.Get, $"{ApiBase}/files/{fileKey}?geometry=paths");
            using var resp = await _http.SendAsync(req, token);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new System.Exception($"Figma API {(int)resp.StatusCode}: {body}");
            return body;
        }

        /// <summary>
        /// GET /v1/images — returns nodeId → CDN URL map.
        ///
        /// Two safety nets:
        ///   1. URL length: ids are batched (default 20) so URL stays well below 2KB.
        ///   2. Figma render timeout (HTTP 400 "Render timeout, try requesting fewer or
        ///      smaller images"): if a batch fails with that error, split it in half and
        ///      retry recursively until either it succeeds or batches of size 1 still fail
        ///      (then the offending node is logged and skipped).
        /// </summary>
        public async Task<Dictionary<string, string>> GetImageUrlsAsync(string fileKey,
            string[] nodeIds, float scale, CancellationToken token)
            => await GetImageUrlsAsync(fileKey, nodeIds, scale, token, null);

        /// <param name="onProgress">
        /// Invoked after each top-level batch with (completedNodeCount, totalNodeCount).
        /// Lets the caller refresh a cancelable progress bar — without this the bar is
        /// never redrawn during a long render fetch, so its Cancel button is never polled.
        /// </param>
        public async Task<Dictionary<string, string>> GetImageUrlsAsync(string fileKey,
            string[] nodeIds, float scale, CancellationToken token, System.Action<int, int> onProgress)
        {
            var result = new Dictionary<string, string>();
            if (nodeIds == null || nodeIds.Length == 0) return result;

            const int InitialBatchSize = 20;
            int total = nodeIds.Length;
            onProgress?.Invoke(0, total);
            for (int offset = 0; offset < nodeIds.Length; offset += InitialBatchSize)
            {
                token.ThrowIfCancellationRequested();
                int count = System.Math.Min(InitialBatchSize, nodeIds.Length - offset);
                var batch = new string[count];
                System.Array.Copy(nodeIds, offset, batch, 0, count);
                await FetchBatchRecursive(fileKey, batch, scale, result, token);
                onProgress?.Invoke(System.Math.Min(offset + count, total), total);
                if (offset + count < nodeIds.Length)
                    await Task.Delay(InterBatchDelayMs, token);
            }
            return result;
        }

        private async Task FetchBatchRecursive(string fileKey, string[] batch, float scale,
            Dictionary<string, string> result, CancellationToken token, int attempt = 0)
        {
            if (batch == null || batch.Length == 0) return;

            var ids = string.Join(",", batch);
            var url = $"{ApiBase}/images/{fileKey}?ids={System.Uri.EscapeDataString(ids)}&format=png&scale={scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            using var req = Request(HttpMethod.Get, url);
            using var resp = await _http.SendAsync(req, token);
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
            {
                foreach (var kvp in ParseImagesResponse(body))
                    result[kvp.Key] = kvp.Value;
                return;
            }

            // Figma render timeout → split + retry per official guidance.
            bool isRenderTimeout = (int)resp.StatusCode == 400
                && body != null
                && (body.IndexOf("Render timeout", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || body.IndexOf("fewer", System.StringComparison.OrdinalIgnoreCase) >= 0);

            int status = (int)resp.StatusCode;

            // Rate limit (429): back off only — NEVER split, because splitting multiplies
            // the request count and makes throttling worse. Honor Retry-After when present.
            if (status == 429)
            {
                if (attempt < MaxRateLimitRetries)
                {
                    int delayMs = RetryAfterMs(resp, RateLimitBaseDelayMs * (attempt + 1));
                    UnityEngine.Debug.LogWarning(
                        $"[Figma2Unity] /v1/images 429 (rate limited) on batch of {batch.Length} " +
                        $"(attempt {attempt + 1}/{MaxRateLimitRetries}); backing off {delayMs}ms.");
                    await Task.Delay(delayMs, token);
                    await FetchBatchRecursive(fileKey, batch, scale, result, token, attempt + 1);
                    return;
                }
                UnityEngine.Debug.LogWarning(
                    $"[Figma2Unity] /v1/images 429 persisted after {MaxRateLimitRetries} retries; " +
                    $"skipping {batch.Length} node(s).");
                return;
            }

            // Server errors (500/502/503/504): retry same batch, then split, then skip.
            bool isServerError = status == 500 || status == 502 || status == 503 || status == 504;

            if (isServerError && attempt < MaxTransientRetries)
            {
                int delayMs = BaseRetryDelayMs * (1 << attempt);
                UnityEngine.Debug.LogWarning(
                    $"[Figma2Unity] /v1/images {status} on batch of {batch.Length} " +
                    $"(attempt {attempt + 1}/{MaxTransientRetries}); retrying in {delayMs}ms.");
                await Task.Delay(delayMs, token);
                await FetchBatchRecursive(fileKey, batch, scale, result, token, attempt + 1);
                return;
            }

            if ((isRenderTimeout || isServerError) && batch.Length > 1)
            {
                UnityEngine.Debug.LogWarning(
                    $"[Figma2Unity] /v1/images {status} on batch of {batch.Length}; splitting and retrying.");
                int mid = batch.Length / 2;
                var left = new string[mid];
                var right = new string[batch.Length - mid];
                System.Array.Copy(batch, 0, left, 0, mid);
                System.Array.Copy(batch, mid, right, 0, batch.Length - mid);
                await FetchBatchRecursive(fileKey, left, scale, result, token);
                await FetchBatchRecursive(fileKey, right, scale, result, token);
                return;
            }

            if ((isRenderTimeout || isServerError) && batch.Length == 1)
            {
                UnityEngine.Debug.LogWarning(
                    $"[Figma2Unity] /v1/images {status} on single node '{batch[0]}' — skipping.");
                return;
            }

            throw new System.Exception($"Figma API {(int)resp.StatusCode}: {body}");
        }

        /// <summary>Retry-After header (delta seconds or HTTP date) → ms, else fallback.</summary>
        private static int RetryAfterMs(System.Net.Http.HttpResponseMessage resp, int fallbackMs)
        {
            try
            {
                var ra = resp.Headers.RetryAfter;
                if (ra != null)
                {
                    // Compute in double to avoid int overflow on absurd/far-future values,
                    // then clamp to [fallbackMs, MaxRetryAfterMs] so a bad Retry-After (or a
                    // skewed local clock) can never produce a multi-day Task.Delay hang.
                    if (ra.Delta.HasValue)
                        return ClampDelay(ra.Delta.Value.TotalMilliseconds, fallbackMs);
                    if (ra.Date.HasValue)
                    {
                        var ms = (ra.Date.Value - System.DateTimeOffset.UtcNow).TotalMilliseconds;
                        if (ms > 0) return ClampDelay(ms, fallbackMs);
                    }
                }
            }
            catch { /* fall through to fallback */ }
            return fallbackMs;
        }

        /// <summary>
        /// Clamp a desired delay (ms, may be huge/garbage) into [fallbackMs, MaxRetryAfterMs].
        /// Guards against int overflow and pathological Retry-After values.
        /// </summary>
        private static int ClampDelay(double desiredMs, int fallbackMs)
        {
            if (double.IsNaN(desiredMs) || desiredMs < fallbackMs) return fallbackMs;
            if (desiredMs > MaxRetryAfterMs) return MaxRetryAfterMs;
            return (int)desiredMs;
        }

        private static Dictionary<string, string> ParseImagesResponse(string json)
        {
            var result = new Dictionary<string, string>();
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                var images = obj["images"] as Newtonsoft.Json.Linq.JObject;
                if (images == null) return result;
                foreach (var kvp in images)
                {
                    if (kvp.Value != null && kvp.Value.Type == Newtonsoft.Json.Linq.JTokenType.String)
                        result[kvp.Key] = kvp.Value.ToString();
                }
            }
            catch { /* return empty */ }
            return result;
        }

        /// <summary>
        /// GET /v1/files/:key/images — returns imageRef → CDN URL for images uploaded into the
        /// file (IMAGE Paint fills). This is the correct channel for fill images: it yields the
        /// original uploaded asset, NOT a node render (which would bake in crop/corner-radius/
        /// scale) and lets callers dedup by imageRef. v1 doesn't consume this yet — the
        /// Prefetch command uses it to fully populate the offline cache for v2.
        /// </summary>
        public async Task<Dictionary<string, string>> GetImageFillsAsync(string fileKey,
            CancellationToken token)
        {
            var result = new Dictionary<string, string>();
            using var req = Request(HttpMethod.Get, $"{ApiBase}/files/{fileKey}/images");
            using var resp = await _http.SendAsync(req, token);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new System.Exception($"Figma API {(int)resp.StatusCode}: {body}");
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(body);
                var meta = obj["meta"]?["images"] as Newtonsoft.Json.Linq.JObject;
                if (meta != null)
                {
                    foreach (var kvp in meta)
                    {
                        if (kvp.Value != null && kvp.Value.Type == Newtonsoft.Json.Linq.JTokenType.String)
                            result[kvp.Key] = kvp.Value.ToString();
                    }
                }
            }
            catch { /* return what we have */ }
            return result;
        }

        public async Task<byte[]> DownloadBytesAsync(string url, CancellationToken token)
        {
            using var resp = await _http.GetAsync(url, token);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsByteArrayAsync();
        }
    }
}
