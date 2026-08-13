using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Fonts;
using TMPro;
using UnityEngine;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// v2 Step #17 (Design.md §三): download fonts via Google Fonts and assign generated
    /// TMP_FontAsset to TEXT nodes' TMP components.
    ///
    /// Failure handling: every per-key failure already falls back inside FontManager (the
    /// returned dict simply omits the failed key). Per-node fallback to
    /// <see cref="F2UConfig.FallbackFont"/> happens here so missing-font characters never
    /// render as the dreaded "tofu" blocks.
    /// </summary>
    public class DownloadFontsStep : PipelineStep
    {
        public override string DisplayName => "Download Fonts";
        public override float ProgressWeight => 3f;

        public override async Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.AllNodes == null) return StepResult.Ok();
            if (ctx.Config != null && !ctx.Config.EnableGoogleFontsDownload)
                return StepResult.Ok();

            var folder = ctx.Config != null ? ctx.Config.FontFolder : "Assets/Fonts";
            var manager = new FontManager(folder);

            System.Collections.Generic.Dictionary<FontKey, TMP_FontAsset> resolved;
            try
            {
                resolved = await manager.ResolveFontsAsync(ctx.AllNodes, token);
            }
            catch (System.OperationCanceledException)
            {
                return StepResult.Fail("Canceled");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Figma2Unity] FontManager.ResolveFontsAsync threw: {ex.Message}. Falling back to F2UConfig.FallbackFont.");
                resolved = new System.Collections.Generic.Dictionary<FontKey, TMP_FontAsset>();
            }

            var fallback = ctx.Config != null ? ctx.Config.FallbackFont : null;
            int applied = 0, fallbackUsed = 0;
            foreach (var fobj in ctx.AllNodes)
            {
                if (fobj?.Style == null) continue;
                var go = ctx.GetGameObject(fobj);
                var tmp = go != null ? go.GetComponent<TextMeshProUGUI>() : null;
                if (tmp == null) continue;

                var key = FontKey.From(fobj.Style);
                if (!key.IsEmpty && resolved.TryGetValue(key, out var asset) && asset != null)
                {
                    tmp.font = asset;
                    applied++;
                }
                else if (tmp.font == null && fallback != null)
                {
                    tmp.font = fallback;
                    fallbackUsed++;
                }
            }
            Debug.Log($"[Figma2Unity] DownloadFonts: {applied} resolved, {fallbackUsed} fell back to FallbackFont.");
            return StepResult.Ok();
        }
    }
}
