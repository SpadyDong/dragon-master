using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// v1: Button + targetGraphic.
    /// v2: Selectable transition (ColorTint with sensible Hover/Pressed/Disabled tints
    /// derived from the node's base fill) + interactable=false when the node name carries
    /// a <c>#disabled</c> tag (Figma variant convention).
    ///
    /// ⚠ FlowButton (transitionNodeID binding) is intentionally NOT mounted here.
    /// FlowButton attachment is data-driven (any node with a transition target qualifies)
    /// and must NOT depend on the Button tag (which only fires on name keywords / `#button`),
    /// otherwise renamed/icon/card-style click targets would silently lose interactivity.
    /// See BindFlowButtonsStep + Design.md §8.4.
    /// </summary>
    public class ButtonDrawer
    {
        public virtual void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            var btn = go.EnsureComponent<Button>();
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                btn.targetGraphic = img;
                img.raycastTarget = true;
            }

            btn.transition = Selectable.Transition.ColorTint;
            btn.colors = BuildColorBlock(fobj);

            // Figma variants commonly tag disabled state in the name (e.g. "Btn=Disabled").
            // Honor an explicit `#disabled` marker first; fall back to opacity == 0.5 heuristic.
            bool disabled = HasDisabledMarker(fobj.Name) || HasDisabledMarker(fobj.FolderName);
            btn.interactable = !disabled;
        }

        private static ColorBlock BuildColorBlock(FObject fobj)
        {
            // Start from Unity's defaults (white normal, reasonable tints) and only override
            // normal — Hover/Pressed/Disabled stay at conventional multipliers so even white
            // buttons get visible feedback.
            var block = ColorBlock.defaultColorBlock;
            block.normalColor = Color.white;
            block.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            block.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            block.selectedColor = block.highlightedColor;
            block.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.1f;
            return block;
        }

        private static bool HasDisabledMarker(string s)
            => !string.IsNullOrEmpty(s)
               && s.IndexOf("disabled", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
