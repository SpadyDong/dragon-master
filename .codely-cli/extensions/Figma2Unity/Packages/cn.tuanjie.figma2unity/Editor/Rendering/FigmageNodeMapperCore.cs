using System.Collections.Generic;

namespace Figma2Unity.Editor.Rendering
{
    /// <summary>
    /// v2 Track A — pure-logic mapping helpers used by <c>FigmageBakerAdapter</c> when
    /// translating an <see cref="FObject"/> to a DA_Assets.Figmage FigmageNode.
    ///
    /// These helpers are factored out so Layer A tests can exercise the enum / paint /
    /// effect / geometry conversions without dragging the Figmage runtime package into
    /// the standalone test csproj.
    ///
    /// Mirrors FCU's FcuFigmageNodeMapper (Assets/DA-Assets/Figma-Converter-for-Unity/
    /// Runtime/Scripts/AssetPipeline/Sprites/Generation/FcuFigmageNodeMapper.cs) but
    /// adapts to our slightly different schemas (Rect vs BoundingBox, float[] vs
    /// List&lt;float?&gt; for CornerRadius, etc.).
    /// </summary>
    public static class FigmageNodeMapperCore
    {
        // ---------- Paint type ----------

        public enum FigmagePaintTypeMirror { Solid, GradientLinear, GradientRadial, GradientAngular, GradientDiamond }

        public static FigmagePaintTypeMirror MapPaintType(PaintType src)
        {
            switch (src)
            {
                case PaintType.GRADIENT_LINEAR:  return FigmagePaintTypeMirror.GradientLinear;
                case PaintType.GRADIENT_RADIAL:  return FigmagePaintTypeMirror.GradientRadial;
                case PaintType.GRADIENT_ANGULAR: return FigmagePaintTypeMirror.GradientAngular;
                case PaintType.GRADIENT_DIAMOND: return FigmagePaintTypeMirror.GradientDiamond;
                default: return FigmagePaintTypeMirror.Solid; // SOLID / IMAGE / EMBOSSED → Solid (IMAGE handled outside baker)
            }
        }

        // ---------- Stroke align ----------

        public enum FigmageStrokeAlignMirror { Inside, Center, Outside }

        public static FigmageStrokeAlignMirror MapStrokeAlign(StrokeAlign src)
        {
            switch (src)
            {
                case StrokeAlign.INSIDE:  return FigmageStrokeAlignMirror.Inside;
                case StrokeAlign.OUTSIDE: return FigmageStrokeAlignMirror.Outside;
                default: return FigmageStrokeAlignMirror.Center;
            }
        }

        // ---------- Effect type ----------

        public enum FigmageEffectTypeMirror { DropShadow, InnerShadow, LayerBlur, BackgroundBlur }

        public static FigmageEffectTypeMirror MapEffectType(EffectType src)
        {
            switch (src)
            {
                case EffectType.INNER_SHADOW:    return FigmageEffectTypeMirror.InnerShadow;
                case EffectType.LAYER_BLUR:      return FigmageEffectTypeMirror.LayerBlur;
                case EffectType.BACKGROUND_BLUR: return FigmageEffectTypeMirror.BackgroundBlur;
                default: return FigmageEffectTypeMirror.DropShadow;
            }
        }

        // ---------- Corner radius ----------

        /// <summary>
        /// Pick the scalar CornerRadius value Figmage expects when the four-corner array
        /// is uniform; returns 0 when corners differ (caller then fills CornerRadiuses).
        /// </summary>
        public static float UniformCornerRadius(float[] cornerRadius)
        {
            if (cornerRadius == null || cornerRadius.Length == 0) return 0f;
            float first = cornerRadius[0];
            for (int i = 1; i < cornerRadius.Length; i++)
                if (cornerRadius[i] != first) return 0f;
            return first;
        }

        /// <summary>
        /// True when corners are non-uniform → Figmage needs the per-corner list.
        /// </summary>
        public static bool HasPerCornerRadius(float[] cornerRadius)
        {
            if (cornerRadius == null || cornerRadius.Length < 2) return false;
            float first = cornerRadius[0];
            for (int i = 1; i < cornerRadius.Length; i++)
                if (cornerRadius[i] != first) return true;
            return false;
        }

        // ---------- Geometry path list ----------

        /// <summary>
        /// Strip null / empty entries from a list of SVG path strings. Returns null when
        /// nothing survives — matches Figmage's "no geometry == fall back to rect" path.
        /// </summary>
        public static List<string> NormalizeGeometryPaths(List<string> paths)
        {
            if (paths == null) return null;
            List<string> kept = null;
            foreach (var p in paths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                (kept ??= new List<string>(paths.Count)).Add(p);
            }
            return kept;
        }

        // ---------- Relative transform ----------

        /// <summary>
        /// Convert our 2D float[2,3] to a 2x3 list-of-lists matching Figmage's expected
        /// shape. Returns null when the input is null/empty.
        /// </summary>
        public static List<List<float>> RelativeTransformToLists(float[,] matrix)
        {
            if (matrix == null) return null;
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            if (rows == 0 || cols == 0) return null;
            var result = new List<List<float>>(rows);
            for (int r = 0; r < rows; r++)
            {
                var row = new List<float>(cols);
                for (int c = 0; c < cols; c++) row.Add(matrix[r, c]);
                result.Add(row);
            }
            return result;
        }

        // ---------- Stroke weights ----------

        /// <summary>
        /// Decide whether the node has non-uniform stroke weights — Figmage will read
        /// <c>IndividualStrokeWeights</c> only when this returns true.
        /// </summary>
        public static bool HasIndividualStrokeWeights(float[] weights)
        {
            if (weights == null || weights.Length < 4) return false;
            float first = weights[0];
            for (int i = 1; i < 4; i++)
                if (weights[i] != first) return true;
            return false;
        }
    }
}
