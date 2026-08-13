using UnityEngine;
using UnityEngine.UI;
using Figma2Unity.Editor.Rendering;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// ImageDrawer — v2: all five branches active. DirectColor sets vertex color directly;
    /// the four sprite-producing strategies (DownloadSprite / Slice9 / BakeSprite /
    /// VectorPngFallback) only prep the Image component — sprite assignment happens in
    /// <c>ApplySpritesStep</c> after <c>DownloadSpritesStep</c> + <c>BakeSpritesStep</c>
    /// finish landing the actual PNG assets (Design.md §三 #11/#15/#16).
    ///
    /// Non-rectangle shapes (ELLIPSE) whose geometry differs from the bounding box are
    /// rendered via <c>F2UCornerRounder</c> when they have a simple solid fill. It uses a
    /// shader to mask the Image quad into the correct shape at runtime — no sprite download
    /// needed. F2UCornerRounder is F2U's own vendored copy of the DA corner rounder, so it
    /// is referenced directly (F2URuntime) without any external DA_Assets.CR dependency.
    /// </summary>
    public class ImageDrawer
    {
        private readonly BakeStrategyResolver _resolver = new BakeStrategyResolver();

        public void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            var strategy = _resolver.Resolve(fobj, ctx.Config);

            // A fill-less container (FRAME/GROUP/INSTANCE without a visible fill) must NOT be
            // painted as an opaque white box — that would occlude its children. Skip drawing
            // an Image entirely so the node stays a transparent layout container.
            bool hasVisibleFill = fobj.Graphic != null
                && (fobj.Graphic.HasSolidFill || fobj.Graphic.HasGradientFill
                    || fobj.Graphic.HasImageFill);
            bool isVectorNode = fobj.Type == "VECTOR" || fobj.Type == "BOOLEAN_OPERATION";
            if (strategy == BakeStrategyResolver.Strategy.DirectColor && !hasVisibleFill && !isVectorNode)
                return;

            var img = go.EnsureComponent<Image>();
            img.raycastTarget = false;

            switch (strategy)
            {
                case BakeStrategyResolver.Strategy.DirectColor:
                {
                    Color c = Color.white;
                    if (fobj.Graphic != null && fobj.Graphic.Fills.Count > 0)
                        c = CompositeSolidFills(fobj.Graphic.Fills);
                    img.sprite = null;
                    img.color = c;
                    img.type = Image.Type.Simple;
                    break;
                }
                case BakeStrategyResolver.Strategy.DownloadSprite:
                {
                    img.type = Image.Type.Simple;
                    img.color = ImageTintColor(fobj);
                    break;
                }
                case BakeStrategyResolver.Strategy.Slice9:
                {
                    img.type = Image.Type.Sliced;
                    img.color = ImageTintColor(fobj);
                    break;
                }
                case BakeStrategyResolver.Strategy.BakeSprite:
                {
                    img.type = Image.Type.Simple;
                    img.color = ImageTintColor(fobj);
                    break;
                }
                case BakeStrategyResolver.Strategy.VectorPngFallback:
                {
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                    break;
                }
            }

            // Apply rounded-corner / circular masking. DirectColor paints a flat-colored
            // quad and DownloadSprite shows the ORIGINAL uploaded image asset — both are
            // rectangular and must be masked into the node's rounded/circular shape via
            // CornerRounder. BakeSprite / VectorPngFallback already bake the true shape into
            // the PNG (masking again would double-clip), and Slice9 must keep its 9-slice
            // borders intact, so they are intentionally excluded.
            if (strategy == BakeStrategyResolver.Strategy.DirectColor
                || strategy == BakeStrategyResolver.Strategy.DownloadSprite)
            {
                TryAddCornerRounder(fobj, go);
            }
        }

        /// <summary>
        /// Tint color for a sprite-backed Image. The sprite shows unmodulated (white) by
        /// default, but a Figma IMAGE fill can carry an <c>opacity</c> &lt; 1 (e.g. a 30%
        /// "Background"/"Overlay" image meant to blend with what's behind it). UGUI multiplies
        /// the sprite by <c>Image.color</c>, so we fold that fill opacity into the alpha. We
        /// read the TOPMOST VISIBLE image fill — the same one DownloadSpritesStep picks as the
        /// sprite — so opacity and sprite stay consistent.
        /// </summary>
        private static Color ImageTintColor(FObject fobj)
        {
            float alpha = 1f;
            if (fobj.Graphic != null && fobj.Graphic.Fills != null)
            {
                foreach (var f in fobj.Graphic.Fills)
                {
                    if (f != null && f.Type == PaintType.IMAGE)
                        alpha = Mathf.Clamp01(f.Opacity); // keep last → topmost image fill
                }
            }
            return new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>
        /// Flatten stacked SOLID fills into a single vertex color using alpha-over
        /// compositing. Figma orders fills bottom→top (<c>Fills[0]</c> is the bottom
        /// layer), which is the order this iterates, painting each higher fill over the
        /// accumulated result. This is the DirectColor fallback used when Figmage baking
        /// is disabled; a common Figma pattern is a base color plus a translucent tint
        /// overlay (e.g. a "selected" pill), and taking only <c>Fills[0]</c> would silently
        /// drop the overlay. Non-solid fills (gradient/image) cannot be expressed as a flat
        /// color here and are skipped. When no solid fill exists, falls back to the first
        /// fill's color (prior behaviour), or opaque white when there are no fills.
        /// </summary>
        private static Color CompositeSolidFills(System.Collections.Generic.List<FFill> fills)
        {
            Color result = new Color(0f, 0f, 0f, 0f);
            bool any = false;
            foreach (var f in fills)
            {
                if (f == null || f.Type != PaintType.SOLID) continue;
                Color src = f.Color;
                src.a *= Mathf.Clamp01(f.Opacity);
                if (!any)
                {
                    result = src; // bottom-most solid fill seeds the accumulator
                    any = true;
                    continue;
                }
                // src (higher in the stack) painted OVER the accumulated result.
                float outA = src.a + result.a * (1f - src.a);
                if (outA <= 0f)
                {
                    result = new Color(0f, 0f, 0f, 0f);
                    continue;
                }
                float r = (src.r * src.a + result.r * result.a * (1f - src.a)) / outA;
                float g = (src.g * src.a + result.g * result.a * (1f - src.a)) / outA;
                float b = (src.b * src.a + result.b * result.a * (1f - src.a)) / outA;
                result = new Color(r, g, b, outA);
            }

            if (!any)
            {
                if (fills.Count > 0 && fills[0] != null)
                {
                    Color c0 = fills[0].Color;
                    c0.a *= Mathf.Clamp01(fills[0].Opacity);
                    return c0;
                }
                return Color.white;
            }
            return result;
        }

        /// <summary>
        /// Add F2UCornerRounder to ELLIPSE nodes and to nodes with rounded corners.
        /// F2UCornerRounder is F2U's own vendored component, referenced directly.
        /// </summary>
        private void TryAddCornerRounder(FObject fobj, GameObject go)
        {
            if (go == null) return;

            // ELLIPSE → circular mask (large radius = circle/oval)
            if (fobj.Type == "ELLIPSE")
            {
                var cr = go.EnsureComponent<F2UCornerRounder>();
                cr.SetRadii(new Vector4(9999f, 9999f, 9999f, 9999f));
                return;
            }

            // RECTANGLE / FRAME / INSTANCE with rounded corners
            if (fobj.CornerRadius != null)
            {
                bool hasCorners = false;
                foreach (var c in fobj.CornerRadius)
                {
                    if (c > 0f) { hasCorners = true; break; }
                }
                if (hasCorners)
                {
                    var cr = go.EnsureComponent<F2UCornerRounder>();
                    cr.SetRadii(GetCornerRadii(fobj));
                }
            }
        }

        private static Vector4 GetCornerRadii(FObject fobj)
        {
            if (fobj.CornerRadius == null || fobj.CornerRadius.Length == 0)
                return Vector4.zero;

            if (fobj.CornerRadius.Length == 1)
                return new Vector4(fobj.CornerRadius[0], fobj.CornerRadius[0],
                                   fobj.CornerRadius[0], fobj.CornerRadius[0]);

            // Figma order for 4-corner array: [TL, TR, BR, BL]
            // CornerRounder Vector4: (BL, BR, TR, TL)
            if (fobj.CornerRadius.Length >= 4)
            {
                return new Vector4(
                    fobj.CornerRadius[3], // BL
                    fobj.CornerRadius[2], // BR
                    fobj.CornerRadius[1], // TR
                    fobj.CornerRadius[0]  // TL
                );
            }

            return new Vector4(fobj.CornerRadius[0], fobj.CornerRadius[0],
                               fobj.CornerRadius[0], fobj.CornerRadius[0]);
        }
    }
}
