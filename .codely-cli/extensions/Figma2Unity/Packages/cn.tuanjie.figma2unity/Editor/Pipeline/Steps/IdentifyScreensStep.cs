using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.PrototypeFlow;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 7: Identify Screens + FlowSections.</summary>
    public class IdentifyScreensStep : PipelineStep
    {
        public override string DisplayName => "Identify Screens";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            new FlowManager().IdentifyScreensAndSections(ctx);
            return Task.FromResult(StepResult.Ok());
        }
    }
}
