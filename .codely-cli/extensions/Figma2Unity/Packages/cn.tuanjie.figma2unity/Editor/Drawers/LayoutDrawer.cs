using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// v1: H/V LayoutGroup with padding, spacing, alignment.
    /// v2: ScrollRect (Viewport + Content child construction).
    /// v3: GridLayoutGroup for native GRID auto-layout and wrapping H/V auto-layout.
    /// </summary>
    public class LayoutDrawer
    {
        public virtual void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            // GRID auto-layout, or a wrapping H/V auto-layout, can't be expressed by a UGUI
            // H/V LayoutGroup (those never wrap) — route to a GridLayoutGroup instead.
            if (UsesGrid(fobj))
            {
                DrawGrid(fobj, ctx, go);
                return;
            }

            RemoveLayoutGroupIfMismatch(go, fobj.LayoutMode);

            HorizontalOrVerticalLayoutGroup layout = null;
            switch (fobj.LayoutMode)
            {
                case LayoutMode.HORIZONTAL: layout = go.EnsureComponent<HorizontalLayoutGroup>(); break;
                case LayoutMode.VERTICAL:   layout = go.EnsureComponent<VerticalLayoutGroup>(); break;
            }
            if (layout == null) return;

            layout.padding = new RectOffset(
                Mathf.RoundToInt(fobj.PaddingLeft),
                Mathf.RoundToInt(fobj.PaddingRight),
                Mathf.RoundToInt(fobj.PaddingTop),
                Mathf.RoundToInt(fobj.PaddingBottom));
            layout.spacing = fobj.ItemSpacing;
            layout.childAlignment = ConvertAlignment(fobj);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth =
                fobj.PrimaryAxisAlignItems == PrimaryAxisAlignItems.SPACE_BETWEEN
                && fobj.LayoutMode == LayoutMode.HORIZONTAL;
            layout.childForceExpandHeight =
                fobj.PrimaryAxisAlignItems == PrimaryAxisAlignItems.SPACE_BETWEEN
                && fobj.LayoutMode == LayoutMode.VERTICAL;

            // childControlWidth/Height drive child sizing, which discards the precise sizeDelta
            // CreateGameObjectsStep set. Preserve each child's Figma size by writing a
            // LayoutElement with preferred width/height (scaled like the rest of the layout).
            ApplyChildPreferredSizes(fobj, ctx);
        }

        /// <summary>
        /// Add ContentSizeFitter when Figma layoutSizing is HUG (hug contents).
        /// Horizontal/vertical fit mode follows the corresponding LayoutSizing axis.
        /// </summary>
        public virtual void DrawFit(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;
            var csf = go.EnsureComponent<ContentSizeFitter>();
            csf.horizontalFit = fobj.LayoutSizingHorizontal == LayoutSizing.HUG
                ? ContentSizeFitter.FitMode.PreferredSize
                : ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = fobj.LayoutSizingVertical == LayoutSizing.HUG
                ? ContentSizeFitter.FitMode.PreferredSize
                : ContentSizeFitter.FitMode.Unconstrained;
        }

        private static void ApplyChildPreferredSizes(FObject fobj, F2UContext ctx)
        {
            if (fobj.Children == null) return;
            // Child preferred sizes live in the same 1:1 design-pixel space as the
            // RectTransform geometry (CreateGameObjectsStep locks the converter to
            // scale=1). Folding in config.SpriteScale here double-sized AutoLayout slots
            // relative to their parent and broke layout.
            foreach (var child in fobj.Children)
            {
                if (child == null) continue;
                if (child.Tags != null && child.Tags.Contains(FcuTag.Ignore)) continue;
                var cgo = ctx.GetGameObject(child);
                if (cgo == null) continue;
                var le = cgo.EnsureComponent<LayoutElement>();

                // Figma "fill container" → UGUI flexible size. A flexible child grows to
                // absorb the parent's leftover space along that axis; a fixed child keeps its
                // design-pixel preferred size. Resolve each axis independently so a node can be
                // FILL on one axis and FIXED on the other.
                bool fillsH = FillsHorizontal(child, fobj.LayoutMode);
                bool fillsV = FillsVertical(child, fobj.LayoutMode);

                if (fillsH)
                {
                    le.flexibleWidth = 1f;
                    le.preferredWidth = -1f; // let flexible drive width
                }
                else
                {
                    le.flexibleWidth = 0f;
                    le.preferredWidth = child.AbsoluteBoundingBox.width;
                }

                if (fillsV)
                {
                    le.flexibleHeight = 1f;
                    le.preferredHeight = -1f;
                }
                else
                {
                    le.flexibleHeight = 0f;
                    le.preferredHeight = child.AbsoluteBoundingBox.height;
                }
            }
        }

        /// <summary>
        /// True when the auto-layout child should stretch on the HORIZONTAL axis. Honors the
        /// modern <c>layoutSizingHorizontal == FILL</c> as well as the legacy spellings:
        /// <c>layoutGrow == 1</c> on a HORIZONTAL parent (primary axis) and
        /// <c>layoutAlign == STRETCH</c> on a VERTICAL parent (counter axis).
        /// </summary>
        private static bool FillsHorizontal(FObject child, LayoutMode parentMode)
        {
            if (child.LayoutSizingHorizontal == LayoutSizing.FILL) return true;
            if (parentMode == LayoutMode.HORIZONTAL && child.LayoutGrow >= 1f) return true;
            if (parentMode == LayoutMode.VERTICAL && child.LayoutAlign == LayoutAlign.STRETCH) return true;
            return false;
        }

        /// <summary>
        /// True when the auto-layout child should stretch on the VERTICAL axis. Mirror of
        /// <see cref="FillsHorizontal"/> with the axes swapped.
        /// </summary>
        private static bool FillsVertical(FObject child, LayoutMode parentMode)
        {
            if (child.LayoutSizingVertical == LayoutSizing.FILL) return true;
            if (parentMode == LayoutMode.VERTICAL && child.LayoutGrow >= 1f) return true;
            if (parentMode == LayoutMode.HORIZONTAL && child.LayoutAlign == LayoutAlign.STRETCH) return true;
            return false;
        }

        // Names of the helper children DrawScroll builds. Kept as constants so the reparent
        // snapshot and StripRemovedTagComponents agree on what is structural vs. real content.
        public const string ViewportName = "F2U_Viewport";
        public const string ContentName = "F2U_Content";

        public virtual void DrawScroll(FObject fobj, F2UContext ctx)
        {
            // Assemble a real, scrollable UGUI ScrollRect on the tagged node:
            //   <fobj> + ScrollRect
            //     └─ F2U_Viewport (RectMask2D + transparent Image stencil, anchor-stretch)
            //          └─ F2U_Content (scroll-axis anchored; holds the node's real children)
            //
            // CreateGameObjectsStep built the node's children as DIRECT children of <fobj>.
            // To make the ScrollRect actually scroll we physically reparent them under
            // F2U_Content and size the content along the scroll axis. Two layout cases:
            //   • Auto-layout (LayoutMode H/V/GRID): the LayoutGroup + ContentSizeFitter that
            //     ran earlier on <fobj> (Tag priority puts AutoLayoutGroup/ContentSizeFitter
            //     before ScrollView) are MOVED onto F2U_Content, which then drives child
            //     arrangement and grows along the scroll axis.
            //   • Free layout (absolute coords): F2U_Content is sized to the children's union
            //     bounds and each child is re-anchored to the content's top-left frame.
            // Re-running is idempotent: a second pass finds only F2U_Viewport under <fobj>
            // (nothing left to reparent) and re-applies the same component/sizing state.
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            var scrollRect = go.EnsureComponent<ScrollRect>();
            bool horizontal = fobj.OverflowDirection == OverflowDirection.HORIZONTAL_SCROLLING
                              || fobj.OverflowDirection == OverflowDirection.HORIZONTAL_AND_VERTICAL_SCROLLING;
            bool vertical = fobj.OverflowDirection == OverflowDirection.VERTICAL_SCROLLING
                            || fobj.OverflowDirection == OverflowDirection.HORIZONTAL_AND_VERTICAL_SCROLLING;
            scrollRect.horizontal = horizontal;
            scrollRect.vertical = vertical;
            // Clamped (not the UGUI default Elastic): a map/content pan must stop at the
            // content edge and STAY there. Elastic rubber-bands back to the bound on release,
            // which reads as the view "snapping back" after a drag.
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            // Inertia OFF: with inertia the view keeps gliding (decelerating) after the pointer
            // is released, which reads as a small unwanted drift once the mouse button is up.
            // Disabling it makes the content stop exactly where the drag ended.
            scrollRect.inertia = false;

            float viewportW = fobj.AbsoluteBoundingBox.width;
            float viewportH = fobj.AbsoluteBoundingBox.height;

            // Viewport — created lazily, marked with RectMask2D so children clip to bounds.
            var viewportTr = go.transform.Find(ViewportName) as RectTransform;
            if (viewportTr == null)
            {
                var viewportGO = new GameObject(ViewportName, typeof(RectTransform));
                viewportTr = viewportGO.GetComponent<RectTransform>();
                viewportTr.SetParent(go.transform, false);
                viewportTr.anchorMin = Vector2.zero;
                viewportTr.anchorMax = Vector2.one;
                viewportTr.offsetMin = Vector2.zero;
                viewportTr.offsetMax = Vector2.zero;
                viewportTr.pivot = new Vector2(0.5f, 0.5f);
                viewportGO.AddComponent<RectMask2D>();
            }
            scrollRect.viewport = viewportTr;

            // The viewport carries a transparent Image whose raycastTarget MUST be true: a UGUI
            // ScrollRect only receives drag/scroll events that hit a raycastable Graphic inside
            // it. Imported children all have raycastTarget=false (ImageDrawer), and the content
            // node has no Graphic, so without this the ScrollRect would never see a pointer and
            // dragging/panning would silently do nothing. Set every pass so an existing viewport
            // from an older import (raycastTarget=false) is corrected on re-sync. Idempotent.
            var viewportImg = viewportTr.gameObject.EnsureComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0f);
            viewportImg.raycastTarget = true;

            // Content — holds the real children. Created under the viewport once, reused after.
            var contentTr = viewportTr.Find(ContentName) as RectTransform;
            if (contentTr == null)
            {
                var contentGO = new GameObject(ContentName, typeof(RectTransform));
                contentTr = contentGO.GetComponent<RectTransform>();
                contentTr.SetParent(viewportTr, false);
            }
            scrollRect.content = contentTr;

            // Anchor the content frame per scroll axis (top-left origin for the common cases).
            ApplyContentAnchors(contentTr, horizontal, vertical);

            // Physically move <fobj>'s real children under content. Skip the structural
            // viewport (and any stray content sibling from older imports).
            ReparentRealChildren(go.transform, viewportTr, contentTr);

            bool isAutoLayout = fobj.LayoutMode == LayoutMode.HORIZONTAL
                                || fobj.LayoutMode == LayoutMode.VERTICAL
                                || fobj.LayoutMode == LayoutMode.GRID;

            if (isAutoLayout)
            {
                // Move the layout components that ran on <fobj> onto content, then make the
                // content grow along the scroll axis via ContentSizeFitter.
                RelocateLayoutTo(go, contentTr.gameObject);
                ApplyContentFitter(contentTr.gameObject, horizontal, vertical);
                // Seed a sensible size; the fitter/layout recomputes at runtime.
                contentTr.sizeDelta = new Vector2(
                    horizontal ? viewportW : 0f,
                    vertical ? viewportH : 0f);
            }
            else
            {
                // Free layout: size content to the children's union bounds and re-anchor each
                // child into the content's top-left coordinate frame.
                var union = ComputeChildrenUnionLocal(fobj);
                float contentW = horizontal ? Mathf.Max(viewportW, union.x) : viewportW;
                float contentH = vertical ? Mathf.Max(viewportH, union.y) : viewportH;
                contentTr.sizeDelta = new Vector2(contentW, contentH);

                if (fobj.Children != null)
                {
                    foreach (var child in fobj.Children)
                    {
                        if (child == null) continue;
                        if (child.Tags != null && child.Tags.Contains(FcuTag.Ignore)) continue;
                        var cgo = ctx.GetGameObject(child);
                        if (cgo == null) continue;
                        ReanchorFreeChild(cgo.GetComponent<RectTransform>(), child, fobj);
                    }
                }
            }
        }

        /// <summary>
        /// Anchor the F2U_Content frame to the scroll origin. Vertical scroll → top-stretch
        /// (grows down); horizontal scroll → left-stretch (grows right); both → top-left
        /// corner anchor (grows in both axes).
        /// </summary>
        private static void ApplyContentAnchors(RectTransform contentTr, bool horizontal, bool vertical)
        {
            if (horizontal && vertical)
            {
                contentTr.anchorMin = new Vector2(0f, 1f);
                contentTr.anchorMax = new Vector2(0f, 1f);
                contentTr.pivot = new Vector2(0f, 1f);
            }
            else if (horizontal)
            {
                contentTr.anchorMin = new Vector2(0f, 0f);
                contentTr.anchorMax = new Vector2(0f, 1f);
                contentTr.pivot = new Vector2(0f, 0.5f);
            }
            else // vertical (default)
            {
                contentTr.anchorMin = new Vector2(0f, 1f);
                contentTr.anchorMax = new Vector2(1f, 1f);
                contentTr.pivot = new Vector2(0.5f, 1f);
            }
            contentTr.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Reparent every direct child of <paramref name="parent"/> into
        /// <paramref name="content"/>, except the structural viewport (and any stray content
        /// node from an older import). Idempotent: on a second pass only the viewport remains
        /// under the parent, so nothing moves.
        /// </summary>
        private static void ReparentRealChildren(Transform parent, RectTransform viewport, RectTransform content)
        {
            var movers = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c == viewport) continue;
                if (c == content) continue;
                if (c != null && c.gameObject != null
                    && (c.gameObject.name == ViewportName || c.gameObject.name == ContentName))
                    continue;
                movers.Add(c);
            }
            foreach (var m in movers)
                m.SetParent(content, false);
        }

        /// <summary>
        /// Move the auto-layout components (H/V/Grid LayoutGroup + ContentSizeFitter) from
        /// <paramref name="from"/> onto <paramref name="to"/>. Unity can't move a component
        /// instance, so we copy fields onto an EnsureComponent'd twin and destroy the source.
        /// Idempotent: when <paramref name="from"/> has none left, this is a no-op.
        /// </summary>
        private static void RelocateLayoutTo(GameObject from, GameObject to)
        {
            if (from == null || to == null || from == to) return;

            var h = from.GetComponent<HorizontalLayoutGroup>();
            if (h != null)
            {
                CopyHvLayout(h, to.EnsureComponent<HorizontalLayoutGroup>());
                UnityEngine.Object.DestroyImmediate(h);
            }
            var v = from.GetComponent<VerticalLayoutGroup>();
            if (v != null)
            {
                CopyHvLayout(v, to.EnsureComponent<VerticalLayoutGroup>());
                UnityEngine.Object.DestroyImmediate(v);
            }
            var g = from.GetComponent<GridLayoutGroup>();
            if (g != null)
            {
                var dst = to.EnsureComponent<GridLayoutGroup>();
                dst.padding = new RectOffset(g.padding.left, g.padding.right, g.padding.top, g.padding.bottom);
                dst.childAlignment = g.childAlignment;
                dst.cellSize = g.cellSize;
                dst.spacing = g.spacing;
                dst.constraint = g.constraint;
                dst.constraintCount = g.constraintCount;
                UnityEngine.Object.DestroyImmediate(g);
            }
            var csf = from.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                var dst = to.EnsureComponent<ContentSizeFitter>();
                dst.horizontalFit = csf.horizontalFit;
                dst.verticalFit = csf.verticalFit;
                UnityEngine.Object.DestroyImmediate(csf);
            }
        }

        private static void CopyHvLayout(HorizontalOrVerticalLayoutGroup src, HorizontalOrVerticalLayoutGroup dst)
        {
            dst.padding = new RectOffset(src.padding.left, src.padding.right, src.padding.top, src.padding.bottom);
            dst.spacing = src.spacing;
            dst.childAlignment = src.childAlignment;
            dst.childControlWidth = src.childControlWidth;
            dst.childControlHeight = src.childControlHeight;
            dst.childForceExpandWidth = src.childForceExpandWidth;
            dst.childForceExpandHeight = src.childForceExpandHeight;
            dst.childScaleWidth = src.childScaleWidth;
            dst.childScaleHeight = src.childScaleHeight;
        }

        /// <summary>
        /// Ensure the content has a ContentSizeFitter set to PreferredSize on the scroll
        /// axis (so it grows with its children) and Unconstrained on the fixed axis (which
        /// the stretch anchors size to the viewport). Idempotent.
        /// </summary>
        private static void ApplyContentFitter(GameObject content, bool horizontal, bool vertical)
        {
            var csf = content.EnsureComponent<ContentSizeFitter>();
            csf.horizontalFit = horizontal
                ? ContentSizeFitter.FitMode.PreferredSize
                : ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = vertical
                ? ContentSizeFitter.FitMode.PreferredSize
                : ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>
        /// Union extent (width, height) of the node's children in the node's local design-pixel
        /// frame (top-left origin). Each child contributes (relX + w, relY + h); the max over
        /// all children is how far content must extend to hold every child.
        /// </summary>
        private static Vector2 ComputeChildrenUnionLocal(FObject fobj)
        {
            float maxRight = 0f, maxBottom = 0f;
            if (fobj.Children != null)
            {
                var pb = fobj.AbsoluteBoundingBox;
                foreach (var child in fobj.Children)
                {
                    if (child == null) continue;
                    if (child.Tags != null && child.Tags.Contains(FcuTag.Ignore)) continue;
                    var cb = child.AbsoluteBoundingBox;
                    float relX = cb.x - pb.x;
                    float relY = cb.y - pb.y;
                    maxRight = Mathf.Max(maxRight, relX + cb.width);
                    maxBottom = Mathf.Max(maxBottom, relY + cb.height);
                }
            }
            return new Vector2(maxRight, maxBottom);
        }

        /// <summary>
        /// Re-anchor a free-layout child into the content's top-left frame. Content uses a
        /// top-anchored pivot, so children pin to anchorMin=anchorMax=(0,1) with a center
        /// pivot and an anchoredPosition computed from the child's local top-left offset.
        /// </summary>
        private static void ReanchorFreeChild(RectTransform rt, FObject child, FObject parent)
        {
            if (rt == null) return;
            var pb = parent.AbsoluteBoundingBox;
            var cb = child.AbsoluteBoundingBox;
            float relX = cb.x - pb.x;
            float relY = cb.y - pb.y;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(cb.width, cb.height);
            // UGUI Y is up; child top-left at (relX, relY) from content's top-left → center
            // x = relX + w/2, center y = -(relY + h/2) relative to the (0,1) anchor.
            rt.anchoredPosition = new Vector2(relX + cb.width * 0.5f, -(relY + cb.height * 0.5f));
        }

        /// <summary>
        /// Undo DrawScroll's structure: move the real children out of F2U_Viewport/F2U_Content
        /// back onto <paramref name="go"/>, then destroy the empty viewport/content helpers.
        /// Called by DrawerCoordinator.StripRemovedTagComponents when the ScrollView tag was
        /// removed on a re-sync. Order matters — children must be rescued BEFORE the content
        /// node is destroyed, otherwise DestroyImmediate(content) would take them with it.
        /// </summary>
        public static void RescueScrollChildrenAndStrip(GameObject go)
        {
            if (go == null) return;
            var viewport = go.transform.Find(ViewportName);
            if (viewport != null)
            {
                var content = viewport.Find(ContentName);
                if (content != null)
                {
                    // Snapshot first: SetParent mutates the child list mid-iteration.
                    var movers = new System.Collections.Generic.List<Transform>();
                    for (int i = 0; i < content.childCount; i++)
                        movers.Add(content.GetChild(i));
                    foreach (var m in movers)
                        m.SetParent(go.transform, false);
                }
                UnityEngine.Object.DestroyImmediate(viewport.gameObject);
            }
            var strayContent = go.transform.Find(ContentName);
            if (strayContent != null) UnityEngine.Object.DestroyImmediate(strayContent.gameObject);
        }

        /// <summary>
        /// True when the node maps to a UGUI GridLayoutGroup rather than an H/V LayoutGroup:
        /// either a native Figma GRID auto-layout, or a HORIZONTAL/VERTICAL auto-layout with
        /// layoutWrap == WRAP (UGUI H/V groups can't wrap).
        /// </summary>
        private static bool UsesGrid(FObject fobj)
            => fobj.LayoutMode == LayoutMode.GRID
               || ((fobj.LayoutMode == LayoutMode.HORIZONTAL || fobj.LayoutMode == LayoutMode.VERTICAL)
                   && fobj.LayoutWrap == LayoutWrap.WRAP);

        /// <summary>
        /// Configure a GridLayoutGroup for GRID / wrapping auto-layout. cellSize is derived
        /// from the largest child (UGUI grids use a uniform cell); spacing maps from the
        /// Figma row/column gaps; constraint + count map from the explicit grid dimensions
        /// (native GRID) or are left flexible (wrap, where Figma reflows by available width).
        /// </summary>
        private void DrawGrid(FObject fobj, F2UContext ctx, GameObject go)
        {
            RemoveLayoutGroupIfMismatch(go, fobj.LayoutMode);
            // GridLayoutGroup is mutually exclusive with H/V groups — strip any stale ones.
            var staleH = go.GetComponent<HorizontalLayoutGroup>();
            if (staleH != null) UnityEngine.Object.DestroyImmediate(staleH);
            var staleV = go.GetComponent<VerticalLayoutGroup>();
            if (staleV != null) UnityEngine.Object.DestroyImmediate(staleV);

            var grid = go.EnsureComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(
                Mathf.RoundToInt(fobj.PaddingLeft),
                Mathf.RoundToInt(fobj.PaddingRight),
                Mathf.RoundToInt(fobj.PaddingTop),
                Mathf.RoundToInt(fobj.PaddingBottom));
            grid.childAlignment = ConvertAlignment(fobj);

            // Spacing: native GRID uses gridColumnGap/gridRowGap; wrap mode uses itemSpacing
            // along the primary axis + counterAxisSpacing across tracks.
            if (fobj.LayoutMode == LayoutMode.GRID)
            {
                grid.spacing = new Vector2(fobj.GridColumnGap, fobj.GridRowGap);
            }
            else if (fobj.LayoutMode == LayoutMode.HORIZONTAL)
            {
                grid.spacing = new Vector2(fobj.ItemSpacing, fobj.CounterAxisSpacing);
            }
            else // VERTICAL wrap: items stack down a track, tracks march across
            {
                grid.spacing = new Vector2(fobj.CounterAxisSpacing, fobj.ItemSpacing);
            }

            grid.cellSize = LargestChildSize(fobj);

            // Fixed column/row count only for native GRID with explicit dimensions; wrap mode
            // stays Flexible so UGUI reflows to the container width like Figma does.
            if (fobj.LayoutMode == LayoutMode.GRID && fobj.GridColumnCount > 0)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = fobj.GridColumnCount;
            }
            else if (fobj.LayoutMode == LayoutMode.GRID && fobj.GridRowCount > 0)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                grid.constraintCount = fobj.GridRowCount;
            }
            else
            {
                grid.constraint = GridLayoutGroup.Constraint.Flexible;
            }
        }

        /// <summary>
        /// UGUI GridLayoutGroup uses one uniform cell size, so pick the largest child box
        /// (in 1:1 design pixels, matching ApplyChildPreferredSizes). Falls back to the
        /// node's own box when it has no eligible children.
        /// </summary>
        private static Vector2 LargestChildSize(FObject fobj)
        {
            float w = 0f, h = 0f;
            if (fobj.Children != null)
            {
                foreach (var child in fobj.Children)
                {
                    if (child == null) continue;
                    if (child.Tags != null && child.Tags.Contains(FcuTag.Ignore)) continue;
                    w = Mathf.Max(w, child.AbsoluteBoundingBox.width);
                    h = Mathf.Max(h, child.AbsoluteBoundingBox.height);
                }
            }
            if (w <= 0f) w = fobj.AbsoluteBoundingBox.width;
            if (h <= 0f) h = fobj.AbsoluteBoundingBox.height;
            return new Vector2(w, h);
        }

        private static void RemoveLayoutGroupIfMismatch(GameObject go, LayoutMode target)
        {
            var existingH = go.GetComponent<HorizontalLayoutGroup>();
            var existingV = go.GetComponent<VerticalLayoutGroup>();
            switch (target)
            {
                case LayoutMode.HORIZONTAL:
                    if (existingV != null) UnityEngine.Object.DestroyImmediate(existingV);
                    break;
                case LayoutMode.VERTICAL:
                    if (existingH != null) UnityEngine.Object.DestroyImmediate(existingH);
                    break;
                default:
                    if (existingH != null) UnityEngine.Object.DestroyImmediate(existingH);
                    if (existingV != null) UnityEngine.Object.DestroyImmediate(existingV);
                    break;
            }
        }

        private static TextAnchor ConvertAlignment(FObject fobj)
        {
            // Map Figma's PrimaryAxis × CounterAxis to UGUI childAlignment.
            bool horizontal = fobj.LayoutMode == LayoutMode.HORIZONTAL;
            var p = fobj.PrimaryAxisAlignItems;
            var c = fobj.CounterAxisAlignItems;

            int row, col; // row 0=top, 1=middle, 2=bottom; col 0=left, 1=center, 2=right
            if (horizontal)
            {
                col = p switch
                {
                    PrimaryAxisAlignItems.MIN => 0,
                    PrimaryAxisAlignItems.CENTER => 1,
                    PrimaryAxisAlignItems.MAX => 2,
                    _ => 0,
                };
                row = c switch
                {
                    CounterAxisAlignItems.MIN => 0,
                    CounterAxisAlignItems.CENTER => 1,
                    CounterAxisAlignItems.MAX => 2,
                    _ => 0,
                };
            }
            else
            {
                row = p switch
                {
                    PrimaryAxisAlignItems.MIN => 0,
                    PrimaryAxisAlignItems.CENTER => 1,
                    PrimaryAxisAlignItems.MAX => 2,
                    _ => 0,
                };
                col = c switch
                {
                    CounterAxisAlignItems.MIN => 0,
                    CounterAxisAlignItems.CENTER => 1,
                    CounterAxisAlignItems.MAX => 2,
                    _ => 0,
                };
            }
            return (row * 3 + col) switch
            {
                0 => TextAnchor.UpperLeft,
                1 => TextAnchor.UpperCenter,
                2 => TextAnchor.UpperRight,
                3 => TextAnchor.MiddleLeft,
                4 => TextAnchor.MiddleCenter,
                5 => TextAnchor.MiddleRight,
                6 => TextAnchor.LowerLeft,
                7 => TextAnchor.LowerCenter,
                8 => TextAnchor.LowerRight,
                _ => TextAnchor.UpperLeft,
            };
        }
    }
}
