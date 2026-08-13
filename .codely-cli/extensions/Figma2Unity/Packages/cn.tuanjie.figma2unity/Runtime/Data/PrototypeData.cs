using System;
using System.Collections.Generic;

namespace Figma2Unity
{
    /// <summary>Figma prototype starting point for a Section.</summary>
    [Serializable]
    public class FlowStartingPoint
    {
        public string NodeId;
        public string Name;
    }

    /// <summary>Record of a Component instance override (raw API data).
    /// ⚠ Only used for identity / Prefab normalization. Not consumed by Drawer / Baker.
    /// See Design.md §3.3.</summary>
    [Serializable]
    public class InstanceOverride
    {
        public string NodeId;
        public List<string> Fields;
    }
}
