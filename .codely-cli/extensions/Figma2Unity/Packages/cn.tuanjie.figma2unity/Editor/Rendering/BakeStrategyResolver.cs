using System.Linq;

namespace Figma2Unity.Editor.Rendering
{
    /// <summary>
    /// Renders a strategy decision from FObject + Config.
    /// v1 switch is the full skeleton — only DirectColor + DownloadSprite produce real output;
    /// Slice9/BakeSprite/VectorPngFallback are v2 territory (Design.md §6.3).
    /// </summary>
    public class BakeStrategyResolver
    {
        public enum Strategy
        {
            DirectColor,
            Slice9,
            BakeSprite,
            DownloadSprite,
            VectorPngFallback,
        }

        /// <summary>
        /// Node types whose shape cannot be expressed by a rounded rectangle alone.
        /// VECTOR / BOOLEAN_OPERATION carry freeform geometry; STAR / REGULAR_POLYGON /
        /// LINE have non-trivial outlines. These always need a sprite (BakeSprite or
        /// VectorPngFallback) because CornerRounder cannot approximate their shapes.
        /// ELLIPSE is NOT listed here — it is handled via DirectColor + CornerRounder
        /// (radius 9999 = circle/oval).
        /// </summary>
        public static readonly string[] SpriteShapeNodeTypes =
        {
            "VECTOR", "BOOLEAN_OPERATION",
            "STAR", "REGULAR_POLYGON", "LINE",
        };

        public virtual Strategy Resolve(FObject fobj, F2UConfig config)
        {
            if (fobj == null) return Strategy.DirectColor;
            var g = fobj.Graphic;

            // Freeform shape nodes whose geometry differs from the bounding box and
            // cannot be approximated by CornerRounder. They must go through BakeSprite
            // or VectorPngFallback so the actual shape is rendered as a PNG.
            bool isSpriteShape = fobj.Type != null && SpriteShapeNodeTypes.Contains(fobj.Type);

            // ELLIPSE with simple solid fill → DirectColor + CornerRounder (radius 9999).
            // Falls through to BakeSprite / VectorPngFallback when the style is complex.
            bool isEllipse = fobj.Type == "ELLIPSE";
            bool isEllipseSimpleSolid = isEllipse && g != null && g.IsSimpleSolid;

            // 1) Simple solid (no rounded corners, no stroke, no gradient, no image) → vertex color
            //    Valid for RECTANGLE / FRAME / INSTANCE / GROUP (bounding box matches visual),
            //    and for ELLIPSE with simple solid fill (ImageDrawer adds CornerRounder).
            if (!isSpriteShape
                && config != null && config.SimpleColorDirectImage
                && g != null && g.IsSimpleSolid
                && (isEllipseSimpleSolid
                    || fobj.CornerRadius == null || fobj.CornerRadius.All(c => c == 0)))
            {
                return Strategy.DirectColor;
            }

            // 2) Auto 9-Slice tag → Sliced Image. Slice9 sprites are produced only by
            // SpriteGenerator/BakeSpritesStep, which no-op when Figmage baking is disabled.
            // Without baking the sprite never lands and the Image would render as an opaque
            // white box, so only choose Slice9 when baking can actually fulfil it.
            if (config != null && config.EnableFigmageBaking
                && fobj.Tags != null && fobj.Tags.Contains(FcuTag.AutoSlice9))
                return Strategy.Slice9;

            // 3) IMAGE Paint → normally download the original uploaded asset via
            //    /v1/files/:key/images (DownloadSprite). Exception: when the node carries a
            //    non-zero `rotation`, Figma renders the image fill with a transform that is
            //    NOT a pure rotation (e.g. a 180° rotation in the canvas comes out as a
            //    horizontal flip on the rendered visual). Re-applying the node rotation in
            //    UGUI on top of the original asset never reproduces that, so we instead ask
            //    Figma to bake the node visual via /v1/images (VectorPngFallback) — which
            //    already returns false from ShouldApplyRotation, so the rendered PNG is
            //    drawn axis-aligned and matches the canvas. Bake-disabled = no-op fallback,
            //    so guard on EnableVectorPngFallback as well.
            if (g != null && g.HasImageFill)
            {
                if (config != null && config.EnableVectorPngFallback
                    && fobj.Rotation != 0f)
                    return Strategy.VectorPngFallback;
                return Strategy.DownloadSprite;
            }

            // 4) Freeform shape nodes (VECTOR / BOOLEAN_OPERATION / STAR /
            //    REGULAR_POLYGON / LINE) — CornerRounder cannot express their shapes.
            if (isSpriteShape)
            {
                if (config != null && config.EnableFigmageBaking
                    && fobj.FillGeometry != null && fobj.FillGeometry.Count > 0)
                    return Strategy.BakeSprite;
                return Strategy.VectorPngFallback;
            }

            // 5) ELLIPSE with complex styling (gradient, stroke, effects, etc.)
            if (isEllipse)
            {
                if (config != null && config.EnableFigmageBaking
                    && fobj.FillGeometry != null && fobj.FillGeometry.Count > 0)
                    return Strategy.BakeSprite;
                return Strategy.VectorPngFallback;
            }

            // 6) Complex styling → GPU bake
            if (config != null && config.EnableFigmageBaking && g != null
                && (g.HasGradientFill || g.HasMultipleFills || g.HasStroke
                    || (fobj.Effects != null && fobj.Effects.Count > 0)
                    || (fobj.CornerRadius != null && fobj.CornerRadius.Any(c => c > 0))))
            {
                return Strategy.BakeSprite;
            }

            return Strategy.DirectColor;
        }
    }
}
