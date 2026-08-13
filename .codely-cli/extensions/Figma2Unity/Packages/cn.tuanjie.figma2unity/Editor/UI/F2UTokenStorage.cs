using UnityEditor;
using UnityEngine;

namespace Figma2Unity.Editor
{
    /// <summary>
    /// Per-machine token storage in EditorPrefs (so PAT never leaks into git through
    /// the F2UConfig ScriptableObject). Design.md §四 "Token 存储".
    /// </summary>
    internal static class F2UTokenStorage
    {
        private static string KeyPrefix => $"Figma2Unity.{Application.productName}.";

        public static string PersonalAccessToken
        {
            get => EditorPrefs.GetString(KeyPrefix + "PAT", "");
            set => EditorPrefs.SetString(KeyPrefix + "PAT", value ?? "");
        }

        public static string GoogleFontsApiKey
        {
            get => EditorPrefs.GetString(KeyPrefix + "GFK", "");
            set => EditorPrefs.SetString(KeyPrefix + "GFK", value ?? "");
        }
    }
}
