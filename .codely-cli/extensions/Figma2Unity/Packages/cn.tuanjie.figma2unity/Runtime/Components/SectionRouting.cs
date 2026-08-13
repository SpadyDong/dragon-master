using System.Collections.Generic;

namespace Figma2Unity
{
    /// <summary>
    /// Strategy for choosing which Section starting point becomes the initial screen at
    /// runtime. Injected via <see cref="PrototypeFlowController.SetRoutingStrategy"/>.
    /// Returns the NodeId of the chosen FlowStartingPoint, or null to fall back to the
    /// controller's serialized <c>InitialScreenId</c> (which is v1 behaviour: the first
    /// CANVAS-level start).
    /// </summary>
    public interface ISectionRoutingStrategy
    {
        string ChooseStartingPoint(IReadOnlyList<FlowStartingPoint> all);
    }

    /// <summary>v1-compatible default: pick the first starting point in the CANVAS list.</summary>
    public sealed class FirstStartingPointStrategy : ISectionRoutingStrategy
    {
        public string ChooseStartingPoint(IReadOnlyList<FlowStartingPoint> all)
        {
            if (all == null || all.Count == 0) return null;
            for (int i = 0; i < all.Count; i++)
            {
                var sp = all[i];
                if (sp != null && !string.IsNullOrEmpty(sp.NodeId)) return sp.NodeId;
            }
            return null;
        }
    }

    /// <summary>
    /// Pick the starting point whose Name matches <see cref="TargetName"/>
    /// (case-insensitive, trimmed). Use this when a Figma file ships multiple Section
    /// starts and the project wants to land on a specific one (e.g. "Onboarding" build vs
    /// "Home" build). Falls back to the first starting point if no match — never null
    /// while the list has entries, so users can rely on it for navigation.
    /// </summary>
    public sealed class NamedSectionStrategy : ISectionRoutingStrategy
    {
        public string TargetName;

        public NamedSectionStrategy() { }
        public NamedSectionStrategy(string targetName) { TargetName = targetName; }

        public string ChooseStartingPoint(IReadOnlyList<FlowStartingPoint> all)
        {
            if (all == null || all.Count == 0) return null;
            if (!string.IsNullOrEmpty(TargetName))
            {
                var needle = TargetName.Trim();
                for (int i = 0; i < all.Count; i++)
                {
                    var sp = all[i];
                    if (sp == null || string.IsNullOrEmpty(sp.NodeId) || string.IsNullOrEmpty(sp.Name)) continue;
                    if (string.Equals(sp.Name.Trim(), needle, System.StringComparison.OrdinalIgnoreCase))
                        return sp.NodeId;
                }
            }
            // No match → first valid entry (mirrors FirstStartingPointStrategy).
            for (int i = 0; i < all.Count; i++)
            {
                var sp = all[i];
                if (sp != null && !string.IsNullOrEmpty(sp.NodeId)) return sp.NodeId;
            }
            return null;
        }
    }
}
