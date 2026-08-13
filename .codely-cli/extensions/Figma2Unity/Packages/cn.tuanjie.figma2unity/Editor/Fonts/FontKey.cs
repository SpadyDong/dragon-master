using System;
using System.Globalization;

namespace Figma2Unity.Editor.Fonts
{
    /// <summary>
    /// Identity of a downloadable font face: (Family, Weight, Italic).
    /// Two TEXT nodes that share these three values share one TTF download + one TMP_FontAsset.
    /// Pure value type — no Unity refs — so Layer A tests can exercise it directly.
    /// </summary>
    public readonly struct FontKey : IEquatable<FontKey>
    {
        public readonly string Family;
        public readonly int Weight;
        public readonly bool Italic;

        public FontKey(string family, int weight, bool italic)
        {
            Family = NormalizeFamily(family);
            Weight = ClampWeight(weight);
            Italic = italic;
        }

        public static FontKey From(TextStyle style)
        {
            if (style == null) return default;
            return new FontKey(style.FontFamily, (int)style.Weight, style.Italic);
        }

        public bool IsEmpty => string.IsNullOrEmpty(Family);

        /// <summary>Filename slug, e.g. "Roboto_700_italic". Stable across runs.</summary>
        public string FileBase
        {
            get
            {
                if (IsEmpty) return "Unknown";
                var family = SanitizeForFile(Family);
                var sb = new System.Text.StringBuilder();
                sb.Append(family);
                sb.Append('_');
                sb.Append(Weight.ToString(CultureInfo.InvariantCulture));
                if (Italic) sb.Append("_italic");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Google Fonts CSS2 spec fragment, e.g. "Roboto:ital,wght@1,700".
        /// CSS2 uses "ital,wght" axis order; italic must come first when both are present.
        /// </summary>
        public string GoogleFontsCss2Spec
        {
            get
            {
                // Replace spaces with '+' per Google Fonts spec.
                var family = Family.Replace(' ', '+');
                return string.Format(CultureInfo.InvariantCulture,
                    "{0}:ital,wght@{1},{2}",
                    family, Italic ? 1 : 0, Weight);
            }
        }

        public bool Equals(FontKey other)
            => string.Equals(Family, other.Family, StringComparison.Ordinal)
               && Weight == other.Weight && Italic == other.Italic;

        public override bool Equals(object obj) => obj is FontKey k && Equals(k);
        public override int GetHashCode()
        {
            unchecked
            {
                int h = Family != null ? StringComparer.Ordinal.GetHashCode(Family) : 0;
                h = (h * 397) ^ Weight;
                h = (h * 397) ^ (Italic ? 1 : 0);
                return h;
            }
        }
        public override string ToString() => FileBase;

        private static string NormalizeFamily(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            // Collapse internal whitespace runs, trim, preserve case (Google Fonts is case-sensitive
            // for the canonical name but their CSS2 endpoint accepts any case — keep original).
            var trimmed = raw.Trim();
            var sb = new System.Text.StringBuilder(trimmed.Length);
            bool lastSpace = false;
            foreach (var c in trimmed)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!lastSpace) sb.Append(' ');
                    lastSpace = true;
                }
                else
                {
                    sb.Append(c);
                    lastSpace = false;
                }
            }
            return sb.ToString();
        }

        private static int ClampWeight(int w)
        {
            // Figma sends arbitrary ints; Google Fonts only serves multiples of 100 in [100, 900].
            if (w <= 0) return 400;
            if (w < 100) return 100;
            if (w > 900) return 900;
            return (w + 50) / 100 * 100;
        }

        private static string SanitizeForFile(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unknown";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('_');
            }
            return sb.Length == 0 ? "Unknown" : sb.ToString();
        }
    }
}
