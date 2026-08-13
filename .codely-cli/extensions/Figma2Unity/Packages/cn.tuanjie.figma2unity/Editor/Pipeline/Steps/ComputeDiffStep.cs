using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Sync;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// v2 Step #9 (Design.md §三): incremental-mode-only Diff computation. Reads
    /// <see cref="F2UContext.ExistingNodeMap"/> (filled by F2UMainWindow before running
    /// the pipeline) and writes the bucketed <see cref="DiffResult"/> to
    /// <see cref="F2UContext.Diff"/>. SyncService later consumes that to do node-level
    /// re-draws.
    ///
    /// Full-mode imports skip this step entirely — they always rebuild the whole tree.
    /// </summary>
    public class ComputeDiffStep : PipelineStep
    {
        public override string DisplayName => "Compute Diff";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx == null) return Task.FromResult(StepResult.Ok());
            if (ctx.Mode != ImportMode.Incremental) return Task.FromResult(StepResult.Ok());

            // FinalizeStep populates fobj.HashData at the end of the pipeline (step 19),
            // but DiffCalculator runs here at step 9 — so without an early populate every
            // node would carry a null HashData and land in the Modified bucket regardless
            // of the actual content delta. Compute hashes upfront for the diff; FinalizeStep
            // recomputes idempotently with the same inputs.
            if (ctx.AllNodes != null)
            {
                foreach (var fobj in ctx.AllNodes)
                {
                    if (fobj == null) continue;
                    fobj.HashData = FObjectHashData.Compute(fobj);
                }
            }

            // First-time Incremental on a scene with no SyncHelpers degrades cleanly to
            // "everything is Added" — equivalent to Full-mode initial import. SyncService
            // then takes the Added branch for every node.
            //
            // Diff only the *instantiable* node set (Screen subtrees) — the exact nodes
            // CreateGameObjectsStep turns into GameObjects. Diffing the full flattened
            // ctx.AllNodes would include the synthetic VirtualPage (__virtual_root__) and
            // every non-Screen library/showcase node; on a first incremental run they'd all
            // land in Added and SyncService would rebuild them flat under the Canvas — the
            // "pages tiled past the canvas" regression. CollectInstantiableNodes mirrors
            // CreateGameObjectsStep's traversal (and falls back to AllNodes when there is no
            // VirtualPage, for Layer A unit tests).
            var diffNodes = ScreenNodeCollector.CollectInstantiableNodes(ctx);
            var diff = new DiffCalculator().ComputeDiff(diffNodes, ctx.ExistingNodeMap);
            ctx.Diff = diff;
            return Task.FromResult(StepResult.Ok());
        }
    }
}
