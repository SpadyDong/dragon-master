namespace Figma2Unity
{
    public enum PaintType
    {
        SOLID,
        GRADIENT_LINEAR,
        GRADIENT_RADIAL,
        GRADIENT_ANGULAR,
        GRADIENT_DIAMOND,
        IMAGE,
        EMBOSSED,
    }

    public enum ImageScaleMode
    {
        FILL,
        FIT,
        CROP,
        TILE,
    }

    public enum PaintBlendMode
    {
        NORMAL,
        MULTIPLY,
        SCREEN,
        OVERLAY,
        DARKEN,
        LIGHTEN,
        COLOR_DODGE,
        COLOR_BURN,
        HARD_LIGHT,
        SOFT_LIGHT,
        DIFFERENCE,
        EXCLUSION,
        HUE,
        SATURATION,
        COLOR,
        LUMINOSITY,
    }

    public enum EffectType
    {
        DROP_SHADOW,
        INNER_SHADOW,
        LAYER_BLUR,
        BACKGROUND_BLUR,
    }

    public enum EffectBlendMode
    {
        NORMAL,
        MULTIPLY,
        SCREEN,
        OVERLAY,
        DARKEN,
        LIGHTEN,
    }

    public enum ConstraintType
    {
        MIN,
        CENTER,
        MAX,
        SCALE,
        STRETCH,
    }

    public enum TextAlignHorizontal
    {
        LEFT,
        CENTER,
        RIGHT,
        JUSTIFIED,
    }

    public enum TextAlignVertical
    {
        TOP,
        CENTER,
        BOTTOM,
    }

    public enum TextAutoResize
    {
        NONE,
        HEIGHT,
        WIDTH_AND_HEIGHT,
    }

    public enum FontWeight
    {
        Thin = 100,
        ExtraLight = 200,
        Light = 300,
        Regular = 400,
        Medium = 500,
        SemiBold = 600,
        Bold = 700,
        ExtraBold = 800,
        Black = 900,
    }

    public enum LayoutMode
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        GRID,
    }

    /// <summary>
    /// Figma `layoutWrap` on a HORIZONTAL/VERTICAL auto-layout — whether items wrap onto new
    /// tracks. WRAP maps to a UGUI GridLayoutGroup (UGUI H/V LayoutGroups never wrap).
    /// </summary>
    public enum LayoutWrap
    {
        NO_WRAP,
        WRAP,
    }

    public enum PrimaryAxisAlignItems
    {
        MIN,
        CENTER,
        MAX,
        SPACE_BETWEEN,
    }

    public enum CounterAxisAlignItems
    {
        MIN,
        CENTER,
        MAX,
    }

    public enum OverflowDirection
    {
        NONE,
        HORIZONTAL_SCROLLING,
        VERTICAL_SCROLLING,
        HORIZONTAL_AND_VERTICAL_SCROLLING,
    }

    public enum LayoutSizing
    {
        // Names mirror Figma REST API values verbatim (FIXED / HUG / FILL) so
        // FigmaDataParser.ParseEnum can match them case-insensitively. HUG = "hug contents"
        // (auto-size to children) → UGUI ContentSizeFitter; FILL = "fill container"
        // (stretch along parent's layout axis) → UGUI LayoutElement.flexible*.
        FIXED,
        HUG,
        FILL,
    }

    /// <summary>
    /// Figma `layoutAlign` on an auto-layout child — how it aligns/sizes on the parent's
    /// COUNTER axis. STRETCH = fill the counter axis (legacy spelling of counter-axis FILL).
    /// </summary>
    public enum LayoutAlign
    {
        INHERIT,
        STRETCH,
        MIN,
        CENTER,
        MAX,
    }

    public enum StrokeAlign
    {
        INSIDE,
        OUTSIDE,
        CENTER,
    }

    public enum StrokeJoin
    {
        MITER,
        ROUND,
        BEVEL,
    }

    public enum StrokeCap
    {
        NONE,
        ROUND,
        SQUARE,
    }

    public enum EasingType
    {
        LINEAR,
        EASE_IN,
        EASE_OUT,
        EASE_IN_AND_OUT,
        EASE_IN_BACK,
        EASE_OUT_BACK,
        EASE_IN_AND_OUT_BACK,
    }

    public enum NavigationType
    {
        NAVIGATE,   // Full-screen swap (default / legacy transitionNodeID).
        OVERLAY,    // Overlay popup stacked on top, background not destroyed.
    }

    public enum ImportMode
    {
        Full,
        Incremental,
    }

    public enum PipelineState
    {
        Idle,
        Running,
        Completed,
        Failed,
    }
}
