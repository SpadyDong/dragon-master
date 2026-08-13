using System.Collections.Generic;

namespace Figma2Unity.Editor.PrototypeFlow
{
    /// <summary>
    /// Unity-free screen / section identification, extracted from <see cref="FlowManager"/>
    /// so both the import pipeline and <see cref="F2UPrefetchCommand"/> resolve the exact
    /// same Screen set. Keeping it free of UnityEngine.UI types lets it compile in the
    /// Layer A standalone test project (FlowManager itself touches Canvas / UGUI and stays
    /// out of that build).
    ///
    /// Mutates <c>ctx.Screens</c> / <c>ctx.FlowSections</c> and adds <see cref="FcuTag.Screen"/>
    /// / <see cref="FcuTag.FlowSection"/> tags — identical behavior to the original
    /// FlowManager.IdentifyScreensAndSections, which now delegates here.
    /// </summary>
    public static class ScreenIdentifier
    {
        public static void Identify(F2UContext ctx)
        {
            if (ctx?.AllNodes == null) return;
            ctx.Screens = new List<FObject>();
            ctx.FlowSections = new List<FObject>();

            // Pass 1: Sections only (a node holding a transitionNodeID is a source, not a screen).
            foreach (var fobj in ctx.AllNodes)
            {
                if (fobj == null) continue;
                if (fobj.Type == "SECTION")
                {
                    AddTag(fobj, FcuTag.FlowSection);
                    ctx.FlowSections.Add(fobj);
                }
            }

            // Screen-eligible ids: direct VirtualPage children OR direct SECTION children.
            var topLevelIds = new HashSet<string>();
            if (ctx.VirtualPage?.Children != null)
            {
                foreach (var top in ctx.VirtualPage.Children)
                {
                    if (top == null || string.IsNullOrEmpty(top.Id)) continue;
                    topLevelIds.Add(top.Id);
                    if (top.Type == "SECTION" && top.Children != null)
                    {
                        foreach (var nested in top.Children)
                        {
                            if (nested != null && !string.IsNullOrEmpty(nested.Id))
                                topLevelIds.Add(nested.Id);
                        }
                    }
                }
            }

            // Pass 2: transition targets become Screens (FRAME / COMPONENT / INSTANCE).
            // NOT restricted to top-level: designers commonly nest destination frames inside
            // a wrapper "Cover" frame, and Figma overlay/popup targets live deeper still. A
            // NAVIGATE/OVERLAY destination is meant to be shown, so promote it to a standalone
            // navigable Screen regardless of nesting depth — otherwise the FlowButton binds
            // but the runtime navigation silently no-ops (dead button). CollectScreenRoots /
            // CreateGameObjectsStep treat each promoted Screen as its own prefab root and stop
            // the parent screen from inlining it.
            var referenced = new HashSet<string>();
            foreach (var fobj in ctx.AllNodes)
            {
                if (fobj != null && !string.IsNullOrEmpty(fobj.TransitionNodeID))
                    referenced.Add(fobj.TransitionNodeID);
            }
            foreach (var fobj in ctx.AllNodes)
            {
                if (fobj == null) continue;
                if (fobj.Type != "FRAME" && fobj.Type != "COMPONENT" && fobj.Type != "INSTANCE")
                    continue;
                if (!referenced.Contains(fobj.Id)) continue;
                if (fobj.Tags.Contains(FcuTag.Screen)) continue;
                AddScreen(ctx, fobj);
            }

            // Pass 3: CANVAS-level flowStartingPoints (hoisted onto VirtualPage by parser).
            if (ctx.VirtualPage?.FlowStartingPoints != null)
            {
                foreach (var sp in ctx.VirtualPage.FlowStartingPoints)
                {
                    if (sp == null || string.IsNullOrEmpty(sp.NodeId)) continue;
                    if (!topLevelIds.Contains(sp.NodeId)) continue;
                    if (ctx.NodeMap.TryGetValue(sp.NodeId, out var target) && target != null
                        && !target.Tags.Contains(FcuTag.Screen))
                    {
                        AddScreen(ctx, target);
                    }
                }
            }

            // Fallback: no prototype wiring → top-level FRAMEs, skipping showcase / oversize.
            if (ctx.Screens.Count == 0 && ctx.VirtualPage?.Children != null)
            {
                var candidates = new List<FObject>();
                foreach (var top in ctx.VirtualPage.Children)
                {
                    if (top == null) continue;
                    if (top.Type == "FRAME") candidates.Add(top);
                    else if (top.Type == "SECTION" && top.Children != null)
                    {
                        foreach (var nested in top.Children)
                            if (nested != null && nested.Type == "FRAME") candidates.Add(nested);
                    }
                }
                float maxScreenW = 0f, maxScreenH = 0f;
                foreach (var top in candidates)
                {
                    if (IsShowcaseFrame(top.Name)) continue;
                    if (top.AbsoluteBoundingBox.width > maxScreenW) maxScreenW = top.AbsoluteBoundingBox.width;
                    if (top.AbsoluteBoundingBox.height > maxScreenH) maxScreenH = top.AbsoluteBoundingBox.height;
                }
                foreach (var top in candidates)
                {
                    if (IsShowcaseFrame(top.Name)) continue;
                    var bb = top.AbsoluteBoundingBox;
                    if (maxScreenW > 0f && bb.width > maxScreenW * 1.5f) continue;
                    if (maxScreenH > 0f && bb.height > maxScreenH * 1.5f) continue;
                    AddScreen(ctx, top);
                }
            }

            // Last-resort fallback: a page whose top level holds only loose content
            // (RECTANGLE / GROUP / TEXT / VECTOR …) and no FRAME/SECTION — a flat design
            // mock-up rather than a screen prototype. Without a Screen the controller's
            // InitialScreenId stays empty and Play shows nothing. Wrap ALL top-level visible
            // children in one synthetic FRAME (sized to their union bounds) and treat it as
            // the single screen, so the whole page renders as one Unity screen.
            if (ctx.Screens.Count == 0)
                SynthesizeSingleScreen(ctx);
        }

        /// <summary>
        /// Build one synthetic FRAME containing every top-level visible child of the
        /// VirtualPage, sized/positioned to the union of their AbsoluteBoundingBoxes, and
        /// register it as the page's sole Screen. The synthetic node is inserted into the
        /// tree (re-parenting the wrapped children under it) and into <c>ctx.AllNodes</c> /
        /// <c>ctx.NodeMap</c> so downstream steps (graphics already computed per-child,
        /// GameObject creation, prefab save, flow controller) operate on it like any real
        /// Screen. No-op when the page has no usable children.
        /// </summary>
        private static void SynthesizeSingleScreen(F2UContext ctx)
        {
            var page = ctx?.VirtualPage;
            if (page?.Children == null || page.Children.Count == 0) return;

            var wrapped = new List<FObject>();
            foreach (var child in page.Children)
            {
                if (child == null) continue;
                if (child.Tags != null && child.Tags.Contains(FcuTag.Ignore)) continue;
                wrapped.Add(child);
            }
            if (wrapped.Count == 0) return;

            // Union bounds across the wrapped children.
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            bool any = false;
            foreach (var c in wrapped)
            {
                var b = c.AbsoluteBoundingBox;
                if (b.width <= 0f && b.height <= 0f) continue;
                any = true;
                if (b.x < minX) minX = b.x;
                if (b.y < minY) minY = b.y;
                if (b.x + b.width > maxX) maxX = b.x + b.width;
                if (b.y + b.height > maxY) maxY = b.y + b.height;
            }
            if (!any) { minX = minY = 0f; maxX = maxY = 0f; }

            var bounds = new UnityEngine.Rect(minX, minY, maxX - minX, maxY - minY);

            var screen = new FObject
            {
                Id = "__synthetic_screen__",
                Name = !string.IsNullOrEmpty(page.Name) ? page.Name : "Screen",
                Type = "FRAME",
                Visible = true,
                Opacity = 1f,
                AbsoluteBoundingBox = bounds,
                AbsoluteRenderBounds = bounds,
                Size = new UnityEngine.Vector2(bounds.width, bounds.height),
                Parent = page,
                Graphic = new FGraphic(),
            };
            screen.FileName = screen.Name;
            screen.FolderName = screen.Name;

            // Re-parent the wrapped children under the synthetic screen, replacing them in the
            // page's child list with the single wrapper.
            foreach (var c in wrapped) c.Parent = screen;
            screen.Children = new List<FObject>(wrapped);
            page.Children.RemoveAll(wrapped.Contains);
            page.Children.Add(screen);

            // Register into the flat views so CollectScreenRoots / Finalize see it. AllNodes
            // may be a List (FlattenTreeStep) — fall back to a rebuilt list otherwise.
            if (ctx.NodeMap != null) ctx.NodeMap[screen.Id] = screen;
            if (ctx.AllNodes is List<FObject> list) list.Insert(0, screen);
            else if (ctx.AllNodes != null)
            {
                var rebuilt = new List<FObject> { screen };
                rebuilt.AddRange(ctx.AllNodes);
                ctx.AllNodes = rebuilt;
            }

            AddScreen(ctx, screen);
        }

        public static bool IsShowcaseFrame(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var n = name.ToLowerInvariant();
            return n.Contains("showcase") || n.Contains("cover") || n.StartsWith("not figma");
        }

        private static void AddScreen(F2UContext ctx, FObject fobj)
        {
            if (fobj == null) return;
            AddTag(fobj, FcuTag.Screen);
            if (!ctx.Screens.Contains(fobj)) ctx.Screens.Add(fobj);
        }

        private static void AddTag(FObject fobj, FcuTag tag)
        {
            if (fobj.Tags == null) fobj.Tags = new List<FcuTag>();
            if (!fobj.Tags.Contains(tag)) fobj.Tags.Add(tag);
        }
    }
}
