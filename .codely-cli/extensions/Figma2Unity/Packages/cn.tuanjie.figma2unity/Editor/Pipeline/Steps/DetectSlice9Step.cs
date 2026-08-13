using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Rendering;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// Step 8: detect 9-slice candidates and tag them (Design.md §三 #8).
    /// Adds <see cref="FcuTag.Slice9"/> for manual layouts, <see cref="FcuTag.AutoSlice9"/>
    /// for plain rounded-rects. Tag mutation is the only side-effect; downstream
    /// strategy resolution + sprite border configuration consume these tags.
    /// </summary>
    public class DetectSlice9Step : PipelineStep
    {
        public override string DisplayName => "Detect 9-Slice";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.AllNodes == null) return Task.FromResult(StepResult.Ok());

            foreach (var fobj in ctx.AllNodes)
            {
                if (fobj == null) continue;
                if (fobj.Tags == null) continue;
                // Skip explicit Ignore — they won't be drawn anyway.
                if (fobj.Tags.Contains(FcuTag.Ignore)) continue;

                if (!fobj.Tags.Contains(FcuTag.Slice9) && Slice9Detector.IsManual9Slice(fobj))
                {
                    fobj.Tags.Add(FcuTag.Slice9);
                    continue;
                }

                if (!fobj.Tags.Contains(FcuTag.AutoSlice9) && Slice9Detector.IsAuto9Slice(fobj))
                    fobj.Tags.Add(FcuTag.AutoSlice9);
            }
            return Task.FromResult(StepResult.Ok());
        }
    }
}
