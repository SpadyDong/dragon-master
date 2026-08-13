using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Tags;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 5: Three-pass tag inference.</summary>
    public class SetTagsStep : PipelineStep
    {
        public override string DisplayName => "Set Tags";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.AllNodes == null)
                return Task.FromResult(StepResult.Fail("AllNodes is null"));

            var setter = new TagSetter(ctx.Config);
            setter.SetTagsByFigmaType(ctx.AllNodes);
            setter.SetSmartTags(ctx.AllNodes);
            setter.SetIgnoredObjects(ctx.AllNodes);
            return Task.FromResult(StepResult.Ok());
        }
    }
}
