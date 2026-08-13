using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Figma2Unity
{
    /// <summary>
    /// Content hash for incremental sync. v1 must produce real SHA256 — null/dummy would
    /// force v2 DiffCalculator to mis-flag all v1-imported nodes as Modified.
    ///
    /// All string concatenation uses InvariantCulture to ensure cross-Culture stability
    /// (e.g. zh-CN Windows vs en-US CI).
    /// </summary>
    [Serializable]
    public class FObjectHashData
    {
        /// <summary>
        /// Bump when fields participating in any of the four hashes change. DiffCalculator
        /// treats Version mismatch as Modified (one-time upgrade), avoiding cross-version
        /// "all nodes modified" false positives.
        /// </summary>
        public const int CurrentVersion = 4;

        public int Version = CurrentVersion;
        public string ContentHash;
        public string GeometryHash;
        public string StyleHash;
        public string TextHash;

        public static FObjectHashData Compute(FObject fobj)
        {
            if (fobj == null) return null;
            var inv = CultureInfo.InvariantCulture;
            var data = new FObjectHashData { Version = CurrentVersion };

            data.GeometryHash = Sha256(string.Format(inv, "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}",
                FormatRect(fobj.AbsoluteBoundingBox, inv),
                fobj.Constraints == null ? "" : fobj.Constraints.Horizontal.ToString(),
                fobj.Constraints == null ? "" : fobj.Constraints.Vertical.ToString(),
                FormatCornerRadius(fobj.CornerRadius, inv),
                fobj.Opacity.ToString("R", inv),
                fobj.LayoutMode,
                fobj.ItemSpacing.ToString("R", inv),
                string.Format(inv, "{0}|{1}|{2}|{3}",
                    fobj.PaddingLeft.ToString("R", inv),
                    fobj.PaddingRight.ToString("R", inv),
                    fobj.PaddingTop.ToString("R", inv),
                    fobj.PaddingBottom.ToString("R", inv)),
                fobj.PrimaryAxisAlignItems,
                fobj.CounterAxisAlignItems,
                fobj.OverflowDirection,
                // Sizing block: per-axis HUG/FILL/FIXED + auto-layout child grow/align. These
                // drive ContentSizeFitter / LayoutElement.flexible* so a HUG↔FILL or
                // grow/align change must re-flag the node as Modified on incremental sync.
                string.Format(inv, "{0}|{1}|{2}|{3}",
                    fobj.LayoutSizingHorizontal,
                    fobj.LayoutSizingVertical,
                    fobj.LayoutGrow.ToString("R", inv),
                    fobj.LayoutAlign),
                // Wrap + native GRID block: drives GridLayoutGroup vs H/V LayoutGroup choice
                // and its cell spacing / track count, so changes must re-flag as Modified.
                string.Format(inv, "{0}|{1}|{2}|{3}|{4}|{5}",
                    fobj.LayoutWrap,
                    fobj.CounterAxisSpacing.ToString("R", inv),
                    fobj.GridRowCount.ToString(inv),
                    fobj.GridColumnCount.ToString(inv),
                    fobj.GridRowGap.ToString("R", inv),
                    fobj.GridColumnGap.ToString("R", inv))));

            data.StyleHash = Sha256(string.Format(inv, "{0}|{1}|{2}",
                SerializeFills(fobj.Fills, inv),
                SerializeStrokes(fobj.Strokes, inv),
                SerializeEffects(fobj.Effects, inv)));

            data.TextHash = Sha256(string.Format(inv, "{0}|{1}|{2}",
                fobj.Characters ?? "",
                (fobj.Style != null ? fobj.Style.FontSize : 0f).ToString("R", inv),
                fobj.Style != null ? (fobj.Style.FontFamily ?? "") : ""));

            data.ContentHash = Sha256(string.Format(inv, "v{0}|{1}|{2}|{3}",
                CurrentVersion, data.GeometryHash, data.StyleHash, data.TextHash));
            return data;
        }

        private static string FormatRect(Rect r, IFormatProvider inv)
            => string.Format(inv, "{0}|{1}|{2}|{3}",
                r.x.ToString("R", inv), r.y.ToString("R", inv),
                r.width.ToString("R", inv), r.height.ToString("R", inv));

        private static string FormatCornerRadius(float[] c, IFormatProvider inv)
        {
            if (c == null || c.Length == 0) return "";
            var sb = new StringBuilder();
            foreach (var v in c) sb.Append(v.ToString("R", inv)).Append(',');
            return sb.ToString();
        }

        private static string SerializeFills(System.Collections.Generic.List<Paint> fills, IFormatProvider inv)
        {
            if (fills == null || fills.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var p in fills)
            {
                if (p == null) { sb.Append("null,"); continue; }
                sb.AppendFormat(inv, "{0}:{1}:{2}:{3}:{4}:{5},",
                    p.Type, p.Visible,
                    p.Opacity.ToString("R", inv),
                    FormatColor(p.Color, inv),
                    p.ImageRef ?? "",
                    p.ScaleMode);
            }
            return sb.ToString();
        }

        private static string SerializeStrokes(System.Collections.Generic.List<Paint> strokes, IFormatProvider inv)
        {
            if (strokes == null || strokes.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var p in strokes)
            {
                if (p == null) { sb.Append("null,"); continue; }
                sb.AppendFormat(inv, "{0}:{1}:{2}:{3},",
                    p.Type, p.Visible,
                    p.Opacity.ToString("R", inv),
                    FormatColor(p.Color, inv));
            }
            return sb.ToString();
        }

        private static string SerializeEffects(System.Collections.Generic.List<Effect> effects, IFormatProvider inv)
        {
            if (effects == null || effects.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var e in effects)
            {
                if (e == null) { sb.Append("null,"); continue; }
                sb.AppendFormat(inv, "{0}:{1}:{2}:{3}:{4}:{5}:{6},",
                    e.Type, e.Visible,
                    FormatColor(e.Color, inv),
                    e.Offset.x.ToString("R", inv),
                    e.Offset.y.ToString("R", inv),
                    e.Radius.ToString("R", inv),
                    e.Spread.ToString("R", inv));
            }
            return sb.ToString();
        }

        private static string FormatColor(Color c, IFormatProvider inv)
            => string.Format(inv, "{0},{1},{2},{3}",
                c.r.ToString("R", inv), c.g.ToString("R", inv),
                c.b.ToString("R", inv), c.a.ToString("R", inv));

        private static string Sha256(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? ""));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }
    }
}
