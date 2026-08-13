using System;

namespace Figma2Unity
{
    [Serializable]
    public class TextStyle
    {
        public string FontFamily;
        public string FontPostScriptName;
        public float FontSize = 16f;
        public float LineHeight;
        public float LetterSpacing;
        public FontWeight Weight = FontWeight.Regular;
        public bool Italic;
        public TextAlignHorizontal TextAlignHorizontal = TextAlignHorizontal.LEFT;
        public TextAlignVertical TextAlignVertical = TextAlignVertical.TOP;
        public TextAutoResize TextAutoResize = TextAutoResize.NONE;
        public int FillStyleId = -1;
        public float Opacity = 1f;
    }
}
