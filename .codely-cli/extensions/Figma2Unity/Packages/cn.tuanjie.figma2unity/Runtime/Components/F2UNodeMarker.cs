using UnityEngine;

namespace Figma2Unity
{
    /// <summary>Marker component attached to every GameObject created from a Figma node.</summary>
    public class F2UNodeMarker : MonoBehaviour
    {
        public string NodeId;
        public string NodeType;
    }
}
