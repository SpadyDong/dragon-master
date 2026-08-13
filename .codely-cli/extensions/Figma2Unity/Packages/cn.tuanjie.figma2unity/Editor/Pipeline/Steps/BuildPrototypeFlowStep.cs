using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.PrototypeFlow;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 12: Build PrototypeFlowController + Screen markers (no Prefab save here).</summary>
    public class BuildPrototypeFlowStep : PipelineStep
    {
        public override string DisplayName => "Build Prototype Flow";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.Config != null && !ctx.Config.BuildPrototypeFlow)
                return Task.FromResult(StepResult.Ok());
            new FlowManager().BuildPrototypeFlow(ctx);
            return Task.FromResult(StepResult.Ok());
        }
    }
}
