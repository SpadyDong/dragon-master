using System.Collections.Generic;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// Shared, Unity-free screen-node selection used by both CreateGameObjectsStep (which
    /// instantiates GameObjects) and ComputeDiffStep (incremental diff input). Keeping this
    /// logic in one place guarantees the incremental diff/sync path operates on exactly the
    /// nodes that get a GameObject in Full mode — never the whole flattened tree (which
    /// includes the synthetic VirtualPage <c>__virtual_root__</c> and non-Screen library /
    /// showcase nodes that would otherwise be rebuilt flat and tiled past the Canvas).
    /// </summary>
    public static class ScreenNodeCollector
    {
        /// <summary>
        /// Ordered top-level Screen roots: Screen-tagged direct children of the VirtualPage,
        /// plus Screen-tagged children one level inside a SECTION (virtually re-parented to
        /// the VirtualPage so RectTransformConverter applies the center-anchor root logic).
        /// The synthetic VirtualPage itself and any non-Screen top-level node are excluded.
        /// </summary>
        /// <summary>
        /// Every node tagged <see cref="FcuTag.Screen"/>, anywhere in the tree, virtually
        /// re-parented to the VirtualPage so RectTransformConverter applies the root-stretch /
        /// center-anchor logic (instead of anchoring against an ancestor that has no
        /// GameObject). This covers three shapes uniformly:
        ///   • top-level Screen frames (direct VirtualPage children),
        ///   • Screens grouped one level inside a SECTION band,
        ///   • nested NAVIGATE/OVERLAY destination frames promoted by ScreenIdentifier (e.g.
        ///     screens designers tuck inside a wrapper "Cover" frame).
        /// The synthetic VirtualPage itself and any non-Screen node are excluded. Each promoted
        /// Screen becomes its own prefab root; <see cref="Accumulate"/> stops the enclosing
        /// screen from inlining it.
        /// </summary>
        public static IEnumerable<FObject> CollectScreenRoots(F2UContext ctx)
        {
            if (ctx?.VirtualPage == null) yield break;
            var source = ctx.AllNodes ?? (IReadOnlyList<FObject>)CollectFlatten(ctx.VirtualPage);
            foreach (var node in source)
            {
                if (node == null) continue;
                if (node == ctx.VirtualPage) continue;
                if (node.Tags == null || !node.Tags.Contains(FcuTag.Screen)) continue;
                // Re-parent virtually so the converter treats it as a Canvas-level root.
                node.Parent = ctx.VirtualPage;
                yield return node;
            }
        }

        private static List<FObject> CollectFlatten(FObject root)
        {
            var list = new List<FObject>();
            void Walk(FObject n)
            {
                if (n == null) return;
                list.Add(n);
                if (n.Children == null) return;
                foreach (var c in n.Children) Walk(c);
            }
            Walk(root);
            return list;
        }


        /// <summary>
        /// Pre-order flatten of every node <see cref="CollectScreenRoots"/> would
        /// instantiate (screen roots + all descendants), skipping Ignore-tagged subtrees
        /// exactly as CreateGameObjectsStep does. Falls back to <c>ctx.AllNodes</c> when
        /// there is no VirtualPage (Layer A unit-test shape).
        /// </summary>
        public static List<FObject> CollectInstantiableNodes(F2UContext ctx)
        {
            var result = new List<FObject>();
            if (ctx?.VirtualPage == null)
            {
                if (ctx?.AllNodes != null) result.AddRange(ctx.AllNodes);
                return result;
            }
            foreach (var root in CollectScreenRoots(ctx))
                Accumulate(root, root, result);
            return result;
        }

        private static void Accumulate(FObject root, FObject fobj, List<FObject> sink)
        {
            if (fobj == null) return;
            if (fobj.Tags != null && fobj.Tags.Contains(FcuTag.Ignore)) return;
            // A nested Screen (Screen-tagged node other than this root) is its own prefab
            // root — don't inline it into the enclosing screen's node set, or it would be
            // collected twice (once here, once via its own CollectScreenRoots entry).
            if (fobj != root && fobj.Tags != null && fobj.Tags.Contains(FcuTag.Screen)) return;
            sink.Add(fobj);
            if (fobj.Children == null) return;
            foreach (var c in fobj.Children)
                Accumulate(root, c, sink);
        }
    }
}
