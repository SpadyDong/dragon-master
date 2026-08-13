using TMPro;
using UnityEngine;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// v2: TMP basics + line height, letter spacing, AutoResize, and rich-text via
    /// <see cref="RichTextBuilder"/>. Font asset assignment happens in
    /// <c>DownloadFontsStep</c>; this drawer falls back to <c>F2UConfig.FallbackFont</c>.
    /// </summary>
    public class TextDrawer
    {
        public virtual void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            var tmp = go.EnsureComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;

            // Build the displayed text — rich-text rebuild only when overrides exist.
            tmp.richText = true;
            tmp.text = RichTextBuilder.Build(
                fobj.Characters ?? string.Empty,
                fobj.Style,
                fobj.CharacterStyleOverrides,
                fobj.StyleOverrideTable);

            if (fobj.Style != null)
            {
                if (fobj.Style.FontSize > 0f)
                    tmp.fontSize = fobj.Style.FontSize;
                tmp.alignment = ConvertAlignment(fobj.Style.TextAlignHorizontal, fobj.Style.TextAlignVertical);
                tmp.fontStyle = ConvertFontStyle(fobj.Style);

                // Figma LineHeight is in pixels; TMP lineSpacing is in font-units relative to
                // the font size. Convert: extra leading = (LineHeightPx - FontSize) / FontSize * 100.
                if (fobj.Style.LineHeight > 0f && fobj.Style.FontSize > 0f)
                {
                    float extra = (fobj.Style.LineHeight - fobj.Style.FontSize) / fobj.Style.FontSize * 100f;
                    tmp.lineSpacing = extra;
                }

                // LetterSpacing: Figma px → TMP characterSpacing units (1 unit ≈ 1/100 em).
                if (fobj.Style.LetterSpacing != 0f && fobj.Style.FontSize > 0f)
                    tmp.characterSpacing = fobj.Style.LetterSpacing / fobj.Style.FontSize * 100f;

                ApplyAutoResize(tmp, fobj.Style.TextAutoResize);
            }

            ApplyFill(tmp, fobj);

            if (ctx.Config != null && ctx.Config.FallbackFont != null && tmp.font == null)
                tmp.font = ctx.Config.FallbackFont;
        }

        private static void ApplyAutoResize(TextMeshProUGUI tmp, TextAutoResize mode)
        {
            switch (mode)
            {
                case TextAutoResize.HEIGHT:
                    tmp.enableAutoSizing = false;
                    tmp.enableWordWrapping = true;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    break;
                case TextAutoResize.WIDTH_AND_HEIGHT:
                    tmp.enableAutoSizing = false;
                    tmp.enableWordWrapping = false;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    break;
                default:
                    // Figma "fixed size" (TextAutoResize.NONE). TMP's Truncate is the wrong
                    // mapping: when the box is even a hair shorter than the line height (common
                    // when the design font falls back to LiberationSans, whose metrics are
                    // taller), Truncate drops the ENTIRE string and the label vanishes — leaving
                    // only the parent background. Use Overflow so glyphs always render even if
                    // they slightly exceed the Figma box; wrapping still flows long text by width.
                    tmp.enableAutoSizing = false;
                    tmp.enableWordWrapping = true;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    break;
            }
        }

        /// <summary>
        /// Resolves the fill of a Figma TEXT node onto the TMP component. Solid fills map to
        /// <see cref="TMP_Text.color"/>; linear gradients map to TMP's per-vertex gradient
        /// (<see cref="TMP_Text.enableVertexGradient"/> + <see cref="TMP_Text.colorGradient"/>).
        /// TMP can only approximate a Figma gradient with the four corner colors of the text
        /// bounds, so the gradient stops are sampled along the Figma gradient direction and
        /// projected onto those corners; full per-character gradients require a custom shader.
        /// </summary>
        private static void ApplyFill(TextMeshProUGUI tmp, FObject fobj)
        {
            var fill = ResolveFill(fobj);
            if (fill == null)
            {
                tmp.enableVertexGradient = false;
                tmp.color = Color.black;
                return;
            }

            if (fill.Type == PaintType.SOLID)
            {
                tmp.enableVertexGradient = false;
                var c = fill.Color;
                c.a *= Mathf.Clamp01(fill.Opacity);
                tmp.color = c;
                return;
            }

            if (IsGradient(fill.Type) && fill.Gradient != null && fill.Gradient.Stops != null
                && fill.Gradient.Stops.Count > 0)
            {
                ApplyGradient(tmp, fill);
                return;
            }

            tmp.enableVertexGradient = false;
            tmp.color = Color.black;
        }

        private static FFill ResolveFill(FObject fobj)
        {
            if (fobj.Graphic == null) return null;
            FFill firstGradient = null;
            foreach (var f in fobj.Graphic.Fills)
            {
                if (f.Type == PaintType.SOLID)
                    return f;
                if (firstGradient == null && IsGradient(f.Type))
                    firstGradient = f;
            }
            return firstGradient;
        }

        private static bool IsGradient(PaintType type)
        {
            return type == PaintType.GRADIENT_LINEAR
                || type == PaintType.GRADIENT_RADIAL
                || type == PaintType.GRADIENT_ANGULAR
                || type == PaintType.GRADIENT_DIAMOND;
        }

        private static void ApplyGradient(TextMeshProUGUI tmp, FFill fill)
        {
            float opacity = Mathf.Clamp01(fill.Opacity);
            Color startColor = SampleGradient(fill.Gradient, 0f);
            Color endColor = SampleGradient(fill.Gradient, 1f);
            startColor.a *= opacity;
            endColor.a *= opacity;

            // Keep base color opaque white so the vertex gradient is shown unmodulated.
            tmp.color = Color.white;
            tmp.enableVertexGradient = true;

            // Determine the dominant gradient direction from Figma's handle positions
            // (start handle -> end handle). Figma's Y axis points down, so a positive
            // dy means the gradient runs top -> bottom.
            float dx = 0f, dy = 1f;
            var h = fill.Gradient.GradientHandlePositions;
            if (h != null && h.Length >= 4)
            {
                dx = h[2] - h[0];
                dy = h[3] - h[1];
            }

            VertexGradient vg;
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                // Horizontal: left = start, right = end.
                vg = new VertexGradient(startColor, endColor, startColor, endColor);
            }
            else
            {
                // Vertical (default): top = start, bottom = end (Figma Y points down).
                vg = new VertexGradient(startColor, startColor, endColor, endColor);
            }
            tmp.colorGradient = vg;
        }

        /// <summary>Samples the gradient color at normalized position t in [0,1].</summary>
        private static Color SampleGradient(GradientData gradient, float t)
        {
            var stops = gradient.Stops;
            if (stops.Count == 1) return stops[0].Color;

            t = Mathf.Clamp01(t);
            if (t <= stops[0].Position) return stops[0].Color;
            if (t >= stops[stops.Count - 1].Position) return stops[stops.Count - 1].Color;

            for (int i = 0; i < stops.Count - 1; i++)
            {
                var a = stops[i];
                var b = stops[i + 1];
                if (t >= a.Position && t <= b.Position)
                {
                    float range = b.Position - a.Position;
                    float f = range <= Mathf.Epsilon ? 0f : (t - a.Position) / range;
                    return Color.Lerp(a.Color, b.Color, f);
                }
            }
            return stops[stops.Count - 1].Color;
        }

        private static TextAlignmentOptions ConvertAlignment(
            Figma2Unity.TextAlignHorizontal h, Figma2Unity.TextAlignVertical v)
        {
            switch (v)
            {
                case Figma2Unity.TextAlignVertical.TOP:
                    switch (h)
                    {
                        case Figma2Unity.TextAlignHorizontal.LEFT: return TextAlignmentOptions.TopLeft;
                        case Figma2Unity.TextAlignHorizontal.CENTER: return TextAlignmentOptions.Top;
                        case Figma2Unity.TextAlignHorizontal.RIGHT: return TextAlignmentOptions.TopRight;
                        case Figma2Unity.TextAlignHorizontal.JUSTIFIED: return TextAlignmentOptions.TopJustified;
                    }
                    break;
                case Figma2Unity.TextAlignVertical.CENTER:
                    switch (h)
                    {
                        case Figma2Unity.TextAlignHorizontal.LEFT: return TextAlignmentOptions.Left;
                        case Figma2Unity.TextAlignHorizontal.CENTER: return TextAlignmentOptions.Center;
                        case Figma2Unity.TextAlignHorizontal.RIGHT: return TextAlignmentOptions.Right;
                        case Figma2Unity.TextAlignHorizontal.JUSTIFIED: return TextAlignmentOptions.Justified;
                    }
                    break;
                case Figma2Unity.TextAlignVertical.BOTTOM:
                    switch (h)
                    {
                        case Figma2Unity.TextAlignHorizontal.LEFT: return TextAlignmentOptions.BottomLeft;
                        case Figma2Unity.TextAlignHorizontal.CENTER: return TextAlignmentOptions.Bottom;
                        case Figma2Unity.TextAlignHorizontal.RIGHT: return TextAlignmentOptions.BottomRight;
                        case Figma2Unity.TextAlignHorizontal.JUSTIFIED: return TextAlignmentOptions.BottomJustified;
                    }
                    break;
            }
            return TextAlignmentOptions.TopLeft;
        }

        private static FontStyles ConvertFontStyle(Figma2Unity.TextStyle s)
        {
            FontStyles fs = FontStyles.Normal;
            if (s.Italic) fs |= FontStyles.Italic;
            if ((int)s.Weight >= (int)Figma2Unity.FontWeight.Bold) fs |= FontStyles.Bold;
            return fs;
        }
    }
}
