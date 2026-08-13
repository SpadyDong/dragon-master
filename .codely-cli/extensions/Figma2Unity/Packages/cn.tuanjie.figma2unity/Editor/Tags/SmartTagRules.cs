using System;
using System.Collections.Generic;

namespace Figma2Unity.Editor.Tags
{
    /// <summary>Smart tag inference rules — name-based + manual `#tag` parsing.</summary>
    public static class SmartTagRules
    {
        public static readonly (string Keyword, FcuTag Tag)[] NameKeywords = new[]
        {
            ("button",       FcuTag.Button),
            ("toggle",       FcuTag.Toggle),
            ("checkbox",     FcuTag.Toggle),
            ("input",        FcuTag.InputField),
            ("textfield",    FcuTag.InputField),
            ("password",     FcuTag.PasswordField),
            ("scrollview",   FcuTag.ScrollView),
            ("scroll",       FcuTag.ScrollView),
            ("mask",         FcuTag.Mask),
            ("placeholder",  FcuTag.Placeholder),
        };

        public static readonly Dictionary<string, FcuTag> ManualTagMap = new Dictionary<string, FcuTag>(StringComparer.OrdinalIgnoreCase)
        {
            { "button",      FcuTag.Button },
            { "toggle",      FcuTag.Toggle },
            { "input",       FcuTag.InputField },
            { "password",    FcuTag.PasswordField },
            { "scroll",      FcuTag.ScrollView },
            { "scrollview",  FcuTag.ScrollView },
            { "image",       FcuTag.Image },
            { "text",        FcuTag.Text },
            { "slice9",      FcuTag.Slice9 },
            { "autoslice9",  FcuTag.AutoSlice9 },
            { "mask",        FcuTag.Mask },
            { "shadow",      FcuTag.Shadow },
            { "canvasgroup", FcuTag.CanvasGroup },
            { "ignore",      FcuTag.Ignore },
            { "frame",       FcuTag.Frame },
            { "container",   FcuTag.Container },
            { "placeholder", FcuTag.Placeholder },
        };

        public static bool ContainsInsensitive(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return false;
            return haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
