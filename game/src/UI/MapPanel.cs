using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地图面板控制器
/// M 键开关，显示静态地图图片 + 玩家位置指示
/// </summary>
public class MapPanel : MonoBehaviour
{
    [Header("地图面板")]
    [SerializeField] private GameObject mapPanel;

    [Header("地图图片")]
    [SerializeField] private RawImage mapImage;

    [Header("玩家指示器")]
    [SerializeField] private RectTransform playerIndicator;

    [Header("世界尺寸")]
    [Tooltip("世界地图 Tilemap 总尺寸（像素），150×120 tiles × 32px = 4800×3840")]
    [SerializeField] private Vector2 mapWorldSize = new Vector2(4800, 3840);

    [Header("指示器动画")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScaleMin = 0.8f;
    [SerializeField] private float pulseScaleMax = 1.2f;

    private bool _isOpen;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = mapPanel?.GetComponent<CanvasGroup>();
        if (mapPanel != null)
            mapPanel.SetActive(false);
    }

    void Update()
    {
        // M 键切换地图
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }

        if (!_isOpen) return;

        // 更新玩家位置指示器
        UpdatePlayerIndicator();

        // 脉冲动画
        if (playerIndicator != null)
        {
            float scale = Mathf.Lerp(pulseScaleMin, pulseScaleMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            playerIndicator.localScale = Vector3.one * scale;
        }
    }

    public void ToggleMap()
    {
        _isOpen = !_isOpen;
        if (mapPanel != null)
            mapPanel.SetActive(_isOpen);
    }

    public void CloseMap()
    {
        _isOpen = false;
        if (mapPanel != null)
            mapPanel.SetActive(false);
    }

    private void UpdatePlayerIndicator()
    {
        if (playerIndicator == null || mapImage == null) return;
        if (PlayerController.Instance == null) return;

        // 玩家世界坐标 → 地图 UI 坐标百分比
        Vector3 playerPos = PlayerController.Instance.transform.position;

        float xPercent = (playerPos.x + mapWorldSize.x / 2f) / mapWorldSize.x;
        float yPercent = (playerPos.y + mapWorldSize.y / 2f) / mapWorldSize.y;

        xPercent = Mathf.Clamp01(xPercent);
        yPercent = Mathf.Clamp01(yPercent);

        RectTransform rt = mapImage.rectTransform;
        Vector2 uiPos = new Vector2(
            xPercent * rt.sizeDelta.x,
            yPercent * rt.sizeDelta.y
        );

        playerIndicator.anchoredPosition = uiPos;
    }
}
