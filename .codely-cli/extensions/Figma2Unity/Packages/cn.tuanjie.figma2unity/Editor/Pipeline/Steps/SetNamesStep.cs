using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 6: Normalize names → FolderName / FileName.</summary>
    public class SetNamesStep : PipelineStep
    {
        public override string DisplayName => "Set Names";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.AllNodes == null)
                return Task.FromResult(StepResult.Fail("AllNodes is null"));

            string sep = ctx.Config?.TagSeparator ?? "#";
            foreach (var fobj in ctx.AllNodes)
            {
                fobj.FileName = SanitizeName(fobj.Name, sep);
                fobj.FolderName = fobj.FileName;
            }
            return Task.FromResult(StepResult.Ok());
        }

        /// <summary>Strip "#tag" suffixes (consumed by TagSetter Pass2), trim, and
        /// sanitize illegal path characters. Empty name → falls back to node Id.</summary>
        public static string SanitizeName(string raw, string sep)
        {
            if (string.IsNullOrEmpty(raw)) return "Node";
            // Strip #tag tokens
            string trimmed = raw;
            int idx = trimmed.IndexOf(sep);
            if (idx >= 0) trimmed = trimmed.Substring(0, idx).Trim();
            if (string.IsNullOrEmpty(trimmed)) trimmed = "Node";

            var sb = new StringBuilder(trimmed.Length);
            foreach (var c in trimmed)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' ')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            return sb.ToString();
        }
    }
}
