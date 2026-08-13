using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Rendering;
using UnityEngine;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// v2 Step #15 (Design.md §三): GPU-bake sprites via Figmage for BakeSprite / Slice9
    /// strategy nodes. Delegates to <see cref="SpriteGenerator"/>; both no-ops cleanly when
    /// <see cref="F2UConfig.EnableFigmageBaking"/> is false (v1 default).
    /// </summary>
    public class BakeSpritesStep : PipelineStep
    {
        public override string DisplayName => "Bake Sprites";
        public override float ProgressWeight => 5f;

        public override async Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx?.Config == null) return StepResult.Ok();
            if (!ctx.Config.EnableFigmageBaking) return StepResult.Ok();

            var baker = ctx.BakerAdapter as FigmageBakerAdapter ?? new FigmageBakerAdapter();
            ctx.BakerAdapter = baker; // expose for downstream consumers / tests
            var generator = new SpriteGenerator(baker);

            try
            {
                await generator.GenerateSpritesAsync(ctx, token);
            }
            catch (System.OperationCanceledException)
            {
                return StepResult.Fail("Canceled");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Figma2Unity] BakeSpritesStep failed: {ex.Message}");
                // Don't fail the entire pipeline — DirectColor fallback still produces a usable scene.
            }
            return StepResult.Ok();
        }
    }
}
