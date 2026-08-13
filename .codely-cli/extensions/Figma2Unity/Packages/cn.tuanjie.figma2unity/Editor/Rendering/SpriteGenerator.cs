using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Figma2Unity.Editor.Rendering
{
    /// <summary>
    /// v2 Track A — bakes the subset of FObject nodes whose <see cref="BakeStrategyResolver"/>
    /// strategy is <c>BakeSprite</c> or <c>Slice9</c>, using <see cref="FigmageBakerAdapter"/>.
    /// Each bake writes a PNG, imports it as Sprite/Single, configures 9-slice borders
    /// when the strategy is Slice9, and records the path on <see cref="F2UContext"/>.
    ///
    /// Yields back to the main thread every <see cref="YieldInterval"/> bakes so the
    /// editor stays responsive on large files. Idempotent: a node that already has a
    /// sprite path is skipped.
    /// </summary>
    public class SpriteGenerator
    {
        public const int YieldInterval = 10;

        private readonly FigmageBakerAdapter _baker;
        private readonly BakeStrategyResolver _resolver;

        public SpriteGenerator(FigmageBakerAdapter baker = null, BakeStrategyResolver resolver = null)
        {
            _baker = baker ?? new FigmageBakerAdapter();
            _resolver = resolver ?? new BakeStrategyResolver();
        }

        public async Task<int> GenerateSpritesAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx == null || ctx.AllNodes == null) return 0;
            if (ctx.Config == null || !ctx.Config.EnableFigmageBaking) return 0;

            string folder = ctx.Config.SpriteFolder;
            if (string.IsNullOrEmpty(folder)) folder = "Assets/Sprites";
            Directory.CreateDirectory(folder);

            float scale = Mathf.Max(1f, ctx.Config.SpriteScale);

            int baked = 0;
            int processed = 0;
            foreach (var fobj in ctx.AllNodes)
            {
                token.ThrowIfCancellationRequested();
                if (fobj == null) continue;

                // Skip nodes that already have an IMAGE Paint sprite (DownloadSpritesStep ran first).
                if (!string.IsNullOrEmpty(ctx.GetSpritePath(fobj))) continue;

                var strategy = _resolver.Resolve(fobj, ctx.Config);
                if (strategy != BakeStrategyResolver.Strategy.BakeSprite
                    && strategy != BakeStrategyResolver.Strategy.Slice9)
                    continue;

                string path = $"{folder}/{SanitizeFileName(fobj.Id)}.png";
                if (TryBakeOne(fobj, path, scale))
                {
                    int border = strategy == BakeStrategyResolver.Strategy.Slice9
                        ? Slice9Detector.AutoBorderPixels(fobj, scale)
                        : 0;
                    ConfigureAsSprite(path, border);
                    ctx.SetSpritePath(fobj, path);
                    baked++;
                }

                processed++;
                if (processed % YieldInterval == 0) await Task.Yield();
            }

            Debug.Log($"[Figma2Unity] SpriteGenerator: {baked} sprites baked into {folder}.");
            return baked;
        }

        private bool TryBakeOne(FObject fobj, string path, float scale)
        {
            try { return _baker.BakeToPngFile(fobj, path, scale); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Figma2Unity] Bake failed for {fobj.Name ?? fobj.Id}: {ex.Message}");
                return false;
            }
        }

        private static void ConfigureAsSprite(string assetPath, int border)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.mipmapEnabled)
            { importer.mipmapEnabled = false; dirty = true; }

            if (border > 0)
            {
                var expected = new Vector4(border, border, border, border);
                if (importer.spriteBorder != expected)
                { importer.spriteBorder = expected; dirty = true; }
            }

            if (dirty) importer.SaveAndReimport();
        }

        private static string SanitizeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "node";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }
    }
}
