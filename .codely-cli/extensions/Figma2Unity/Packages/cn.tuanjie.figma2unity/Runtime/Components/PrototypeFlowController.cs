using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity
{
    /// <summary>Runtime entry for a single Figma Screen prefab.</summary>
    [Serializable]
    public class FlowScreen
    {
        public string NodeId;
        public string Name;
        public GameObject Prefab;
    }

    /// <summary>Runtime entry for a Figma Section.
    /// API exposes flowStartingPoints on CANVAS, not on SECTION, so Sections are stored
    /// as plain markers. The full CANVAS-level start list lives on
    /// PrototypeFlowController.AllStartingPoints (Design.md §3 "AllStartingPoints 完整保存"
    /// — v1 picks [0]; v2 multi-start routing consumes the full list with zero data
    /// migration).</summary>
    [Serializable]
    public class FlowSection
    {
        public string SectionId;
        public string Name;
    }

    /// <summary>
    /// Runtime controller for prototype navigation. Uses List + Dictionary via
    /// ISerializationCallbackReceiver: List is what Unity serializes; Dictionary is the
    /// O(1) runtime lookup view.
    ///
    /// v1: SetCurrentScreen direct swap (no fade). v2: TransitionToScreen upgrades to a
    /// Fade In/Out coroutine — interface stays identical.
    /// </summary>
    public class PrototypeFlowController : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private RectTransform _screenParent;
        [SerializeField] private string _initialScreenId;
        [SerializeField] private List<FlowScreen> _screenList = new List<FlowScreen>();
        [SerializeField] private List<FlowSection> _sectionList = new List<FlowSection>();
        // CANVAS-level flowStartingPoints — full list preserved for v2 multi-start routing
        // (Design.md "AllStartingPoints 完整保存"). v1 picks [0] for InitialScreenId.
        [SerializeField] private List<FlowStartingPoint> _allStartingPoints = new List<FlowStartingPoint>();

        private Dictionary<string, FlowScreen> _screens = new Dictionary<string, FlowScreen>();
        private Dictionary<string, FlowSection> _sections = new Dictionary<string, FlowSection>();
        // Overlay stack: parallel lists of the popup instance and its backdrop. Index N in
        // both lists belong to the same overlay; the top of the stack is the last element.
        private readonly List<GameObject> _overlayInstances = new List<GameObject>();
        private readonly List<GameObject> _overlayBackdrops = new List<GameObject>();

        public List<FlowScreen> ScreenList => _screenList;
        public List<FlowSection> SectionList => _sectionList;
        public List<FlowStartingPoint> AllStartingPoints => _allStartingPoints;

        public RectTransform ScreenParent
        {
            get => _screenParent;
            set => _screenParent = value;
        }

        public string InitialScreenId
        {
            get => _initialScreenId;
            set => _initialScreenId = value;
        }

        public void OnAfterDeserialize() => RebuildLookups();

        public void OnBeforeSerialize() { }

        // Awake also rebuilds the lookups so the controller works even when no
        // deserialization round-trip happens on entering Play Mode. With
        // "Enter Play Mode Options" set to Disable Domain/Scene Reload (this project's
        // setting), Unity does NOT reserialize the scene on Play, so OnAfterDeserialize
        // never fires — and a controller freshly created in-editor by the import pipeline
        // (AddComponent) was never deserialized either. Without this, _screens stays empty
        // and the initial SetCurrentScreen() at Start() no-ops with a "no navigable screen"
        // warning. RebuildLookups is idempotent, so running both callbacks is harmless.
        private void Awake() => RebuildLookups();

        private void RebuildLookups()
        {
            _screens = new Dictionary<string, FlowScreen>();
            if (_screenList != null)
            {
                foreach (var s in _screenList)
                    if (s != null && !string.IsNullOrEmpty(s.NodeId)) _screens[s.NodeId] = s;
            }
            _sections = new Dictionary<string, FlowSection>();
            if (_sectionList != null)
            {
                foreach (var s in _sectionList)
                    if (s != null && !string.IsNullOrEmpty(s.SectionId)) _sections[s.SectionId] = s;
            }
        }

        private GameObject _currentScreenInstance;
        private string _currentScreenId;
        private Coroutine _transitionRoutine;
        private ISectionRoutingStrategy _routingStrategy;

        public event Action<string> OnScreenChanged;

        public string CurrentScreenId => _currentScreenId;

        /// <summary>
        /// Inject a custom Section routing strategy. Must be called BEFORE the controller's
        /// Start() runs (typically from a bootstrap component's Awake) for the strategy to
        /// influence the initial screen choice. Pass null to fall back to v1 behaviour
        /// (use serialized _initialScreenId, which is the CANVAS-level
        /// flowStartingPoints[0] FlowManager picked at import time).
        /// </summary>
        public void SetRoutingStrategy(ISectionRoutingStrategy strategy)
            => _routingStrategy = strategy;

        private void Start()
        {
            // v1 path: just use the serialized initial screen id (the CANVAS-level first
            // starting point, picked by FlowManager.DetermineInitialScreen at import time).
            string startId = _initialScreenId;

            // v2 path: if a routing strategy was injected, let it choose from the full
            // AllStartingPoints list (the multi-start data is already preserved on the
            // controller). Strategy can return null to fall back to v1.
            if (_routingStrategy != null)
            {
                var chosen = _routingStrategy.ChooseStartingPoint(_allStartingPoints);
                if (!string.IsNullOrEmpty(chosen)) startId = chosen;
            }

            if (!string.IsNullOrEmpty(startId))
                SetCurrentScreen(startId);
        }

        /// <summary>
        /// v1: direct swap alias. v2: Fade Out → swap → Fade In via coroutine. The interface
        /// is preserved (FlowButton/users calling without duration get the
        /// F2UConfig.DefaultTransitionDuration default that BindFlowButtonsStep wires in).
        /// </summary>
        public void TransitionToScreen(string screenNodeId)
            => TransitionToScreen(screenNodeId, 0.3f, EasingType.LINEAR);

        public void TransitionToScreen(string screenNodeId, float duration, EasingType easing)
        {
            if (string.IsNullOrEmpty(screenNodeId)) return;
            // Switching the background screen dismisses any lingering overlays.
            CloseAllOverlays();
            if (!_screens.TryGetValue(screenNodeId, out var screen) || screen == null || screen.Prefab == null)
            {
                Debug.LogWarning($"[Figma2Unity] TransitionToScreen: no navigable screen for node id " +
                    $"'{screenNodeId}'. The target is not registered as a Screen (e.g. an overlay/popup or a " +
                    "frame outside the imported screen set) or its prefab is missing — the click is a no-op.");
                return;
            }

            // Self-navigation guard: already showing this screen and idle → ignore. Without
            // this, clicking a nav button for the page you're already on destroys and
            // reinstantiates it (visible flash + loss of runtime state). A request made while
            // a transition is still mid-flight is honored so an interrupted fade can settle.
            if (_transitionRoutine == null && _currentScreenInstance != null
                && string.Equals(screenNodeId, _currentScreenId, System.StringComparison.Ordinal))
                return;

            // Interrupting mid-fade: stop the previous coroutine cleanly AND destroy any
            // half-faded instance it left behind. StopCoroutine aborts execution at the
            // yield point so the coroutine's own "Destroy + null-out" lines never run —
            // without this guard, every interruption strands one half-transparent Screen
            // GameObject in the scene (memory + raycast bleed).
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
                if (_currentScreenInstance != null)
                {
                    Destroy(_currentScreenInstance);
                    _currentScreenInstance = null;
                    _currentScreenId = null;
                }
            }

            // Zero / negative duration → direct swap (preserves v1 semantics for non-fade
            // tests + avoids spawning a one-frame coroutine for no reason).
            if (duration <= 0f || !isActiveAndEnabled)
            {
                SetCurrentScreen(screenNodeId);
                _transitionRoutine = null;
                return;
            }

            _transitionRoutine = StartCoroutine(TransitionRoutine(screen, duration, easing));
        }

        private IEnumerator TransitionRoutine(FlowScreen screen, float duration, EasingType easing)
        {
            // Fade out current instance (if any).
            if (_currentScreenInstance != null)
            {
                var oldGroup = EnsureCanvasGroup(_currentScreenInstance);
                yield return Fade(oldGroup, oldGroup.alpha, 0f, duration, easing);
                Destroy(_currentScreenInstance);
                _currentScreenInstance = null;
            }

            // Instantiate the new screen at alpha 0 and fade in.
            var parent = _screenParent != null ? _screenParent : transform;
            _currentScreenInstance = Instantiate(screen.Prefab, parent);
            _currentScreenInstance.SetActive(true);
            _currentScreenId = screen.NodeId;

            var newGroup = EnsureCanvasGroup(_currentScreenInstance);
            newGroup.alpha = 0f;
            yield return Fade(newGroup, 0f, 1f, duration, easing);
            newGroup.alpha = 1f;

            OnScreenChanged?.Invoke(_currentScreenId);
            _transitionRoutine = null;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration, EasingType easing)
        {
            if (group == null || duration <= 0f) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (group == null) yield break; // Destroyed mid-fade.
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, ApplyEasing(t, easing));
                yield return null;
            }
            if (group != null) group.alpha = to;
        }

        private static float ApplyEasing(float t, EasingType easing)
        {
            switch (easing)
            {
                case EasingType.EASE_IN:
                    return t * t;
                case EasingType.EASE_OUT:
                    return 1f - (1f - t) * (1f - t);
                case EasingType.EASE_IN_AND_OUT:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case EasingType.EASE_IN_BACK:
                {
                    const float c = 1.70158f;
                    return (c + 1f) * t * t * t - c * t * t;
                }
                case EasingType.EASE_OUT_BACK:
                {
                    const float c = 1.70158f;
                    return 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
                }
                case EasingType.EASE_IN_AND_OUT_BACK:
                {
                    const float c = 1.70158f * 1.525f;
                    return t < 0.5f
                        ? Mathf.Pow(2f * t, 2f) * ((c + 1f) * 2f * t - c) * 0.5f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c + 1f) * (t * 2f - 2f) + c) + 2f) * 0.5f;
                }
                default:
                    return t;
            }
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            if (go == null) return null;
            return go.TryGetComponent<CanvasGroup>(out var cg) ? cg : go.AddComponent<CanvasGroup>();
        }

        public void SetCurrentScreen(string screenNodeId)
        {
            if (string.IsNullOrEmpty(screenNodeId)) return;
            // Switching the background screen dismisses any lingering overlays.
            CloseAllOverlays();
            if (!_screens.TryGetValue(screenNodeId, out var screen) || screen == null || screen.Prefab == null)
            {
                Debug.LogWarning($"[Figma2Unity] SetCurrentScreen: no navigable screen for node id " +
                    $"'{screenNodeId}' (target not registered as a Screen, or its prefab is missing).");
                return;
            }

            // Cancel any in-flight fade so its tail Destroy doesn't fire on the new
            // instance we're about to create. Same orphan-prevention as TransitionToScreen.
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }

            if (_currentScreenInstance != null)
                Destroy(_currentScreenInstance);

            var parent = _screenParent != null ? _screenParent : transform;
            _currentScreenInstance = Instantiate(screen.Prefab, parent);
            _currentScreenInstance.SetActive(true);
            _currentScreenId = screen.NodeId;

            OnScreenChanged?.Invoke(_currentScreenId);
        }

        public bool TryGetScreen(string nodeId, out FlowScreen screen)
            => _screens.TryGetValue(nodeId ?? "", out screen);

        public bool TryGetSection(string sectionId, out FlowSection section)
            => _sections.TryGetValue(sectionId ?? "", out section);

        /// <summary>
        /// Show <paramref name="screenNodeId"/> as an OVERLAY popup stacked on top of the
        /// current background screen. The background instance is NOT destroyed. A transparent
        /// backdrop is inserted behind the popup to block clicks on the background (the
        /// backdrop is invisible — no dimming — and does not dismiss the overlay; only an
        /// in-popup CLOSE action does, via <see cref="CloseTopOverlay"/>).
        /// </summary>
        public void ShowOverlay(string screenNodeId)
            => ShowOverlay(screenNodeId, false, Vector2.zero);

        /// <summary>
        /// Overload that places the popup at <paramref name="overlayPosition"/> — the
        /// popup's screen-local TL (pixels from the source screen's top-left, Figma Y-down).
        /// <see cref="Figma2Unity.Editor.Pipeline.Steps.BindFlowButtonsStep"/> resolves this
        /// from Figma's per-trigger <c>overlayRelativePosition</c> (which is itself the
        /// offset relative to the trigger node) by adding the trigger's screen-local TL —
        /// so the popup lands at the same screen-local coordinates as an equivalent static
        /// element placed inside the screen. When <paramref name="hasPosition"/> is false
        /// (CENTER mode, no <c>overlayRelativePosition</c>, or trigger lacks a Screen
        /// ancestor) the popup is centered in <see cref="ScreenParent"/>, matching Figma's
        /// default OVERLAY positioning.
        /// </summary>
        public void ShowOverlay(string screenNodeId, bool hasPosition, Vector2 overlayPosition)
        {
            if (string.IsNullOrEmpty(screenNodeId)) return;
            if (!_screens.TryGetValue(screenNodeId, out var screen) || screen == null || screen.Prefab == null)
            {
                Debug.LogWarning($"[Figma2Unity] ShowOverlay: no navigable screen for node id " +
                    $"'{screenNodeId}' (target not registered as a Screen, or its prefab is missing).");
                return;
            }

            // Guard against re-stacking the same popup on top of itself (e.g. double click).
            if (_overlayInstances.Count > 0)
            {
                var top = _overlayInstances[_overlayInstances.Count - 1];
                if (top != null && top.name == OverlayInstanceName(screen)) return;
            }

            var parent = _screenParent != null ? _screenParent : transform;

            // 1) Full-screen, invisible backdrop rendered just under the popup. raycastTarget
            //    is on so background clicks are swallowed (but the backdrop never dismisses).
            var backdrop = new GameObject("OverlayBackdrop", typeof(RectTransform), typeof(Image));
            var backdropRt = backdrop.GetComponent<RectTransform>();
            backdropRt.SetParent(parent, false);
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;
            var backdropImg = backdrop.GetComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0f); // Fully transparent (no dimming).
            backdropImg.raycastTarget = true; // Block clicks on background (but don't close).

            // 2) Popup instance, rendered above the backdrop (later sibling = on top).
            var instance = Instantiate(screen.Prefab, parent);
            instance.name = OverlayInstanceName(screen);
            instance.SetActive(true);

            var overlayRt = instance.GetComponent<RectTransform>();
            if (hasPosition)
                PlaceOverlayAtScreenLocal(overlayRt, overlayPosition);
            else
                CenterOverlay(overlayRt);

            _overlayBackdrops.Add(backdrop);
            _overlayInstances.Add(instance);
        }

        /// <summary>
        /// Force-center an overlay popup inside <see cref="ScreenParent"/>. Mirrors the
        /// PAGE-branch placement <c>RectTransformConverter.ApplyRootStretch</c> bakes into
        /// every Screen prefab's root: center anchor + center pivot + zero anchoredPosition,
        /// preserving the prefab's authored sizeDelta (design-time popup size).
        /// </summary>
        private static void CenterOverlay(RectTransform overlayRt)
        {
            if (overlayRt == null) return;
            overlayRt.anchorMin = new Vector2(0.5f, 0.5f);
            overlayRt.anchorMax = new Vector2(0.5f, 0.5f);
            overlayRt.pivot = new Vector2(0.5f, 0.5f);
            overlayRt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Place an overlay popup so its top-left lands at <paramref name="screenLocalTopLeft"/>
        /// — screen-local pixels (Figma Y-down) measured from the source screen's top-left.
        /// We anchor the popup to <see cref="ScreenParent"/>'s top-left corner with a center
        /// pivot, then translate using the relative TL plus a half-size offset to align the
        /// pivot with the popup's center.
        ///
        /// ⚠ The screen does NOT necessarily fill <see cref="ScreenParent"/>: under a
        /// CanvasScaler "match height" (or any viewport aspect different from the reference
        /// resolution) the background screen is centered inside a larger ScreenParent,
        /// leaving a horizontal/vertical margin. Anchoring the popup straight to the parent's
        /// top-left then shifts it by that margin (the reported "popup is slightly to the
        /// left" bug). We compensate by first offsetting to the *current screen's* top-left
        /// in parent-local space, so screen-local coords map onto the screen — not the parent.
        /// </summary>
        private void PlaceOverlayAtScreenLocal(RectTransform overlayRt, Vector2 screenLocalTopLeft)
        {
            if (overlayRt == null) return;
            var parent = _screenParent != null ? _screenParent : (RectTransform)transform;
            var size = overlayRt.rect.size;

            Vector2 screenOriginInParent = GetCurrentScreenTopLeftInParent(parent);

            overlayRt.anchorMin = new Vector2(0f, 1f);
            overlayRt.anchorMax = new Vector2(0f, 1f);
            overlayRt.pivot = new Vector2(0.5f, 0.5f);
            overlayRt.anchoredPosition = new Vector2(
                screenOriginInParent.x + screenLocalTopLeft.x + size.x * 0.5f,
                screenOriginInParent.y - screenLocalTopLeft.y - size.y * 0.5f);
        }

        /// <summary>
        /// Offset of the current background screen instance's top-left corner relative to
        /// <paramref name="parent"/>'s top-left anchor reference (rect.xMin, rect.yMax), in
        /// parent-local pixels. Zero when the screen is flush with the parent's top-left
        /// (e.g. the viewport aspect matches the reference resolution exactly). Falls back to
        /// zero when there is no current screen so OVERLAY-from-orphan paths still resolve.
        /// </summary>
        private Vector2 GetCurrentScreenTopLeftInParent(RectTransform parent)
        {
            if (parent == null || _currentScreenInstance == null) return Vector2.zero;
            var screenRt = _currentScreenInstance.GetComponent<RectTransform>();
            if (screenRt == null) return Vector2.zero;

            var corners = new Vector3[4];
            screenRt.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR.
            Vector3 screenTopLeftLocal = parent.InverseTransformPoint(corners[1]);
            var pr = parent.rect;
            return new Vector2(screenTopLeftLocal.x - pr.xMin, screenTopLeftLocal.y - pr.yMax);
        }

        private static string OverlayInstanceName(FlowScreen screen)
            => $"Overlay::{screen.NodeId}";

        /// <summary>Dismiss the top-most overlay popup and its backdrop. No-op if empty.</summary>
        public void CloseTopOverlay()
        {
            int last = _overlayInstances.Count - 1;
            if (last < 0) return;
            var instance = _overlayInstances[last];
            var backdrop = _overlayBackdrops[last];
            _overlayInstances.RemoveAt(last);
            _overlayBackdrops.RemoveAt(last);
            if (instance != null) Destroy(instance);
            if (backdrop != null) Destroy(backdrop);
        }

        /// <summary>Dismiss every overlay in the stack (called when the background changes).</summary>
        public void CloseAllOverlays()
        {
            for (int i = _overlayInstances.Count - 1; i >= 0; i--)
            {
                if (_overlayInstances[i] != null) Destroy(_overlayInstances[i]);
                if (_overlayBackdrops[i] != null) Destroy(_overlayBackdrops[i]);
            }
            _overlayInstances.Clear();
            _overlayBackdrops.Clear();
        }
    }
}
