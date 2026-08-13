using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 博物馆面板 — 捐赠计数 + 里程碑 + 已读书籍
/// </summary>
public class MuseumPanel : MonoBehaviour
{
    [Header("统计信息")]
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("图鉴计数")]
    [SerializeField] private Transform collectionContainer;
    [SerializeField] private GameObject collectionEntryPrefab;

    [Header("关闭")]
    [SerializeField] private Button closeButton;

    private List<GameObject> _entryObjects = new();

    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (MuseumManager.Instance == null) return;

        if (statsText != null)
            statsText.text = $"总捐赠: {MuseumManager.Instance.GetTotalDonatedCount()} 件  |  已读书籍: {MuseumManager.Instance.ReadBookCount}";

        if (collectionContainer == null || collectionEntryPrefab == null) return;

        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();

        foreach (CollectionType type in System.Enum.GetValues(typeof(CollectionType)))
        {
            var entry = Instantiate(collectionEntryPrefab, collectionContainer);
            _entryObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var countText = entry.transform.Find("CountText")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null) nameText.text = type.ToString();
            if (countText != null) countText.text = $"{MuseumManager.Instance.GetDonatedCount(type)} 件";
        }
    }

    private void OnCloseClicked()
    {
        UIManager.Instance?.CloseStandalonePanels();
    }
}
