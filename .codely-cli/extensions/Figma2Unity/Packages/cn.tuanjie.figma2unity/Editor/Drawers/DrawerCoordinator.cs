using System.Collections.Generic;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// Per-node Drawer dispatch with mutex filtering + Tag priority.
    /// v1 must match v2 exactly — this is an architecture contract (Design.md §四
    /// "Drawer 互斥规则" / "Drawer 幂等性"). New "outer-loop-by-node" architecture
    /// (vs FCU's "outer-loop-by-Tag") requires explicit ApplyTagExclusions because
    /// multiple Image-family tags on the same node would otherwise serialize-conflict
    /// inside the single Image component.
    /// </summary>
    public class DrawerCoordinator
    {
        // Only Tags that dispatch to a Drawer. Marker-only Tags (Page/FlowSection/Screen/
        // Container/Frame/Ignore/Placeholder) are excluded — otherwise the DrawByTag switch
        // would silently fail on them.
        public static readonly FcuTag[] TagPriority = new[]
        {
            FcuTag.AutoLayoutGroup, FcuTag.ContentSizeFitter,
            FcuTag.Image, FcuTag.Text, FcuTag.Slice9, FcuTag.AutoSlice9,
            FcuTag.Button, FcuTag.Toggle, FcuTag.InputField, FcuTag.PasswordField,
            FcuTag.ScrollView, FcuTag.Shadow, FcuTag.Mask, FcuTag.CanvasGroup,
        };

        protected ImageDrawer _imageDrawer;
        protected TextDrawer _textDrawer;
        protected LayoutDrawer _layoutDrawer;
        protected ButtonDrawer _buttonDrawer;
        protected ToggleDrawer _toggleDrawer;
        protected InputFieldDrawer _inputFieldDrawer;
        protected ShadowDrawer _shadowDrawer;
        protected MaskDrawer _maskDrawer;
        protected CanvasGroupDrawer _canvasGroupDrawer;

        public DrawerCoordinator()
        {
            _imageDrawer = new ImageDrawer();
            _textDrawer = new TextDrawer();
            _layoutDrawer = new LayoutDrawer();
            _buttonDrawer = new ButtonDrawer();
            _toggleDrawer = new ToggleDrawer();
            _inputFieldDrawer = new InputFieldDrawer();
            _shadowDrawer = new ShadowDrawer();
            _maskDrawer = new MaskDrawer();
            _canvasGroupDrawer = new CanvasGroupDrawer();
        }

        public virtual void DrawAll(F2UContext ctx)
        {
            if (ctx?.AllNodes == null) return;
            foreach (var fobj in ctx.AllNodes)
            {
                if (fobj == null || fobj.Tags == null) continue;
                if (fobj.Tags.Contains(FcuTag.Ignore)) continue;
                if (ctx.GetGameObject(fobj) == null) continue;

                ApplyTagExclusions(fobj);

                foreach (var tag in TagPriority)
                {
                    if (fobj.Tags.Contains(tag))
                        DrawByTag(fobj, tag, ctx);
                }
            }
        }

        /// <summary>Drop conflicting Tags: Image/Slice9/AutoSlice9 three-way, InputField/PasswordField two-way.</summary>
        public static void ApplyTagExclusions(FObject fobj)
        {
            if (fobj?.Tags == null) return;
            if (fobj.Tags.Contains(FcuTag.AutoSlice9))
            {
                fobj.Tags.Remove(FcuTag.Slice9);
                fobj.Tags.Remove(FcuTag.Image);
            }
            else if (fobj.Tags.Contains(FcuTag.Slice9))
            {
                fobj.Tags.Remove(FcuTag.Image);
            }
            if (fobj.Tags.Contains(FcuTag.PasswordField))
                fobj.Tags.Remove(FcuTag.InputField);
        }

        /// <summary>
        /// Strip Drawer-owned components for tags that existed in <paramref name="previous"/>
        /// but no longer in <paramref name="current"/>. Called by SyncService.RedrawNode so
        /// stale Shadow/Button/Toggle/etc. don't survive a Modified node whose semantics
        /// changed (e.g. designer removed an effect / renamed off "#button").
        ///
        /// Image-family (Image/Slice9/AutoSlice9) is collapsed: if ANY image-family tag is
        /// still present the Image component stays. AutoLayoutGroup removes whichever H/V
        /// LayoutGroup variant is currently attached. Marker-only tags (Page/FlowSection/
        /// Screen/Container/Frame/Ignore/ContentSizeFitter) own no component → skipped.
        /// </summary>
        public static void StripRemovedTagComponents(UnityEngine.GameObject go,
            System.Collections.Generic.IList<FcuTag> previous,
            System.Collections.Generic.IList<FcuTag> current)
        {
            if (go == null || previous == null) return;
            var now = current != null
                ? new System.Collections.Generic.HashSet<FcuTag>(current)
                : new System.Collections.Generic.HashSet<FcuTag>();

            bool imageGone = (previous.Contains(FcuTag.Image)
                              || previous.Contains(FcuTag.Slice9)
                              || previous.Contains(FcuTag.AutoSlice9))
                          && !(now.Contains(FcuTag.Image)
                              || now.Contains(FcuTag.Slice9)
                              || now.Contains(FcuTag.AutoSlice9));
            if (imageGone)
            {
                RemoveIfExists<UnityEngine.UI.Image>(go);
                // ImageDrawer.TryAddCornerRounder may have attached F2UCornerRounder to
                // ELLIPSE / rounded-corner nodes. Mirror that removal here so a dropped
                // image-family tag leaves no orphaned mask component behind.
                RemoveIfExists<Figma2Unity.F2UCornerRounder>(go);
            }

            bool layoutGone = previous.Contains(FcuTag.AutoLayoutGroup)
                              && !now.Contains(FcuTag.AutoLayoutGroup);
            if (layoutGone)
            {
                RemoveIfExists<UnityEngine.UI.HorizontalLayoutGroup>(go);
                RemoveIfExists<UnityEngine.UI.VerticalLayoutGroup>(go);
                RemoveIfExists<UnityEngine.UI.GridLayoutGroup>(go);
            }

            bool fitGone = previous.Contains(FcuTag.ContentSizeFitter)
                           && !now.Contains(FcuTag.ContentSizeFitter);
            if (fitGone) RemoveIfExists<UnityEngine.UI.ContentSizeFitter>(go);

            foreach (var tag in previous)
            {
                if (now.Contains(tag)) continue;
                switch (tag)
                {
                    case FcuTag.Text: RemoveIfExists<TMPro.TextMeshProUGUI>(go); break;
                    case FcuTag.Button: RemoveIfExists<UnityEngine.UI.Button>(go); break;
                    case FcuTag.Toggle: RemoveIfExists<UnityEngine.UI.Toggle>(go); break;
                    case FcuTag.InputField:
                    case FcuTag.PasswordField:
                        // Don't double-remove if the other variant is still present.
                        if (!now.Contains(FcuTag.InputField) && !now.Contains(FcuTag.PasswordField))
                            RemoveIfExists<TMPro.TMP_InputField>(go);
                        break;
                    case FcuTag.ScrollView:
                        RemoveIfExists<UnityEngine.UI.ScrollRect>(go);
                        // LayoutDrawer.DrawScroll reparented the node's real children into
                        // F2U_Viewport/F2U_Content. Rescue them back onto <go> BEFORE deleting
                        // the helper nodes — otherwise destroying F2U_Content would take the
                        // real content with it. The structural knowledge lives in LayoutDrawer.
                        LayoutDrawer.RescueScrollChildrenAndStrip(go);
                        break;
                    case FcuTag.Shadow: RemoveIfExists<UnityEngine.UI.Shadow>(go); break;
                    case FcuTag.Mask: RemoveIfExists<UnityEngine.UI.Mask>(go); break;
                    case FcuTag.CanvasGroup: RemoveIfExists<UnityEngine.CanvasGroup>(go); break;
                }
            }
        }

        private static void RemoveIfExists<T>(UnityEngine.GameObject go) where T : UnityEngine.Component
        {
            if (go == null) return;
            var c = go.GetComponent<T>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }

        private static void RemoveIfExists(UnityEngine.GameObject go, string assemblyQualifiedTypeName)
        {
            if (go == null || string.IsNullOrEmpty(assemblyQualifiedTypeName)) return;
            var t = System.Type.GetType(assemblyQualifiedTypeName);
            if (t == null) return; // optional package not present — nothing to strip
            var c = go.GetComponent(t);
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }

        private static void RemoveChildIfExists(UnityEngine.GameObject go, string childName)
        {
            if (go == null || string.IsNullOrEmpty(childName)) return;
            var child = go.transform.Find(childName);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        protected virtual void DrawByTag(FObject fobj, FcuTag tag, F2UContext ctx)
        {
            switch (tag)
            {
                case FcuTag.Image:
                case FcuTag.Slice9:
                case FcuTag.AutoSlice9:
                    _imageDrawer.Draw(fobj, ctx); break;
                case FcuTag.Text:
                    _textDrawer.Draw(fobj, ctx); break;
                case FcuTag.AutoLayoutGroup:
                    _layoutDrawer.Draw(fobj, ctx); break;
                case FcuTag.ContentSizeFitter:
                    _layoutDrawer.DrawFit(fobj, ctx); break;
                case FcuTag.Button:
                    _buttonDrawer.Draw(fobj, ctx); break;
                case FcuTag.Toggle:
                    _toggleDrawer.Draw(fobj, ctx); break;
                case FcuTag.InputField:
                case FcuTag.PasswordField:
                    _inputFieldDrawer.Draw(fobj, ctx); break;
                case FcuTag.ScrollView:
                    _layoutDrawer.DrawScroll(fobj, ctx); break;
                case FcuTag.Shadow:
                    _shadowDrawer.Draw(fobj, ctx); break;
                case FcuTag.Mask:
                    _maskDrawer.Draw(fobj, ctx); break;
                case FcuTag.CanvasGroup:
                    _canvasGroupDrawer.Draw(fobj, ctx); break;
            }
        }
    }
}
