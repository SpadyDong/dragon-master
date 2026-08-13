using System;
using UnityEngine;

namespace Figma2Unity
{
    [Serializable]
    public class Effect
    {
        public EffectType Type;
        public bool Visible = true;
        public Color Color;
        public Vector2 Offset;
        public float Radius;
        public float Spread;
        public EffectBlendMode BlendMode = EffectBlendMode.NORMAL;

        // Advanced fields (mapped by FcuFigmageNodeMapper)
        public string BlurType;
        public float StartRadius;
        public Vector2? StartOffset;
        public Vector2? EndOffset;
    }
}
