using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Figma2Unity.Editor.Fonts
{
    /// <summary>
    /// One-shot editor helper: builds a Dynamic CJK TMP_FontAsset from NotoSansSC.ttf and
    /// registers it on TMP_Settings' global fallback list so every TextMeshPro component
    /// (existing imported screens + future ones) can render Chinese instead of tofu.
    /// Run via menu: Figma2Unity/Setup CJK Fallback Font.
    /// </summary>
    public static class CjkFallbackSetup
    {
        private const string SourceFontPath = "Packages/cn.tuanjie.figma2unity/Fonts/NotoSansSC.ttf";
        private const string SdfPath = "Packages/cn.tuanjie.figma2unity/Fonts/NotoSansSC_SDF.asset";

        [MenuItem("Figma2Unity/Setup CJK Fallback Font")]
        public static void Setup()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
            if (fontAsset == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
                if (font == null)
                {
                    Debug.LogError($"[F2U] Source font not found at {SourceFontPath}");
                    return;
                }

                // Dynamic atlas: glyphs rasterized on demand, so the whole 20k+ CJK set
                // doesn't need pre-baking. 1024² atlas with SDFAA.
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                if (fontAsset == null)
                {
                    Debug.LogError("[F2U] CreateFontAsset returned null.");
                    return;
                }
                fontAsset.name = "NotoSansSC_SDF";
                AssetDatabase.CreateAsset(fontAsset, SdfPath);
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "NotoSansSC_SDF Material";
                    if (!AssetDatabase.IsSubAsset(fontAsset.material))
                        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
                if (fontAsset.atlasTexture != null)
                {
                    fontAsset.atlasTexture.name = "NotoSansSC_SDF Atlas";
                    if (!AssetDatabase.IsSubAsset(fontAsset.atlasTexture))
                        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                }
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(SdfPath);
                Debug.Log($"[F2U] Created CJK TMP font asset: {SdfPath}");
            }
            else
            {
                Debug.Log($"[F2U] Reusing existing CJK TMP font asset: {SdfPath}");
            }

            RegisterGlobalFallback(fontAsset);
        }

        private static void RegisterGlobalFallback(TMP_FontAsset cjk)
        {
            var settings = TMP_Settings.instance;
            if (settings == null)
            {
                Debug.LogError("[F2U] TMP_Settings not found. Open Window > TextMeshPro > Import TMP Essentials first.");
                return;
            }

            var so = new SerializedObject(settings);
            var listProp = so.FindProperty("m_fallbackFontAssets");
            if (listProp == null)
            {
                Debug.LogError("[F2U] m_fallbackFontAssets property not found on TMP_Settings.");
                return;
            }

            // Skip if already present.
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == cjk)
                {
                    Debug.Log("[F2U] CJK font already in TMP global fallback list.");
                    return;
                }
            }

            listProp.InsertArrayElementAtIndex(listProp.arraySize);
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = cjk;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[F2U] Added NotoSansSC_SDF to TMP_Settings global fallback list.");
        }
    }
}
