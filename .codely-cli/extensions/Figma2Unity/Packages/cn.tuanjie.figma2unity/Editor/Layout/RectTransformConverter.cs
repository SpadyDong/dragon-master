using UnityEngine;

namespace Figma2Unity.Editor.Layout
{
    /// <summary>
    /// Converts Figma absolute coordinates → UGUI RectTransform anchors/offsets, and applies
    /// Z rotation extracted from the node's RelativeTransform (synthesized from the node-level
    /// `rotation` field when Figma omits the matrix — see FigmaDataParser).
    ///
    /// Rotation gating: <paramref name="applyRotation"/> on the constraint/auto-layout paths
    /// lets the caller skip rotation for nodes whose pixels are already baked rotated (e.g.
    /// VectorPngFallback / BakeSprite / Slice9 /v1/images renders). Re-rotating those would
    /// double-rotate the icon. The F2UContext-aware overload (RectTransformConverterContext)
    /// computes this flag from the node's BakeStrategyResolver strategy.
    /// </summary>
    public class RectTransformConverter
    {
        protected readonly float _scaleFactor;

        public RectTransformConverter(float scaleFactor)
        {
            _scaleFactor = scaleFactor <= 0f ? 1f : scaleFactor;
        }

        /// <summary>
        /// Pure overload — takes RectTransform directly. Layer A tests call this without
        /// needing a F2UContext / Canvas / GameObject hierarchy.
        /// </summary>
        public void ApplyToRect(FObject fobj, RectTransform rt)
            => ApplyToRect(fobj, rt, fobj == null || fobj.Parent == null);

        /// <summary>
        /// <paramref name="treatAsRoot"/> lets the caller force root-stretch when the parent
        /// FObject has no corresponding GameObject (e.g. the virtual page / a skipped node) —
        /// otherwise the node would anchor against a zero-sized parent. See Design.md §5.2.
        /// <paramref name="applyRotation"/> (default true) gates ApplyRotation so callers can
        /// skip rotation for already-baked-rotated sprite nodes (see class summary).
        /// </summary>
        public void ApplyToRect(FObject fobj, RectTransform rt, bool treatAsRoot, bool applyRotation = true)
        {
            if (fobj == null || rt == null) return;

            if (treatAsRoot || fobj.Parent == null)
            {
                ApplyRootStretch(rt, fobj);
                return;
            }

            if (fobj.Parent.Tags != null && fobj.Parent.Tags.Contains(FcuTag.AutoLayoutGroup))
            {
                ApplyAutoLayoutChild(rt, fobj);
                return;
            }

            ApplyWithConstraints(rt, fobj, applyRotation);
        }

        private void ApplyRootStretch(RectTransform rt, FObject fobj)
        {
            float w = fobj.AbsoluteBoundingBox.width * _scaleFactor;
            float h = fobj.AbsoluteBoundingBox.height * _scaleFactor;

            // Screen root (parent is the VirtualPage / "PAGE") must NOT stretch — Figma frames
            // carry a fixed design size (e.g. 1920x1200). Stretching them to fill Canvas with
            // sizeDelta=(figmaW, figmaH) layered every screen on top of each other and pushed
            // children off-canvas. Center-anchor at design size and let PrototypeFlowController's
            // Screens parent (anchor stretch, offset 0) provide the viewport.
            if (fobj.Parent != null && fobj.Parent.Type == "PAGE")
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(w, h);
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        private void ApplyAutoLayoutChild(RectTransform rt, FObject fobj)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var alBounds = EffectiveBounds(fobj);
            rt.sizeDelta = new Vector2(
                alBounds.width * _scaleFactor,
                alBounds.height * _scaleFactor);
            // AutoLayout children: Figma's auto-layout already absorbs the visual rotation
            // into the layout slot — UGUI's LayoutGroup is the source of truth here. Skip
            // ApplyRotation so we don't double-rotate the child. (Design.md §5.2 case 4.)
        }

        public void ApplyWithConstraints(RectTransform rt, FObject fobj, bool applyRotation = true)
        {
            var pb = fobj.Parent.AbsoluteBoundingBox;
            // applyRotation == false means this node's visual is a Figma-baked PNG produced by
            // /v1/images (VectorPngFallback / BakeSprite / Slice9). Figma crops that render to
            // the node's absoluteRenderBounds (the actual drawn extent incl. stroke overflow /
            // partial-arc geometry), NOT its absoluteBoundingBox. Sizing the RectTransform to
            // the geometric box while showing a render-bounds-cropped PNG non-uniformly
            // stretches it — e.g. a stroked progress ARC's tight PNG (0.72 aspect) squeezed
            // into the near-square full-circle box (0.93 aspect) distorts the circle. So for
            // baked-PNG nodes we size + position from renderBounds to match the PNG 1:1.
            var bounds = EffectiveBounds(fobj, preferRenderBounds: !applyRotation);

            var constraints = fobj.Constraints ?? new Constraints
            {
                Horizontal = ConstraintType.MIN,
                Vertical = ConstraintType.MIN,
            };

            var anchor = AnchorMapper.Map(constraints, fobj, fobj.Parent);
            rt.anchorMin = anchor.Min;
            rt.anchorMax = anchor.Max;
            rt.pivot = new Vector2(0.5f, 0.5f);

            float scaledW = bounds.width * _scaleFactor;
            float scaledH = bounds.height * _scaleFactor;
            float parentW = pb.width * _scaleFactor;
            float parentH = pb.height * _scaleFactor;

            float relX = (bounds.x - pb.x) * _scaleFactor;
            float relY = (bounds.y - pb.y) * _scaleFactor;

            // UGUI Y is up; convert from Figma Y-down local offset.
            float uguiX = relX;
            float uguiY = parentH - relY - scaledH;

            bool stretchX = !Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x);
            bool stretchY = !Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y);

            float anchorMinXPx = rt.anchorMin.x * parentW;
            float anchorMaxXPx = rt.anchorMax.x * parentW;
            float anchorMinYPx = rt.anchorMin.y * parentH;
            float anchorMaxYPx = rt.anchorMax.y * parentH;

            Vector2 anchorCenter = (rt.anchorMin + rt.anchorMax) * 0.5f;
            float ax = anchorCenter.x * parentW;
            float ay = anchorCenter.y * parentH;

            float anchoredX = uguiX + scaledW * 0.5f - ax;
            float anchoredY = uguiY + scaledH * 0.5f - ay;
            rt.anchoredPosition = new Vector2(anchoredX, anchoredY);
            rt.sizeDelta = new Vector2(scaledW, scaledH);

            if (stretchX || stretchY)
            {
                float left = uguiX - anchorMinXPx;
                float right = uguiX + scaledW - anchorMaxXPx;
                float bottom = uguiY - anchorMinYPx;
                float top = uguiY + scaledH - anchorMaxYPx;

                var oMin = rt.offsetMin;
                var oMax = rt.offsetMax;
                if (stretchX)
                {
                    oMin.x = left;
                    oMax.x = right;
                }
                if (stretchY)
                {
                    oMin.y = bottom;
                    oMax.y = top;
                }
                rt.offsetMin = oMin;
                rt.offsetMax = oMax;
            }

            // Rotation is applied to LEAF nodes only — never to containers. This importer
            // flattens layout into global coordinates: every node is placed by its own global
            // (already-rotated) AABB and a child's local offset is computed as
            // childAABB − parentAABB assuming the parent stays axis-aligned. Rotating a
            // container would spin its globally positioned children around the container,
            // flipping/displacing them (the "180° wrong" bug).
            //
            // This holds for BOTH rotation sources:
            //   • GLOBAL-synthesized rotation (from the node-level `rotation` field of a
            //     matrix-less export) is accumulated through ancestors by the parser, so a
            //     leaf already carries its full global angle.
            //   • An explicit Figma relativeTransform on a rotated container (e.g. a navigate
            //     icon / arrow INSTANCE) is NOT applied either: the container's visual is a
            //     baked sprite produced by Figma's /v1/images render, which already bakes the
            //     node's global rotation into the PNG pixels. Re-rotating that sprite via UGUI
            //     localRotation double-rotates the icon (the navicon/arrow orientation bug).
            //     Mirrors FCU's TransformSetter: "Sprite pixels are already produced by
            //     download/generation, so Unity transform rotation must not be applied on top
            //     of them."
            if (applyRotation && IsLeaf(fobj) && ApplyRotation(rt, fobj))
            {
                // When localRotation is applied, sizeDelta must use the node's LOCAL (unrotated)
                // size — AbsoluteBoundingBox is the post-rotation world AABB, so using it here
                // counts the rotation twice (the rotated-rectangle double-transform bug: e.g. a
                // 3×20 bar rotated 90° has size {3,20} but AABB {20,3}; sizeDelta {20,3} + 90°
                // rotation renders a 3×20 vertical bar instead of the intended 20×3 horizontal).
                // Position is unaffected: pivot is (0.5,0.5) and the AABB center coincides with
                // the rotated rect's center, so anchoredPosition above stays correct. Stretch
                // nodes are excluded — their geometry is driven by offsetMin/Max, not sizeDelta.
                if (!stretchX && !stretchY && fobj.Size.x > 0f && fobj.Size.y > 0f)
                    rt.sizeDelta = new Vector2(fobj.Size.x * _scaleFactor, fobj.Size.y * _scaleFactor);
            }
        }

        private static bool IsLeaf(FObject fobj)
            => fobj == null || fobj.Children == null || fobj.Children.Count == 0;

        /// <summary>
        /// The bounds to size/position the RectTransform from. Normally the node's
        /// AbsoluteBoundingBox (the geometric box, matching the baked sprite's box).
        ///
        /// <paramref name="preferRenderBounds"/>: for nodes whose visual is a Figma /v1/images
        /// render (VectorPngFallback / BakeSprite / Slice9 — the caller passes this when
        /// rotation is NOT applied), the returned PNG is cropped to the node's
        /// AbsoluteRenderBounds — the ACTUAL drawn extent (stroke overflow, partial-arc
        /// geometry, effects), which for a stroked/partial shape differs from the geometric
        /// box. Sizing to the geometric box while showing a render-bounds PNG non-uniformly
        /// stretches it (e.g. a progress ARC's 0.72-aspect PNG forced into the ~square
        /// full-circle box → distorted circle). So when render bounds are valid we size from
        /// them to match the PNG 1:1. Only trusted when render bounds are non-degenerate.
        ///
        /// Exception (always applied, even without <paramref name="preferRenderBounds"/>): a
        /// thin stroke/line VECTOR can have a DEGENERATE box with zero width or height (e.g. a
        /// vertical line has size {0, H}). Its visible pixels come entirely from the stroke,
        /// whose extent Figma reports in AbsoluteRenderBounds. Sizing from the zero-width box
        /// collapses the Image to nothing (the "Vector 17 invisible" bug). Render bounds share
        /// the same center, so position stays correct under the (0.5,0.5) pivot.
        ///
        /// For NON-baked nodes (DirectColor / DownloadSprite) render bounds are NOT preferred:
        /// they grow for shadows/blur, so using them unconditionally would oversize the box.
        /// </summary>
        private static Rect EffectiveBounds(FObject fobj, bool preferRenderBounds = false)
        {
            var box = fobj.AbsoluteBoundingBox;
            var rb = fobj.AbsoluteRenderBounds;
            bool boxDegenerate = box.width <= 0f || box.height <= 0f;
            bool rbValid = rb.width > 0f && rb.height > 0f;

            // Baked-PNG nodes: match the render-bounds crop the sprite was produced from.
            if (preferRenderBounds && rbValid) return rb;

            if (!boxDegenerate) return box;

            // Degenerate geometric box → fall back to render bounds so the sprite isn't
            // collapsed to zero size.
            if (rbValid) return rb;
            return box;
        }

        /// <summary>
        /// Extracts the Z rotation from Figma's RelativeTransform (2x3 affine matrix) and
        /// writes it to <c>rt.localRotation</c>. Mirrors FCU's
        /// <c>TransformExtensions.GetAngleFromMatrix</c>:
        ///   a = m[1,0] (sin θ in Figma's Y-down basis)
        ///   b = m[1,1] (cos θ)
        ///   rotRad = -Atan2(a, b)   ← negation accounts for Figma Y-down vs UGUI Y-up
        ///   rotDeg = rotRad * Rad2Deg
        /// Identity (or near-identity) matrices early-out so non-rotated nodes keep
        /// <c>Quaternion.identity</c>.
        /// Returns <c>true</c> when a non-identity rotation was written, so the caller can
        /// switch sizeDelta to the node's local (unrotated) size and avoid double-transform.
        /// </summary>
        private static bool ApplyRotation(RectTransform rt, FObject fobj)
        {
            var m = fobj.RelativeTransform;
            if (m == null) return false;
            if (m.GetLength(0) < 2 || m.GetLength(1) < 2) return false;

            float a = m[1, 0];
            float b = m[1, 1];

            // Identity / no rotation: skip to keep Quaternion.identity (avoids -0 noise in
            // snapshots and lets the test for "no RelativeTransform → identity" pass cleanly).
            if (Mathf.Approximately(a, 0f) && Mathf.Approximately(b, 1f)) return false;

            float rotRad = -Mathf.Atan2(a, b);
            float rotDeg = rotRad * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotDeg);
            return true;
        }
    }
}
