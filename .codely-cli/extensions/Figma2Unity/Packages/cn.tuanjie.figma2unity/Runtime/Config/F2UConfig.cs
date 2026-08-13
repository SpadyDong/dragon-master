using TMPro;
using UnityEngine;

namespace Figma2Unity
{
    /// <summary>
    /// ScriptableObject config for Figma2Unity import.
    /// ⚠ Personal Access Token / Google Fonts API key are NOT stored here — they live in
    /// F2UTokenStorage (EditorPrefs). Putting tokens in a ScriptableObject would leak them
    /// into git. See Design.md §四 "Token 存储".
    /// </summary>
    [CreateAssetMenu(fileName = "F2UConfig", menuName = "Figma2Unity/Config")]
    public class F2UConfig : ScriptableObject
    {
        [Header("Import")]
        public float SpriteScale = 2f;
        public int MaxRenderSize = 4096;
        public bool ImportHiddenNodes = false;
        public bool BuildPrototypeFlow = true;

        [Header("Naming")]
        public string TagSeparator = "#";
        public string OutputFolder = "Assets";
        public string ScreenPrefabFolder = "Assets/Screens";
        public string SpriteFolder = "Assets/Sprites";
        public string FontFolder = "Assets/Fonts";

        [Header("Sync")]
        public bool EnableIncrementalSync = true;

        [Header("Rendering")]
        // v1 default OFF — Figmage baker is v2 territory (Design.md §1.2). v1 nodes that would
        // otherwise bake fall back to flat color rendering. Flip to true once SpriteGenerator
        // and FigmageBakerAdapter land in v2.
        public bool EnableFigmageBaking = false;
        public bool SimpleColorDirectImage = true;
        public bool EnableVectorPngFallback = true;

        [Header("Sprite Atlas")]
        public bool EnableSpriteAtlas = true;

        [Header("Prototype")]
        public float DefaultTransitionDuration = 0.3f;
        public bool CreateScreenPrefabs = true;
        // The Figma file is a single document holding many pages (CANVAS nodes), and EACH
        // page can declare its own prototype flowStartingPoints. Without scoping, the
        // parser aggregates starts from ALL pages, so an unrelated page's "Flow 10" can
        // win InitialScreenId over the page you actually want.
        //
        // Two ways to scope (TargetNodeId wins when both are set):
        //   • TargetNodeId  — a node id (colon form, e.g. "12013:1517940") that lives on
        //     the page you want. The parser resolves which CANVAS contains it and keeps
        //     only that page's starts. This is what the window's "Current Page" option
        //     feeds, derived from the node-id in a pasted Figma URL.
        //   • TargetPageName — the exact page (CANVAS) name, e.g. "行车故事线 · Prototype".
        // Leave both empty to keep the legacy "aggregate every page" behaviour.
        public string TargetPageName = "";
        public string TargetNodeId = "";

        [Header("Font")]
        public bool EnableGoogleFontsDownload = true;
        public TMP_FontAsset FallbackFont;
    }
}
