using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Figma2Unity.Editor.Api
{
    /// <summary>
    /// On-disk layout for the F2U offline cache. One directory per file key:
    ///
    /// <code>
    /// &lt;rootDir&gt;/&lt;fileKey&gt;/
    /// ├── document.json                 // GET /v1/files/:key raw response
    /// ├── meta.json                     // FigmaCacheMeta (version / lastModified / counts)
    /// ├── image-fills.json              // imageRef → /v1/files/:key/images URL (debug only; URLs expire)
    /// ├── fills/&lt;imageRef&gt;.bin         // raw uploaded image bytes (one per imageRef, dedup'd)
    /// └── render/&lt;nodeId&gt;.png          // /v1/images node renders (VECTOR/BOOLEAN_OPERATION, keyed by node id)
    /// </code>
    ///
    /// <para>
    /// The store is purely structural: it does NOT know how to hit Figma. The
    /// Prefetch command (live FigmaApiClient) and OfflineFigmaApiClient (replay)
    /// both go through this class so layout stays consistent.
    /// </para>
    ///
    /// <para>
    /// v1 keys all bytes by <em>imageRef</em>, mirroring DownloadSpritesStep: many
    /// nodes can share one upload, so we dedup at this layer and the node→sprite
    /// mapping is reconstructed at import time from the parsed FObject tree.
    /// </para>
    /// </summary>
    public class FigmaCacheStore
    {
        public string Root { get; }
        public string FileKey { get; }

        public FigmaCacheStore(string rootDir, string fileKey)
        {
            if (string.IsNullOrEmpty(rootDir)) throw new System.ArgumentException("rootDir is empty");
            if (string.IsNullOrEmpty(fileKey)) throw new System.ArgumentException("fileKey is empty");
            Root = rootDir;
            FileKey = fileKey;
        }

        public string FileRoot => Path.Combine(Root, FileKey);
        public string DocumentJsonPath => Path.Combine(FileRoot, "document.json");
        public string MetaJsonPath => Path.Combine(FileRoot, "meta.json");
        public string ImageFillsJsonPath => Path.Combine(FileRoot, "image-fills.json");

        public string FillsDir => Path.Combine(FileRoot, "fills");
        public string FillPath(string imageRef)
            => Path.Combine(FillsDir, SanitizeFileName(imageRef) + ".bin");

        public string RenderDir => Path.Combine(FileRoot, "render");
        public string RenderPath(string nodeId)
            => Path.Combine(RenderDir, SanitizeFileName(nodeId) + ".png");

        public bool HasDocument => File.Exists(DocumentJsonPath);
        public bool HasMeta => File.Exists(MetaJsonPath);

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(FileRoot);
            Directory.CreateDirectory(FillsDir);
            Directory.CreateDirectory(RenderDir);
        }

        // ---------- document.json ----------

        public void WriteDocumentJson(string json)
        {
            Directory.CreateDirectory(FileRoot);
            File.WriteAllText(DocumentJsonPath, json ?? "");
        }

        public string ReadDocumentJson()
        {
            if (!HasDocument)
                throw new FileNotFoundException(
                    $"F2U cache miss: document.json not found at '{DocumentJsonPath}'. " +
                    "Run Prefetch first.", DocumentJsonPath);
            return File.ReadAllText(DocumentJsonPath);
        }

        // ---------- meta.json ----------

        public void WriteMeta(FigmaCacheMeta meta)
        {
            Directory.CreateDirectory(FileRoot);
            File.WriteAllText(MetaJsonPath, JsonConvert.SerializeObject(meta, Formatting.Indented));
        }

        public FigmaCacheMeta ReadMeta()
        {
            if (!HasMeta) return null;
            return JsonConvert.DeserializeObject<FigmaCacheMeta>(File.ReadAllText(MetaJsonPath));
        }

        // ---------- image-fills.json (debug aid; URLs expire) ----------

        public void WriteImageFills(Dictionary<string, string> fills)
        {
            Directory.CreateDirectory(FileRoot);
            File.WriteAllText(ImageFillsJsonPath,
                JsonConvert.SerializeObject(fills ?? new Dictionary<string, string>(),
                    Formatting.Indented));
        }

        public Dictionary<string, string> ReadImageFills()
        {
            if (!File.Exists(ImageFillsJsonPath)) return new Dictionary<string, string>();
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(
                       File.ReadAllText(ImageFillsJsonPath))
                   ?? new Dictionary<string, string>();
        }

        // ---------- fill bytes ----------

        public void WriteFill(string imageRef, byte[] bytes)
        {
            Directory.CreateDirectory(FillsDir);
            File.WriteAllBytes(FillPath(imageRef), bytes ?? System.Array.Empty<byte>());
        }

        public bool HasFill(string imageRef)
            => !string.IsNullOrEmpty(imageRef) && File.Exists(FillPath(imageRef));

        public byte[] ReadFill(string imageRef)
        {
            var path = FillPath(imageRef);
            if (!File.Exists(path))
                throw new FileNotFoundException($"F2U cache miss: fill not found at '{path}'.", path);
            return File.ReadAllBytes(path);
        }

        // ---------- node render bytes (VECTOR/BOOLEAN_OPERATION via /v1/images) ----------

        public void WriteRender(string nodeId, byte[] bytes)
        {
            Directory.CreateDirectory(RenderDir);
            File.WriteAllBytes(RenderPath(nodeId), bytes ?? System.Array.Empty<byte>());
        }

        public bool HasRender(string nodeId)
            => !string.IsNullOrEmpty(nodeId) && File.Exists(RenderPath(nodeId));

        public byte[] ReadRender(string nodeId)
        {
            var path = RenderPath(nodeId);
            if (!File.Exists(path))
                throw new FileNotFoundException($"F2U cache miss: render not found at '{path}'.", path);
            return File.ReadAllBytes(path);
        }

        // ---------- helpers ----------

        public static string SanitizeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "node";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        /// <summary>
        /// Translates an arbitrary cache URI (file://…) back to a local path.
        /// OfflineFigmaApiClient hands these URIs to DownloadSpritesStep so the
        /// existing "URL → bytes" contract stays unchanged.
        /// </summary>
        public static bool TryGetLocalPathFromCacheUri(string uri, out string path)
        {
            path = null;
            if (string.IsNullOrEmpty(uri)) return false;
            const string FilePrefix = "file://";
            if (uri.StartsWith(FilePrefix, System.StringComparison.Ordinal))
            {
                path = uri.Substring(FilePrefix.Length);
                return true;
            }
            return false;
        }

        public static string ToFileUri(string absolutePath)
            => "file://" + absolutePath.Replace('\\', '/');
    }

    /// <summary>
    /// Cache freshness/identity. Used by the import command to decide whether the
    /// on-disk snapshot is still current vs. the live Figma file.
    /// </summary>
    public class FigmaCacheMeta
    {
        public string FileKey;
        public string FileName;
        /// <summary>Figma file version string (changes on every save).</summary>
        public string Version;
        /// <summary>ISO 8601 lastModified from /v1/files response.</summary>
        public string LastModified;
        public int FillCount;
        public int RenderCount;
        public string PrefetchedAtUtc;
    }
}
