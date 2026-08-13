namespace Figma2Unity
{
    /// <summary>Result of a single pipeline step.</summary>
    public class StepResult
    {
        public bool Success;
        public string Reason;

        public static StepResult Ok() => new StepResult { Success = true };
        public static StepResult Fail(string reason) => new StepResult { Success = false, Reason = reason };
    }
}
