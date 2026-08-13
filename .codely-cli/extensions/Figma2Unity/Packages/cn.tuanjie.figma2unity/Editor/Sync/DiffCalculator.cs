using System.Collections.Generic;

namespace Figma2Unity.Editor.Sync
{
    /// <summary>
    /// v2 (Design.md §9.2) — node-granularity diff between freshly imported FObject tree
    /// and the SyncHelpers persisted in the scene from a previous import.
    ///
    /// Strategy: detect node-level change only (no field-level diff). Modified nodes get
    /// full re-draw in SyncService.RedrawNode.
    /// </summary>
    public class DiffCalculator
    {
        public virtual DiffResult ComputeDiff(IEnumerable<FObject> newNodes,
            Dictionary<string, SyncHelper> existingMap)
        {
            var result = new DiffResult();
            existingMap = existingMap ?? new Dictionary<string, SyncHelper>();

            var seenIds = new HashSet<string>();

            if (newNodes != null)
            {
                foreach (var fobj in newNodes)
                {
                    if (fobj == null || string.IsNullOrEmpty(fobj.Id)) continue;
                    seenIds.Add(fobj.Id);

                    if (!existingMap.TryGetValue(fobj.Id, out var sync) || sync == null)
                    {
                        result.Added.Add(fobj);
                        continue;
                    }

                    // Cross-version: any change in the hash algorithm (new fields, format
                    // tweaks) bumps FObjectHashData.CurrentVersion. We treat mismatched
                    // versions as Modified for this one re-import; after SyncService writes
                    // the fresh hash, subsequent imports settle back to Unchanged.
                    var oldHash = sync.Data != null ? sync.Data.HashData : null;
                    bool versionMismatch = oldHash == null
                        || oldHash.Version != FObjectHashData.CurrentVersion;

                    if (versionMismatch
                        || fobj.HashData == null
                        || oldHash.ContentHash != fobj.HashData.ContentHash)
                    {
                        result.Modified.Add(fobj);
                    }
                    else
                    {
                        result.Unchanged.Add(fobj);
                    }
                }
            }

            foreach (var kvp in existingMap)
            {
                if (kvp.Value == null) continue;
                if (string.IsNullOrEmpty(kvp.Key)) continue;
                if (!seenIds.Contains(kvp.Key))
                    result.Deleted.Add(kvp.Value);
            }

            return result;
        }
    }

    /// <summary>Bucketed diff output consumed by SyncService.ApplyDiffAsync.</summary>
    public class DiffResult
    {
        public List<FObject> Added = new List<FObject>();
        public List<FObject> Modified = new List<FObject>();
        public List<FObject> Unchanged = new List<FObject>();
        public List<SyncHelper> Deleted = new List<SyncHelper>();

        public int TotalChanged => Added.Count + Modified.Count + Deleted.Count;
        public bool HasChanges => TotalChanged > 0;
    }
}
