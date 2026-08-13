using UnityEngine;

namespace Figma2Unity.Editor.Layout
{
    public struct AnchorPreset
    {
        public Vector2 Min;
        public Vector2 Max;

        public AnchorPreset(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Maps Figma's 5×5 constraint matrix (= 25 combos) to UGUI Anchor.
    /// SCALE is approximated to STRETCH-with-ratio (Design.md §5.3 caveat applies when
    /// parent/child aspect ratios differ).
    /// </summary>
    public static class AnchorMapper
    {
        public static AnchorPreset Map(Constraints constraints, FObject fobj, FObject parent)
        {
            if (constraints == null)
                return new AnchorPreset(new Vector2(0f, 1f), new Vector2(0f, 1f));

            float hMin = 0f, hMax = 0f, vMin = 0f, vMax = 0f;

            switch (constraints.Horizontal)
            {
                case ConstraintType.MIN: hMin = 0f; hMax = 0f; break;
                case ConstraintType.MAX: hMin = 1f; hMax = 1f; break;
                case ConstraintType.CENTER: hMin = 0.5f; hMax = 0.5f; break;
                case ConstraintType.STRETCH: hMin = 0f; hMax = 1f; break;
                case ConstraintType.SCALE:
                    if (parent != null && parent.AbsoluteBoundingBox.width > 0)
                    {
                        var b = fobj.AbsoluteBoundingBox;
                        var pb = parent.AbsoluteBoundingBox;
                        hMin = (b.x - pb.x) / pb.width;
                        hMax = (b.x - pb.x + b.width) / pb.width;
                    }
                    else { hMin = 0f; hMax = 1f; }
                    break;
            }

            switch (constraints.Vertical)
            {
                // UGUI Y is up, Figma Y is down — invert.
                case ConstraintType.MIN: vMin = 1f; vMax = 1f; break;  // Figma top → UGUI top
                case ConstraintType.MAX: vMin = 0f; vMax = 0f; break;  // Figma bottom → UGUI bottom
                case ConstraintType.CENTER: vMin = 0.5f; vMax = 0.5f; break;
                case ConstraintType.STRETCH: vMin = 0f; vMax = 1f; break;
                case ConstraintType.SCALE:
                    if (parent != null && parent.AbsoluteBoundingBox.height > 0)
                    {
                        var b = fobj.AbsoluteBoundingBox;
                        var pb = parent.AbsoluteBoundingBox;
                        float topFromParent = (b.y - pb.y) / pb.height;
                        float bottomFromParent = (pb.y + pb.height - b.y - b.height) / pb.height;
                        vMin = bottomFromParent;
                        vMax = 1f - topFromParent;
                    }
                    else { vMin = 0f; vMax = 1f; }
                    break;
            }

            return new AnchorPreset(new Vector2(hMin, vMin), new Vector2(hMax, vMax));
        }
    }
}
