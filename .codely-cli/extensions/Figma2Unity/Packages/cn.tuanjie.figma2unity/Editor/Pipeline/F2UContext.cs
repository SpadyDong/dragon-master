using System.Collections.Generic;
using UnityEngine;

namespace Figma2Unity.Editor
{
    /// <summary>
    /// Pipeline-wide context. Holds the FObject tree, flat views, Unity GO/Sprite
    /// associations (kept OFF FObject to keep the data model pure), services, and
    /// progress state.
    /// </summary>
    public class F2UContext
    {
        // Input
        public string FileId;
        public ImportMode Mode = ImportMode.Full;

        // Tree (source of truth)
        public FObject VirtualPage;

        // Flat read-only view (built by FlattenTreeStep)
        public IReadOnlyList<FObject> AllNodes;
        public Dictionary<string, FObject> NodeMap = new Dictionary<string, FObject>();

        // Tag groups
        public Dictionary<FcuTag, List<FObject>> TagGroups = new Dictionary<FcuTag, List<FObject>>();

        // Prototype
        public List<FObject> Screens = new List<FObject>();
        public List<FObject> FlowSections = new List<FObject>();

        // Unity associations (Editor-only — never on FObject)
        public Dictionary<string, GameObject> NodeGameObjectMap = new Dictionary<string, GameObject>();
        public Dictionary<string, string> NodeSpritePathMap = new Dictionary<string, string>();

        // Existing data (incremental mode)
        public SyncHelper[] ExistingSyncHelpers;
        public Dictionary<string, SyncHelper> ExistingNodeMap = new Dictionary<string, SyncHelper>();

        // Incremental sync diff (written by ComputeDiffStep; consumed by SyncService).
        // Null in Full-mode imports.
        public Sync.DiffResult Diff;

        // Unity refs
        public Canvas TargetCanvas;
        public F2UConfig Config;

        // Services (injected by ImportPipeline before run)
        public Api.IFigmaApiClient ApiClient;
        // BakerAdapter is v2; v1 keeps it null.
        public object BakerAdapter;

        // Auth
        public string PersonalAccessToken;

        // Progress
        public PipelineState State = PipelineState.Idle;
        public float Progress;
        public string CurrentStepName;

        // Raw JSON (set by DownloadDocumentStep)
        public string DocumentJson;

        // Convenience
        public GameObject GetGameObject(FObject fobj)
            => fobj != null && NodeGameObjectMap.TryGetValue(fobj.Id, out var go) ? go : null;

        public void SetGameObject(FObject fobj, GameObject go)
        {
            if (fobj == null || string.IsNullOrEmpty(fobj.Id)) return;
            NodeGameObjectMap[fobj.Id] = go;
        }

        public string GetSpritePath(FObject fobj)
            => fobj != null && NodeSpritePathMap.TryGetValue(fobj.Id, out var p) ? p : null;

        public void SetSpritePath(FObject fobj, string path)
        {
            if (fobj == null || string.IsNullOrEmpty(fobj.Id)) return;
            NodeSpritePathMap[fobj.Id] = path;
        }
    }
}
