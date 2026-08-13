using UnityEngine;

namespace Figma2Unity.Editor.Drawers
{
    /// <summary>EnsureComponent: idempotent — get-or-add. All Drawers MUST use this
    /// instead of AddComponent to satisfy "import twice → identical result" + incremental
    /// sync RedrawNode safety. See Design.md §四 "Drawer 幂等性".</summary>
    public static class GameObjectExtensions
    {
        public static T EnsureComponent<T>(this GameObject go) where T : Component
        {
            if (go == null) return null;
            return go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();
        }

        public static Component EnsureComponent(this GameObject go, System.Type type)
        {
            if (go == null || type == null) return null;
            return go.TryGetComponent(type, out var c) ? c : go.AddComponent(type);
        }
    }
}
