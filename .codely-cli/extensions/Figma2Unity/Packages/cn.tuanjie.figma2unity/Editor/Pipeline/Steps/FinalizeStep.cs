using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// Step 19: Save Screen Prefabs + write SyncHelper.HashData (real SHA256).
    /// HashData write is REQUIRED for v1 — without it, v2's DiffCalculator will mis-flag
    /// all nodes as Modified. See Design.md §附录 D.2.
    /// </summary>
    public class FinalizeStep : PipelineStep
    {
        public override string DisplayName => "Finalize";
        public override float ProgressWeight => 2f;

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            // 1) Write real HashData onto every SyncHelper on the canvas.
            WriteHashData(ctx);

            // 2) Save Screen prefabs and back-fill controller references.
            if (ctx.Config != null && ctx.Config.BuildPrototypeFlow)
                SaveScreenPrefabs(ctx);

            AssetDatabase.SaveAssets();
            return Task.FromResult(StepResult.Ok());
        }

        private static void WriteHashData(F2UContext ctx)
        {
            if (ctx.AllNodes == null) return;
            foreach (var fobj in ctx.AllNodes)
            {
                fobj.HashData = FObjectHashData.Compute(fobj);
                var go = ctx.GetGameObject(fobj);
                if (go == null) continue;
                var sync = go.GetComponent<SyncHelper>();
                if (sync == null) sync = go.AddComponent<SyncHelper>();
                if (sync.Data == null) sync.Data = new SyncData();
                sync.Data.FigmaId = fobj.Id;
                sync.Data.FigmaName = fobj.Name;
                sync.Data.FileName = fobj.FileName;
                sync.Data.FolderName = fobj.FolderName;
                sync.Data.Tags = new System.Collections.Generic.List<FcuTag>(fobj.Tags);
                sync.Data.HashData = fobj.HashData;
                sync.Data.SpritePath = ctx.GetSpritePath(fobj);
            }
        }

        private static void SaveScreenPrefabs(F2UContext ctx)
        {
            if (ctx.Screens == null || ctx.TargetCanvas == null) return;
            var folder = ctx.Config != null ? ctx.Config.ScreenPrefabFolder : "Assets/Screens";
            Directory.CreateDirectory(folder);

            var controller = ctx.TargetCanvas.GetComponentInChildren<PrototypeFlowController>(true);
            // Two FRAMEs sharing the same FileName (e.g. duplicate "Home" siblings) would
            // otherwise overwrite the same prefab path and collapse the controller list.
            var usedPaths = new System.Collections.Generic.HashSet<string>();
            var savedNodeIds = new System.Collections.Generic.HashSet<string>();
            foreach (var screen in ctx.Screens)
            {
                var go = ctx.GetGameObject(screen);
                if (go == null) continue;
                var baseName = SanitizeFileName(screen.FileName ?? screen.Name ?? screen.Id);
                var prefabPath = $"{folder}/{baseName}.prefab";
                int dup = 1;
                while (usedPaths.Contains(prefabPath))
                    prefabPath = $"{folder}/{baseName}_{++dup}.prefab";
                usedPaths.Add(prefabPath);

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                if (controller != null && prefab != null)
                {
                    foreach (var fs in controller.ScreenList)
                    {
                        if (fs != null && fs.NodeId == screen.Id) fs.Prefab = prefab;
                    }
                }
                if (prefab != null) savedNodeIds.Add(screen.Id);
                // Destroy the source Screen instance — controller instantiates the prefab
                // at runtime, so the in-scene copy is pure overhead (each Screen carries
                // hundreds of children → scene balloons to ~20k+ objects). HashData for v2
                // increment is already serialized inside the saved prefab's SyncHelper.
                Object.DestroyImmediate(go);
            }

            if (controller != null)
            {
                // Drop ScreenList entries whose Prefab never got filled in. They originate
                // from upstream identification edge cases (e.g. a transition target that
                // couldn't be instantiated) and would only manifest at runtime as silent
                // dead-link navigations inside SetCurrentScreen.
                int removed = controller.ScreenList.RemoveAll(
                    fs => fs == null || fs.Prefab == null || !savedNodeIds.Contains(fs.NodeId));
                if (removed > 0)
                {
                    Debug.LogWarning(
                        $"[Figma2Unity] FinalizeStep dropped {removed} Screen entr"
                        + (removed == 1 ? "y" : "ies")
                        + " without a saved prefab. These were typically nested transition "
                        + "targets that never received a top-level GameObject. Check "
                        + "FlowManager.IdentifyScreensAndSections if you expected them.");
                }

                // Drop *unreachable duplicates*: when the Figma file contains multiple
                // top-level Frames with identical Name (designers commonly paste a screen
                // and tweak only one copy), any duplicate that no FlowButton targets AND
                // that is not a starting point cannot ever be shown at runtime. Keeping it
                // wastes a prefab on disk and bloats ScreenList. The "kept" sibling already
                // covers the user-visible behavior. See Audit Bug 2.
                int prunedDup = PruneUnreachableDuplicates(controller, folder);
                if (prunedDup > 0)
                {
                    Debug.LogWarning(
                        $"[Figma2Unity] FinalizeStep pruned {prunedDup} unreachable duplicate "
                        + "Screen prefab(s) — same Name as a reachable sibling, zero "
                        + "FlowButton references, not a starting point.");
                }

                // If the Figma source carries no flowStartingPoints, FlowManager fell back
                // to ctx.Screens[0] for InitialScreenId — that's order-dependent and
                // therefore unstable. Surface it so the user can pin a deterministic start
                // via Figma's "Set as starting frame" rather than relying on import order.
                if (controller.AllStartingPoints == null || controller.AllStartingPoints.Count == 0)
                {
                    Debug.LogWarning(
                        "[Figma2Unity] Figma file has no flowStartingPoints. "
                        + $"InitialScreenId fell back to '{controller.InitialScreenId}' "
                        + "(first detected Screen, order-dependent). Mark a starting frame "
                        + "in Figma's Prototype tab to make this deterministic.");
                }
                EditorUtility.SetDirty(controller);
            }
        }

        /// <summary>
        /// Returns the number of duplicate Screen entries removed. Two-pass:
        ///   1. Build the set of NodeIds referenced by any FlowButton in any kept prefab,
        ///      plus AllStartingPoints + InitialScreenId.
        ///   2. For every Name with &gt;1 ScreenList entries, drop the entries that are
        ///      not in the referenced set. If every duplicate is unreferenced (no twin is
        ///      reachable either), keep the first to avoid wiping the screen out entirely.
        /// </summary>
        private static int PruneUnreachableDuplicates(PrototypeFlowController controller, string folder)
        {
            var referenced = new System.Collections.Generic.HashSet<string>();
            if (!string.IsNullOrEmpty(controller.InitialScreenId))
                referenced.Add(controller.InitialScreenId);
            if (controller.AllStartingPoints != null)
                foreach (var sp in controller.AllStartingPoints)
                    if (sp != null && !string.IsNullOrEmpty(sp.NodeId)) referenced.Add(sp.NodeId);
            foreach (var fs in controller.ScreenList)
            {
                if (fs?.Prefab == null) continue;
                foreach (var fb in fs.Prefab.GetComponentsInChildren<FlowButton>(true))
                {
                    if (fb != null && !string.IsNullOrEmpty(fb.TargetScreenNodeId))
                        referenced.Add(fb.TargetScreenNodeId);
                }
            }

            int removed = 0;
            var byName = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<FlowScreen>>();
            foreach (var fs in controller.ScreenList)
            {
                if (fs == null || string.IsNullOrEmpty(fs.Name)) continue;
                if (!byName.TryGetValue(fs.Name, out var bucket))
                {
                    bucket = new System.Collections.Generic.List<FlowScreen>();
                    byName[fs.Name] = bucket;
                }
                bucket.Add(fs);
            }
            foreach (var bucket in byName.Values)
            {
                if (bucket.Count <= 1) continue;
                // Keep every entry that's reachable; if none is reachable keep the first
                // (preserves at least one screen with this Name so the asset list is still
                // navigable manually in the editor).
                bool anyReachable = false;
                foreach (var fs in bucket) if (referenced.Contains(fs.NodeId)) { anyReachable = true; break; }
                foreach (var fs in bucket)
                {
                    if (referenced.Contains(fs.NodeId)) continue;
                    if (!anyReachable) { anyReachable = true; continue; }
                    // Drop this duplicate.
                    var path = fs.Prefab != null ? AssetDatabase.GetAssetPath(fs.Prefab) : null;
                    if (!string.IsNullOrEmpty(path) && path.StartsWith(folder, System.StringComparison.Ordinal))
                        AssetDatabase.DeleteAsset(path);
                    controller.ScreenList.Remove(fs);
                    removed++;
                }
            }
            return removed;
        }

        private static string SanitizeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Screen";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            return sb.ToString();
        }
    }
}
