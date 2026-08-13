using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// InputFieldDrawer — attaches TMP_InputField. Looks for "Text" / "Placeholder"
    /// children (case-insensitive) by convention to wire <c>textComponent</c> and
    /// <c>placeholder</c>. <see cref="FcuTag.PasswordField"/> flips contentType to
    /// Password (after DrawerCoordinator's PasswordField-wins exclusion runs).
    /// </summary>
    public class InputFieldDrawer
    {
        public virtual void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            var input = go.EnsureComponent<TMP_InputField>();

            var textTmp = FindChildText(go, "text");
            var placeholderTmp = FindChildText(go, "placeholder");
            if (textTmp != null) input.textComponent = textTmp;
            if (placeholderTmp != null) input.placeholder = placeholderTmp;

            // The TMP input needs an Image with raycast on so clicks land. Without children
            // ImageDrawer may have skipped; insert a transparent raycast surface (mirrors
            // BindFlowButtonsStep's transparent-Image trick).
            var img = go.GetComponent<Image>();
            if (img == null)
            {
                img = go.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0f);
            }
            img.raycastTarget = true;

            if (fobj.Tags != null && fobj.Tags.Contains(FcuTag.PasswordField))
            {
                input.contentType = TMP_InputField.ContentType.Password;
                input.inputType = TMP_InputField.InputType.Password;
            }
            else
            {
                input.contentType = TMP_InputField.ContentType.Standard;
            }
        }

        private static TextMeshProUGUI FindChildText(GameObject parent, string nameNeedle)
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
                    var tmp = child.gameObject.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) return tmp;
                }
            }
            return null;
        }
    }
}
