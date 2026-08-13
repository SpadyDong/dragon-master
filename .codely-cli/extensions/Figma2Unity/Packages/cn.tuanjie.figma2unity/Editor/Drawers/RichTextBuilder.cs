using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// v2 Track D follow-up — pure-logic rich-text builder for <see cref="TextDrawer"/>.
    /// Translates Figma's <c>CharacterStyleOverrides</c> + <c>StyleOverrideTable</c> into
    /// TMP rich-text tags (<c>&lt;size&gt;</c> / <c>&lt;b&gt;</c> / <c>&lt;i&gt;</c>).
    /// Lives outside TextDrawer so Layer A tests can exercise it.
    /// </summary>
    public static class RichTextBuilder
    {
        [System.Flags]
        private enum OpenTagFlags
        {
            None = 0,
            Size = 1 << 0,
            Bold = 1 << 1,
            Italic = 1 << 2,
        }

        /// <summary>
        /// Build a TMP rich-text string from a node's characters + per-char overrides.
        /// Returns <paramref name="characters"/> unchanged when no overrides apply.
        /// </summary>
        public static string Build(
            string characters,
            TextStyle baseStyle,
            IReadOnlyList<int> characterStyleOverrides,
            IReadOnlyDictionary<string, TextStyle> styleOverrideTable)
        {
            if (string.IsNullOrEmpty(characters)) return characters ?? string.Empty;
            if (characterStyleOverrides == null || characterStyleOverrides.Count == 0
                || styleOverrideTable == null || styleOverrideTable.Count == 0)
                return characters;

            var sb = new StringBuilder(characters.Length + 32);
            int lastOverrideId = int.MinValue;
            OpenTagFlags openFlags = OpenTagFlags.None;

            for (int i = 0; i < characters.Length; i++)
            {
                int id = i < characterStyleOverrides.Count ? characterStyleOverrides[i] : 0;
                if (id != lastOverrideId)
                {
                    CloseOpenTags(sb, openFlags);
                    openFlags = OpenTagFlags.None;
                    if (id != 0 && styleOverrideTable.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var style))
                    {
                        openFlags = OpenTags(sb, baseStyle, style);
                    }
                    lastOverrideId = id;
                }
                sb.Append(characters[i]);
            }
            CloseOpenTags(sb, openFlags);
            return sb.ToString();
        }

        private static OpenTagFlags OpenTags(StringBuilder sb, TextStyle baseStyle, TextStyle ovr)
        {
            OpenTagFlags flags = OpenTagFlags.None;

            if (ovr.FontSize > 0f && (baseStyle == null || !FloatEq(ovr.FontSize, baseStyle.FontSize)))
            {
                sb.Append("<size=").Append(ovr.FontSize.ToString("0.##", CultureInfo.InvariantCulture)).Append('>');
                flags |= OpenTagFlags.Size;
            }
            bool baseBold = baseStyle != null && (int)baseStyle.Weight >= (int)FontWeight.Bold;
            bool ovrBold = (int)ovr.Weight >= (int)FontWeight.Bold;
            if (ovrBold && !baseBold) { sb.Append("<b>"); flags |= OpenTagFlags.Bold; }
            bool baseItalic = baseStyle != null && baseStyle.Italic;
            if (ovr.Italic && !baseItalic) { sb.Append("<i>"); flags |= OpenTagFlags.Italic; }
            return flags;
        }

        private static void CloseOpenTags(StringBuilder sb, OpenTagFlags flags)
        {
            // Close in reverse order opened (italic, bold, size).
            if ((flags & OpenTagFlags.Italic) != 0) sb.Append("</i>");
            if ((flags & OpenTagFlags.Bold) != 0) sb.Append("</b>");
            if ((flags & OpenTagFlags.Size) != 0) sb.Append("</size>");
        }

        private static bool FloatEq(float a, float b) => Mathf.Abs(a - b) < 0.0001f;
    }
}
