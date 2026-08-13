using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Drawers;
using Figma2Unity.Editor.Layout;
using Figma2Unity.Editor.Rendering;
using Figma2Unity.Editor.Sync;
using UnityEngine;

namespace Figma2Unity.Editor.Sync
{
    /// <summary>
    /// Editor-side wiring: produces a <see cref="SyncService"/> instance preconfigured
    /// with the real Drawer + RectTransformConverter for node-level redraw and creation.
    /// Layer A tests inject their own simple seams instead — that's why SyncService
    /// itself only carries delegate hooks.
    /// </summary>
    public static class SyncServiceFactory
    {
        public static SyncService CreateForEditor()
        {
            var drawer = new DrawerCoordinator();
            var converter = new RectTransformConverter(1f);

            return new SyncService
            {
                RedrawNode = (fobj, go, ctx) =>
                {
                    if (go == null) return;

                    // Drop Drawer-owned components for tags that disappeared since the
                    // previous import (e.g. designer removed an effect, renamed off
                    // "#button"). Without this, stale Shadow/Button/Toggle/etc. linger.
                    var sync = go.GetComponent<SyncHelper>();
                    var previousTags = sync != null && sync.Data != null ? sync.Data.Tags : null;
                    if (previousTags != null)
                        DrawerCoordinator.StripRemovedTagComponents(go, previousTags, fobj.Tags);

                    // Re-apply geometry first (anchors / size / rotation may have changed).
                    converter.Apply(fobj, ctx);
                    RedrawSingleNode(drawer, fobj, ctx);
                },

                CreateAndDrawNode = (fobj, ctx) =>
                {
                    var parentGo = ResolveParentGameObject(fobj, ctx);
                    var parentTransform = parentGo != null ? parentGo.transform : ctx.TargetCanvas?.transform;
                    if (parentTransform == null) return;

                    var goName = !string.IsNullOrEmpty(fobj.FileName) ? fobj.FileName
                               : (!string.IsNullOrEmpty(fobj.Name) ? fobj.Name : (fobj.Id ?? "Node"));
                    var go = new GameObject(goName, typeof(RectTransform));
                    go.transform.SetParent(parentTransform, false);

                    var marker = go.AddComponent<F2UNodeMarker>();
                    marker.NodeId = fobj.Id;
                    marker.NodeType = fobj.Type;

                    ctx.SetGameObject(fobj, go);
                    converter.Apply(fobj, ctx);
                    if (!fobj.Visible) go.SetActive(false);

                    RedrawSingleNode(drawer, fobj, ctx);
                },

                // Track A integration — rebake Sprites for changed Image-family nodes.
                // SpriteGenerator skips nodes that already have a SpritePath, so clear the
                // stale paths first (Modified nodes carry the OLD path from the prior
                // import via SyncHelper.Data.SpritePath restored into ctx). Wrap the
                // AllNodes view in a transient single-batch list so SpriteGenerator only
                // walks the changed nodes, not the entire scene.
                RebakeSprites = async (changed, ctx, token) =>
                {
                    if (changed == null || changed.Count == 0) return;
                    if (ctx?.Config == null || !ctx.Config.EnableFigmageBaking) return;

                    foreach (var n in changed)
                    {
                        if (n != null) ctx.SetSpritePath(n, null);
                    }

                    var prev = ctx.AllNodes;
                    ctx.AllNodes = changed;
                    try
                    {
                        var generator = new SpriteGenerator();
                        await generator.GenerateSpritesAsync(ctx, token);
                    }
                    finally
                    {
                        ctx.AllNodes = prev;
                    }
                },
            };
        }

        private static GameObject ResolveParentGameObject(FObject fobj, F2UContext ctx)
        {
            if (fobj.Parent == null) return ctx.TargetCanvas?.gameObject;
            return ctx.GetGameObject(fobj.Parent);
        }

        private static void RedrawSingleNode(DrawerCoordinator drawer, FObject fobj, F2UContext ctx)
        {
            if (fobj == null || fobj.Tags == null) return;
            if (fobj.Tags.Contains(FcuTag.Ignore)) return;
            if (ctx.GetGameObject(fobj) == null) return;

            DrawerCoordinator.ApplyTagExclusions(fobj);
            // Drive a tag-priority pass for just this node by leveraging the coordinator's
            // public DrawAll over a 1-element AllNodes view.
            var prevAllNodes = ctx.AllNodes;
            ctx.AllNodes = new System.Collections.Generic.List<FObject> { fobj };
            try
            {
                drawer.DrawAll(ctx);
            }
            finally
            {
                ctx.AllNodes = prevAllNodes;
            }
        }
    }
}
