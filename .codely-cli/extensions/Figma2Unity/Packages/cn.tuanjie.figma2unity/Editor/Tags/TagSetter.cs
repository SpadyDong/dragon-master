using System.Collections.Generic;

namespace Figma2Unity.Editor.Tags
{
    /// <summary>
    /// Three-pass tag inference (FCU-style, simplified for v1 + Shadow inference).
    /// Pass 1: Figma type → initial Tag.
    /// Pass 2: Smart inference — name keywords, manual `#tag`, Shadow, CanvasGroup, Mask.
    /// Pass 3: Mark empty nodes as Ignore.
    /// </summary>
    public class TagSetter
    {
        protected readonly F2UConfig _config;
        private readonly string _separator;

        public TagSetter(F2UConfig config)
        {
            _config = config;
            _separator = config != null && !string.IsNullOrEmpty(config.TagSeparator) ? config.TagSeparator : "#";
        }

        public virtual void SetTagsByFigmaType(IReadOnlyList<FObject> nodes)
        {
            if (nodes == null) return;
            foreach (var fobj in nodes)
            {
                if (fobj == null) continue;
                switch (fobj.Type)
                {
                    case "TEXT":
                        AddTag(fobj, FcuTag.Text);
                        break;
                    case "FRAME":
                    case "GROUP":
                    case "INSTANCE":
                    case "COMPONENT":
                        AddTag(fobj, FcuTag.Frame);
                        AddTag(fobj, FcuTag.Image);
                        break;
                    case "COMPONENT_SET":
                        // Variant container: tag as Frame only. Its swatch-background fill is
                        // editor chrome, not real UI — do not render it as an Image.
                        AddTag(fobj, FcuTag.Frame);
                        break;
                    case "RECTANGLE":
                    case "ELLIPSE":
                    case "STAR":
                    case "REGULAR_POLYGON":
                    case "VECTOR":
                    case "LINE":
                    case "BOOLEAN_OPERATION":
                        AddTag(fobj, FcuTag.Image);
                        break;
                    case "SECTION":
                        AddTag(fobj, FcuTag.FlowSection);
                        break;
                    case "PAGE":
                        AddTag(fobj, FcuTag.Page);
                        break;
                }

                // H/V auto-layout, native GRID, and wrapping H/V all become an AutoLayoutGroup
                // (LayoutDrawer picks H/V LayoutGroup vs GridLayoutGroup based on mode/wrap).
                if (fobj.LayoutMode == LayoutMode.HORIZONTAL
                    || fobj.LayoutMode == LayoutMode.VERTICAL
                    || fobj.LayoutMode == LayoutMode.GRID)
                    AddTag(fobj, FcuTag.AutoLayoutGroup);

                if (fobj.LayoutSizingHorizontal == LayoutSizing.HUG
                    || fobj.LayoutSizingVertical == LayoutSizing.HUG)
                    AddTag(fobj, FcuTag.ContentSizeFitter);

                if (fobj.OverflowDirection != OverflowDirection.NONE)
                    AddTag(fobj, FcuTag.ScrollView);
            }
        }

        public virtual void SetSmartTags(IReadOnlyList<FObject> nodes)
        {
            if (nodes == null) return;
            foreach (var fobj in nodes)
            {
                if (fobj == null) continue;

                // Manual #tag tokens, e.g. "BuyBtn #button #shadow"
                ParseManualTags(fobj);

                // Name keyword inference
                if (!string.IsNullOrEmpty(fobj.Name))
                {
                    foreach (var (kw, tag) in SmartTagRules.NameKeywords)
                    {
                        if (SmartTagRules.ContainsInsensitive(fobj.Name, kw))
                            AddTag(fobj, tag);
                    }
                    // PasswordField wins over InputField on same node
                    if (SmartTagRules.ContainsInsensitive(fobj.Name, "password"))
                        AddTag(fobj, FcuTag.PasswordField);
                }

                // Shadow inference: any visible DROP_SHADOW effect
                if (fobj.Effects != null)
                {
                    foreach (var e in fobj.Effects)
                    {
                        if (e != null && e.Visible && e.Type == EffectType.DROP_SHADOW)
                        {
                            AddTag(fobj, FcuTag.Shadow);
                            break;
                        }
                    }
                }

                // CanvasGroup inference: opacity < 1 on non-root nodes
                if (fobj.Opacity < 1f && fobj.Parent != null)
                    AddTag(fobj, FcuTag.CanvasGroup);

                // Mask inference
                if (fobj.IsMask)
                    AddTag(fobj, FcuTag.Mask);
            }
        }

        public virtual void SetIgnoredObjects(IReadOnlyList<FObject> nodes)
        {
            if (nodes == null) return;
            foreach (var fobj in nodes)
            {
                if (fobj == null) continue;

                // Descendants of a VECTOR / BOOLEAN_OPERATION are not independent UI nodes:
                // the parent shape is rendered to a single sprite (Figmage bake or /v1/images
                // VectorPngFallback) that already composites the whole subtree. A
                // BOOLEAN_OPERATION's children are boolean operands (Union/Subtract/Exclude/…)
                // painted with the PARENT's fill, never on their own. Instantiating them as
                // separate Images draws the raw operand shape (often a different color) on top
                // of the correct composited result. Mark the entire subtree Ignore so it never
                // gets a GameObject or a wasted vector render.
                if (HasShapeRenderAncestor(fobj))
                {
                    AddTag(fobj, FcuTag.Ignore);
                    continue;
                }

                if (IsVisuallyEmpty(fobj) && (fobj.Children == null || fobj.Children.Count == 0))
                    AddTag(fobj, FcuTag.Ignore);
            }
        }

        /// <summary>
        /// True when any ancestor is a VECTOR or BOOLEAN_OPERATION — i.e. the node lives inside
        /// a shape that is baked/rendered as one atomic sprite, so the node must not be
        /// instantiated independently.
        /// </summary>
        protected static bool HasShapeRenderAncestor(FObject fobj)
        {
            for (var p = fobj.Parent; p != null; p = p.Parent)
            {
                if (p.Type == "VECTOR" || p.Type == "BOOLEAN_OPERATION")
                    return true;
            }
            return false;
        }

        protected static void AddTag(FObject fobj, FcuTag tag)
        {
            if (fobj.Tags == null) fobj.Tags = new List<FcuTag>();
            if (!fobj.Tags.Contains(tag)) fobj.Tags.Add(tag);
        }

        protected void ParseManualTags(FObject fobj)
        {
            if (string.IsNullOrEmpty(fobj.Name) || string.IsNullOrEmpty(_separator)) return;
            int idx = fobj.Name.IndexOf(_separator, System.StringComparison.Ordinal);
            if (idx < 0) return;
            var rest = fobj.Name.Substring(idx);
            var tokens = rest.Split(new[] { _separator }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawToken in tokens)
            {
                var token = rawToken.Trim();
                if (token.Length == 0) continue;
                if (SmartTagRules.ManualTagMap.TryGetValue(token, out var tag))
                    AddTag(fobj, tag);
            }
        }

        /// <summary>True when node has no visible fills/strokes/effects/text — likely a layout helper.</summary>
        protected static bool IsVisuallyEmpty(FObject fobj)
        {
            if (fobj.Graphic != null)
            {
                if (fobj.Graphic.HasSolidFill || fobj.Graphic.HasGradientFill
                    || fobj.Graphic.HasImageFill || fobj.Graphic.HasStroke)
                    return false;
            }
            else
            {
                // No Graphic computed → fall back to raw Fills count
                if (fobj.Fills != null && fobj.Fills.Count > 0) return false;
                if (fobj.Strokes != null && fobj.Strokes.Count > 0) return false;
            }
            if (fobj.Effects != null && fobj.Effects.Count > 0) return false;
            if (!string.IsNullOrEmpty(fobj.Characters)) return false;
            if (fobj.Type == "TEXT") return false;
            return true;
        }
    }
}
