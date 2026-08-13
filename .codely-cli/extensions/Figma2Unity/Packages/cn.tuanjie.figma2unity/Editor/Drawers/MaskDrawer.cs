using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// MaskDrawer — attaches UGUI Mask. A Mask requires a Graphic on the same node to
    /// supply its alpha; if none exists yet we add a near-transparent Image (visually
    /// invisible but participates in the stencil pass).
    ///
    /// showMaskGraphic gates whether the mask's own graphic is rendered to the color buffer:
    ///   • true  → the Figma node's own visual (rounded frame, solid/image fill) keeps drawing.
    ///   • false → the graphic feeds ONLY the stencil; nothing is painted.
    /// It must follow the Figma fill visibility. A clip frame whose fill is invisible in Figma
    /// (fill `visible:false` or alpha 0 — common on icon masks like `navicon`) must NOT render
    /// its graphic: doing so paints the near-transparent placeholder (alpha 0.01) as a faint
    /// translucent rect that blends with the background, which does not exist in the design.
    /// </summary>
    public class MaskDrawer
    {
        public virtual void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            var img = go.GetComponent<Image>();
            if (img == null)
            {
                img = go.AddComponent<Image>();
                // Slight non-zero alpha so the Mask stencil writes; pure 0 would skip the
                // draw call entirely on some platforms and break masking.
                img.color = new Color(1f, 1f, 1f, 0.01f);
            }

            var mask = go.EnsureComponent<Mask>();
            // Only render the mask graphic when the Figma node actually carries a visible fill
            // (mirrors ImageDrawer.hasVisibleFill). Otherwise keep it stencil-only so an
            // invisible-fill clip mask doesn't paint a faint translucent box.
            mask.showMaskGraphic = HasVisibleFill(fobj);
        }

        /// <summary>
        /// True when the node has a visible fill that should be painted. Mirrors
        /// <see cref="ImageDrawer"/>'s visibility test — FGraphic flags are computed by
        /// ComputeGraphicsStep, which already drops fills with <c>visible:false</c>.
        /// </summary>
        private static bool HasVisibleFill(FObject fobj)
            => fobj?.Graphic != null
               && (fobj.Graphic.HasSolidFill
                   || fobj.Graphic.HasGradientFill
                   || fobj.Graphic.HasImageFill);
    }
}
