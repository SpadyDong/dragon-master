using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor
{
    /// <summary>Base class for a pipeline step.</summary>
    public abstract class PipelineStep
    {
        public abstract string DisplayName { get; }
        public virtual float ProgressWeight { get; } = 1f;

        public abstract Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token);
    }
}
