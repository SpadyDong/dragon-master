using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Figma2Unity.Editor.Fonts
{
    /// <summary>
    /// v2 Track B: resolve TEXT-node fonts via Google Fonts CSS2, download TTF/OTF bytes,
    /// import as Unity Font assets, then call TMP_FontAsset.CreateFontAsset to produce the
    /// SDF asset DownloadFontsStep assigns to TMP components.
    ///
    /// Pure logic (key collection / URL build / CSS parse) lives in <see cref="FontManagerCore"/>
    /// so it's Layer A testable. This class is the UnityEditor / network glue.
    /// </summary>
    public class FontManager
    {
        private readonly string _fontFolder;
        // Google Fonts CSS2 negotiates the font format from the User-Agent:
        //   • Modern browsers (Chrome/120, Firefox…) → WOFF2
        //   • Older browsers (Chrome/22–36)          → WOFF
        //   • Unrecognized / non-browser UAs         → TTF
        // Unity's FontImporter can only consume TTF/OTF — NOT WOFF/WOFF2 — so we must NOT
        // present as a real browser (empirically both Chrome/120→woff2 and Chrome/36→woff,
        // neither importable). A generic non-browser UA makes Google serve plain TTF, which
        // Unity imports directly. See Design.md "Google Fonts CSS2 endpoint … 拿 TTF".
        private const string TtfUserAgent = "Figma2Unity/1.0";

        public FontManager(string fontFolder)
        {
            _fontFolder = string.IsNullOrEmpty(fontFolder)
                ? "Assets/Fonts"
                : fontFolder;
        }

        /// <summary>
        /// Resolve every (Family, Weight, Italic) referenced by TEXT nodes in <paramref name="nodes"/>,
        /// downloading + importing what's missing. Returns a map from FontKey → TMP_FontAsset.
        /// Keys that fail any step (network / parse / import) are simply absent from the map;
        /// the caller (DownloadFontsStep) falls back to F2UConfig.FallbackFont.
        /// </summary>
        public async Task<Dictionary<FontKey, TMP_FontAsset>> ResolveFontsAsync(
            IReadOnlyList<FObject> nodes, CancellationToken token)
        {
            var result = new Dictionary<FontKey, TMP_FontAsset>();
            if (nodes == null || nodes.Count == 0) return result;

            var keys = FontManagerCore.CollectFontKeys(nodes);
            if (keys.Count == 0) return result;

            Directory.CreateDirectory(_fontFolder);

            // Reuse what's already on disk before hitting the network at all.
            var remaining = new List<FontKey>();
            foreach (var k in keys)
            {
                var existing = LoadExistingFontAsset(k);
                if (existing != null) result[k] = existing;
                else remaining.Add(k);
            }
            if (remaining.Count == 0) return result;

            var url = FontManagerCore.BuildGoogleFontsCss2Url(remaining);
            if (string.IsNullOrEmpty(url)) return result;

            string css;
            try { css = await FetchCssAsync(url, token); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Figma2Unity] Google Fonts CSS request failed: {ex.Message}");
                return result;
            }

            var urls = FontManagerCore.ParseCss2(css, remaining);
            if (urls.Count == 0) return result;

            foreach (var kv in urls)
            {
                token.ThrowIfCancellationRequested();
                var key = kv.Key;
                var fontUrl = kv.Value;
                try
                {
                    var asset = await DownloadAndImportAsync(key, fontUrl, token);
                    if (asset != null) result[key] = asset;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Figma2Unity] Font download failed for {key.FileBase}: {ex.Message}");
                }
            }

            return result;
        }

        private TMP_FontAsset LoadExistingFontAsset(FontKey key)
        {
            var assetPath = $"{_fontFolder}/{key.FileBase}_SDF.asset";
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }

        private async Task<string> FetchCssAsync(string url, CancellationToken token)
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(1);
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd(TtfUserAgent);
            using var resp = await http.SendAsync(req, token);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)resp.StatusCode}: {body}");
            return body;
        }

        private async Task<TMP_FontAsset> DownloadAndImportAsync(FontKey key, string url, CancellationToken token)
        {
            // The desktop UA in FetchCssAsync makes Google Fonts serve TTF/OTF (Unity's
            // FontImporter can't consume WOFF/WOFF2). If we still got handed a woff(2) URL,
            // bail out cleanly — writing the blob to disk and hitting Unity's importer would
            // just produce a null Font asset and a confusing log line.
            if (!string.IsNullOrEmpty(url))
            {
                var lowerUrl = url.ToLowerInvariant();
                if (lowerUrl.EndsWith(".woff2") || lowerUrl.EndsWith(".woff"))
                {
                    Debug.LogWarning(
                        $"[Figma2Unity] Skipping {key.FileBase}: Google Fonts returned a WOFF/WOFF2 URL " +
                        $"({url}) which Unity's FontImporter can't consume. Falling back to F2UConfig.FallbackFont.");
                    return null;
                }
            }

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(2);
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd(TtfUserAgent);
            using var resp = await http.SendAsync(req, token);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)resp.StatusCode}");
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            if (bytes == null || bytes.Length == 0)
                throw new Exception("Empty payload");

            var ext = GuessFontExtension(url);
            var ttfPath = $"{_fontFolder}/{key.FileBase}{ext}";
            File.WriteAllBytes(ttfPath, bytes);
            AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceSynchronousImport);

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"[Figma2Unity] Unity could not import font file {ttfPath} (unsupported format?).");
                return null;
            }

            var sdfPath = $"{_fontFolder}/{key.FileBase}_SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
            if (existing != null) return existing;

            var tmpAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (tmpAsset == null)
            {
                Debug.LogWarning($"[Figma2Unity] TMP_FontAsset.CreateFontAsset returned null for {key.FileBase}.");
                return null;
            }
            AssetDatabase.CreateAsset(tmpAsset, sdfPath);
            if (tmpAsset.material != null)
            {
                tmpAsset.material.name = $"{key.FileBase} Atlas Material";
                if (!AssetDatabase.IsSubAsset(tmpAsset.material))
                    AssetDatabase.AddObjectToAsset(tmpAsset.material, tmpAsset);
            }
            if (tmpAsset.atlasTexture != null)
            {
                tmpAsset.atlasTexture.name = $"{key.FileBase} Atlas";
                if (!AssetDatabase.IsSubAsset(tmpAsset.atlasTexture))
                    AssetDatabase.AddObjectToAsset(tmpAsset.atlasTexture, tmpAsset);
            }
            EditorUtility.SetDirty(tmpAsset);
            return tmpAsset;
        }

        private static string GuessFontExtension(string url)
        {
            if (string.IsNullOrEmpty(url)) return ".ttf";
            var lower = url.ToLowerInvariant();
            if (lower.EndsWith(".otf")) return ".otf";
            if (lower.EndsWith(".woff2")) return ".woff2";
            if (lower.EndsWith(".woff")) return ".woff";
            return ".ttf";
        }
    }
}
