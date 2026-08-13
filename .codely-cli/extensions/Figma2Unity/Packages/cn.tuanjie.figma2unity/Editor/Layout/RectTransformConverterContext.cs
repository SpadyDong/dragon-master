using Figma2Unity.Editor.Rendering;
using UnityEngine;

namespace Figma2Unity.Editor.Layout
{
    /// <summary>
    /// F2UContext-aware overload of RectTransformConverter. Lives in its own file so the
    /// pure ApplyToRect overload stays compilable under Layer A standalone tests
    /// (which can't depend on F2UContext → ApiClient → HttpClient chain).
    /// </summary>
    public static class RectTransformConverterContextExtensions
    {
        private static readonly BakeStrategyResolver _strategyResolver = new BakeStrategyResolver();

        public static void Apply(this RectTransformConverter converter, FObject fobj, F2UContext ctx)
        {
            if (converter == null || fobj == null || ctx == null) return;
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            // Treat as root when the parent FObject has no GameObject (virtual page or a
            // skipped/ignored node) — anchoring against a zero-sized parent would misplace it.
            bool parentHasGo = fobj.Parent != null && ctx.GetGameObject(fobj.Parent) != null;
            converter.ApplyToRect(fobj, rt, !parentHasGo, ShouldApplyRotation(fobj, ctx));
        }

        /// <summary>
        /// Rotation must be applied only to nodes drawn live (DirectColor solid fills, IMAGE
        /// Paint downloads of the original uploaded asset). Sprite strategies that render the
        /// node itself via /v1/images (VectorPngFallback / BakeSprite / Slice9) bake the
        /// rotation into the PNG pixels — re-rotating those would double-rotate the icon.
        /// </summary>
        private static bool ShouldApplyRotation(FObject fobj, F2UContext ctx)
        {
            var strategy = _strategyResolver.Resolve(fobj, ctx.Config);
            switch (strategy)
            {
                case BakeStrategyResolver.Strategy.VectorPngFallback:
                case BakeStrategyResolver.Strategy.BakeSprite:
                case BakeStrategyResolver.Strategy.Slice9:
                    return false; // rotation already baked into the rendered/baked sprite
                default:
                    return true;  // DirectColor / DownloadSprite (original image) → apply
            }
        }
    }
}
