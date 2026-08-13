using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Api;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 2: Parse JSON → POCO → FObject tree.</summary>
    public class ParseDocumentStep : PipelineStep
    {
        public override string DisplayName => "Parse Document";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (string.IsNullOrEmpty(ctx.DocumentJson))
                return Task.FromResult(StepResult.Fail("DocumentJson is empty"));
            try
            {
                var parser = new FigmaDataParser();
                ctx.VirtualPage = parser.ParseJson(ctx.DocumentJson, ctx.Config);
                return Task.FromResult(StepResult.Ok());
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(StepResult.Fail("Parse failed: " + ex.Message));
            }
        }
    }
}
