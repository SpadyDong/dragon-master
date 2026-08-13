using System;
using System.Collections.Generic;
using UnityEngine;

namespace Figma2Unity
{
    [Serializable]
    public class GradientStop
    {
        public Color Color;
        public float Position;
    }

    [Serializable]
    public class GradientData
    {
        public List<GradientStop> Stops = new List<GradientStop>();
        public float[] GradientHandlePositions;
    }

    /// <summary>Figma fill/stroke generic model.</summary>
    [Serializable]
    public class Paint
    {
        public PaintType Type;
        public bool Visible = true;
        public float Opacity = 1f;
        public Color Color;
        public GradientData Gradient;
        public string ImageRef;
        public float ScaleX = 1f;
        public float ScaleY = 1f;
        public float Rotation;
        public ImageScaleMode ScaleMode = ImageScaleMode.FILL;
        public PaintBlendMode BlendMode = PaintBlendMode.NORMAL;
    }
}
