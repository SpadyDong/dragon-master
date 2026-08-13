using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 成就面板 — 成就列表 + 图鉴收集（双标签）
/// </summary>
public class AchievementPanel : MonoBehaviour
{
    [Header("标签切换")]
    [SerializeField] private Button achievementTabButton;
    [SerializeField] private Button collectionTabButton;
    [SerializeField] private Color tabActiveColor = new(1f, 0.85f, 0.4f);
    [SerializeField] private Color tabInactiveColor = new(0.35f, 0.3f, 0.25f);

    [Header("列表")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private GameObject emptyHint;

    private bool _showingAchievements = true;
    private List<GameObject> _entryObjects = new();

    void Start()
    {
        if (achievementTabButton != null) achievementTabButton.onClick.AddListener(() => ShowTab(true));
        if (collectionTabButton != null) collectionTabButton.onClick.AddListener(() => ShowTab(false));
    }

    public void Refresh()
    {
        ShowTab(_showingAchievements);
    }

    private void ShowTab(bool showAchievements)
    {
        _showingAchievements = showAchievements;

        var achImg = achievementTabButton?.GetComponent<Image>();
        var colImg = collectionTabButton?.GetComponent<Image>();
        if (achImg != null) achImg.color = showAchievements ? tabActiveColor : tabInactiveColor;
        if (colImg != null) colImg.color = !showAchievements ? tabActiveColor : tabInactiveColor;

        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();

        if (AchievementManager.Instance == null) return;

        if (showAchievements)
        {
            ShowAchievements();
        }
        else
        {
            ShowCollections();
        }
    }

    private void ShowAchievements()
    {
        var achievements = Resources.LoadAll<AchievementData>("Achievements");
        if (emptyHint != null) emptyHint.SetActive(achievements.Length == 0);

        foreach (var achievement in achievements)
        {
            var entry = Instantiate(entryPrefab, listContainer);
            _entryObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = entry.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            var progressText = entry.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();

            bool unlocked = AchievementManager.Instance.IsUnlocked(achievement.achievementId);
            int progress = AchievementManager.Instance.GetProgress(achievement.achievementId);

            if (nameText != null)
                nameText.text = $"{(unlocked ? "<color=#FFD700>★</color> " : "")}{achievement.achievementName}";
            if (descText != null) descText.text = achievement.description;
            if (progressText != null)
                progressText.text = unlocked ? "已解锁" : $"{progress}/{achievement.targetCount}";
        }
    }

    private void ShowCollections()
    {
        foreach (CollectionType type in System.Enum.GetValues(typeof(CollectionType)))
        {
            var entry = Instantiate(entryPrefab, listContainer);
            _entryObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = entry.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            var progressText = entry.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();

            int count = AchievementManager.Instance.GetCollectionCount(type);

            if (nameText != null) nameText.text = GetCollectionName(type);
            if (descText != null) descText.text = $"已收集 {count} 种";
            if (progressText != null) progressText.text = $"{count}";
        }
    }

    private string GetCollectionName(CollectionType type) => type switch
    {
        CollectionType.Crop       => "作物图鉴",
        CollectionType.Fish       => "鱼类图鉴",
        CollectionType.Dragon     => "龙种图鉴",
        CollectionType.WildAnimal => "野怪图鉴",
        CollectionType.Equipment  => "装备图鉴",
        CollectionType.NPC        => "NPC档案",
        _ => "?"
    };
}
