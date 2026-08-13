using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Figma2Unity.Editor.Fonts
{
    /// <summary>
    /// Package-change bootstrap. The TMP check runs whenever the cn.tuanjie.figma2unity package
    /// is added or updated (via the Package Manager registration event). It:
    ///   1. Silently imports TMP Essential Resources (equivalent to
    ///      Window > TextMeshPro > Import TMP Essentials) if they are missing.
    ///   2. Runs <see cref="CjkFallbackSetup.Setup"/> to build + register the CJK fallback font.
    ///
    /// When the package is removed, the check is intentionally NOT run.
    /// Both steps are idempotent, so re-running on every package change is safe.
    /// </summary>
    [InitializeOnLoad]
    public static class F2UAutoSetup
    {
        private const string PackageName = "cn.tuanjie.figma2unity";
        private const string TmpSettingsAssetPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string TmpEssentialsPackageName = "TMP Essential Resources";

        static F2UAutoSetup()
        {
            Events.registeredPackages += OnRegisteredPackages;
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            // Run only when our package is added or changed (updated); skip on removal.
            bool relevant = false;

            if (args.added != null)
            {
                foreach (var p in args.added)
                {
                    if (p.name == PackageName) { relevant = true; break; }
                }
            }

            if (!relevant && args.changedTo != null)
            {
                foreach (var p in args.changedTo)
                {
                    if (p.name == PackageName) { relevant = true; break; }
                }
            }

            if (!relevant)
                return;

            // Defer until the editor is idle so AssetDatabase / TMP_Settings are ready.
            EditorApplication.delayCall += RunCheck;
        }

        private static void RunCheck()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                // Try again on a later tick when the editor is idle.
                EditorApplication.delayCall += RunCheck;
                return;
            }

            if (IsTmpEssentialsImported())
            {
                CjkFallbackSetup.Setup();
                return;
            }

            // Essentials missing: import silently, then run CJK setup once the import completes.
            Debug.Log($"[F2U] Package '{PackageName}' change detected — importing TMP Essentials automatically...");
            AssetDatabase.importPackageCompleted += OnPackageImported;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        private static void OnPackageImported(string packageName)
        {
            if (packageName != TmpEssentialsPackageName)
                return;

            AssetDatabase.importPackageCompleted -= OnPackageImported;

            // Run after the import settles so TMP_Settings is available.
            EditorApplication.delayCall += () =>
            {
                CjkFallbackSetup.Setup();
                Debug.Log("[F2U] Auto setup complete (TMP Essentials + CJK fallback font).");
            };
        }

        private static bool IsTmpEssentialsImported()
        {
            return File.Exists(TmpSettingsAssetPath) && TMP_Settings.instance != null;
        }
    }
}
