using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Figma2Unity
{
    /// <summary>
    /// Click target that asks the PrototypeFlowController to navigate to a target screen.
    ///
    /// ⚠ This component does NOT hold a direct Controller reference: FlowButton lives inside
    /// a Screen Prefab while PrototypeFlowController lives at the Canvas root — a cross-Prefab
    /// boundary. Unity nulls direct refs when saving Prefabs, so we resolve at runtime via
    /// GetComponentInParent → FindObjectOfType and cache the result. See Design.md §四.
    /// </summary>
    public class FlowButton : MonoBehaviour
    {
        [SerializeField] private string _targetScreenNodeId;
        [SerializeField] private float _transitionDuration = 0.3f;
        [SerializeField] private EasingType _transitionEasing = EasingType.LINEAR;
        [SerializeField] private NavigationType _navigationType = NavigationType.NAVIGATE;
        [SerializeField] private bool _isCloseAction;
        [SerializeField] private bool _hasOverlayPosition;
        [SerializeField] private Vector2 _overlayPosition;

        public string TargetScreenNodeId
        {
            get => _targetScreenNodeId;
            set => _targetScreenNodeId = value;
        }

        public float TransitionDuration
        {
            get => _transitionDuration;
            set => _transitionDuration = value;
        }

        public EasingType TransitionEasing
        {
            get => _transitionEasing;
            set => _transitionEasing = value;
        }

        public NavigationType NavigationType
        {
            get => _navigationType;
            set => _navigationType = value;
        }

        public bool IsCloseAction
        {
            get => _isCloseAction;
            set => _isCloseAction = value;
        }

        public bool HasOverlayPosition
        {
            get => _hasOverlayPosition;
            set => _hasOverlayPosition = value;
        }

        public Vector2 OverlayPosition
        {
            get => _overlayPosition;
            set => _overlayPosition = value;
        }

        private PrototypeFlowController _cachedController;
        private bool _hooked;
        // Child Button(s) we attached a forwarding listener to (see Awake). Tracked so
        // OnDestroy can cleanly remove them.
        private readonly List<Button> _hookedChildButtons = new List<Button>();

        public PrototypeFlowController ResolveController()
        {
            if (_cachedController != null) return _cachedController;
            _cachedController = GetComponentInParent<PrototypeFlowController>()
                ?? FindObjectOfType<PrototypeFlowController>();
            return _cachedController;
        }

        private void Awake()
        {
            // Awake runs synchronously inside Instantiate() for active GameObjects, so the
            // listener is wired before any code (including same-frame scripted clicks) can
            // call onClick.Invoke(). Start would defer this to the next frame, which made
            // the very first click after a freshly-instantiated screen no-op for tooling /
            // automated tests. End-user clicks still worked because real input is at least
            // one frame later, but Awake is strictly safer.
            if (_hooked) return;
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(OnButtonClick);
                _hooked = true;
            }

            HookInterceptingChildButtons();
        }

        /// <summary>
        /// A Figma node can be a navigation target (this FlowButton) while a child node is
        /// name-tagged "button" and gets its own UGUI Button + raycast-target Image from
        /// ButtonDrawer. That child sits above us in the raycast and UGUI delivers the click
        /// to it — but it has no onClick listener, so the navigation is swallowed and the
        /// button appears dead. Forward those orphan child Buttons' clicks to us.
        ///
        /// We only adopt child Buttons that do NOT carry their own FlowButton (a nested
        /// independent navigation target must keep handling its own clicks).
        /// </summary>
        private void HookInterceptingChildButtons()
        {
            var childButtons = GetComponentsInChildren<Button>(true);
            foreach (var cb in childButtons)
            {
                if (cb == null || cb.gameObject == gameObject) continue;
                // Skip child Buttons that belong to a different FlowButton (their own, or a
                // closer FlowButton ancestor between them and us).
                var owner = cb.GetComponentInParent<FlowButton>();
                if (owner != this) continue;
                cb.onClick.AddListener(OnButtonClick);
                _hookedChildButtons.Add(cb);
            }
        }

        private void OnDestroy()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(OnButtonClick);
            foreach (var cb in _hookedChildButtons)
            {
                if (cb != null) cb.onClick.RemoveListener(OnButtonClick);
            }
            _hookedChildButtons.Clear();
        }

        public void OnButtonClick()
        {
            var ctrl = ResolveController();
            if (ctrl == null) return;
            // CLOSE action: dismiss the top-most overlay regardless of target.
            if (_isCloseAction) { ctrl.CloseTopOverlay(); return; }
            if (string.IsNullOrEmpty(_targetScreenNodeId)) return;
            // OVERLAY: stack the popup over the current background (no destroy).
            if (_navigationType == NavigationType.OVERLAY) { ctrl.ShowOverlay(_targetScreenNodeId, _hasOverlayPosition, _overlayPosition); return; }
            // NAVIGATE: instant full-screen switch. Pass duration 0 so TransitionToScreen
            // routes through the direct-swap path (no fade out/in). The serialized
            // _transitionDuration / _transitionEasing are intentionally bypassed here.
            ctrl.TransitionToScreen(_targetScreenNodeId, 0f, _transitionEasing);
        }
    }
}
