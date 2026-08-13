using System;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor
{
    /// <summary>Async pipeline runner with progress + cancellation propagation.</summary>
    public class PipelineRunner
    {
        public event Action<float, string> ProgressChanged;

        public async Task<ImportResult> RunAsync(ImportPipeline pipeline, F2UContext ctx,
            CancellationToken token)
        {
            var progressTask = ReportLoop(ctx, token);
            try
            {
                return await pipeline.RunAsync(ctx, token);
            }
            finally
            {
                ctx.State = ctx.State == PipelineState.Running ? PipelineState.Failed : ctx.State;
            }
        }

        private async Task ReportLoop(F2UContext ctx, CancellationToken token)
        {
            while (!token.IsCancellationRequested && ctx.State == PipelineState.Running)
            {
                ProgressChanged?.Invoke(ctx.Progress, ctx.CurrentStepName);
                await Task.Delay(80, token);
            }
        }
    }
}
