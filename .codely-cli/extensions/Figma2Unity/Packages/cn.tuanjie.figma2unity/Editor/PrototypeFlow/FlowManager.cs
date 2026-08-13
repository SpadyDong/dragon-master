using System.Collections.Generic;
using UnityEngine;
using Figma2Unity.Editor.Drawers;

namespace Figma2Unity.Editor.PrototypeFlow
{
    /// <summary>
    /// Identifies Screens / FlowSections from the FObject tree and wires the runtime
    /// PrototypeFlowController. Prefab saving lives in FinalizeStep (Design.md §三 #19).
    /// </summary>
    public class FlowManager
    {
        public virtual void IdentifyScreensAndSections(F2UContext ctx)
        {
            // Delegated to the Unity-free ScreenIdentifier so the import pipeline and
            // F2UPrefetchCommand resolve an identical Screen set (prefetch must cache the
            // same vector renders the import requests, or icons fall back to white boxes).
            ScreenIdentifier.Identify(ctx);
        }

        public virtual void BuildPrototypeFlow(F2UContext ctx)
        {
            if (ctx?.TargetCanvas == null) return;
            var canvas = ctx.TargetCanvas;

            var flowGo = EnsureCanvasChild(canvas, "PrototypeFlowController");
            var controller = flowGo.EnsureComponent<PrototypeFlowController>();

            var screenParent = EnsureCanvasChild(canvas, "Screens");
            var screenRt = screenParent.EnsureComponent<RectTransform>();
            screenRt.anchorMin = Vector2.zero;
            screenRt.anchorMax = Vector2.one;
            screenRt.offsetMin = Vector2.zero;
            screenRt.offsetMax = Vector2.zero;
            controller.ScreenParent = screenRt;

            ConfigureCanvasScaler(canvas, ctx);

            controller.ScreenList.Clear();
            if (ctx.Screens != null)
            {
                foreach (var screen in ctx.Screens)
                {
                    if (screen == null) continue;
                    controller.ScreenList.Add(new FlowScreen
                    {
                        NodeId = screen.Id,
                        Name = screen.Name,
                        Prefab = null, // Filled in FinalizeStep.
                    });
                    // Source screen instance is destroyed in FinalizeStep right after its
                    // prefab is saved — keep this loop side-effect-free.
                }
            }

            // Sections: API does NOT expose per-Section flowStartingPoints (those live on
            // CANVAS). Keep Section entries as markers — v2 multi-start routing reads
            // CANVAS-level starts via PrototypeFlowController.AllStartingPoints below.
            controller.SectionList.Clear();
            if (ctx.FlowSections != null)
            {
                foreach (var section in ctx.FlowSections)
                {
                    if (section == null) continue;
                    controller.SectionList.Add(new FlowSection
                    {
                        SectionId = section.Id,
                        Name = section.Name,
                    });
                }
            }

            // Stash CANVAS-level starting points on the controller so v2 routing has the
            // full list without re-importing. v1 uses [0] for InitialScreenId below.
            controller.AllStartingPoints.Clear();
            if (ctx.VirtualPage?.FlowStartingPoints != null)
            {
                foreach (var sp in ctx.VirtualPage.FlowStartingPoints)
                {
                    if (sp == null) continue;
                    controller.AllStartingPoints.Add(new FlowStartingPoint
                    {
                        NodeId = sp.NodeId,
                        Name = sp.Name,
                    });
                }
            }

            var initial = DetermineInitialScreen(ctx);
            controller.InitialScreenId = initial != null ? initial.Id : null;
        }

        private static FObject DetermineInitialScreen(F2UContext ctx)
        {
            // CANVAS-level flowStartingPoints[0] wins; else first Screen.
            if (ctx.VirtualPage?.FlowStartingPoints != null)
            {
                foreach (var sp in ctx.VirtualPage.FlowStartingPoints)
                {
                    if (sp == null || string.IsNullOrEmpty(sp.NodeId)) continue;
                    if (ctx.NodeMap.TryGetValue(sp.NodeId, out var t) && t != null)
                        return t;
                }
            }
            if (ctx.Screens != null && ctx.Screens.Count > 0) return ctx.Screens[0];
            return null;
        }

        /// <summary>
        /// Largest Screen design size across <paramref name="ctx"/>.Screens, used as the
        /// CanvasScaler reference resolution. Pure + Unity-free so Layer A can verify it.
        /// Returns (0,0) when there are no screens.
        /// </summary>
        public static Vector2 ComputeReferenceResolution(F2UContext ctx)
        {
            float maxW = 0f, maxH = 0f;
            if (ctx?.Screens != null)
            {
                foreach (var s in ctx.Screens)
                {
                    if (s == null) continue;
                    if (s.AbsoluteBoundingBox.width > maxW) maxW = s.AbsoluteBoundingBox.width;
                    if (s.AbsoluteBoundingBox.height > maxH) maxH = s.AbsoluteBoundingBox.height;
                }
            }
            return new Vector2(maxW, maxH);
        }

        /// <summary>
        /// Wire the target Canvas's CanvasScaler so screens fit the game view. The tool emits
        /// geometry at 1:1 Figma design pixels and center-anchors each Screen root at its native
        /// design size (e.g. 1920x1200); fitting that to the actual resolution is delegated to
        /// the CanvasScaler (Design.md: "几何 1:1 设计像素，DPI 适配交给 CanvasScaler.referenceResolution").
        /// The default ConstantPixelSize scaler renders 1:1, so any screen taller/wider than the
        /// game view overflows and clips (e.g. the bottom dock gets cut off). ScaleWithScreenSize
        /// + Expand scales by the smaller axis factor, guaranteeing the whole design stays visible
        /// (extra space is letterboxed rather than clipped).
        /// </summary>
        private static void ConfigureCanvasScaler(Canvas canvas, F2UContext ctx)
        {
            var refRes = ComputeReferenceResolution(ctx);
            if (refRes.x <= 0f || refRes.y <= 0f) return; // no screens — leave the scaler as-is.

            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();

            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = refRes;
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
        }

        private static GameObject EnsureCanvasChild(Canvas canvas, string name)
        {
            var t = canvas.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            return go;
        }
    }
}
