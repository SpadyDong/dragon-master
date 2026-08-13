using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>
    /// ShadowDrawer — attaches UGUI Shadow with color/offset from the first visible
    /// DROP_SHADOW effect. Inner shadow, layer blur, background blur are out of scope for
    /// v2 (no UGUI native component); they remain parsed but unused. Effect blur radius
    /// is not representable on UnityEngine.UI.Shadow either, so we only carry color + offset.
    /// </summary>
    public class ShadowDrawer
    {
        public virtual void Draw(FObject fobj, F2UContext ctx)
        {
            var go = ctx.GetGameObject(fobj);
            if (go == null) return;
            if (fobj.Effects == null) return;

            Effect drop = null;
            for (int i = 0; i < fobj.Effects.Count; i++)
            {
                var e = fobj.Effects[i];
                if (e == null) continue;
                if (!e.Visible) continue;
                if (e.Type == EffectType.DROP_SHADOW) { drop = e; break; }
            }
            if (drop == null) return;

            var shadow = go.EnsureComponent<Shadow>();
            shadow.effectColor = drop.Color;
            // Figma offset is in Figma's Y-down basis; UGUI Shadow effectDistance Y up.
            // y is flipped to match the visual direction.
            shadow.effectDistance = new Vector2(drop.Offset.x, -drop.Offset.y);
            shadow.useGraphicAlpha = true;
        }
    }
}
