using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Editor.Pipeline.Steps
{
    /// <summary>Step 4: Aggregate Fill/Stroke → FGraphic boolean flags + simplified lists.</summary>
    public class ComputeGraphicsStep : PipelineStep
    {
        public override string DisplayName => "Compute Graphics";

        public override Task<StepResult> ExecuteAsync(F2UContext ctx, CancellationToken token)
        {
            if (ctx.AllNodes == null)
                return Task.FromResult(StepResult.Fail("AllNodes is null"));

            foreach (var fobj in ctx.AllNodes)
            {
                fobj.Graphic = Compute(fobj);
            }
            return Task.FromResult(StepResult.Ok());
        }

        public static FGraphic Compute(FObject fobj)
        {
            var g = new FGraphic();
            if (fobj.Fills != null)
            {
                foreach (var p in fobj.Fills)
                {
                    if (p == null || !p.Visible) continue;
                    switch (p.Type)
                    {
                        case PaintType.SOLID:
                            g.HasSolidFill = true;
                            g.Fills.Add(new FFill { Type = p.Type, Color = p.Color, Opacity = p.Opacity });
                            break;
                        case PaintType.GRADIENT_LINEAR:
                        case PaintType.GRADIENT_RADIAL:
                        case PaintType.GRADIENT_ANGULAR:
                        case PaintType.GRADIENT_DIAMOND:
                            g.HasGradientFill = true;
                            g.Fills.Add(new FFill { Type = p.Type, Gradient = p.Gradient, Opacity = p.Opacity });
                            break;
                        case PaintType.IMAGE:
                            g.HasImageFill = true;
                            g.Fills.Add(new FFill { Type = p.Type, Opacity = p.Opacity });
                            break;
                    }
                }
            }
            if (fobj.Strokes != null)
            {
                foreach (var s in fobj.Strokes)
                {
                    if (s == null || !s.Visible) continue;
                    g.HasStroke = true;
                    if (s.Type == PaintType.SOLID) g.HasSolidStroke = true;
                    else g.HasGradientStroke = true;
                    g.Strokes.Add(new FStroke
                    {
                        Type = s.Type,
                        Color = s.Color,
                        Weight = fobj.StrokeWeight,
                        Align = fobj.StrokeAlign,
                    });
                }
            }
            return g;
        }
    }
}
