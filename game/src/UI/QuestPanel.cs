using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 任务面板 — 进行中 / 已完成 双标签
/// </summary>
public class QuestPanel : MonoBehaviour
{
    [Header("标签切换")]
    [SerializeField] private Button activeTabButton;
    [SerializeField] private Button completedTabButton;
    [SerializeField] private Color tabActiveColor = new(1f, 0.85f, 0.4f);
    [SerializeField] private Color tabInactiveColor = new(0.35f, 0.3f, 0.25f);

    [Header("任务列表")]
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questEntryPrefab;
    [SerializeField] private GameObject emptyHint;

    [Header("详情")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questNpcText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private TextMeshProUGUI questCompletionText;
    [SerializeField] private Transform objectiveContainer;
    [SerializeField] private GameObject objectivePrefab;
    [SerializeField] private TextMeshProUGUI questRewardText;

    private bool _showingActive = true;
    private List<GameObject> _entryObjects = new();
    private List<GameObject> _objectiveObjects = new();

    void Start()
    {
        if (activeTabButton != null) activeTabButton.onClick.AddListener(() => ShowTab(true));
        if (completedTabButton != null) completedTabButton.onClick.AddListener(() => ShowTab(false));
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    public void Refresh()
    {
        ShowTab(_showingActive);
    }

    private void ShowTab(bool showActive)
    {
        _showingActive = showActive;

        // 按钮颜色
        var activeImg = activeTabButton?.GetComponent<Image>();
        var compImg = completedTabButton?.GetComponent<Image>();
        if (activeImg != null) activeImg.color = showActive ? tabActiveColor : tabInactiveColor;
        if (compImg != null) compImg.color = !showActive ? tabActiveColor : tabInactiveColor;

        // 清除旧条目
        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();
        if (detailPanel != null) detailPanel.SetActive(false);

        if (QuestManager.Instance == null) return;

        // 获取对应的任务列表
        var allQuests = QuestManager.Instance.GetAllQuestData();
        var filtered = new List<QuestData>();

        foreach (var q in allQuests)
        {
            var state = QuestManager.Instance.GetState(q.questId);
            if (showActive && state == QuestState.Active)
                filtered.Add(q);
            else if (!showActive && state == QuestState.Completed)
                filtered.Add(q);
        }

        // 空提示
        if (emptyHint != null)
            emptyHint.SetActive(filtered.Count == 0);

        // 创建任务条目
        foreach (var quest in filtered)
        {
            if (questEntryPrefab == null || questListContainer == null) continue;
            var entry = Instantiate(questEntryPrefab, questListContainer);
            _entryObjects.Add(entry);

            // 设置条目文本
            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var npcText = entry.transform.Find("NpcText")?.GetComponent<TextMeshProUGUI>();
            var progressBar = entry.transform.Find("ProgressBar")?.GetComponent<Slider>();
            var button = entry.GetComponent<Button>();

            if (nameText != null) nameText.text = quest.questName;

            // 查找 NPC 名
            string npcName = quest.npcId;
            var npcData = NPCDatabase.BuildAll();
            if (npcData.TryGetValue(quest.npcId, out var data))
                npcName = data.npcName;
            if (npcText != null) npcText.text = npcName;

            // 进度条（仅进行中任务）
            if (progressBar != null)
            {
                if (showActive && quest.objectives != null && quest.objectives.Length > 0)
                {
                    var progress = QuestManager.Instance.GetProgress(quest.questId);
                    int done = 0;
                    for (int i = 0; i < quest.objectives.Length; i++)
                    {
                        if (progress != null && i < progress.Length && progress[i] >= quest.objectives[i].requiredCount)
                            done++;
                    }
                    progressBar.value = (float)done / quest.objectives.Length;
                    progressBar.gameObject.SetActive(true);
                }
                else
                {
                    progressBar.gameObject.SetActive(false);
                }
            }

            // 点击事件
            if (button != null)
            {
                var q = quest;
                button.onClick.AddListener(() => ShowQuestDetail(q));
            }
        }
    }

    private void ShowQuestDetail(QuestData quest)
    {
        if (detailPanel == null) return;
        detailPanel.SetActive(true);

        if (questTitleText != null) questTitleText.text = quest.questName;

        // NPC 名
        string npcName = quest.npcId;
        var npcData = NPCDatabase.BuildAll();
        if (npcData.TryGetValue(quest.npcId, out var data))
            npcName = data.npcName;
        if (questNpcText != null) questNpcText.text = $"委托人: {npcName}";

        // 描述
        var state = QuestManager.Instance.GetState(quest.questId);
        if (questDescriptionText != null)
        {
            if (state == QuestState.Completed)
                questDescriptionText.text = quest.completionText ?? quest.description;
            else
                questDescriptionText.text = quest.description;
        }

        if (questCompletionText != null)
            questCompletionText.gameObject.SetActive(state == QuestState.Completed);

        // 目标列表
        ClearObjectives();
        if (quest.objectives != null && state == QuestState.Active)
        {
            var progress = QuestManager.Instance.GetProgress(quest.questId);
            for (int i = 0; i < quest.objectives.Length; i++)
            {
                var obj = quest.objectives[i];
                int current = (progress != null && i < progress.Length) ? progress[i] : 0;
                bool done = current >= obj.requiredCount;

                if (objectivePrefab != null && objectiveContainer != null)
                {
                    var objGO = Instantiate(objectivePrefab, objectiveContainer);
                    _objectiveObjects.Add(objGO);

                    var objText = objGO.GetComponent<TextMeshProUGUI>();
                    var toggle = objGO.GetComponent<Toggle>();

                    if (objText != null)
                    {
                        objText.text = $"{(done ? "<color=#88FF88>✓</color> " : "<color=#FF8888>□</color> ")}{obj.description} ({current}/{obj.requiredCount})";
                    }
                    if (toggle != null) toggle.isOn = done;
                }
            }
        }

        // 奖励
        if (questRewardText != null)
        {
            string rewards = "奖励: ";
            if (quest.rewards != null)
            {
                foreach (var r in quest.rewards)
                    rewards += $"{r.description}×{r.amount}  ";
            }
            if (quest.rewardGold > 0) rewards += $"{quest.rewardGold}文  ";
            if (quest.rewardAffection > 0) rewards += $"好感+{quest.rewardAffection}";
            questRewardText.text = rewards;
        }
    }

    private void ClearObjectives()
    {
        foreach (var obj in _objectiveObjects) Destroy(obj);
        _objectiveObjects.Clear();
    }
}
