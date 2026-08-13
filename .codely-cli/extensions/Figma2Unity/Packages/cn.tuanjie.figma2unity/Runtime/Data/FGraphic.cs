using System;
using System.Collections.Generic;
using UnityEngine;

namespace Figma2Unity
{
    /// <summary>
    /// Aggregate of Fill/Stroke booleans + simplified lists, used by Drawer / BakeStrategy.
    /// Computed by ComputeGraphicsStep.
    /// </summary>
    [Serializable]
    public class FGraphic
    {
        public bool HasSolidFill;
        public bool HasGradientFill;
        public bool HasImageFill;
        public bool HasStroke;
        public bool HasSolidStroke;
        public bool HasGradientStroke;

        public List<FFill> Fills = new List<FFill>();
        public List<FStroke> Strokes = new List<FStroke>();

        public bool HasMultipleFills => Fills.Count > 1;

        public bool IsSimpleSolid =>
            HasSolidFill && !HasGradientFill && !HasImageFill && !HasStroke && Fills.Count == 1;
    }
}
