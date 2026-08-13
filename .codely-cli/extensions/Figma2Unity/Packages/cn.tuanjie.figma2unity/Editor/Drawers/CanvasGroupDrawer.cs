using UnityEngine;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// CanvasGroupDrawer — attaches CanvasGroup and writes the node's opacity to alpha.
    /// TagSetter Pass 2 adds the CanvasGroup tag when a node's opacity &lt; 1, so this
    /// drawer is only reached for transparent groups. interactable / blocksRaycasts stay
    /// at defaults (true) — semi-transparent UI should still receive input unless the
    /// project opts out at runtime.
    /// </summary>
    public class CanvasGroupDrawer
    {
        public virtual void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;

            var cg = go.EnsureComponent<CanvasGroup>();
            cg.alpha = Mathf.Clamp01(fobj.Opacity);
        }
    }
}
