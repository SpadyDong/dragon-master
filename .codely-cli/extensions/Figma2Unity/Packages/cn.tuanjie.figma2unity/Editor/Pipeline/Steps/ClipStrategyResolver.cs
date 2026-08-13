using UnityEngine;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// Decides how a Figma "clip content" frame should be clipped on the UGUI side.
    ///
    /// UGUI's <c>RectMask2D</c> clips in canvas space using an axis-aligned rectangle
    /// (see Clipping.FindCullAndClipWorldRect) — it ignores node rotation entirely. So when
    /// the clip frame, or any ancestor, ends up rotated in UGUI, the RectMask2D's
    /// axis-aligned clip box no longer lines up with the rotated content and slices it on one
    /// side (the "vertical hard cut" symptom). A stencil <c>Mask</c> clips against the mask
    /// graphic's mesh, which rotates with the node, so it is the correct fallback for any
    /// rotated clip frame.
    ///
    /// This resolver is Unity-free (FObject + Mathf only) so it stays unit-testable under the
    /// offline Layer A test project, mirroring <see cref="Layout.RectTransformConverter"/>'s
    /// rotation-application gate exactly.
    /// </summary>
    public static class ClipStrategyResolver
    {
        /// <summary>
        /// True when a <c>RectMask2D</c> will clip correctly — i.e. the clip node is
        /// axis-aligned in canvas space because neither it nor any ancestor has an applied
        /// rotation. When false, callers must fall back to a stencil <c>Mask</c>.
        /// </summary>
        public static bool CanUseRectMask2D(FObject fobj)
        {
            for (var n = fobj; n != null; n = n.Parent)
                if (IsRotationApplied(n)) return false;
            return true;
        }

        /// <summary>
        /// Mirrors RectTransformConverter's rotation gate: a node ends up rotated in UGUI when
        /// its RelativeTransform encodes a non-identity Z rotation AND the converter applies
        /// it. The converter skips rotation for AutoLayout children (the layout slot absorbs
        /// it) and for GLOBAL-synthesized rotation on containers (applied to leaves only) —
        /// such nodes stay axis-aligned, so RectMask2D remains valid for them.
        /// </summary>
        internal static bool IsRotationApplied(FObject fobj)
        {
            if (fobj == null) return false;

            // AutoLayout children: converter skips ApplyRotation (Design.md §5.2 case 4).
            if (fobj.Parent != null && fobj.Parent.Tags != null
                && fobj.Parent.Tags.Contains(FcuTag.AutoLayoutGroup))
                return false;

            // GLOBAL-synthesized rotation is applied to LEAF nodes only; on a container the
            // converter leaves it axis-aligned to avoid spinning its globally-placed children.
            if (fobj.RotationIsGlobalSynthesized && !IsLeaf(fobj))
                return false;

            return HasNonIdentityZRotation(fobj.RelativeTransform);
        }

        private static bool IsLeaf(FObject fobj)
            => fobj == null || fobj.Children == null || fobj.Children.Count == 0;

        private static bool HasNonIdentityZRotation(float[,] m)
        {
            if (m == null) return false;
            if (m.GetLength(0) < 2 || m.GetLength(1) < 2) return false;
            // Same components RectTransformConverter.ApplyRotation reads: a = m[1,0] (sinθ),
            // b = m[1,1] (cosθ). Identity (a≈0, b≈1) ⇒ no rotation.
            float a = m[1, 0];
            float b = m[1, 1];
            return !(Mathf.Approximately(a, 0f) && Mathf.Approximately(b, 1f));
        }
    }
}
