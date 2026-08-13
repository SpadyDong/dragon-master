using System.Linq;

namespace Figma2Unity.Editor.Rendering
{
    /// <summary>
    /// v2 Track A — pure-function 9-slice detection (Design.md §4.2 / §模块 6).
    /// No Tag mutation, no Sprite side-effects — callers (DetectSlice9Step) decide
    /// whether to add <see cref="FcuTag.Slice9"/> or <see cref="FcuTag.AutoSlice9"/>.
    ///
    /// Mirrors FCU TagSetter.Is9slice / IsAutoSlice9 (Assets/DA-Assets/Figma-Converter-for-Unity/
    /// Runtime/Scripts/App/Setters/TagSetter.cs lines ~770-880).
    /// </summary>
    public static class Slice9Detector
    {
        /// <summary>
        /// Manual 9-slice: exactly 9 children with the canonical 3×3 constraint grid:
        ///   row 0 (top)   : (MIN,MIN)     (STRETCH,MIN)     (MAX,MIN)
        ///   row 1 (center): (MIN,STRETCH) (STRETCH,STRETCH) (MAX,STRETCH)
        ///   row 2 (bottom): (MIN,MAX)     (STRETCH,MAX)     (MAX,MAX)
        /// Child order is read left-to-right, top-to-bottom (Figma's normal sibling order
        /// inside a FRAME). When the order matches, return true.
        /// </summary>
        public static bool IsManual9Slice(FObject fobj)
        {
            if (fobj == null || fobj.Children == null) return false;
            if (fobj.Children.Count != 9) return false;

            // Expected (Horizontal, Vertical) constraints per slot.
            // Vertical: MIN = Figma top, MAX = Figma bottom (matches AnchorMapper).
            var expected = new[]
            {
                (ConstraintType.MIN,     ConstraintType.MIN),     // TL
                (ConstraintType.STRETCH, ConstraintType.MIN),     // top-stretch
                (ConstraintType.MAX,     ConstraintType.MIN),     // TR
                (ConstraintType.MIN,     ConstraintType.STRETCH), // left-stretch
                (ConstraintType.STRETCH, ConstraintType.STRETCH), // center
                (ConstraintType.MAX,     ConstraintType.STRETCH), // right-stretch
                (ConstraintType.MIN,     ConstraintType.MAX),     // BL
                (ConstraintType.STRETCH, ConstraintType.MAX),     // bottom-stretch
                (ConstraintType.MAX,     ConstraintType.MAX),     // BR
            };

            for (int i = 0; i < 9; i++)
            {
                var c = fobj.Children[i]?.Constraints;
                if (c == null) return false;
                if (c.Horizontal != expected[i].Item1 || c.Vertical != expected[i].Item2)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Auto 9-slice: rounded-rect + solid fills/strokes only, no children, and
        /// any effects must be 9-slice-compatible (symmetric DROP/INNER_SHADOW with
        /// offset==0, or uniform LAYER_BLUR / BACKGROUND_BLUR).
        /// </summary>
        public static bool IsAuto9Slice(FObject fobj)
        {
            if (fobj == null) return false;
            // Must NOT have non-ignored children.
            if (fobj.Children != null && fobj.Children.Count > 0)
                return false;

            // Rounded corner: at least one corner radius > 0.
            if (fobj.CornerRadius == null || !fobj.CornerRadius.Any(r => r > 0f))
                return false;

            // Rectangle-like: RECTANGLE or FRAME with no FillGeometry (= not a custom path).
            bool isRectangleLike = fobj.Type == "RECTANGLE"
                || ((fobj.Type == "FRAME" || fobj.Type == "INSTANCE" || fobj.Type == "COMPONENT")
                    && (fobj.FillGeometry == null || fobj.FillGeometry.Count == 0));
            if (!isRectangleLike) return false;

            // Fills/Strokes must be visible solid paints (or absent).
            if (fobj.Fills != null)
            {
                foreach (var p in fobj.Fills)
                {
                    if (p == null || !p.Visible) continue;
                    if (p.Type != PaintType.SOLID) return false;
                }
            }
            if (fobj.Strokes != null)
            {
                foreach (var p in fobj.Strokes)
                {
                    if (p == null || !p.Visible) continue;
                    if (p.Type != PaintType.SOLID) return false;
                }
            }

            // Effects: must be 9-slice safe.
            if (fobj.Effects != null)
            {
                foreach (var e in fobj.Effects)
                {
                    if (e == null || !e.Visible) continue;
                    switch (e.Type)
                    {
                        case EffectType.DROP_SHADOW:
                        case EffectType.INNER_SHADOW:
                            // Only symmetric (offset == 0) shadows keep center uniform.
                            if (e.Offset.x != 0f || e.Offset.y != 0f) return false;
                            break;
                        case EffectType.LAYER_BLUR:
                        case EffectType.BACKGROUND_BLUR:
                            // Uniform blur is always 9-slice compatible.
                            break;
                        default:
                            return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Pixel border for the four edges of an Auto-Slice9 sprite: returns the largest
        /// corner radius scaled by spriteScale, rounded up to an integer pixel.
        /// </summary>
        public static int AutoBorderPixels(FObject fobj, float spriteScale)
        {
            if (fobj?.CornerRadius == null || fobj.CornerRadius.Length == 0) return 0;
            float max = 0f;
            for (int i = 0; i < fobj.CornerRadius.Length; i++)
                if (fobj.CornerRadius[i] > max) max = fobj.CornerRadius[i];
            return (int)System.Math.Ceiling(max * spriteScale);
        }
    }
}
