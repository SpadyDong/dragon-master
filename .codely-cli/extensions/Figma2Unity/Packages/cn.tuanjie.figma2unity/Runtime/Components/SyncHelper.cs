using UnityEngine;

namespace Figma2Unity
{
    /// <summary>Per-node sync helper component (used by incremental sync diff).</summary>
    public class SyncHelper : MonoBehaviour
    {
        public SyncData Data = new SyncData();
    }
}
