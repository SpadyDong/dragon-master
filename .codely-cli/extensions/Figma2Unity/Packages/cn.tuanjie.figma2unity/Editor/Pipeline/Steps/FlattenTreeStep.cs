using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 3: Flatten the FObject tree into a read-only list + NodeMap.</summary>
    public class FlattenTreeStep : PipelineStep
    {
        public override string DisplayName => "Flatten Tree";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.VirtualPage == null)
                return Task.FromResult(StepResult.Fail("VirtualPage is null"));

            var flat = new List<FObject>();
            var map = new Dictionary<string, FObject>();
            Flatten(ctx.VirtualPage, flat, map);

            ctx.AllNodes = flat;
            ctx.NodeMap = map;
            return Task.FromResult(StepResult.Ok());
        }

        public static void Flatten(FObject root, List<FObject> flat, Dictionary<string, FObject> map)
        {
            if (root == null) return;
            flat.Add(root);
            if (!string.IsNullOrEmpty(root.Id))
                map[root.Id] = root;
            if (root.Children != null)
            {
                foreach (var c in root.Children)
                {
                    if (c != null) c.Parent = root;
                    Flatten(c, flat, map);
                }
            }
        }
    }
}
