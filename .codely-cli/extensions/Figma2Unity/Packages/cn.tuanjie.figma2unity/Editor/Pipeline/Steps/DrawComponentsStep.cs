using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Drawers;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 11: Per-node Drawer dispatch.</summary>
    public class DrawComponentsStep : PipelineStep
    {
        public override string DisplayName => "Draw Components";
        public override float ProgressWeight => 2f;

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            var coord = new DrawerCoordinator();
            coord.DrawAll(ctx);
            return Task.FromResult(StepResult.Ok());
        }
    }
}
