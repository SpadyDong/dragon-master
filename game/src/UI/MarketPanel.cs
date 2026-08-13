using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 菜市场面板 — 7 品类当日价格 + 买卖
/// </summary>
public class MarketPanel : MonoBehaviour
{
    [Header("价格列表")]
    [SerializeField] private Transform priceListContainer;
    [SerializeField] private GameObject priceEntryPrefab;

    [Header("信息")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("关闭")]
    [SerializeField] private Button closeButton;

    private List<GameObject> _entryObjects = new();

    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        EventBus.Subscribe<int>(GameEvent.PlayerGoldChanged, _ => UpdateGold());
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.PlayerGoldChanged, _ => UpdateGold());
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (MarketManager.Instance == null) return;
        if (priceListContainer == null || priceEntryPrefab == null) return;

        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();

        var board = MarketManager.Instance.GetDailyBoard();

        foreach (MarketCategory category in System.Enum.GetValues(typeof(MarketCategory)))
        {
            var entry = Instantiate(priceEntryPrefab, priceListContainer);
            _entryObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var priceText = entry.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();

            float mult = board.TryGetValue(category, out var m) ? m : 1.0f;
            float percent = (mult - 1f) * 100f;

            if (nameText != null) nameText.text = MarketCategoryUtils.GetCategoryName(category);
            if (priceText != null)
            {
                string color = percent >= 0 ? "#FF6666" : "#66FF66";
                priceText.text = $"<color={color}>{percent:+0;-0}%</color>";
            }
        }

        UpdateGold();
    }

    private void UpdateGold()
    {
        if (goldText != null && GameManager.Instance != null)
            goldText.text = GameManager.Instance.GetGoldString();
    }

    private void OnCloseClicked()
    {
        UIManager.Instance?.CloseStandalonePanels();
    }
}
