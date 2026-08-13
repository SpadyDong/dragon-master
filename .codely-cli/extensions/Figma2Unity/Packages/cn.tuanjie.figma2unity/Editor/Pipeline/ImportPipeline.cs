using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor
{
    /// <summary>
    /// Composable step list — replaces FCU's hardcoded 26-step pipeline.
    /// Conditional steps (e.g. ComputeDiff) are added only in the corresponding mode.
    ///
    /// Configure() lives in ImportPipeline.Configure.cs so Layer A tests can include
    /// this core file without dragging in the 20-step (Incremental: 21-step)
    /// concrete-step dependency chain (many of which touch UnityEditor / AssetDatabase
    /// APIs).
    /// </summary>
    public partial class ImportPipeline
    {
        private readonly List<PipelineStep> _steps = new List<PipelineStep>();

        public IReadOnlyList<PipelineStep> Steps => _steps;

        public async Task<ImportResult> RunAsync(F2UContext ctx, CancellationToken token)
        {
            return await RunStepsAsync(_steps, ctx, token);
        }

        /// <summary>
        /// Static helper that runs an arbitrary step list. Exposed so Layer A tests
        /// can drive the pipeline with mock steps without depending on Configure()'s
        /// 20-step (Incremental: 21-step) bootstrap.
        /// </summary>
        public static async Task<ImportResult> RunStepsAsync(
            IReadOnlyList<PipelineStep> steps, F2UContext ctx, CancellationToken token)
        {
            if (ctx == null) return ImportResult.Fail("(null)", "Context is null");
            ctx.State = PipelineState.Running;

            float totalWeight = 0f;
            foreach (var s in steps) totalWeight += s.ProgressWeight;
            if (totalWeight <= 0f) totalWeight = steps.Count;
            float accumulated = 0f;

            int stepIndex = 0;
            foreach (var step in steps)
            {
                token.ThrowIfCancellationRequested();
                ctx.CurrentStepName = step.DisplayName;
                ctx.Progress = accumulated / totalWeight;

                // Breadcrumb to Editor.log: if a step hangs the editor (frozen UI, no console
                // access), this last line still flushes to disk so you can `tail Editor.log`
                // from a shell and see which step was entered last.
                UnityEngine.Debug.Log(
                    $"[Figma2Unity] Step {++stepIndex}/{steps.Count}: {step.DisplayName} (progress {ctx.Progress:P0})");

                // Yield back to the caller before running each step. Many steps complete
                // synchronously (Task.FromResult — e.g. cache reads, JSON parsing), so without
                // this the whole pipeline runs inline inside the very first RunAsync() call and
                // the editor's progress-bar polling loop never gets a chance to redraw — the UI
                // appears frozen on "Starting...". Yielding suspends the task so the UI thread
                // can repaint the current step name/progress and poll the Cancel button.
                await Task.Yield();
                token.ThrowIfCancellationRequested();

                StepResult result;
                try
                {
                    result = await step.ExecuteAsync(ctx, token);
                }
                catch (System.Exception ex)
                {
                    ctx.State = PipelineState.Failed;
                    return ImportResult.Fail(step.DisplayName, ex.Message);
                }

                if (result == null || !result.Success)
                {
                    ctx.State = PipelineState.Failed;
                    return ImportResult.Fail(step.DisplayName, result?.Reason ?? "Unknown");
                }

                accumulated += step.ProgressWeight;
                ctx.Progress = accumulated / totalWeight;
            }

            ctx.State = PipelineState.Completed;
            return ImportResult.Ok();
        }

        /// <summary>Layer A test helper: pre-populate the step list (bypasses Configure).</summary>
        internal void AddStepForTests(PipelineStep step) => _steps.Add(step);
    }
}
