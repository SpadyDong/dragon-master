using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 秘密纸条面板 — 已收集纸条编号列表
/// </summary>
public class SecretNotePanel : MonoBehaviour
{
    [Header("纸条列表")]
    [SerializeField] private Transform noteListContainer;
    [SerializeField] private GameObject noteEntryPrefab;
    [SerializeField] private GameObject emptyHint;

    [Header("统计")]
    [SerializeField] private TextMeshProUGUI statsText;

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
        if (SecretNoteManager.Instance == null) return;

        if (statsText != null)
        {
            string complete = SecretNoteManager.Instance.IsComplete ? "（集齐！）" : "";
            statsText.text = $"已收集 {SecretNoteManager.Instance.FoundNoteCount}/50 {complete}";
        }

        if (noteListContainer == null || noteEntryPrefab == null) return;

        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();

        var found = SecretNoteManager.Instance.GetFoundNotes();
        if (emptyHint != null) emptyHint.SetActive(found.Count == 0);

        foreach (var noteId in found)
        {
            var entry = Instantiate(noteEntryPrefab, noteListContainer);
            _entryObjects.Add(entry);

            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = $"秘密纸条 #{noteId}";
        }
    }

    private void OnCloseClicked()
    {
        UIManager.Instance?.CloseStandalonePanels();
    }
}
