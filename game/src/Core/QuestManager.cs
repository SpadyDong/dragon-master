using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 支线任务管理器
/// 追踪所有任务的解锁/接取/进度/完成状态
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    private Dictionary<string, QuestData> _allQuests = new();
    private Dictionary<string, QuestProgress> _progress = new();

    /// <summary>当前激活的任务数</summary>
    public int ActiveQuestCount => _progress.Values.Count(p => p.state == QuestState.Active);

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 加载全部任务数据
        var quests = NPCQuests.BuildAll();
        foreach (var q in quests)
        {
            _allQuests[q.questId] = q;
            int objCount = q.objectives?.Length ?? 0;
            _progress[q.questId] = new QuestProgress(q.questId, QuestState.Locked, objCount);
        }

        // 每日检查解锁
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Subscribe<int>(GameEvent.NPCAffectionChanged, OnAffectionChanged);

        // 检查初始可用任务
        CheckUnlocks();
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Unsubscribe<int>(GameEvent.NPCAffectionChanged, OnAffectionChanged);
    }

    /// <summary>获取某NPC的所有任务</summary>
    public List<QuestData> GetQuestsForNPC(string npcId)
    {
        return _allQuests.Values.Where(q => q.npcId == npcId).ToList();
    }

    /// <summary>获取任务数据</summary>
    public QuestData GetQuest(string questId) => _allQuests.TryGetValue(questId, out var q) ? q : null;

    /// <summary>获取任务状态</summary>
    public QuestState GetState(string questId) => _progress.TryGetValue(questId, out var p) ? p.state : QuestState.Locked;

    /// <summary>接取任务</summary>
    public bool AcceptQuest(string questId)
    {
        if (!_progress.TryGetValue(questId, out var p)) return false;
        if (p.state != QuestState.Available) return false;

        p.state = QuestState.Active;
        _allQuests[questId].state = QuestState.Active;

        Debug.Log($"接取任务: {_allQuests[questId].questName}");
        EventBus.Publish(GameEvent.PlayerInteracted, $"quest_accept_{questId}");
        return true;
    }

    /// <summary>推进任务目标（收集/交付/击杀等）</summary>
    public void ProgressObjective(string questId, string objectiveTargetId, int count = 1)
    {
        if (!_progress.TryGetValue(questId, out var p)) return;
        if (p.state != QuestState.Active) return;

        var quest = _allQuests[questId];
        if (quest.objectives == null) return;

        for (int i = 0; i < quest.objectives.Length; i++)
        {
            if (quest.objectives[i].targetId == objectiveTargetId)
            {
                p.objectiveProgress[i] = Mathf.Min(p.objectiveProgress[i] + count, quest.objectives[i].requiredCount);
            }
        }

        CheckCompletion(questId);
    }

    /// <summary>检查任务是否完成</summary>
    public bool CheckCompletion(string questId)
    {
        if (!_progress.TryGetValue(questId, out var p)) return false;
        if (p.state != QuestState.Active) return false;

        var quest = _allQuests[questId];
        if (quest.objectives == null) return false;

        bool allDone = true;
        for (int i = 0; i < quest.objectives.Length; i++)
        {
            if (p.objectiveProgress[i] < quest.objectives[i].requiredCount)
                allDone = false;
        }

        if (allDone) CompleteQuest(questId);
        return allDone;
    }

    /// <summary>完成任务</summary>
    public void CompleteQuest(string questId)
    {
        if (!_progress.TryGetValue(questId, out var p)) return;
        p.state = QuestState.Completed;
        _allQuests[questId].state = QuestState.Completed;

        var quest = _allQuests[questId];

        // 发放奖励
        if (quest.rewards != null)
        {
            foreach (var reward in quest.rewards)
                ApplyReward(reward);
        }
        if (quest.rewardGold > 0)
            GameManager.Instance.AddGold(quest.rewardGold);
        if (quest.rewardAffection != 0)
            EventBus.Publish(GameEvent.NPCAffectionChanged, quest.rewardAffection);

        Debug.Log($"任务完成: {quest.questName}");
        EventBus.Publish(GameEvent.PlayerInteracted, $"quest_complete_{questId}");

        // 完成后再检查解锁
        CheckUnlocks();
    }

    private void ApplyReward(QuestReward reward)
    {
        switch (reward.type)
        {
            case QuestRewardType.Item:
                InventoryManager.Instance?.AddItem(reward.rewardId, reward.amount);
                break;
            case QuestRewardType.Recipe:
                InventoryManager.Instance?.AddItem(reward.rewardId, 1);
                EventBus.Publish(GameEvent.PlayerInteracted, $"learn_recipe_{reward.rewardId}");
                break;
            case QuestRewardType.Gold:
                GameManager.Instance.AddGold(reward.amount);
                break;
            case QuestRewardType.Perk:
            case QuestRewardType.Furniture:
            case QuestRewardType.Equipment:
                InventoryManager.Instance?.AddItem(reward.rewardId, 1);
                break;
            case QuestRewardType.Affection:
                EventBus.Publish(GameEvent.NPCAffectionChanged, reward.amount);
                break;
            case QuestRewardType.Unlock:
                EventBus.Publish(GameEvent.PlayerInteracted, $"unlock_{reward.rewardId}");
                break;
        }
    }

    private void OnDayChanged(int day)
    {
        CheckUnlocks();
    }

    private void OnAffectionChanged(int change)
    {
        CheckUnlocks();
    }

    /// <summary>检查并解锁新任务</summary>
    public void CheckUnlocks()
    {
        foreach (var kv in _allQuests)
        {
            if (_progress[kv.Key].state != QuestState.Locked) continue;
            if (CanUnlock(kv.Value))
            {
                _progress[kv.Key].state = QuestState.Available;
                kv.Value.state = QuestState.Available;
            }
        }
    }

    private bool CanUnlock(QuestData quest)
    {
        int day = GameManager.Instance.day;
        int season = GameManager.Instance.season;

        // 天数检查
        if (quest.requiredDay > 0 && day < quest.requiredDay) return false;
        // 季节检查
        if (quest.requiredSeason >= 0 && season != quest.requiredSeason) return false;
        // 前置任务检查
        if (quest.prerequisiteQuests != null)
        {
            foreach (var preId in quest.prerequisiteQuests)
                if (GetState(preId) != QuestState.Completed) return false;
        }

        return true;
    }

    /// <summary>获取存档数据</summary>
    public QuestSaveData GetSaveData()
    {
        return new QuestSaveData { quests = _progress.Values.ToList() };
    }

    /// <summary>加载存档数据</summary>
    public void LoadSaveData(QuestSaveData data)
    {
        if (data?.quests == null) return;
        foreach (var p in data.quests)
        {
            if (_progress.ContainsKey(p.questId))
            {
                _progress[p.questId] = p;
                if (_allQuests.ContainsKey(p.questId))
                    _allQuests[p.questId].state = p.state;
            }
        }
    }
}
