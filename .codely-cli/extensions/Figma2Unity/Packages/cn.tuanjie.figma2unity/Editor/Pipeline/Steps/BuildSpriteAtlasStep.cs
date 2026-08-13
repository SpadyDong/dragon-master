using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Editor.Rendering;
using UnityEngine;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>
    /// v2 Step #18 (Design.md §三): pack every imported / baked sprite into a single
    /// SpriteAtlas asset so Draw-Call batching survives. Skipped when
    /// <see cref="F2UConfig.EnableSpriteAtlas"/> is false or no sprites exist.
    ///
    /// Atlas asset is created at <c>{OutputFolder}/F2USpriteAtlas.spriteatlas</c>.
    /// Re-running converges on the current sprite list (idempotent).
    /// </summary>
    public class BuildSpriteAtlasStep : PipelineStep
    {
        public override string DisplayName => "Build Sprite Atlas";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx?.Config == null) return Task.FromResult(StepResult.Ok());
            if (!ctx.Config.EnableSpriteAtlas) return Task.FromResult(StepResult.Ok());
            if (ctx.NodeSpritePathMap == null || ctx.NodeSpritePathMap.Count == 0)
                return Task.FromResult(StepResult.Ok());

            string outputFolder = string.IsNullOrEmpty(ctx.Config.OutputFolder)
                ? "Assets"
                : ctx.Config.OutputFolder;
            string atlasPath = $"{outputFolder}/F2USpriteAtlas.spriteatlas";

            try
            {
                var builder = new SpriteAtlasBuilder();
                var atlas = builder.Build(ctx, atlasPath);
                if (atlas != null)
                    Debug.Log($"[Figma2Unity] BuildSpriteAtlas: packed atlas at {atlasPath}.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Figma2Unity] BuildSpriteAtlasStep failed: {ex.Message}");
            }
            return Task.FromResult(StepResult.Ok());
        }
    }
}
