using System.Collections.Generic;
using System.Linq;
using Figma2Unity.Editor.Pipeline.Steps;

namespace Figma2Unity.Editor.Rendering
{
    /// <summary>
    /// Single source of truth for "which nodes need a /v1/images render
    /// (VectorPngFallback)". Both <see cref="Pipeline.Steps.DownloadSpritesStep"/>
    /// (online + offline import) and <see cref="F2UPrefetchCommand"/> (cache warm-up) call
    /// this so the set prefetch caches is exactly the set the import requests — otherwise a
    /// mismatch resurfaces as white icon boxes in cache mode.
    ///
    /// Typical members: VECTOR / BOOLEAN_OPERATION / STAR / REGULAR_POLYGON / LINE
    /// (CornerRounder can't express these), complex ELLIPSEs, and rotated IMAGE-fill
    /// rectangles (Figma renders rotation on image fills with a transform that is not a pure
    /// rotation, so we let Figma bake the visual). The single source of truth for which
    /// strategy each node uses is <see cref="BakeStrategyResolver"/>; this selector simply
    /// asks the resolver and collects everyone tagged VectorPngFallback.
    ///
    /// Scope: only nodes that actually get a GameObject (screen subtrees per
    /// <see cref="ScreenNodeCollector.CollectInstantiableNodes"/>), excluding the synthetic
    /// VirtualPage, non-Screen library / showcase frames, and Ignore-tagged subtrees.
    /// </summary>
    public static class VectorRenderSelector
    {
        /// <summary>
        /// Ordered, de-duplicated node ids whose render strategy is VectorPngFallback.
        /// Returns empty when the fallback is disabled in config.
        /// </summary>
        public static List<string> SelectNodeIds(F2UContext ctx)
        {
            var ids = new List<string>();
            if (ctx == null) return ids;
            if (ctx.Config != null && !ctx.Config.EnableVectorPngFallback) return ids;

            var resolver = new BakeStrategyResolver();
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var node in ScreenNodeCollector.CollectInstantiableNodes(ctx))
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) continue;
                if (resolver.Resolve(node, ctx.Config) != BakeStrategyResolver.Strategy.VectorPngFallback)
                    continue;
                if (seen.Add(node.Id)) ids.Add(node.Id);
            }
            return ids;
        }
    }
}
