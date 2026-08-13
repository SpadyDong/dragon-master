using System.Collections.Generic;
using System.IO;
using DA_Assets.Figmage;
using UnityEngine;

namespace Figma2Unity.Editor.Rendering
{
    /// <summary>
    /// v2 Track A — adapter that bakes an <see cref="FObject"/> to a PNG via the Figmage
    /// runtime package. The pure-logic enum/geometry conversions live in
    /// <see cref="FigmageNodeMapperCore"/> so they can be tested Layer A; this class is
    /// the thin Unity / Figmage-typed glue.
    ///
    /// Mirrors FCU's <c>FcuFigmageNodeMapper</c> / <c>FcuFigmageSpriteBaker</c>, adapted to
    /// our FObject schema (UnityEngine.Rect bounds, float[] CornerRadius, etc.).
    /// </summary>
    public class FigmageBakerAdapter
    {
        /// <summary>
        /// Build the FigmageNode tree from an FObject and bake it to PNG bytes at the
        /// requested scale. Returns null when the node has no renderable geometry.
        /// </summary>
        public virtual byte[] BakeToPng(FObject fobj, float scale)
        {
            if (fobj == null) return null;
            var node = ToFigmageNode(fobj);
            if (node == null) return null;
            return FigmageBakeUtility.BakePngBytes(node, scale);
        }

        /// <summary>
        /// Convenience overload: bake to PNG and write to <paramref name="outputPath"/>.
        /// Caller is responsible for AssetDatabase.ImportAsset.
        /// </summary>
        public virtual bool BakeToPngFile(FObject fobj, string outputPath, float scale)
        {
            var bytes = BakeToPng(fobj, scale);
            if (bytes == null || bytes.Length == 0) return false;
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(outputPath, bytes);
            return true;
        }

        public static FigmageNode ToFigmageNode(FObject source)
        {
            if (source == null) return null;
            var node = new FigmageNode
            {
                Id = source.Id,
                Name = source.Name,
                AbsoluteBoundingBox = ToRect(source.AbsoluteBoundingBox),
                AbsoluteRenderBounds = ToRect(source.AbsoluteRenderBounds),
                Size = new Vector2(source.AbsoluteBoundingBox.width, source.AbsoluteBoundingBox.height),
                Rotation = 0f, // 2D rotation is baked into RelativeTransform; Figmage reads that path.
                RelativeTransform = FigmageNodeMapperCore.RelativeTransformToLists(source.RelativeTransform),
                Opacity = Mathf.Clamp01(source.Opacity),
                CornerRadius = FigmageNodeMapperCore.UniformCornerRadius(source.CornerRadius),
                CornerSmoothing = source.CornerSmoothing,
                StrokeWeight = source.StrokeWeight,
                StrokeAlign = ToStrokeAlign(source.StrokeAlign),
                StrokeJoin = ToStrokeJoin(source.StrokeJoin),
                StrokeCap = ToStrokeCap(source.StrokeCap),
                StrokeMiterLimit = source.StrokeMiterLimit,
                DashPattern = source.DashPattern != null ? new List<float>(source.DashPattern) : null,
                Fills = ToPaints(source.Fills),
                Strokes = ToPaints(source.Strokes),
                Effects = ToEffects(source.Effects),
                FillGeometry = ToGeometry(source.FillGeometry),
                StrokeGeometry = ToGeometry(source.StrokeGeometry),
                Children = ToChildren(source.Children),
            };

            if (FigmageNodeMapperCore.HasPerCornerRadius(source.CornerRadius))
                node.CornerRadiuses = new List<float>(source.CornerRadius);

            if (FigmageNodeMapperCore.HasIndividualStrokeWeights(source.IndividualStrokeWeights))
            {
                var w = source.IndividualStrokeWeights;
                // Figma order: [top, right, bottom, left]
                node.IndividualStrokeWeights = new FigmageStrokeWeights(w[0], w[2], w[3], w[1]);
            }
            else
            {
                node.IndividualStrokeWeights = FigmageStrokeWeights.Uniform(source.StrokeWeight);
            }

            return node;
        }

        private static FigmageRect ToRect(Rect r) => new FigmageRect(r.x, r.y, r.width, r.height);

        private static FigmageStrokeAlign ToStrokeAlign(StrokeAlign src)
        {
            switch (FigmageNodeMapperCore.MapStrokeAlign(src))
            {
                case FigmageNodeMapperCore.FigmageStrokeAlignMirror.Inside: return FigmageStrokeAlign.Inside;
                case FigmageNodeMapperCore.FigmageStrokeAlignMirror.Outside: return FigmageStrokeAlign.Outside;
                default: return FigmageStrokeAlign.Center;
            }
        }

        private static FigmageStrokeJoin ToStrokeJoin(StrokeJoin src)
        {
            switch (src)
            {
                case StrokeJoin.BEVEL: return FigmageStrokeJoin.Bevel;
                case StrokeJoin.ROUND: return FigmageStrokeJoin.Round;
                default: return FigmageStrokeJoin.Miter;
            }
        }

        private static FigmageStrokeCap ToStrokeCap(StrokeCap src)
        {
            switch (src)
            {
                case StrokeCap.ROUND: return FigmageStrokeCap.Round;
                case StrokeCap.SQUARE: return FigmageStrokeCap.Square;
                default: return FigmageStrokeCap.None;
            }
        }

        private static List<FigmagePaint> ToPaints(List<Paint> src)
        {
            var result = new List<FigmagePaint>();
            if (src == null) return result;
            foreach (var p in src)
                if (p != null) result.Add(ToPaint(p));
            return result;
        }

        private static FigmagePaint ToPaint(Paint src)
        {
            var p = new FigmagePaint
            {
                Type = ToPaintType(FigmageNodeMapperCore.MapPaintType(src.Type)),
                Color = src.Color,
                Visible = src.Visible,
                BlendMode = src.BlendMode.ToString(),
                Opacity = Mathf.Clamp01(src.Opacity),
            };
            if (src.Gradient != null)
            {
                if (src.Gradient.GradientHandlePositions != null && src.Gradient.GradientHandlePositions.Length >= 6)
                {
                    p.GradientHandlePositions = new List<Vector2>(3);
                    for (int i = 0; i < 6; i += 2)
                        p.GradientHandlePositions.Add(new Vector2(
                            src.Gradient.GradientHandlePositions[i],
                            src.Gradient.GradientHandlePositions[i + 1]));
                }
                if (src.Gradient.Stops != null)
                {
                    p.GradientStops = new List<FigmageGradientStop>(src.Gradient.Stops.Count);
                    foreach (var s in src.Gradient.Stops)
                        p.GradientStops.Add(new FigmageGradientStop(s.Position, s.Color));
                }
            }
            return p;
        }

        private static FigmagePaintType ToPaintType(FigmageNodeMapperCore.FigmagePaintTypeMirror mirror)
        {
            switch (mirror)
            {
                case FigmageNodeMapperCore.FigmagePaintTypeMirror.GradientLinear: return FigmagePaintType.GradientLinear;
                case FigmageNodeMapperCore.FigmagePaintTypeMirror.GradientRadial: return FigmagePaintType.GradientRadial;
                case FigmageNodeMapperCore.FigmagePaintTypeMirror.GradientAngular: return FigmagePaintType.GradientAngular;
                case FigmageNodeMapperCore.FigmagePaintTypeMirror.GradientDiamond: return FigmagePaintType.GradientDiamond;
                default: return FigmagePaintType.Solid;
            }
        }

        private static List<FigmageEffect> ToEffects(List<Effect> src)
        {
            var result = new List<FigmageEffect>();
            if (src == null) return result;
            foreach (var e in src)
            {
                if (e == null) continue;
                result.Add(new FigmageEffect
                {
                    Type = ToEffectType(FigmageNodeMapperCore.MapEffectType(e.Type)),
                    Visible = e.Visible,
                    Color = e.Color,
                    Opacity = 1f,
                    BlendMode = e.BlendMode.ToString(),
                    Offset = e.Offset,
                    Radius = e.Radius,
                    Spread = e.Spread,
                    BlurType = e.BlurType,
                    StartRadius = e.StartRadius,
                    StartOffset = e.StartOffset ?? Vector2.zero,
                    EndOffset = e.EndOffset ?? Vector2.right,
                });
            }
            return result;
        }

        private static FigmageEffectType ToEffectType(FigmageNodeMapperCore.FigmageEffectTypeMirror mirror)
        {
            switch (mirror)
            {
                case FigmageNodeMapperCore.FigmageEffectTypeMirror.InnerShadow: return FigmageEffectType.InnerShadow;
                case FigmageNodeMapperCore.FigmageEffectTypeMirror.LayerBlur: return FigmageEffectType.LayerBlur;
                case FigmageNodeMapperCore.FigmageEffectTypeMirror.BackgroundBlur: return FigmageEffectType.BackgroundBlur;
                default: return FigmageEffectType.DropShadow;
            }
        }

        private static List<FigmageGeometry> ToGeometry(List<string> paths)
        {
            var normalized = FigmageNodeMapperCore.NormalizeGeometryPaths(paths);
            if (normalized == null) return null;
            var result = new List<FigmageGeometry>(normalized.Count);
            foreach (var p in normalized) result.Add(new FigmageGeometry { Path = p });
            return result;
        }

        private static List<FigmageNode> ToChildren(List<FObject> src)
        {
            if (src == null) return null;
            var result = new List<FigmageNode>(src.Count);
            foreach (var c in src) result.Add(ToFigmageNode(c));
            return result;
        }
    }
}
