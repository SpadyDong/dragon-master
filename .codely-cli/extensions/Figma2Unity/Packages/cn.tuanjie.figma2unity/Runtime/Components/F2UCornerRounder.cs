/*
 * F2UCornerRounder — self-contained rounded-corner / circle mask for UGUI Images.
 *
 * Vendored into Figma2Unity from DA-Corner-Rounder (D.A. Assets, MIT License,
 * Copyright (c) 2019 Kirill Evdokimov) so the importer no longer needs a runtime
 * assembly reference to DA_Assets.CR. Class name, namespace and the backing shader
 * ("UI/Figma2Unity/CornerRounder") are all F2U-specific to avoid clashing with the
 * original DA_Assets.CR package, which may still be present because DA_Assets.FCU
 * depends on it.
 *
 * Only the runtime masking behaviour is kept — the DA Config / Localization / custom
 * Editor inspector (which pull in the wider DA framework) are intentionally dropped.
 *
 * MIT License. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
 */

using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity
{
    /// <summary>
    /// Masks a rectangular <see cref="Graphic"/> quad into a rounded-rectangle or circle
    /// at runtime via a shared SDF shader — no sprite / geometry rebuild required.
    /// Radii are stored as a <see cref="Vector4"/> in px order (BL, BR, TR, TL). Passing
    /// a very large uniform radius (e.g. 9999) collapses the shape into a circle / oval.
    /// </summary>
    [AddComponentMenu("UI/Effects/Figma2Unity CornerRounder")]
    public class F2UCornerRounder : BaseMeshEffect
    {
        private static readonly Vector2 wNorm = new Vector2(.7071068f, -.7071068f);
        private static readonly Vector2 hNorm = new Vector2(.7071068f, .7071068f);

        public bool independent = false;

        public Vector4 radiiSerialized = new Vector4(40f, 40f, 40f, 40f);
        private Vector4 _radii => GetNormalizedRadii();

        private const AdditionalCanvasShaderChannels RequiredChannels =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.TexCoord2 |
            AdditionalCanvasShaderChannels.TexCoord3 |
            AdditionalCanvasShaderChannels.Tangent;

        private static Material __material;
        private static Material _sharedMaterial
        {
            get
            {
                if (__material == null)
                {
                    __material = new Material(Shader.Find("UI/Figma2Unity/CornerRounder"));
                }
                return __material;
            }
        }

        private Vector4 GetNormalizedRadii()
        {
            if (graphic == null)
                return radiiSerialized;

            float width = Mathf.Abs(graphic.rectTransform.rect.width);
            float height = Mathf.Abs(graphic.rectTransform.rect.height);

            Vector4 r = new Vector4();
            r.x = NormalizeAngleToSize(radiiSerialized.x, width, height);
            r.y = NormalizeAngleToSize(radiiSerialized.y, width, height);
            r.z = NormalizeAngleToSize(radiiSerialized.z, width, height);
            r.w = NormalizeAngleToSize(radiiSerialized.w, width, height);

            return r;
        }

        private int NormalizeAngleToSize(float val, float width, float height)
        {
            if (val <= 0)
            {
                val = 0;
                return (int)val;
            }

            float resW = width / val;
            float resH = height / val;

            if (resW < 2 || resH < 2)
            {
                int min = Mathf.Min((int)(width / 2), (int)(height / 2));
                val = min;
            }

            if (val <= 0)
                val = 0;

            return (int)val;
        }

        public void SetRadii(Vector4 r)
        {
            if (r.x == r.y &&
                r.x == r.z &&
                r.x == r.w)
            {
                independent = false;
            }
            else
            {
                independent = true;
            }

            radiiSerialized = r;

            Refresh();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (graphic != null)
            {
                EnsureShaderChannels();
                graphic.material = _sharedMaterial;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (graphic != null)
            {
                graphic.material = null;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (graphic != null)
            {
                EnsureShaderChannels();
                graphic.material = _sharedMaterial;
                graphic.SetVerticesDirty();
            }
        }
#endif

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            EnsureShaderChannels();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || graphic == null)
                return;

            Rect rect = graphic.rectTransform.rect;
            Vector2 halfSize = rect.size * 0.5f;
            Vector4 radii = _radii;
            Vector4 rect2props = RecalculateProps(rect.size);

            UIVertex vert = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);

                vert.uv1 = halfSize;
                vert.uv2 = new Vector2(radii.x, radii.y);
                vert.uv3 = new Vector2(radii.z, radii.w);
                vert.tangent = rect2props;

                vh.SetUIVertex(vert, i);
            }
        }

        public void Refresh()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        private void EnsureShaderChannels()
        {
            Canvas canvas = graphic != null ? graphic.canvas : GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            if ((canvas.additionalShaderChannels & RequiredChannels) != RequiredChannels)
            {
                canvas.additionalShaderChannels |= RequiredChannels;
            }
        }

        private Vector4 RecalculateProps(Vector2 size)
        {
            Vector4 rect2props = new Vector4();

            // Vector that goes from left to right sides of rect2
            Vector2 aVec = new Vector2(size.x, -size.y + _radii.x + _radii.z);

            // Project vector aVec to wNorm to get magnitude of rect2 width vector
            float halfWidth = Vector2.Dot(aVec, wNorm) * .5f;
            rect2props.z = halfWidth;

            // Vector that goes from bottom to top sides of rect2
            Vector2 bVec = new Vector2(size.x, size.y - _radii.w - _radii.y);

            // Project vector bVec to hNorm to get magnitude of rect2 height vector
            float halfHeight = Vector2.Dot(bVec, hNorm) * .5f;
            rect2props.w = halfHeight;

            // Vector that goes from left to top sides of rect2
            Vector2 efVec = new Vector2(size.x - _radii.x - _radii.y, 0);

            // Vector that goes from point E to point G, which is top-left of rect2
            Vector2 egVec = hNorm * Vector2.Dot(efVec, hNorm);

            // Position of point E relative to center of coord system
            Vector2 ePoint = new Vector2(_radii.x - (size.x / 2), size.y / 2);

            // Origin of rect2 relative to center of coord system
            // ePoint + egVec == vector to top-left corner of rect2
            // wNorm * halfWidth + hNorm * -halfHeight == vector from top-left corner to center
            Vector2 origin = ePoint + egVec + wNorm * halfWidth + hNorm * -halfHeight;
            rect2props.x = origin.x;
            rect2props.y = origin.y;

            return rect2props;
        }
    }
}
