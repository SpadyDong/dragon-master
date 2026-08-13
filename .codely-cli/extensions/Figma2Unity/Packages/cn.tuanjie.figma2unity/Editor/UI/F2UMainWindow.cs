using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Figma2Unity.Editor.Api;
using Figma2Unity.Editor.Sync;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Figma2Unity.Editor
{
    /// <summary>
    /// v1 Editor UI: File Key + PAT + Config + Import button. The Canvas is resolved
    /// automatically on Import (existing scene Canvas wins; otherwise a default
    /// ScreenSpaceOverlay Canvas + EventSystem are created).
    /// PAT lives in F2UTokenStorage (EditorPrefs), NOT in F2UConfig.
    ///
    /// v2: Incremental mode wires SyncHelper scan → DiffCalculator → SyncService.
    /// Cache controls (Use cache / Prefetch / Cache dir) drive an on-disk snapshot
    /// of the Figma file so repeated imports can run fully offline.
    /// </summary>
    public class F2UMainWindow : EditorWindow
    {
        private string _fileKey;
        private string _token;
        private F2UConfig _config;
        private ImportMode _mode = ImportMode.Full;
        private bool _useCache;
        private string _cacheDir;
        private PageScope _pageScope = PageScope.CurrentPage;
        private bool _running;

        // Where prototype starting points come from. "Current Page" uses the node-id in the
        // Figma File Key/URL field to resolve which page (CANVAS) you're on, then keeps only
        // that page's starts — so a stray flow on another page can't hijack InitialScreenId.
        private enum PageScope { AllPages = 0, CurrentPage = 1 }

        private const string FileKeyPref = "Figma2Unity.FileKey";
        private const string UseCachePref = "Figma2Unity.UseCache";
        private const string CacheDirPref = "Figma2Unity.CacheDir";
        private const string ModePref = "Figma2Unity.Mode";
        private const string PageScopePref = "Figma2Unity.PageScope";

        [MenuItem("Figma2Unity/Open Main Window")]
        public static void Open()
        {
            var win = GetWindow<F2UMainWindow>("Figma2Unity");
            win.minSize = new Vector2(440, 320);
            win.Show();
        }

        private void OnEnable()
        {
            _token = F2UTokenStorage.PersonalAccessToken;
            _fileKey = EditorPrefs.GetString(FileKeyPref, "");
            // Use cache always starts OFF when the window opens, regardless of any previously
            // stored value; the user can still toggle it on per session.
            _useCache = false;
            EditorPrefs.SetBool(UseCachePref, false);
            _cacheDir = EditorPrefs.GetString(CacheDirPref, DefaultCacheDir());
            // Target Page and Mode are hidden from the UI and pinned to their defaults
            // (Current Page / Full). They no longer read from EditorPrefs so stale stored
            // values can't override the intended defaults.
            _mode = ImportMode.Full;
            _pageScope = PageScope.CurrentPage;
        }

        private static string DefaultCacheDir()
            => Path.Combine(Directory.GetCurrentDirectory(), "F2UCache");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Figma2Unity (v1 MVP)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _fileKey = EditorGUILayout.TextField("Figma File Key", _fileKey ?? "");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString(FileKeyPref, _fileKey ?? "");

            EditorGUI.BeginChangeCheck();
            _token = EditorGUILayout.PasswordField("Personal Access Token", _token ?? "");
            if (EditorGUI.EndChangeCheck())
                F2UTokenStorage.PersonalAccessToken = _token;

            // Config field intentionally hidden from the UI: import / prefetch fall back to a
            // default F2UConfig when _config is null (see ImportAsync / PrefetchAsync), so the
            // common path needs no manual config asset. Re-expose the ObjectField here if
            // per-import config overrides are needed again.

            // Target Page and Mode controls are intentionally hidden from the UI. They are
            // pinned to their defaults (Current Page / Full) in OnEnable. "Current Page" still
            // relies on a node-id embedded in the Figma File Key/URL above; import falls back
            // to All Pages when none is present (see ImportAsync).

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Offline cache", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _useCache = EditorGUILayout.Toggle(new GUIContent("Use cache",
                "When ON, Import reads document.json + PNGs from the cache directory instead of hitting Figma."), _useCache);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(UseCachePref, _useCache);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _cacheDir = EditorGUILayout.TextField("Cache dir", _cacheDir ?? DefaultCacheDir());
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetString(CacheDirPref, _cacheDir ?? "");
                if (GUILayout.Button("…", GUILayout.Width(28)))
                {
                    var picked = EditorUtility.OpenFolderPanel("F2U cache directory",
                        Directory.Exists(_cacheDir) ? _cacheDir : Directory.GetCurrentDirectory(), "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        _cacheDir = picked;
                        EditorPrefs.SetString(CacheDirPref, _cacheDir);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(_running))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Prefetch", GUILayout.Height(26)))
                        _ = PrefetchAsync();
                    if (GUILayout.Button("Import", GUILayout.Height(26)))
                        _ = ImportAsync();
                }
            }

            if (_running)
                EditorGUILayout.HelpBox("Working… check the progress bar.", MessageType.Info);
        }

        private async System.Threading.Tasks.Task PrefetchAsync()
        {
            if (!ValidatePrefetchInputs(out var key)) return;
            _running = true;
            try
            {
                var store = new FigmaCacheStore(_cacheDir, key);
                var cmd = new F2UPrefetchCommand(new FigmaApiClient(_token), store);
                var cts = new CancellationTokenSource();

                // Progress<T>.Report posts callbacks asynchronously to the main-thread
                // SynchronizationContext, so late reports can still be queued when RunAsync
                // returns. If one fires after we open the result dialog it re-shows the
                // cancelable progress bar on top of the modal dialog and steals focus, making
                // the dialog impossible to close. This flag makes the handler a no-op once
                // prefetch has finished.
                bool finished = false;
                EditorUtility.DisplayProgressBar("Figma2Unity Prefetch", "Starting…", 0f);
                var progress = new Progress<F2UPrefetchCommand.Progress>(p =>
                {
                    if (finished) return;
                    float pct = p.Total > 0 ? (float)p.Current / p.Total : 0f;
                    if (EditorUtility.DisplayCancelableProgressBar("Figma2Unity Prefetch", p.Phase, pct)
                        && !cts.IsCancellationRequested)
                        cts.Cancel();
                });
                F2UPrefetchCommand.Result result;
                try { result = await cmd.RunAsync(progress, _config, cts.Token); }
                finally
                {
                    finished = true;
                    EditorUtility.ClearProgressBar();
                }

                if (result.Success)
                {
                    Debug.Log($"[Figma2Unity] Prefetch: {result}");
                    EditorUtility.DisplayDialog("Figma2Unity Prefetch",
                        $"Cache ready at:\n{result.CacheDir}\n\nFills: {result.FillCount}\nRenders: {result.RenderCount}\nVersion: {result.Version}",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Figma2Unity Prefetch", "Failed: " + result.FailureReason, "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Figma2Unity Prefetch", "Threw: " + ex.Message, "OK");
            }
            finally
            {
                _running = false;
                Repaint();
            }
        }

        private bool ValidatePrefetchInputs(out string resolvedKey)
        {
            resolvedKey = null;
            if (string.IsNullOrEmpty(_fileKey)) { EditorUtility.DisplayDialog("Figma2Unity", "File Key is empty.", "OK"); return false; }
            if (string.IsNullOrEmpty(_token))   { EditorUtility.DisplayDialog("Figma2Unity", "PAT is empty.", "OK"); return false; }
            if (string.IsNullOrEmpty(_cacheDir)) { EditorUtility.DisplayDialog("Figma2Unity", "Cache directory is empty.", "OK"); return false; }
            // Resolve to a bare key for the API call, but DO NOT overwrite _fileKey: keeping
            // the user's original input (which may be a full URL carrying ?node-id=...) lets
            // the "Current Page" scope keep working across repeated Prefetch/Import clicks.
            resolvedKey = ExtractFileKey(_fileKey);
            return true;
        }

        private async System.Threading.Tasks.Task ImportAsync()
        {
            if (string.IsNullOrEmpty(_fileKey)) { EditorUtility.DisplayDialog("Figma2Unity", "File Key is empty.", "OK"); return; }
            if (!_useCache && string.IsNullOrEmpty(_token))
            {
                EditorUtility.DisplayDialog("Figma2Unity", "PAT is empty (required when 'Use cache' is OFF).", "OK");
                return;
            }
            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<F2UConfig>();
                Debug.Log("[Figma2Unity] No config provided — using defaults.");
            }

            // The Target Page dropdown is the explicit, per-import user intent, so it drives
            // page scoping. "Current Page" resolves the page via the node-id embedded in the
            // Figma File Key/URL; "All Pages" clears the scope (legacy aggregate behaviour).
            if (_pageScope == PageScope.CurrentPage)
            {
                var nodeId = ExtractNodeId(_fileKey);
                if (string.IsNullOrEmpty(nodeId))
                    Debug.LogWarning("[Figma2Unity] Target Page = Current Page but the File Key "
                        + "has no node-id — importing All Pages instead.");
                _config.TargetNodeId = nodeId ?? "";
            }
            else
            {
                _config.TargetNodeId = "";
            }

            // Locate the import target Canvas in the active scene, creating a default
            // Screen-Space Overlay Canvas (+ EventSystem) if none exists. This replaces
            // the legacy "Target Canvas" object field: users just click Import and the
            // window figures out where to land the screens.
            var targetCanvas = ResolveOrCreateCanvas();
            if (targetCanvas == null)
            {
                EditorUtility.DisplayDialog("Figma2Unity",
                    "Could not resolve or create a Canvas in the active scene.", "OK");
                return;
            }

            // Resolve to a bare key for the API call, but DO NOT overwrite _fileKey: keeping
            // the user's original input (which may be a full URL carrying ?node-id=...) lets
            // the "Current Page" scope keep working across repeated Import clicks. Otherwise
            // the first import would strip the URL down to a key and the next import would
            // silently fall back to All Pages.
            var resolvedKey = ExtractFileKey(_fileKey);
            if (resolvedKey != _fileKey)
                Debug.Log($"[Figma2Unity] Extracted file key '{resolvedKey}' from input.");

            IFigmaApiClient apiClient;
            if (_useCache)
            {
                if (string.IsNullOrEmpty(_cacheDir))
                {
                    EditorUtility.DisplayDialog("Figma2Unity", "Cache directory is empty.", "OK");
                    return;
                }
                var store = new FigmaCacheStore(_cacheDir, resolvedKey);
                if (!store.HasDocument)
                {
                    EditorUtility.DisplayDialog("Figma2Unity",
                        $"No cache at:\n{store.FileRoot}\n\nRun Prefetch first.", "OK");
                    return;
                }
                apiClient = new OfflineFigmaApiClient(store);
            }
            else
            {
                apiClient = new FigmaApiClient(_token);
            }

            _running = true;
            try
            {
                var ctx = new F2UContext
                {
                    FileId = resolvedKey,
                    Mode = _mode,
                    TargetCanvas = targetCanvas,
                    Config = _config,
                    PersonalAccessToken = _token,
                    ApiClient = apiClient,
                };

                // Incremental mode: collect SyncHelpers already in the scene so
                // ComputeDiffStep has a baseline to diff against. First run on a clean
                // scene leaves the map empty and degrades cleanly to "everything Added".
                if (ctx.Mode == ImportMode.Incremental)
                    PopulateExistingSyncHelpers(ctx);

                var pipeline = new ImportPipeline();
                pipeline.Configure(ctx.Mode, _config);
                var cts = new CancellationTokenSource();

                EditorUtility.DisplayProgressBar("Figma2Unity", "Starting...", 0f);
                ImportResult result;
                try
                {
                    var runTask = pipeline.RunAsync(ctx, cts.Token);
                    while (!runTask.IsCompleted)
                    {
                        bool canceled = EditorUtility.DisplayCancelableProgressBar(
                            "Figma2Unity",
                            ctx.CurrentStepName ?? "Working…",
                            Mathf.Clamp01(ctx.Progress));
                        if (canceled && !cts.IsCancellationRequested)
                            cts.Cancel();
                        await System.Threading.Tasks.Task.Delay(120);
                    }
                    result = await runTask;
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                if (!result.Success)
                {
                    EditorUtility.DisplayDialog("Figma2Unity",
                        $"Import failed at '{result.FailedStepName}': {result.FailedReason}", "OK");
                    return;
                }

                // Incremental post-processing: apply the computed diff via SyncService and
                // surface the result to the user. Full mode skips this entirely.
                if (ctx.Mode == ImportMode.Incremental && ctx.Diff != null)
                {
                    // First incremental run (no baseline SyncHelpers in the scene) means the
                    // diff is "everything Added". CreateGameObjectsStep + FinalizeStep already
                    // produced the correct Full-mode result (centered Screen prefabs under a
                    // PrototypeFlowController viewport). Running SyncService here would rebuild
                    // every node a second time, flat under the Canvas — the "pages tiled past
                    // the canvas" regression. Skip the apply on a cold scene; still show the
                    // diff so the user sees what was imported. Genuine re-syncs (non-empty
                    // baseline) run normally over the Screen-filtered diff set.
                    bool hasBaseline = ctx.ExistingNodeMap != null && ctx.ExistingNodeMap.Count > 0;
                    if (hasBaseline)
                    {
                        try
                        {
                            var svc = SyncServiceFactory.CreateForEditor();
                            await svc.ApplyDiffAsync(ctx.Diff, ctx, cts.Token);
                            AssetDatabase.SaveAssets();
                        }
                        catch (Exception applyEx)
                        {
                            Debug.LogException(applyEx);
                            EditorUtility.DisplayDialog("Figma2Unity",
                                "Diff apply threw: " + applyEx.Message, "OK");
                            return;
                        }
                    }

                    DiffPreviewWindow.Show(ctx.Diff);
                }
                else
                {
                    EditorUtility.DisplayDialog("Figma2Unity", "Import completed successfully.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Figma2Unity", "Import threw: " + ex.Message, "OK");
            }
            finally
            {
                _running = false;
                Repaint();
            }
        }

        /// <summary>
        /// Walk the resolved Canvas hierarchy collecting SyncHelpers from the previous import
        /// and wire them into the context for ComputeDiffStep + SyncService consumption.
        /// </summary>
        private static void PopulateExistingSyncHelpers(F2UContext ctx)
        {
            if (ctx == null || ctx.TargetCanvas == null) return;
            var helpers = ctx.TargetCanvas.GetComponentsInChildren<SyncHelper>(includeInactive: true);
            ctx.ExistingSyncHelpers = helpers;
            ctx.ExistingNodeMap = new Dictionary<string, SyncHelper>(helpers.Length);
            foreach (var h in helpers)
            {
                if (h == null || h.Data == null || string.IsNullOrEmpty(h.Data.FigmaId)) continue;
                ctx.ExistingNodeMap[h.Data.FigmaId] = h;
            }
        }

        /// <summary>
        /// Find a Canvas to import into. Strategy:
        ///   1. Reuse an existing Canvas already in the active scene (prefer one that
        ///      previously hosted a Figma2Unity import — detected via a child
        ///      PrototypeFlowController — then fall back to the first Canvas found).
        ///   2. Otherwise create a fresh Screen-Space Overlay Canvas (+ CanvasScaler +
        ///      GraphicRaycaster) and ensure an EventSystem exists so FlowButtons /
        ///      Selectables receive input at runtime.
        /// Returns null only if the active scene itself is invalid.
        /// </summary>
        private static Canvas ResolveOrCreateCanvas()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid()) return null;

            Canvas firstCanvas = null;
            Canvas figmaCanvas = null;
            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>(includeInactive: true))
            {
                if (canvas == null || canvas.gameObject.scene != activeScene) continue;
                if (firstCanvas == null) firstCanvas = canvas;
                if (figmaCanvas == null
                    && canvas.GetComponentInChildren<PrototypeFlowController>(includeInactive: true) != null)
                {
                    figmaCanvas = canvas;
                }
            }

            var picked = figmaCanvas ?? firstCanvas;
            if (picked != null)
            {
                EnsureEventSystem(activeScene);
                return picked;
            }

            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var created = go.GetComponent<Canvas>();
            created.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(go, "Create Figma2Unity Canvas");
            EnsureEventSystem(activeScene);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Figma2Unity] No Canvas in scene — created a Screen-Space Overlay Canvas.");
            return created;
        }

        private static void EnsureEventSystem(UnityEngine.SceneManagement.Scene activeScene)
        {
            foreach (var existing in UnityEngine.Object.FindObjectsOfType<EventSystem>(includeInactive: true))
            {
                if (existing != null && existing.gameObject.scene == activeScene) return;
            }
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create Figma2Unity EventSystem");
        }

        /// <summary>
        /// Accepts either a raw file key or a full Figma URL and returns just the key.
        /// Supports `/file/<key>/...`, `/design/<key>/...`, `/proto/<key>/...` patterns.
        /// </summary>
        internal static string ExtractFileKey(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var trimmed = input.Trim();
            // Quick path: not a URL, assume it's already a key.
            if (!trimmed.Contains("figma.com")) return trimmed;

            var m = System.Text.RegularExpressions.Regex.Match(
                trimmed, @"figma\.com/(?:file|design|proto)/([A-Za-z0-9]+)");
            if (m.Success) return m.Groups[1].Value;
            return trimmed;
        }

        /// <summary>
        /// Pulls the node-id out of a Figma URL's "node-id=" query param (URL form uses a
        /// hyphen, e.g. node-id=12013-1517940). Returns null when the input is a bare key or
        /// carries no node-id. The colon/hyphen normalisation happens in FigmaDataParser.
        /// </summary>
        internal static string ExtractNodeId(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(
                input, @"node-id=([A-Za-z0-9:%\-]+)");
            if (!m.Success) return null;
            var raw = System.Uri.UnescapeDataString(m.Groups[1].Value).Trim();
            return string.IsNullOrEmpty(raw) ? null : raw;
        }
    }
}
