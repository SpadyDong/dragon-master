using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Figma2Unity.Editor.Api
{
    /// <summary>
    /// Two-stage parser: JSON → POCO (Newtonsoft) → FObject tree (this class).
    /// All fields are parsed even if v1 doesn't yet apply them — this is the
    /// "Parse vs Apply" separation that keeps v1→v2 incremental upgrades safe.
    /// </summary>
    public class FigmaDataParser
    {
        public virtual FObject ParseJson(string json, F2UConfig config)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var response = JsonConvert.DeserializeObject<FigmaDocumentResponse>(json);
            return Parse(response, config);
        }

        public FObject Parse(FigmaDocumentResponse response, F2UConfig config)
        {
            var virtualPage = new FObject
            {
                Id = "__virtual_root__",
                Type = "PAGE",
                Name = response?.Name ?? "VirtualPage",
            };
            if (response?.Document?.Children != null)
            {
                // Figma tree is DOCUMENT -> CANVAS[] -> FRAME[]. CANVAS nodes carry no
                // absoluteBoundingBox; keeping them as real parents gives top-level frames a
                // zero-sized parent and breaks RectTransform conversion. Hoist each CANVAS's
                // children directly under the virtual page so top-level frames are roots.
                //
                // ⚠ flowStartingPoints lives on CANVAS, not on SECTION (Figma REST API §6
                // CanvasNode). When hoisting children we must also lift the CANVAS-level
                // starting points up onto the virtual page so FlowManager can find them.
                //
                // A Figma file holds MANY pages (CANVAS), and each page can declare its own
                // flowStartingPoints. Aggregating across every page lets an unrelated page's
                // start (e.g. "Flow 10" on the "Design draft" page) win InitialScreenId over
                // the page the user actually targeted. So we collect starts per page, then
                // pick the right scope below.
                //
                // Two scoping inputs (TargetNodeId wins over TargetPageName):
                //   • TargetNodeId  — a node living on the wanted page. We keep the starts of
                //     the CANVAS whose subtree contains that node ("Current Page").
                //   • TargetPageName — exact CANVAS name match.
                var aggregatedStarts = new List<FlowStartingPoint>();
                var targetPageStarts = new List<FlowStartingPoint>();
                var targetPageName = config != null ? config.TargetPageName : null;
                var targetNodeId = config != null ? config.TargetNodeId : null;
                bool hasNodeTarget = !string.IsNullOrWhiteSpace(targetNodeId);
                bool hasNameTarget = !hasNodeTarget && !string.IsNullOrWhiteSpace(targetPageName);
                bool hasTarget = hasNodeTarget || hasNameTarget;
                bool matchedTargetPage = false;
                var normalizedNodeId = hasNodeTarget ? NormalizeNodeId(targetNodeId) : null;
                foreach (var canvas in response.Document.Children)
                {
                    if (canvas == null) continue;
                    if (canvas.Type == "CANVAS" || canvas.Type == "DOCUMENT")
                    {
                        CollectFlowStartingPoints(canvas, aggregatedStarts);
                        bool isTargetPage =
                            (hasNodeTarget && CanvasContainsNode(canvas, normalizedNodeId)) ||
                            (hasNameTarget && PageNameMatches(canvas.Name, targetPageName));
                        if (isTargetPage)
                        {
                            matchedTargetPage = true;
                            CollectFlowStartingPoints(canvas, targetPageStarts);
                        }
                        if (canvas.Children == null) continue;
                        foreach (var child in canvas.Children)
                            AddParsedChild(child, virtualPage);
                    }
                    else
                    {
                        AddParsedChild(canvas, virtualPage);
                    }
                }

                // Scope resolution:
                //   - No target configured            → legacy behaviour (aggregate all pages).
                //   - Target configured and matched    → only that page's starts.
                //   - Target configured but no match   → fall back to aggregate + warn, so a
                //                                         bad node-id / typo'd page name never
                //                                         silently wipes out all starts.
                List<FlowStartingPoint> chosenStarts;
                if (!hasTarget)
                {
                    chosenStarts = aggregatedStarts;
                }
                else if (matchedTargetPage)
                {
                    chosenStarts = targetPageStarts;
                }
                else
                {
                    var what = hasNodeTarget
                        ? $"TargetNodeId '{targetNodeId}'"
                        : $"TargetPageName '{targetPageName}'";
                    UnityEngine.Debug.LogWarning(
                        $"[Figma2Unity] {what} matched no page in the Figma document — falling "
                        + "back to aggregating flowStartingPoints from every page. Check the "
                        + "node-id / page name.");
                    chosenStarts = aggregatedStarts;
                }

                if (chosenStarts.Count > 0)
                    virtualPage.FlowStartingPoints = chosenStarts;
            }
            return virtualPage;
        }

        // Page-name match is trimmed + case-insensitive so users don't trip over stray
        // whitespace or capitalisation when typing the target page into the config field.
        private static bool PageNameMatches(string pageName, string target)
        {
            if (string.IsNullOrEmpty(pageName) || string.IsNullOrEmpty(target)) return false;
            return string.Equals(pageName.Trim(), target.Trim(),
                System.StringComparison.OrdinalIgnoreCase);
        }

        // Figma URLs encode node ids with a hyphen ("12013-1517940") while the document
        // JSON uses a colon ("12013:1517940"). Normalise to the colon form so a node-id
        // pulled from a pasted URL lines up with the parsed tree.
        private static string NormalizeNodeId(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            return id.Trim().Replace('-', ':');
        }

        // True if the CANVAS subtree contains a node with the given (normalised) id. Used
        // to resolve which page a "Current Page" node-id belongs to.
        private static bool CanvasContainsNode(FigmaApiNode canvas, string normalizedNodeId)
        {
            if (canvas == null || string.IsNullOrEmpty(normalizedNodeId)) return false;
            if (NormalizeNodeId(canvas.Id) == normalizedNodeId) return true;
            if (canvas.Children == null) return false;
            foreach (var child in canvas.Children)
                if (CanvasContainsNode(child, normalizedNodeId)) return true;
            return false;
        }

        private static void CollectFlowStartingPoints(FigmaApiNode canvas, List<FlowStartingPoint> sink)
        {
            if (canvas?.FlowStartingPoints == null) return;
            foreach (var fp in canvas.FlowStartingPoints)
            {
                if (fp == null || string.IsNullOrEmpty(fp.NodeId)) continue;
                sink.Add(new FlowStartingPoint { NodeId = fp.NodeId, Name = fp.Name });
            }
        }

        private void AddParsedChild(FigmaApiNode api, FObject parent)
        {
            var fobj = ParseNode(api, 0f);
            if (fobj == null) return;
            fobj.Parent = parent;
            parent.Children.Add(fobj);
        }

        /// <param name="ancestorRotationRad">
        /// Sum of the node-level `rotation` (radians) of every ancestor. Figma's `rotation`
        /// field is relative to the parent, but this importer keeps container GameObjects
        /// axis-aligned (rotation is applied to leaves only — see RectTransformConverter),
        /// so a leaf must carry its GLOBAL rotation = ancestors + own. We accumulate that here
        /// and synthesize the matrix from the global angle in ParseRelativeTransform.
        /// </param>
        private FObject ParseNode(FigmaApiNode api, float ancestorRotationRad)
        {
            if (api == null) return null;
            var fobj = new FObject
            {
                Id = api.Id ?? "",
                Name = api.Name ?? "",
                Type = api.Type ?? "",
                Visible = api.Visible ?? true,
                Opacity = api.Opacity ?? 1f,
            };

            ParseBoundingBox(api, fobj);
            ParseCornerRadius(api, fobj);
            ParseConstraints(api, fobj);
            ParseRelativeTransform(api, fobj, ancestorRotationRad);
            ParseGeometryPaths(api, fobj);
            ParseFills(api, fobj);
            ParseStrokes(api, fobj);
            ParseStrokeMisc(api, fobj);
            ParseEffects(api, fobj);
            if (fobj.Type == "TEXT") ParseText(api, fobj);
            ParseLayout(api, fobj);
            ParsePrototype(api, fobj);
            fobj.IsMask = api.IsMask ?? false;
            fobj.ClipsContent = api.ClipsContent ?? false;
            ParseComponentInstance(api, fobj);

            if (api.Children != null)
            {
                float childAncestorRotation = ancestorRotationRad + (api.Rotation ?? 0f);
                foreach (var c in api.Children)
                {
                    var child = ParseNode(c, childAncestorRotation);
                    if (child != null)
                    {
                        child.Parent = fobj;
                        fobj.Children.Add(child);
                    }
                }
            }
            return fobj;
        }

        private static void ParseBoundingBox(FigmaApiNode api, FObject fobj)
        {
            if (api.AbsoluteBoundingBox != null)
            {
                var b = api.AbsoluteBoundingBox;
                fobj.AbsoluteBoundingBox = new Rect(b.X, b.Y, b.Width, b.Height);
            }
            if (api.AbsoluteRenderBounds != null)
            {
                var r = api.AbsoluteRenderBounds;
                fobj.AbsoluteRenderBounds = new Rect(r.X, r.Y, r.Width, r.Height);
            }
            else
            {
                fobj.AbsoluteRenderBounds = fobj.AbsoluteBoundingBox;
            }

            // Local (unrotated) size. Falls back to the AABB width/height when Figma omits
            // `size` — for non-rotated nodes the two are identical, so this is a safe default.
            if (api.Size != null)
                fobj.Size = new Vector2(api.Size.X, api.Size.Y);
            else
                fobj.Size = new Vector2(fobj.AbsoluteBoundingBox.width, fobj.AbsoluteBoundingBox.height);
        }

        private static void ParseCornerRadius(FigmaApiNode api, FObject fobj)
        {
            fobj.CornerSmoothing = api.CornerSmoothing ?? 0f;
            if (api.RectangleCornerRadii != null && api.RectangleCornerRadii.Count == 4)
            {
                fobj.CornerRadius = new[] {
                    api.RectangleCornerRadii[0],
                    api.RectangleCornerRadii[1],
                    api.RectangleCornerRadii[2],
                    api.RectangleCornerRadii[3]
                };
            }
            else if (api.CornerRadius.HasValue)
            {
                float r = api.CornerRadius.Value;
                fobj.CornerRadius = new[] { r, r, r, r };
            }
        }

        private static void ParseConstraints(FigmaApiNode api, FObject fobj)
        {
            if (api.Constraints == null) return;
            fobj.Constraints = new Constraints
            {
                Horizontal = ParseConstraintType(api.Constraints.Horizontal),
                Vertical   = ParseConstraintType(api.Constraints.Vertical),
            };
        }

        private static ConstraintType ParseConstraintType(string s)
        {
            // Figma REST API LayoutConstraint enums (docs/figma-rest-api-data-structure.md §LayoutConstraint):
            //   horizontal: LEFT | RIGHT | CENTER | LEFT_RIGHT | SCALE
            //   vertical:   TOP  | BOTTOM | CENTER | TOP_BOTTOM | SCALE
            // Internal ConstraintType is the abstract MIN/MAX/STRETCH/CENTER/SCALE that
            // AnchorMapper consumes. Without translating LEFT/RIGHT/LEFT_RIGHT/TOP/BOTTOM/TOP_BOTTOM,
            // every node defaulted to MIN and collapsed to the parent's top-left corner.
            // The MIN/MAX/STRETCH cases are kept for back-compat with Layer A unit tests that
            // construct Constraints directly.
            if (string.IsNullOrEmpty(s)) return ConstraintType.MIN;
            switch (s.ToUpperInvariant())
            {
                case "LEFT":
                case "TOP":
                case "MIN":         return ConstraintType.MIN;
                case "CENTER":      return ConstraintType.CENTER;
                case "RIGHT":
                case "BOTTOM":
                case "MAX":         return ConstraintType.MAX;
                case "LEFT_RIGHT":
                case "TOP_BOTTOM":
                case "STRETCH":     return ConstraintType.STRETCH;
                case "SCALE":       return ConstraintType.SCALE;
                default:            return ConstraintType.MIN;
            }
        }

        private static void ParseRelativeTransform(FigmaApiNode api, FObject fobj, float ancestorRotationRad)
        {
            // Preserve the node-level rotation (radians) regardless of which path populates
            // the matrix below — downstream may want the raw angle.
            fobj.Rotation = api.Rotation ?? 0f;

            if (api.RelativeTransform != null && api.RelativeTransform.Count >= 2)
            {
                // Explicit matrix wins. Figma's relativeTransform is relative to the parent and
                // UGUI composes rotation through the hierarchy, so it is consumed as-is.
                var t = new float[2, 3];
                for (int row = 0; row < 2; row++)
                {
                    var r = api.RelativeTransform[row];
                    if (r == null) continue;
                    for (int col = 0; col < 3 && col < r.Count; col++)
                        t[row, col] = r[col] ?? 0f;
                }
                fobj.RelativeTransform = t;
                return;
            }

            // Fallback: some Figma exports omit relativeTransform and encode rotation only via
            // the node-level `rotation` field (radians, relative to parent). We keep container
            // GameObjects axis-aligned and apply rotation to leaves only (see
            // RectTransformConverter.IsLeaf) — so a leaf must encode its GLOBAL rotation, i.e.
            // the sum of every ancestor's rotation plus its own. Synthesize the 2x3 affine from
            // that global angle. Matrix layout matches the converter's GetAngleFromMatrix:
            //   m = [[cos g, -sin g, 0], [sin g, cos g, 0]]  → UGUI Z = -globalDeg.
            float globalRotation = ancestorRotationRad + fobj.Rotation;
            if (Mathf.Approximately(globalRotation, 0f)) return;

            float c = Mathf.Cos(globalRotation);
            float s = Mathf.Sin(globalRotation);
            fobj.RelativeTransform = new float[2, 3]
            {
                { c, -s, 0f },
                { s,  c, 0f },
            };
            fobj.RotationIsGlobalSynthesized = true;
        }

        private static void ParseGeometryPaths(FigmaApiNode api, FObject fobj)
        {
            if (api.FillGeometry != null && api.FillGeometry.Count > 0)
            {
                fobj.FillGeometry = new List<string>(api.FillGeometry.Count);
                foreach (var g in api.FillGeometry) if (g != null) fobj.FillGeometry.Add(g.Path);
            }
            if (api.StrokeGeometry != null && api.StrokeGeometry.Count > 0)
            {
                fobj.StrokeGeometry = new List<string>(api.StrokeGeometry.Count);
                foreach (var g in api.StrokeGeometry) if (g != null) fobj.StrokeGeometry.Add(g.Path);
            }
        }

        private static void ParseFills(FigmaApiNode api, FObject fobj)
        {
            if (api.Fills == null) return;
            foreach (var p in api.Fills)
            {
                var paint = ParsePaint(p);
                if (paint != null) fobj.Fills.Add(paint);
            }
        }

        private static void ParseStrokes(FigmaApiNode api, FObject fobj)
        {
            if (api.Strokes == null) return;
            foreach (var p in api.Strokes)
            {
                var paint = ParsePaint(p);
                if (paint != null) fobj.Strokes.Add(paint);
            }
        }

        private static void ParseStrokeMisc(FigmaApiNode api, FObject fobj)
        {
            fobj.StrokeWeight = api.StrokeWeight ?? 0f;
            fobj.StrokeAlign = ParseEnum(api.StrokeAlign, StrokeAlign.INSIDE);
            fobj.StrokeJoin = ParseEnum(api.StrokeJoin, StrokeJoin.MITER);
            fobj.StrokeCap = ParseEnum(api.StrokeCap, StrokeCap.NONE);
            fobj.StrokeMiterLimit = api.StrokeMiterLimit ?? 4f;
            fobj.DashPattern = api.DashPattern;
            if (api.IndividualStrokeWeights != null)
            {
                var w = api.IndividualStrokeWeights;
                fobj.IndividualStrokeWeights = new[] { w.Top, w.Right, w.Bottom, w.Left };
            }
        }

        private static T ParseEnum<T>(string s, T fallback) where T : struct
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            return Enum.TryParse<T>(s, true, out var v) ? v : fallback;
        }

        private static Paint ParsePaint(FigmaApiPaint p)
        {
            if (p == null) return null;
            var paint = new Paint
            {
                Type = ParseEnum(p.Type, PaintType.SOLID),
                Visible = p.Visible ?? true,
                Opacity = p.Opacity ?? 1f,
                Color = p.Color != null ? new Color(p.Color.R, p.Color.G, p.Color.B, p.Color.A) : Color.white,
                BlendMode = ParseEnum(p.BlendMode, PaintBlendMode.NORMAL),
                ImageRef = p.ImageRef,
                ScaleMode = ParseImageScaleMode(p.ScaleMode),
                Rotation = p.Rotation ?? 0f,
                ScaleX = p.ScalingFactor ?? 1f,
                ScaleY = p.ScalingFactor ?? 1f,
            };
            if (paint.Type != PaintType.SOLID && paint.Type != PaintType.IMAGE)
            {
                paint.Gradient = ParseGradient(p);
            }
            return paint;
        }

        private static ImageScaleMode ParseImageScaleMode(string s)
        {
            if (string.IsNullOrEmpty(s)) return ImageScaleMode.FILL;
            switch (s.ToUpperInvariant())
            {
                case "FILL": return ImageScaleMode.FILL;
                case "FIT":  return ImageScaleMode.FIT;
                case "CROP":
                case "STRETCH": return ImageScaleMode.CROP;
                case "TILE": return ImageScaleMode.TILE;
                default: return ImageScaleMode.FILL;
            }
        }

        private static GradientData ParseGradient(FigmaApiPaint p)
        {
            var g = new GradientData();
            if (p.GradientStops != null)
            {
                foreach (var s in p.GradientStops)
                {
                    if (s == null || s.Color == null) continue;
                    g.Stops.Add(new GradientStop
                    {
                        Position = s.Position,
                        Color = new Color(s.Color.R, s.Color.G, s.Color.B, s.Color.A),
                    });
                }
            }
            if (p.GradientHandlePositions != null && p.GradientHandlePositions.Count >= 3)
            {
                g.GradientHandlePositions = new float[6];
                for (int i = 0; i < 3; i++)
                {
                    g.GradientHandlePositions[i * 2] = p.GradientHandlePositions[i].X;
                    g.GradientHandlePositions[i * 2 + 1] = p.GradientHandlePositions[i].Y;
                }
            }
            return g;
        }

        private static void ParseEffects(FigmaApiNode api, FObject fobj)
        {
            if (api.Effects == null) return;
            foreach (var e in api.Effects)
            {
                if (e == null) continue;
                fobj.Effects.Add(new Effect
                {
                    Type = ParseEnum(e.Type, EffectType.DROP_SHADOW),
                    Visible = e.Visible ?? true,
                    Color = e.Color != null ? new Color(e.Color.R, e.Color.G, e.Color.B, e.Color.A) : Color.black,
                    Offset = e.Offset != null ? new Vector2(e.Offset.X, e.Offset.Y) : Vector2.zero,
                    Radius = e.Radius ?? 0f,
                    Spread = e.Spread ?? 0f,
                    BlendMode = ParseEnum(e.BlendMode, EffectBlendMode.NORMAL),
                    BlurType = e.BlurType,
                    StartRadius = e.StartRadius ?? 0f,
                    StartOffset = e.StartOffset != null ? new Vector2(e.StartOffset.X, e.StartOffset.Y) : (Vector2?)null,
                    EndOffset = e.EndOffset != null ? new Vector2(e.EndOffset.X, e.EndOffset.Y) : (Vector2?)null,
                });
            }
        }

        private static void ParseText(FigmaApiNode api, FObject fobj)
        {
            fobj.Characters = api.Characters;
            if (api.Style != null) fobj.Style = ConvertTextStyle(api.Style);
            fobj.CharacterStyleOverrides = api.CharacterStyleOverrides;
            if (api.StyleOverrideTable != null)
            {
                fobj.StyleOverrideTable = new Dictionary<string, TextStyle>();
                foreach (var kvp in api.StyleOverrideTable)
                {
                    // Per-character overrides are PARTIAL: a Figma override often changes only
                    // fills (color) while leaving fontSize unspecified to inherit the base. Pass
                    // isOverride=true so a missing fontSize parses to 0 ("inherit") instead of the
                    // 16f base fallback — otherwise RichTextBuilder emits a spurious <size=16> tag
                    // that shrinks that glyph (e.g. the º in "20º" rendering tiny). See
                    // RichTextBuilder.OpenTags, which only emits <size> when FontSize > 0.
                    if (kvp.Value != null) fobj.StyleOverrideTable[kvp.Key] = ConvertTextStyle(kvp.Value, isOverride: true);
                }
            }
        }

        private static TextStyle ConvertTextStyle(FigmaApiTextStyle s, bool isOverride = false)
        {
            // Base style: default a missing fontSize to 16 (rare; TMP needs a usable size).
            // Override style: default to 0 = "inherit base size" so partial fill-only overrides
            // do not inject a font-size change downstream.
            float fontSize = s.FontSize ?? (isOverride ? 0f : 16f);
            return new TextStyle
            {
                FontFamily = s.FontFamily,
                FontPostScriptName = s.FontPostScriptName,
                FontSize = fontSize,
                LineHeight = s.LineHeightPx ?? 0f,
                LetterSpacing = s.LetterSpacing ?? 0f,
                Weight = WeightFromFloat(s.FontWeight ?? 400f),
                Italic = s.Italic ?? false,
                TextAlignHorizontal = ParseEnum(s.TextAlignHorizontal, TextAlignHorizontal.LEFT),
                TextAlignVertical = ParseEnum(s.TextAlignVertical, TextAlignVertical.TOP),
                TextAutoResize = ParseEnum(s.TextAutoResize, TextAutoResize.NONE),
                FillStyleId = s.FillStyleId ?? -1,
                Opacity = s.Opacity ?? 1f,
            };
        }

        private static FontWeight WeightFromFloat(float w)
        {
            int i = Mathf.RoundToInt(w / 100f) * 100;
            return (FontWeight)Mathf.Clamp(i, 100, 900);
        }

        private static void ParseLayout(FigmaApiNode api, FObject fobj)
        {
            fobj.LayoutMode = ParseEnum(api.LayoutMode, LayoutMode.NONE);
            fobj.ItemSpacing = api.ItemSpacing ?? 0f;
            fobj.PaddingLeft = api.PaddingLeft ?? 0f;
            fobj.PaddingRight = api.PaddingRight ?? 0f;
            fobj.PaddingTop = api.PaddingTop ?? 0f;
            fobj.PaddingBottom = api.PaddingBottom ?? 0f;
            fobj.PrimaryAxisAlignItems = ParseEnum(api.PrimaryAxisAlignItems, PrimaryAxisAlignItems.MIN);
            fobj.CounterAxisAlignItems = ParseEnum(api.CounterAxisAlignItems, CounterAxisAlignItems.MIN);
            fobj.OverflowDirection = ParseEnum(api.OverflowDirection, OverflowDirection.NONE);
            fobj.LayoutSizingHorizontal = ParseEnum(api.LayoutSizingHorizontal, LayoutSizing.FIXED);
            fobj.LayoutSizingVertical = ParseEnum(api.LayoutSizingVertical, LayoutSizing.FIXED);
            fobj.LayoutGrow = api.LayoutGrow ?? 0f;
            fobj.LayoutAlign = ParseEnum(api.LayoutAlign, LayoutAlign.INHERIT);
            fobj.LayoutWrap = ParseEnum(api.LayoutWrap, LayoutWrap.NO_WRAP);
            fobj.CounterAxisSpacing = api.CounterAxisSpacing ?? 0f;
            fobj.GridRowCount = api.GridRowCount ?? 0;
            fobj.GridColumnCount = api.GridColumnCount ?? 0;
            fobj.GridRowGap = api.GridRowGap ?? 0f;
            fobj.GridColumnGap = api.GridColumnGap ?? 0f;
        }

        private static void ParsePrototype(FigmaApiNode api, FObject fobj)
        {
            fobj.TransitionNodeID = api.TransitionNodeID;
            fobj.TransitionDuration = api.TransitionDuration ?? 0f;
            fobj.TransitionEasing = ParseEnum(api.TransitionEasing, EasingType.LINEAR);
            // Legacy transitionNodeID path is always a full-screen NAVIGATE (enum default).
            fobj.NavigationType = NavigationType.NAVIGATE;

            // New-style: interactions[].action(s). Fold the first navigation target into the
            // legacy TransitionNodeID so Drawer / FlowManager don't need two code paths.
            // Legacy field still wins for the destination when both are present (back-compat),
            // but we still walk interactions to capture navigation (NAVIGATE/OVERLAY) and to
            // detect CLOSE actions (which carry no destinationId).
            //
            // ⚠ A node frequently carries BOTH a legacy transitionNodeID AND an interactions[]
            // entry with the same destination (Figma duplicates the data). We must capture the
            // navigation flag even when the legacy field already filled TransitionNodeID —
            // otherwise OVERLAY popups silently fall back to NAVIGATE (full-screen swap).
            if (api.Interactions != null)
            {
                bool navigationCaptured = false;
                foreach (var ix in api.Interactions)
                {
                    if (ix == null) continue;
                    var dest = ExtractInteractionDestination(ix, out var duration, out var easing,
                        out var navigation, out var isClose, out var overlayPos);
                    if (isClose) fobj.IsCloseOverlay = true;
                    if (string.IsNullOrEmpty(dest))
                        continue;

                    // Adopt this destination if the legacy field left TransitionNodeID empty.
                    if (string.IsNullOrEmpty(fobj.TransitionNodeID))
                    {
                        fobj.TransitionNodeID = dest;
                        if (fobj.TransitionDuration <= 0f && duration.HasValue) fobj.TransitionDuration = duration.Value;
                        if (!string.IsNullOrEmpty(easing)) fobj.TransitionEasing = ParseEnum(easing, fobj.TransitionEasing);
                    }

                    // Capture navigation for the interaction backing the active destination.
                    // Prefer the one whose destination matches TransitionNodeID; fall back to
                    // the first navigation-bearing interaction so OVERLAY is never lost.
                    if (!navigationCaptured && !string.IsNullOrEmpty(navigation)
                        && (string.Equals(dest, fobj.TransitionNodeID, StringComparison.Ordinal)
                            || string.IsNullOrEmpty(fobj.TransitionNodeID)))
                    {
                        fobj.NavigationType = ParseEnum(navigation, NavigationType.NAVIGATE);
                        if (fobj.NavigationType == NavigationType.OVERLAY && overlayPos.HasValue)
                        {
                            fobj.HasOverlayPosition = true;
                            fobj.OverlayPosition = overlayPos.Value;
                        }
                        navigationCaptured = true;
                    }
                }
            }

            if (api.FlowStartingPoints != null && api.FlowStartingPoints.Count > 0)
            {
                fobj.FlowStartingPoints = new List<FlowStartingPoint>(api.FlowStartingPoints.Count);
                foreach (var fp in api.FlowStartingPoints)
                {
                    if (fp == null) continue;
                    fobj.FlowStartingPoints.Add(new FlowStartingPoint { NodeId = fp.NodeId, Name = fp.Name });
                }
            }
        }

        private static string ExtractInteractionDestination(FigmaApiInteraction ix, out float? durationMs,
            out string easing, out string navigation, out bool isClose, out Vector2? overlayPos)
        {
            durationMs = null;
            easing = null;
            navigation = null;
            isClose = false;
            overlayPos = null;
            if (ix == null) return null;

            // Tolerate both "actions" (plural) and "action" (singular) payload shapes.
            if (ix.Actions != null)
            {
                foreach (var a in ix.Actions)
                {
                    var d = ReadAction(a, out durationMs, out easing, out navigation, out var close, out overlayPos);
                    if (close) isClose = true;
                    if (!string.IsNullOrEmpty(d)) return d;
                }
            }
            var single = ReadAction(ix.Action, out durationMs, out easing, out navigation, out var singleClose, out overlayPos);
            if (singleClose) isClose = true;
            return single;
        }

        private static string ReadAction(FigmaApiInteractionAction action, out float? durationMs,
            out string easing, out string navigation, out bool isClose, out Vector2? overlayPos)
        {
            durationMs = null;
            easing = null;
            navigation = null;
            isClose = false;
            overlayPos = null;
            if (action == null) return null;
            if (!string.IsNullOrEmpty(action.Type)
                && string.Equals(action.Type, "CLOSE", StringComparison.OrdinalIgnoreCase))
                isClose = true;
            navigation = action.Navigation;
            if (action.OverlayRelativePosition != null)
                overlayPos = new Vector2(action.OverlayRelativePosition.X, action.OverlayRelativePosition.Y);
            if (string.IsNullOrEmpty(action.DestinationId)) return null;
            if (action.Transition != null)
            {
                // Interaction transition.duration is in seconds → align with legacy ms unit
                // so ButtonDrawer's ms→s conversion stays single-sourced.
                if (action.Transition.Duration.HasValue)
                    durationMs = action.Transition.Duration.Value * 1000f;
                if (action.Transition.Easing != null) easing = action.Transition.Easing.Type;
            }
            return action.DestinationId;
        }

        private static void ParseComponentInstance(FigmaApiNode api, FObject fobj)
        {
            if (string.IsNullOrEmpty(api.ComponentId)) return;
            fobj.IsComponentInstance = true;
            fobj.ComponentId = api.ComponentId;
            if (api.Overrides != null && api.Overrides.Count > 0)
            {
                fobj.Overrides = new List<InstanceOverride>(api.Overrides.Count);
                foreach (var o in api.Overrides)
                {
                    if (o == null) continue;
                    fobj.Overrides.Add(new InstanceOverride { NodeId = o.Id, Fields = o.OverriddenFields });
                }
            }
        }
    }
}
