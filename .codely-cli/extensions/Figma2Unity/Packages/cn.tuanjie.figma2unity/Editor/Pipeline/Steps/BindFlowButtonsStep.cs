using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Drawers;
using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// Attach FlowButton to every node that carries a prototype navigation target
    /// (legacy <c>transitionNodeID</c> or new-style <c>interactions[].action.destinationId</c>,
    /// both folded into <see cref="FObject.TransitionNodeID"/> by the parser).
    ///
    /// ⚠ This step is intentionally independent of the Button tag — a node only gets the
    /// Button tag from name keywords / manual <c>#button</c>. Renamed icons or card-style
    /// click targets would otherwise silently lose interactivity. Runs AFTER
    /// DrawComponentsStep so any Image is already in place to serve as the click target,
    /// and BEFORE BuildPrototypeFlowStep so the controller wiring is coherent.
    /// </summary>
    public class BindFlowButtonsStep : PipelineStep
    {
        public override string DisplayName => "Bind Flow Buttons";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.Config != null && !ctx.Config.BuildPrototypeFlow)
                return Task.FromResult(StepResult.Ok());
            if (ctx?.AllNodes == null)
                return Task.FromResult(StepResult.Ok());

            float defaultDuration = ctx.Config != null ? ctx.Config.DefaultTransitionDuration : 0.3f;

            foreach (var fobj in ctx.AllNodes)
            {
                if (fobj == null) continue;
                if (string.IsNullOrEmpty(fobj.TransitionNodeID) && !fobj.IsCloseOverlay) continue;
                var go = ctx.GetGameObject(fobj);
                if (go == null) continue;

                // Ensure a UGUI Button + raycast target so clicks reach FlowButton even when
                // the node wasn't tagged Button by name. Idempotent via EnsureComponent.
                var btn = go.EnsureComponent<Button>();
                var img = go.GetComponent<Image>();
                bool isMaskNode = go.GetComponent<Mask>() != null
                                  || (fobj.Tags != null && fobj.Tags.Contains(FcuTag.Mask));
                if (img == null)
                {
                    // Many prototype click targets are INSTANCE/GROUP container nodes whose
                    // fills are empty (icons/labels live in their children). ImageDrawer
                    // intentionally skips those to keep the node a transparent container —
                    // but UGUI needs *some* Graphic with raycastTarget=true under the
                    // pointer or the click is swallowed by the EventSystem. Add a fully
                    // transparent Image purely as a raycast hit area.
                    img = go.AddComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0f);
                    img.sprite = null;
                    img.type = Image.Type.Simple;
                }
                else if (isMaskNode)
                {
                    // Node is both a Mask source AND a click target. Mask uses this Image as
                    // its stencil — forcing alpha=0 would invalidate the mask shape. Leave
                    // color/sprite alone and only flip raycastTarget so Mask + FlowButton
                    // coexist (UGUI Mask still hit-tests on the underlying Image bounds).
                }
                img.raycastTarget = true;
                if (btn.targetGraphic == null) btn.targetGraphic = img;

                var flow = go.EnsureComponent<FlowButton>();
                flow.TargetScreenNodeId = fobj.TransitionNodeID;
                flow.TransitionDuration = fobj.TransitionDuration > 0f
                    ? fobj.TransitionDuration / 1000f // Figma ms → seconds
                    : defaultDuration;
                flow.TransitionEasing = fobj.TransitionEasing;
                flow.NavigationType = fobj.NavigationType;
                flow.IsCloseAction = fobj.IsCloseOverlay;

                // Figma `overlayRelativePosition` is defined as "the offset by which the
                // overlay is opened relative to THIS NODE" (the trigger), not the source
                // screen. Resolve at import time: trigger.screen_local_TL + offset =
                // popup screen-local TL, which the runtime then maps 1:1 onto ScreenParent
                // (screens fill that parent at the canvas reference resolution). This makes
                // the popup land at the same screen-local coordinates the designer authored
                // when placing an equivalent static element inside the screen.
                if (fobj.HasOverlayPosition && fobj.NavigationType == NavigationType.OVERLAY)
                {
                    var screen = FindScreenAncestor(fobj);
                    if (screen != null)
                    {
                        var pb = screen.AbsoluteBoundingBox;
                        var tb = fobj.AbsoluteBoundingBox;
                        flow.HasOverlayPosition = true;
                        flow.OverlayPosition = new Vector2(
                            (tb.x - pb.x) + fobj.OverlayPosition.x,
                            (tb.y - pb.y) + fobj.OverlayPosition.y);
                    }
                    else
                    {
                        // Trigger has no Screen ancestor (component master / orphan).
                        // Drop the placement so the controller falls back to CENTER.
                        flow.HasOverlayPosition = false;
                        flow.OverlayPosition = Vector2.zero;
                    }
                }
                else
                {
                    flow.HasOverlayPosition = false;
                    flow.OverlayPosition = Vector2.zero;
                }
            }

            return Task.FromResult(StepResult.Ok());
        }

        /// <summary>
        /// Walk up <c>fobj.Parent</c> until we find a node tagged as a Screen (set by
        /// <see cref="Figma2Unity.Editor.PrototypeFlow.ScreenIdentifier"/>). Returns null
        /// when no Screen ancestor exists — typically for COMPONENT-master children that
        /// were not instantiated inside any Screen subtree.
        /// </summary>
        private static FObject FindScreenAncestor(FObject fobj)
        {
            for (var cur = fobj?.Parent; cur != null; cur = cur.Parent)
            {
                if (cur.Tags != null && cur.Tags.Contains(FcuTag.Screen))
                    return cur;
            }
            return null;
        }
    }
}