using System.Collections.Generic;
using Newtonsoft.Json;

namespace Figma2Unity.Editor.Api
{
    // POCO mirror of Figma REST API node shape. Field set follows Design.md §3.1 v1
    // "全字段解析" requirement: parse everything even if v1 doesn't apply it, so v2 can
    // light up features without forcing full re-imports.

    public class FigmaDocumentResponse
    {
        [JsonProperty("name")]         public string Name;
        [JsonProperty("lastModified")] public string LastModified;
        [JsonProperty("version")]      public string Version;
        [JsonProperty("document")]     public FigmaApiNode Document;
        [JsonProperty("components")]   public Dictionary<string, FigmaApiComponent> Components;
    }

    public class FigmaApiComponent
    {
        [JsonProperty("key")]         public string Key;
        [JsonProperty("name")]        public string Name;
        [JsonProperty("description")] public string Description;
    }

    public class FigmaApiNode
    {
        [JsonProperty("id")]       public string Id;
        [JsonProperty("name")]     public string Name;
        [JsonProperty("type")]     public string Type;
        [JsonProperty("visible")]  public bool? Visible;
        [JsonProperty("opacity")]  public float? Opacity;
        [JsonProperty("children")] public List<FigmaApiNode> Children;

        // Geometry
        [JsonProperty("absoluteBoundingBox")] public FigmaApiRect AbsoluteBoundingBox;
        [JsonProperty("absoluteRenderBounds")] public FigmaApiRect AbsoluteRenderBounds;
        // Local (unrotated) size of the node. Unlike absoluteBoundingBox — which is the node's
        // world-space AABB *after* rotation — `size` is the node's own width/height in its local
        // frame. For a rotated node these differ (e.g. a 3x20 bar rotated 90° has size {3,20} but
        // an AABB of {20,3}); sizeDelta must use this local size when localRotation is applied,
        // otherwise the rotation is counted twice (the rotated-rectangle double-transform bug).
        [JsonProperty("size")] public FigmaApiVector Size;
        [JsonProperty("cornerRadius")]   public float? CornerRadius;
        [JsonProperty("rectangleCornerRadii")] public List<float> RectangleCornerRadii;
        [JsonProperty("cornerSmoothing")] public float? CornerSmoothing;
        [JsonProperty("constraints")] public FigmaApiConstraints Constraints;
        // Inner element is nullable: some Figma exports emit a null inside the 2x3 matrix
        // (seen on certain vector/boolean-op nodes). A non-nullable float throws
        // "converting null to System.Single" during deserialization and aborts the whole
        // document parse — nulls are defaulted to 0 in FigmaDataParser.ParseRelativeTransform.
        [JsonProperty("relativeTransform")] public List<List<float?>> RelativeTransform;
        // Node rotation in radians. Some Figma exports omit relativeTransform entirely and
        // express rotation only through this field; the parser synthesizes an equivalent
        // 2x3 matrix when relativeTransform is absent. See FigmaDataParser.ParseRelativeTransform.
        [JsonProperty("rotation")] public float? Rotation;
        [JsonProperty("fillGeometry")]   public List<FigmaApiGeometry> FillGeometry;
        [JsonProperty("strokeGeometry")] public List<FigmaApiGeometry> StrokeGeometry;

        // Style
        [JsonProperty("fills")]   public List<FigmaApiPaint> Fills;
        [JsonProperty("strokes")] public List<FigmaApiPaint> Strokes;
        [JsonProperty("strokeWeight")] public float? StrokeWeight;
        [JsonProperty("strokeAlign")]  public string StrokeAlign;
        [JsonProperty("strokeJoin")]   public string StrokeJoin;
        [JsonProperty("strokeCap")]    public string StrokeCap;
        [JsonProperty("strokeMiterLimit")] public float? StrokeMiterLimit;
        [JsonProperty("dashPattern")]  public List<float> DashPattern;
        [JsonProperty("individualStrokeWeights")] public FigmaApiStrokeWeights IndividualStrokeWeights;
        [JsonProperty("effects")] public List<FigmaApiEffect> Effects;

        // Text
        [JsonProperty("characters")] public string Characters;
        [JsonProperty("style")] public FigmaApiTextStyle Style;
        [JsonProperty("characterStyleOverrides")] public List<int> CharacterStyleOverrides;
        [JsonProperty("styleOverrideTable")] public Dictionary<string, FigmaApiTextStyle> StyleOverrideTable;

        // Layout
        [JsonProperty("layoutMode")]    public string LayoutMode;
        [JsonProperty("itemSpacing")]   public float? ItemSpacing;
        [JsonProperty("paddingLeft")]   public float? PaddingLeft;
        [JsonProperty("paddingRight")]  public float? PaddingRight;
        [JsonProperty("paddingTop")]    public float? PaddingTop;
        [JsonProperty("paddingBottom")] public float? PaddingBottom;
        [JsonProperty("primaryAxisAlignItems")] public string PrimaryAxisAlignItems;
        [JsonProperty("counterAxisAlignItems")] public string CounterAxisAlignItems;
        [JsonProperty("overflowDirection")]    public string OverflowDirection;
        [JsonProperty("layoutSizingHorizontal")] public string LayoutSizingHorizontal;
        [JsonProperty("layoutSizingVertical")]   public string LayoutSizingVertical;
        // Auto-layout child sizing hints (present on children of an auto-layout frame).
        // layoutGrow: 0 = fixed, 1 = stretch along the parent's primary axis.
        // layoutAlign: STRETCH = stretch along the parent's counter axis.
        [JsonProperty("layoutGrow")]  public float? LayoutGrow;
        [JsonProperty("layoutAlign")] public string LayoutAlign;
        // Wrap (HORIZONTAL/VERTICAL auto-layout that wraps onto new tracks) + native GRID
        // auto-layout. Both map to a UGUI GridLayoutGroup.
        [JsonProperty("layoutWrap")]        public string LayoutWrap;
        [JsonProperty("counterAxisSpacing")] public float? CounterAxisSpacing;
        [JsonProperty("gridRowCount")]      public int? GridRowCount;
        [JsonProperty("gridColumnCount")]   public int? GridColumnCount;
        [JsonProperty("gridRowGap")]        public float? GridRowGap;
        [JsonProperty("gridColumnGap")]     public float? GridColumnGap;

        // Prototype
        [JsonProperty("transitionNodeID")]   public string TransitionNodeID;
        [JsonProperty("transitionDuration")] public float? TransitionDuration;
        [JsonProperty("transitionEasing")]   public string TransitionEasing;
        [JsonProperty("flowStartingPoints")] public List<FigmaApiFlowStartingPoint> FlowStartingPoints;
        [JsonProperty("interactions")]       public List<FigmaApiInteraction> Interactions;

        // Mask + Component
        [JsonProperty("isMask")]      public bool? IsMask;
        [JsonProperty("clipsContent")] public bool? ClipsContent;
        [JsonProperty("componentId")] public string ComponentId;
        [JsonProperty("overrides")]   public List<FigmaApiInstanceOverride> Overrides;
    }

    public class FigmaApiRect
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("width")]  public float Width;
        [JsonProperty("height")] public float Height;
    }

    public class FigmaApiConstraints
    {
        [JsonProperty("horizontal")] public string Horizontal;
        [JsonProperty("vertical")]   public string Vertical;
    }

    public class FigmaApiGeometry
    {
        [JsonProperty("path")]       public string Path;
        [JsonProperty("windingRule")] public string WindingRule;
    }

    public class FigmaApiStrokeWeights
    {
        [JsonProperty("top")]    public float Top;
        [JsonProperty("right")]  public float Right;
        [JsonProperty("bottom")] public float Bottom;
        [JsonProperty("left")]   public float Left;
    }

    public class FigmaApiColor
    {
        [JsonProperty("r")] public float R;
        [JsonProperty("g")] public float G;
        [JsonProperty("b")] public float B;
        [JsonProperty("a")] public float A = 1f;
    }

    public class FigmaApiColorStop
    {
        [JsonProperty("position")] public float Position;
        [JsonProperty("color")] public FigmaApiColor Color;
    }

    public class FigmaApiVector
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
    }

    public class FigmaApiPaint
    {
        [JsonProperty("type")]    public string Type;
        [JsonProperty("visible")] public bool? Visible;
        [JsonProperty("opacity")] public float? Opacity;
        [JsonProperty("color")]   public FigmaApiColor Color;
        [JsonProperty("blendMode")] public string BlendMode;
        // Gradient
        [JsonProperty("gradientHandlePositions")] public List<FigmaApiVector> GradientHandlePositions;
        [JsonProperty("gradientStops")] public List<FigmaApiColorStop> GradientStops;
        // Image
        [JsonProperty("imageRef")] public string ImageRef;
        [JsonProperty("scaleMode")] public string ScaleMode;
        [JsonProperty("imageTransform")] public List<List<float>> ImageTransform;
        [JsonProperty("scalingFactor")] public float? ScalingFactor;
        [JsonProperty("rotation")] public float? Rotation;
    }

    public class FigmaApiEffect
    {
        [JsonProperty("type")]    public string Type;
        [JsonProperty("visible")] public bool? Visible;
        [JsonProperty("color")]   public FigmaApiColor Color;
        [JsonProperty("offset")]  public FigmaApiVector Offset;
        [JsonProperty("radius")]  public float? Radius;
        [JsonProperty("spread")]  public float? Spread;
        [JsonProperty("blendMode")] public string BlendMode;
        [JsonProperty("blurType")]   public string BlurType;
        [JsonProperty("startRadius")] public float? StartRadius;
        [JsonProperty("startOffset")] public FigmaApiVector StartOffset;
        [JsonProperty("endOffset")]   public FigmaApiVector EndOffset;
    }

    public class FigmaApiTextStyle
    {
        [JsonProperty("fontFamily")]         public string FontFamily;
        [JsonProperty("fontPostScriptName")] public string FontPostScriptName;
        [JsonProperty("fontSize")]           public float? FontSize;
        [JsonProperty("fontWeight")]         public float? FontWeight;
        [JsonProperty("italic")]             public bool? Italic;
        [JsonProperty("lineHeightPx")]       public float? LineHeightPx;
        [JsonProperty("letterSpacing")]      public float? LetterSpacing;
        [JsonProperty("textAlignHorizontal")] public string TextAlignHorizontal;
        [JsonProperty("textAlignVertical")]   public string TextAlignVertical;
        [JsonProperty("textAutoResize")]      public string TextAutoResize;
        [JsonProperty("fillStyleId")]         public int? FillStyleId;
        [JsonProperty("opacity")]             public float? Opacity;
    }

    public class FigmaApiFlowStartingPoint
    {
        [JsonProperty("nodeId")] public string NodeId;
        [JsonProperty("name")]   public string Name;
    }

    // New-style prototype interaction. v1 only consumes the navigation target —
    // destinationId on a NAVIGATE-like action — and folds it into FObject.TransitionNodeID
    // (legacy field) so downstream Drawer / FlowManager logic stays unchanged.
    public class FigmaApiInteraction
    {
        [JsonProperty("trigger")] public FigmaApiInteractionTrigger Trigger;
        [JsonProperty("actions")] public List<FigmaApiInteractionAction> Actions;
        // Some payloads expose a single "action" instead of "actions"; tolerate both.
        [JsonProperty("action")]  public FigmaApiInteractionAction Action;
    }

    public class FigmaApiInteractionTrigger
    {
        [JsonProperty("type")] public string Type; // ON_CLICK / ON_HOVER / ...
    }

    public class FigmaApiInteractionAction
    {
        [JsonProperty("type")]              public string Type; // NODE / NAVIGATE / BACK / ...
        [JsonProperty("destinationId")]     public string DestinationId;
        [JsonProperty("navigation")]        public string Navigation;
        [JsonProperty("transition")]        public FigmaApiInteractionTransition Transition;
        // Overlay placement (screen-local, from the screen's top-left) for navigation == OVERLAY.
        [JsonProperty("overlayRelativePosition")] public FigmaApiVector OverlayRelativePosition;
    }

    public class FigmaApiInteractionTransition
    {
        [JsonProperty("type")]     public string Type;     // SMART_ANIMATE / DISSOLVE / ...
        [JsonProperty("duration")] public float? Duration; // seconds (interaction-level), ms varies
        [JsonProperty("easing")]   public FigmaApiInteractionEasing Easing;
    }

    public class FigmaApiInteractionEasing
    {
        [JsonProperty("type")] public string Type;
    }

    public class FigmaApiInstanceOverride
    {
        [JsonProperty("id")]              public string Id;
        [JsonProperty("overriddenFields")] public List<string> OverriddenFields;
    }
}
