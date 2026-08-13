using Figma2Unity.Editor.Pipeline.Steps;

namespace Figma2Unity.Editor
{
    /// <summary>
    /// Configure() — auto-populates the 19-step pipeline (20 steps in Incremental mode,
    /// where ComputeDiffStep is inserted before CreateGameObjectsStep). Kept in its own
    /// file so Layer A tests can compile ImportPipeline core (RunStepsAsync) without
    /// dragging in all concrete step types (some of which touch UnityEditor APIs).
    /// </summary>
    public partial class ImportPipeline
    {
        public void Configure(ImportMode mode, F2UConfig config)
        {
            _steps.Clear();
            _steps.Add(new DownloadDocumentStep());
            _steps.Add(new ParseDocumentStep());
            _steps.Add(new FlattenTreeStep());
            _steps.Add(new ComputeGraphicsStep());
            _steps.Add(new SetTagsStep());
            _steps.Add(new SetNamesStep());
            _steps.Add(new IdentifyScreensStep());
            _steps.Add(new DetectSlice9Step());

            if (mode == ImportMode.Incremental)
                _steps.Add(new ComputeDiffStep());

            _steps.Add(new CreateGameObjectsStep(config));
            _steps.Add(new DrawComponentsStep());
            _steps.Add(new BindFlowButtonsStep());
            _steps.Add(new BuildPrototypeFlowStep());
            _steps.Add(new DownloadSpritesStep());
            _steps.Add(new BakeSpritesStep());
            _steps.Add(new ApplySpritesStep());
            _steps.Add(new DownloadFontsStep());
            _steps.Add(new BuildSpriteAtlasStep());
            _steps.Add(new FinalizeStep());
        }
    }
}
