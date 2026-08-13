using System;
using UnityEngine;

namespace Figma2Unity
{
    /// <summary>Simplified fill record for direct Drawer consumption.</summary>
    [Serializable]
    public class FFill
    {
        public PaintType Type;
        public Color Color;
        public GradientData Gradient;
        public float Opacity = 1f;
    }

    /// <summary>Simplified stroke record for direct Drawer consumption.</summary>
    [Serializable]
    public class FStroke
    {
        public PaintType Type;
        public Color Color;
        public float Weight;
        public StrokeAlign Align;
    }
}
