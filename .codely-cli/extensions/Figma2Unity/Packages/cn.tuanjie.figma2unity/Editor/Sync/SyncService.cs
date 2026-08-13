using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Figma2Unity.Editor.Sync
{
    /// <summary>
    /// v2 (Design.md §9.3) — applies a <see cref="DiffResult"/> to the live scene:
    ///   1. Delete buckets first (avoids stale parent refs).
    ///   2. Re-draw Modified nodes in-place (same GameObject, refreshed components).
    ///   3. Create Added nodes under their parents, draw, attach SyncHelper + fresh hash.
    ///   4. (Future Track A integration) Re-bake changed Sprites.
    ///
    /// Drawing / creation is delegated via injected seams so Layer A tests don't need to
    /// drag in UnityEditor APIs or the full Drawer stack. The Editor pipeline wires the
    /// real implementations from CreateGameObjectsStep / DrawerCoordinator.
    /// </summary>
    public class SyncService
    {
        /// <summary>Re-draw the existing GameObject (geometry + components) for a Modified node.</summary>
        public System.Action<FObject, GameObject, F2UContext> RedrawNode;

        /// <summary>
        /// Create a fresh GameObject for an Added node, parent it under the resolved parent
        /// GO (or the TargetCanvas root if no parent has a GO), draw all components, and
        /// register it in ctx.NodeGameObjectMap. Implementation is shared with
        /// CreateGameObjectsStep + DrawComponentsStep in the Editor wiring.
        /// </summary>
        public System.Action<FObject, F2UContext> CreateAndDrawNode;

        /// <summary>
        /// Optional Sprite re-bake hook (Track A integration point). Receives the list of
        /// nodes that changed visually and need their Sprite refreshed. v2 Track C ships
        /// with this null; Track A wires it once SpriteGenerator lands.
        /// </summary>
        public System.Func<System.Collections.Generic.List<FObject>, F2UContext, CancellationToken, Task> RebakeSprites;

        public virtual async Task ApplyDiffAsync(DiffResult diff, F2UContext ctx, CancellationToken token)
        {
            if (diff == null || ctx == null) return;

            // 1) Deletes — DestroyImmediate cleans up GameObjects in the Editor; ctx.NodeGameObjectMap
            //    doesn't yet hold these (Modified/Added populate it), so no explicit map cleanup.
            for (int i = 0; i < diff.Deleted.Count; i++)
            {
                var sync = diff.Deleted[i];
                if (sync == null) continue;
                var go = sync.gameObject;
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            // 2) Modified — point ctx at the existing GO, then redraw all components.
            for (int i = 0; i < diff.Modified.Count; i++)
            {
                var fobj = diff.Modified[i];
                if (fobj == null) continue;
                if (!ctx.ExistingNodeMap.TryGetValue(fobj.Id, out var sync) || sync == null) continue;
                var go = sync.gameObject;
                if (go == null) continue;

                ctx.SetGameObject(fobj, go);
                RedrawNode?.Invoke(fobj, go, ctx);

                // Refresh persisted sync metadata to match the fresh node state.
                sync.Data.Tags = new System.Collections.Generic.List<FcuTag>(fobj.Tags ?? new System.Collections.Generic.List<FcuTag>());
                sync.Data.HashData = fobj.HashData;
                sync.Data.FigmaName = fobj.Name;
                sync.Data.FolderName = fobj.FolderName;
                sync.Data.FileName = fobj.FileName;
            }

            // 3) Added — create + draw + attach a fresh SyncHelper with the current hash.
            for (int i = 0; i < diff.Added.Count; i++)
            {
                var fobj = diff.Added[i];
                if (fobj == null) continue;
                CreateAndDrawNode?.Invoke(fobj, ctx);

                var go = ctx.GetGameObject(fobj);
                if (go == null) continue;
                var sync = go.GetComponent<SyncHelper>() ?? go.AddComponent<SyncHelper>();
                if (sync.Data == null) sync.Data = new SyncData();
                sync.Data.FigmaId = fobj.Id;
                sync.Data.FigmaName = fobj.Name;
                sync.Data.FolderName = fobj.FolderName;
                sync.Data.FileName = fobj.FileName;
                sync.Data.Tags = new System.Collections.Generic.List<FcuTag>(fobj.Tags ?? new System.Collections.Generic.List<FcuTag>());
                sync.Data.HashData = fobj.HashData;
            }

            // 4) Sprite re-bake hook (Track A integration). Collect nodes whose visuals
            //    changed (Modified Image-family + new Added Image-family).
            if (RebakeSprites != null)
            {
                var changed = new System.Collections.Generic.List<FObject>();
                AccumulateImageNodes(diff.Modified, changed);
                AccumulateImageNodes(diff.Added, changed);
                if (changed.Count > 0)
                    await RebakeSprites(changed, ctx, token);
            }
        }

        private static void AccumulateImageNodes(System.Collections.Generic.List<FObject> source,
            System.Collections.Generic.List<FObject> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                var n = source[i];
                if (n == null || n.Tags == null) continue;
                if (n.Tags.Contains(FcuTag.Image)
                    || n.Tags.Contains(FcuTag.Slice9)
                    || n.Tags.Contains(FcuTag.AutoSlice9))
                {
                    dest.Add(n);
                }
            }
       }
    }
}
