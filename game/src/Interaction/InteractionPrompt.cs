using UnityEngine;
using TMPro;

/// <summary>
/// 交互提示浮层 UI
/// 显示 "[F] 交谈" / "[F] 拾取" 等提示
/// </summary>
public class InteractionPrompt : MonoBehaviour
{
    [Header("组件")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("动画")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float floatHeight = 2f;
    [SerializeField] private float bobAmount = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    private bool _isShowing;
    private Vector3 _worldTarget;
    private Camera _mainCam;
    private float _bobOffset;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        _mainCam = Camera.main;
    }

    void Update()
    {
        float targetAlpha = _isShowing ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        if (_isShowing)
        {
            _bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            Vector3 screenPos = _mainCam.WorldToScreenPoint(_worldTarget + Vector3.up * floatHeight + Vector3.up * _bobOffset);
            transform.position = screenPos;
        }
    }

    public void Show(string text, Vector3 worldPosition)
    {
        _isShowing = true;
        _worldTarget = worldPosition;
        if (promptText != null)
            promptText.text = $"[F] {text}";
    }

    public void Hide()
    {
        _isShowing = false;
    }
}
