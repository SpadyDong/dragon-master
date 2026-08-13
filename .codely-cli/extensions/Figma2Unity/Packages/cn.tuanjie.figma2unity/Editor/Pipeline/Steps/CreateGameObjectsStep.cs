using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Drawers;
using Figma2Unity.Editor.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 10: Create GameObject tree + RectTransform conversion.</summary>
    public class CreateGameObjectsStep : PipelineStep
    {
        public override string DisplayName => "Create GameObjects";
        public override float ProgressWeight => 2f;

        private readonly RectTransformConverter _rectConverter;
        private readonly MaskDrawer _maskDrawer = new MaskDrawer();

        public CreateGameObjectsStep(F2UConfig config)
        {
            // RectTransform geometry is 1:1 with Figma absolute coords. SpriteScale only
            // affects sprite pixel density / TMP font multipliers (see LayoutDrawer /
            // TextDrawer); folding it into rect conversion double-scaled the Screen root
            // and pushed children off-canvas. Canvas resolution is controlled by
            // CanvasScaler.referenceResolution, not by this converter.
            _rectConverter = new RectTransformConverter(1f);
        }

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.TargetCanvas == null)
                return Task.FromResult(StepResult.Fail("TargetCanvas is null"));
            if (ctx.VirtualPage == null)
                return Task.FromResult(StepResult.Fail("VirtualPage is null"));

            // Skip the VirtualPage wrapper. Only Screen-tagged top-level nodes are instantiated
            // under the Canvas — the Figma CANVAS frequently carries 100+ reusable COMPONENT /
            // INSTANCE / RECTANGLE entries (component library, showcase frames) alongside real
            // screens. Without this filter every one of them becomes a stretched, overlapping
            // GameObject under Canvas. Screen tag is set by IdentifyScreensStep (Step 7) which
            // runs before this step (Step 10).
            //
            // We also descend one level into SECTION nodes so that Screens grouped inside a
            // visual Section band (a common Figma layout convention) are still instantiated
            // directly under Canvas. They keep their Figma absolute geometry — RectTransform
            // conversion at the Section level isn't applied since the Section itself never
            // becomes a GameObject.
            foreach (var screenRoot in ScreenNodeCollector.CollectScreenRoots(ctx))
                CreateRecursive(screenRoot, ctx.TargetCanvas.transform, ctx, isRoot: true);
            return Task.FromResult(StepResult.Ok());
        }

        private void CreateRecursive(FObject fobj, Transform parent, F2UContext ctx, bool isRoot)
        {
            if (fobj == null) return;
            if (fobj.Tags != null && fobj.Tags.Contains(FcuTag.Ignore)) return;
            // A nested Screen is instantiated + saved as its own prefab root (see
            // ScreenNodeCollector.CollectScreenRoots). Don't inline it inside the enclosing
            // screen, or it would exist both inline and as a standalone screen instance.
            if (!isRoot && fobj.Tags != null && fobj.Tags.Contains(FcuTag.Screen)) return;

            var goName = !string.IsNullOrEmpty(fobj.FileName) ? fobj.FileName
                       : (!string.IsNullOrEmpty(fobj.Name) ? fobj.Name : (fobj.Id ?? "Node"));
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var marker = go.AddComponent<F2UNodeMarker>();
            marker.NodeId = fobj.Id;
            marker.NodeType = fobj.Type;

            ctx.SetGameObject(fobj, go);
            _rectConverter.Apply(fobj, ctx);

            // Figma frames with "clip content" enabled hide children that extend past their
            // bounds. Mirror that with UGUI clipping so showcase artwork / extended icons
            // don't bleed outside the screen. v1: applied to FRAME-family only (matches
            // Figma — RECTANGLE/VECTOR clipping is not exposed via this flag).
            //
            // RectMask2D clips in canvas space with an axis-aligned rectangle and ignores
            // node rotation, so a rotated clip frame (or one under a rotated ancestor) gets
            // its content sliced on one side. In that case fall back to a stencil Mask, which
            // clips against the mask graphic's mesh and therefore rotates with the node.
            if (fobj.ClipsContent
                && (fobj.Type == "FRAME" || fobj.Type == "COMPONENT" || fobj.Type == "INSTANCE"))
            {
                if (ClipStrategyResolver.CanUseRectMask2D(fobj))
                {
                    if (go.GetComponent<RectMask2D>() == null) go.AddComponent<RectMask2D>();
                }
                else
                {
                    _maskDrawer.Draw(fobj, ctx);
                }
            }

            if (!fobj.Visible) go.SetActive(false);

            if (fobj.Children != null)
            {
                foreach (var c in fobj.Children)
                    CreateRecursive(c, go.transform, ctx, isRoot: false);
            }
        }
    }
}
