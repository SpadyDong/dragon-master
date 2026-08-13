using System;
using System.Collections.Generic;
using UnityEngine;

namespace Figma2Unity
{
    /// <summary>
    /// Lean Figma node model. Pure data: NO references to GameObject/Sprite (those live in
    /// F2UContext.NodeGameObjectMap / NodeSpritePathMap).
    ///
    /// v1: All fields parsed (incl. RelativeTransform / Effects / AbsoluteRenderBounds /
    /// CornerRadius / ComponentId) even when not yet applied — this is the "Parse vs Apply"
    /// separation that keeps v1→v2 incremental upgrades from forcing full re-imports.
    /// </summary>
    [Serializable]
    public class FObject
    {
        // Identity
        public string Id;
        public string Name;
        public string Type;
        public bool Visible = true;
        public float Opacity = 1f;

        // Hierarchy
        [NonSerialized] public FObject Parent;
        public List<FObject> Children = new List<FObject>();

        // Geometry
        public Rect AbsoluteBoundingBox;
        public Rect AbsoluteRenderBounds;
        // Local (unrotated) size. AbsoluteBoundingBox is the post-rotation world AABB; Size is
        // the node's own width/height in its local frame. They differ for rotated nodes. When a
        // leaf's localRotation is applied, sizeDelta must use Size (not the AABB) so the rotation
        // is not double-counted. Defaults to the AABB width/height when Figma omits `size`.
        public Vector2 Size;
        public float[] CornerRadius = new float[4];
        public float CornerSmoothing;
        public Constraints Constraints;
        public float[,] RelativeTransform; // 2x3 affine
        public float Rotation;             // radians (Figma node-level rotation field)
        // True when RelativeTransform was synthesized from the node-level `rotation` field
        // (export without a relativeTransform matrix). Such matrices encode the node's GLOBAL
        // rotation and must be applied to LEAF nodes only — applying them to a container would
        // double-rotate its globally-positioned children. An explicit relativeTransform from
        // Figma is parent-relative and composes through the UGUI hierarchy normally.
        [NonSerialized] public bool RotationIsGlobalSynthesized;
        public List<string> FillGeometry;
        public List<string> StrokeGeometry;

        // Style
        public List<Paint> Fills = new List<Paint>();
        public List<Paint> Strokes = new List<Paint>();
        public float StrokeWeight;
        public StrokeAlign StrokeAlign;
        public StrokeJoin StrokeJoin;
        public StrokeCap StrokeCap;
        public float StrokeMiterLimit = 4f;
        public List<float> DashPattern;
        public float[] IndividualStrokeWeights;
        public List<Effect> Effects = new List<Effect>();

        // Text
        public string Characters;
        public TextStyle Style;
        public List<int> CharacterStyleOverrides;
        public Dictionary<string, TextStyle> StyleOverrideTable;

        // Layout
        public LayoutMode LayoutMode;
        public float ItemSpacing;
        public float PaddingLeft, PaddingRight, PaddingTop, PaddingBottom;
        public PrimaryAxisAlignItems PrimaryAxisAlignItems;
        public CounterAxisAlignItems CounterAxisAlignItems;
        public OverflowDirection OverflowDirection;
        public LayoutSizing LayoutSizingHorizontal;
        public LayoutSizing LayoutSizingVertical;
        // Auto-layout child sizing (only meaningful when Parent is an auto-layout frame).
        // LayoutGrow 1 = stretch along parent's primary axis ("fill container" on that axis);
        // LayoutAlign STRETCH = stretch along parent's counter axis. Modern exports also
        // express these via LayoutSizingHorizontal/Vertical == FILL — both are honored.
        public float LayoutGrow;
        public LayoutAlign LayoutAlign;
        // Wrap + native GRID auto-layout. When LayoutWrap == WRAP (on an H/V LayoutMode) or
        // LayoutMode == GRID, the node maps to a UGUI GridLayoutGroup instead of an
        // H/V LayoutGroup. CounterAxisSpacing is the track gap in wrap mode; GridRow/Column*
        // describe a native GRID.
        public LayoutWrap LayoutWrap;
        public float CounterAxisSpacing;
        public int GridRowCount;
        public int GridColumnCount;
        public float GridRowGap;
        public float GridColumnGap;

        // Prototype
        public string TransitionNodeID;
        public float TransitionDuration;
        public EasingType TransitionEasing;
        public NavigationType NavigationType;   // NAVIGATE (default) or OVERLAY popup.
        public bool IsCloseOverlay;             // Action type is CLOSE (no destination).
        // Overlay placement (screen-local px from the source screen's top-left). Only
        // meaningful when NavigationType == OVERLAY; HasOverlayPosition gates its use.
        public bool HasOverlayPosition;
        public Vector2 OverlayPosition;
        public List<FlowStartingPoint> FlowStartingPoints;

        // Component instance (identity only — no property inheritance, see Design.md §3.3)
        public bool IsComponentInstance;
        public string ComponentId;
        public List<InstanceOverride> Overrides;

        // FCU extensions
        public List<FcuTag> Tags = new List<FcuTag>();
        public FGraphic Graphic;
        public FObjectHashData HashData;
        public string FolderName;
        public string FileName;
        public bool IsMask;
        // Figma Frame "clip content" flag — when true, children visually clipped to this
        // frame's bounds. UGUI side maps it to RectMask2D (CreateGameObjectsStep).
        public bool ClipsContent;
    }
}
