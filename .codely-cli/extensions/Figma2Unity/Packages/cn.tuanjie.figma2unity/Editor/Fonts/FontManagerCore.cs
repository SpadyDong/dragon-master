using System;
using System.Collections.Generic;

namespace Figma2Unity.Editor.Fonts
{
    /// <summary>
    /// Layer A (pure-logic) portion of FontManager. Collects font keys from a node list
    /// and builds the Google Fonts CSS2 request URL. No UnityEditor / TMP / Networking APIs
    /// here — those live in <c>FontManager.cs</c>.
    /// </summary>
    public static class FontManagerCore
    {
        /// <summary>
        /// Walk an FObject list and collect unique (Family, Weight, Italic) triples from
        /// TEXT nodes. Nodes without Style or with empty FontFamily are skipped.
        /// </summary>
        public static IReadOnlyList<FontKey> CollectFontKeys(IEnumerable<FObject> nodes)
        {
            var set = new HashSet<FontKey>();
            var list = new List<FontKey>();
            if (nodes == null) return list;
            foreach (var n in nodes)
            {
                if (n == null) continue;
                if (!IsTextNode(n)) continue;
                var key = FontKey.From(n.Style);
                if (key.IsEmpty) continue;
                if (set.Add(key)) list.Add(key);
            }
            return list;
        }

        private static bool IsTextNode(FObject n)
        {
            // Either node is typed TEXT, or has been tagged Text by SetTagsStep, or simply
            // carries a TextStyle. Any of these is enough — the only thing that matters is
            // whether there's a font to resolve.
            if (n.Style != null && !string.IsNullOrEmpty(n.Style.FontFamily)) return true;
            if (string.Equals(n.Type, "TEXT", StringComparison.OrdinalIgnoreCase)) return true;
            if (n.Tags != null)
            {
                foreach (var t in n.Tags)
                    if (t == FcuTag.Text) return true;
            }
            return false;
        }

        /// <summary>
        /// Build a single Google Fonts CSS2 endpoint URL covering all <paramref name="keys"/>.
        /// CSS2 supports multiple families in one request via repeated <c>family=</c> params.
        /// Returns null if <paramref name="keys"/> is empty.
        /// </summary>
        public static string BuildGoogleFontsCss2Url(IEnumerable<FontKey> keys)
        {
            if (keys == null) return null;

            // Group keys by family so each family contributes a single tuple list.
            var byFamily = new Dictionary<string, List<FontKey>>(StringComparer.Ordinal);
            var order = new List<string>();
            foreach (var k in keys)
            {
                if (k.IsEmpty) continue;
                if (!byFamily.TryGetValue(k.Family, out var bucket))
                {
                    bucket = new List<FontKey>();
                    byFamily[k.Family] = bucket;
                    order.Add(k.Family);
                }
                bucket.Add(k);
            }
            if (order.Count == 0) return null;

            var sb = new System.Text.StringBuilder("https://fonts.googleapis.com/css2?");
            for (int i = 0; i < order.Count; i++)
            {
                if (i > 0) sb.Append('&');
                sb.Append("family=");
                var family = order[i];
                var bucket = byFamily[family];
                // Sort axes deterministically: (italic asc, weight asc).
                bucket.Sort((a, b) =>
                {
                    int cmp = a.Italic.CompareTo(b.Italic);
                    return cmp != 0 ? cmp : a.Weight.CompareTo(b.Weight);
                });
                sb.Append(family.Replace(' ', '+'));
                sb.Append(":ital,wght@");
                for (int j = 0; j < bucket.Count; j++)
                {
                    if (j > 0) sb.Append(';');
                    var k = bucket[j];
                    sb.Append(k.Italic ? '1' : '0');
                    sb.Append(',');
                    sb.Append(k.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            // Ask Google for TTF where possible by setting an honest UA — but UA goes in
            // headers, not the URL. The URL itself is enough; the HTTP client picks the UA.
            return sb.ToString();
        }

        /// <summary>
        /// Parse a Google Fonts CSS2 response and extract one font URL per matching
        /// (Family, Weight, Italic) triple in <paramref name="wanted"/>. Returns a map
        /// from FontKey → font file URL (TTF/OTF/WOFF2 — whatever CSS yields for that UA).
        /// Unmatched keys are simply missing from the output.
        /// </summary>
        public static Dictionary<FontKey, string> ParseCss2(string cssBody, IEnumerable<FontKey> wanted)
        {
            var result = new Dictionary<FontKey, string>();
            if (string.IsNullOrEmpty(cssBody) || wanted == null) return result;

            // We walk @font-face blocks. Each block has:
            //   font-family: 'Roboto';
            //   font-style: normal | italic;
            //   font-weight: 400;
            //   src: url(https://...) format('...');
            // Parse with cheap string scanning — no real CSS engine, no regex backtracking.
            int idx = 0;
            while (true)
            {
                int faceStart = cssBody.IndexOf("@font-face", idx, StringComparison.OrdinalIgnoreCase);
                if (faceStart < 0) break;
                int braceOpen = cssBody.IndexOf('{', faceStart);
                if (braceOpen < 0) break;
                int braceClose = cssBody.IndexOf('}', braceOpen);
                if (braceClose < 0) break;
                var block = cssBody.Substring(braceOpen + 1, braceClose - braceOpen - 1);
                idx = braceClose + 1;

                var family = ExtractProperty(block, "font-family");
                var style = ExtractProperty(block, "font-style");
                var weightStr = ExtractProperty(block, "font-weight");
                var url = ExtractUrl(block);
                if (family == null || url == null) continue;
                family = family.Trim().Trim('\'', '"');
                bool italic = string.Equals(style?.Trim(), "italic", StringComparison.OrdinalIgnoreCase);
                int weight = 400;
                if (!string.IsNullOrEmpty(weightStr))
                    int.TryParse(weightStr.Trim(), System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out weight);

                foreach (var w in wanted)
                {
                    if (w.IsEmpty) continue;
                    if (!string.Equals(w.Family, family, StringComparison.OrdinalIgnoreCase)) continue;
                    if (w.Italic != italic) continue;
                    if (w.Weight != weight) continue;
                    // First match wins; CSS may list multiple unicode-range subsets per face —
                    // any subset URL is usable for our TTF fallback purposes.
                    if (!result.ContainsKey(w)) result[w] = url;
                }
            }
            return result;
        }

        private static string ExtractProperty(string block, string propertyName)
        {
            int i = block.IndexOf(propertyName, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            int colon = block.IndexOf(':', i);
            if (colon < 0) return null;
            int semi = block.IndexOf(';', colon);
            if (semi < 0) semi = block.Length;
            return block.Substring(colon + 1, semi - colon - 1).Trim();
        }

        private static string ExtractUrl(string block)
        {
            // Find first url(...) occurrence.
            int i = block.IndexOf("url(", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            int end = block.IndexOf(')', i + 4);
            if (end < 0) return null;
            var raw = block.Substring(i + 4, end - i - 4).Trim().Trim('\'', '"');
            return string.IsNullOrEmpty(raw) ? null : raw;
        }
    }
}
