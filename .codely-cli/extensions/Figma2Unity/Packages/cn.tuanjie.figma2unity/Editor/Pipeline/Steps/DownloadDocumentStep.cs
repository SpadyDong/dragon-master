using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 1: Download Figma document JSON.</summary>
    public class DownloadDocumentStep : PipelineStep
    {
        public override string DisplayName => "Download Document";
        public override float ProgressWeight => 3f;

        public override async Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (string.IsNullOrEmpty(ctx.FileId))
                return StepResult.Fail("FileId is empty");
            if (ctx.ApiClient == null)
                return StepResult.Fail("ApiClient not configured");
            try
            {
                ctx.DocumentJson = await ctx.ApiClient.GetDocumentJsonAsync(ctx.FileId, token);
                return StepResult.Ok();
            }
            catch (System.Exception ex)
            {
                return StepResult.Fail("Download failed: " + ex.Message);
            }
        }
    }
}
