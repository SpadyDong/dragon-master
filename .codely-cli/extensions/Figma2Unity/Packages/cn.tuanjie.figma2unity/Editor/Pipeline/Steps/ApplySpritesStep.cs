using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// Step 16: Apply downloaded/baked sprites to Image components (Design.md §附录 D.1).
    /// Both IMAGE Paint (DownloadSpritesStep) and Figmage-baked sprites (BakeSpritesStep)
    /// land in <see cref="F2UContext.NodeSpritePathMap"/>; this step consumes them uniformly
    /// and flips Image.type to Sliced when the sprite carries a 9-slice border.
    ///
    /// A second pass repairs any sprite-strategy Image whose sprite never landed (failed
    /// /v1/images render, baking disabled, etc.). <see cref="ImageDrawer"/> pre-paints those
    /// Images opaque white as a placeholder, so without this guard they render as white
    /// boxes at runtime. We instead repaint them as the node's solid fill color, or disable
    /// the Image entirely when there is no visible fill.
    /// </summary>
    public class ApplySpritesStep : PipelineStep
    {
        public override string DisplayName => "Apply Sprites";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx == null) return Task.FromResult(StepResult.Ok());

            if (ctx.NodeSpritePathMap != null && ctx.NodeSpritePathMap.Count > 0)
            {
                foreach (var kvp in ctx.NodeSpritePathMap)
                {
                    if (!ctx.NodeMap.TryGetValue(kvp.Key, out var fobj)) continue;
                    var go = ctx.GetGameObject(fobj);
                    if (go == null) continue;
                    var img = go.GetComponent<Image>();
                    if (img == null) continue;

                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(kvp.Value);
                    if (sprite == null) continue;
                    img.sprite = sprite;
                    // When a sprite is assigned, color should default to white so the sprite shows.
                    if (img.color == default(Color)) img.color = Color.white;

                    // A non-zero border means the importer set 9-slice borders (SpriteGenerator path):
                    // promote Image.type so the corners stay crisp at runtime.
                    if (sprite.border != Vector4.zero && img.type != Image.Type.Sliced)
                        img.type = Image.Type.Sliced;
                }
            }

            RepairUnresolvedSpriteImages(ctx);
            return Task.FromResult(StepResult.Ok());
        }

        /// <summary>
        /// Repaint or hide Images whose sprite-producing strategy never delivered a sprite,
        /// so they don't linger as the opaque white placeholder set by <see cref="ImageDrawer"/>.
        /// </summary>
        private static void RepairUnresolvedSpriteImages(F2UContext ctx)
        {
            if (ctx.AllNodes == null) return;
            var resolver = new BakeStrategyResolver();

            foreach (var fobj in ScreenNodeCollector.CollectInstantiableNodes(ctx))
            {
                if (fobj == null) continue;

                var strategy = resolver.Resolve(fobj, ctx.Config);
                if (strategy == BakeStrategyResolver.Strategy.DirectColor)
                    continue; // DirectColor Images intentionally show their fill color.

                var go = ctx.GetGameObject(fobj);
                if (go == null) continue;
                var img = go.GetComponent<Image>();
                if (img == null || img.sprite != null) continue;

                // Sprite-strategy Image with no sprite — undo the white placeholder.
                if (TryGetSolidFillColor(fobj, out var fill))
                {
                    img.color = fill;
                    img.type = Image.Type.Simple;
                }
                else
                {
                    // No usable fill: hide so it never paints an opaque white quad.
                    img.enabled = false;
                }
            }
        }

        /// <summary>
        /// Mirror of <see cref="ImageDrawer"/>'s DirectColor fill resolution: the first fill's
        /// color premultiplied by its opacity. Returns false when there is no visible fill.
        /// </summary>
        private static bool TryGetSolidFillColor(FObject fobj, out Color color)
        {
            color = Color.white;
            if (fobj.Graphic == null || fobj.Graphic.Fills == null || fobj.Graphic.Fills.Count == 0)
                return false;

            var first = fobj.Graphic.Fills[0];
            if (first == null) return false;

            var c = first.Color;
            c.a *= Mathf.Clamp01(first.Opacity);
            if (c.a <= 0f) return false; // fully transparent — treat as no fill, hide instead.

            color = c;
            return true;
        }
    }
}
