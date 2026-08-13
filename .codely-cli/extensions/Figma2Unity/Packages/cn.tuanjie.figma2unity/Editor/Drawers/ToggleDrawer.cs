using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// ToggleDrawer — attaches UGUI Toggle. Looks for conventional child names
    /// "Background" / "Checkmark" (case-insensitive) to wire <c>targetGraphic</c> and
    /// <c>graphic</c>; if neither child has a Graphic the node's own Image is reused.
    /// Initial <c>isOn</c> follows the <c>#checked</c> tag convention (matches FCU's
    /// manual tagging style; see SmartTagRules).
    /// </summary>
    public class ToggleDrawer
    {
        public virtual void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            var toggle = go.EnsureComponent<Toggle>();

            // Wire targetGraphic + graphic from conventionally-named children, falling back
            // to the toggle node's own Image when no children match.
            var bg = FindChildGraphic(go, "background");
            var check = FindChildGraphic(go, "checkmark");

            toggle.targetGraphic = bg ?? go.GetComponent<Image>();
            toggle.graphic = check;

            // The toggle root itself needs a raycast-receiving graphic, else clicks miss.
            if (toggle.targetGraphic != null)
                toggle.targetGraphic.raycastTarget = true;

            // Initial state from manual "#checked" hint on the Figma node name.
            toggle.isOn = !string.IsNullOrEmpty(fobj.Name)
                && fobj.Name.IndexOf("#checked", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Graphic FindChildGraphic(GameObject parent, string nameNeedle)
        {
            if (parent == null) return null;
            var t = parent.transform;
            int n = t.childCount;
            for (int i = 0; i < n; i++)
            {
                var child = t.GetChild(i);
                if (child == null || child.gameObject == null) continue;
                if (child.gameObject.name != null
                    && child.gameObject.name.IndexOf(nameNeedle, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var g = child.gameObject.GetComponent<Graphic>();
                    if (g != null) return g;
                }
            }
            return null;
        }
    }
}
