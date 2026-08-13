namespace Figma2Unity
{
    /// <summary>Result of an entire import run.</summary>
    public class ImportResult
    {
        public bool Success;
        public string FailedStepName;
        public string FailedReason;

        public static ImportResult Ok() => new ImportResult { Success = true };

        public static ImportResult Fail(string stepName, string reason) =>
            new ImportResult { Success = false, FailedStepName = stepName, FailedReason = reason };
    }
}
