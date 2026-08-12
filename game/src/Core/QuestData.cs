using System.Collections.Generic;

/// <summary>任务状态</summary>
public enum QuestState { Locked, Available, Active, Completed }

/// <summary>任务类型</summary>
public enum QuestType { Side, Heart, Story, Daily }

/// <summary>任务奖励类型</summary>
public enum QuestRewardType { Item, Gold, Recipe, Perk, Furniture, Equipment, Affection, Unlock }

/// <summary>任务目标类型</summary>
public enum QuestObjectiveType { Collect, Deliver, TalkTo, Kill, ReachPlace, Wait, Craft, Fish }

[System.Serializable]
public class QuestObjective
{
    public QuestObjectiveType type;
    public string targetId;       // 物品ID/NPC ID/地点名/怪物ID
    public int requiredCount;
    public string description;    // "收集 5 块铜矿石"
}

[System.Serializable]
public class QuestReward
{
    public QuestRewardType type;
    public string rewardId;       // 物品ID/配方ID/家具ID/装备ID
    public int amount;
    public string description;    // "获得 铜锄头"
}

[System.Serializable]
public class QuestData
{
    public string questId;
    public string questName;
    public string npcId;             // 发布NPC
    public QuestType questType;
    public string description;       // 任务描述文本
    public string completionText;    // 完成时的对话

    // 触发条件
    public int requiredDay;
    public int requiredAffection;    // 好感度门槛
    public int requiredSeason;       // -1=不限, 0-3
    public string[] prerequisiteQuests; // 前置任务ID

    // 目标
    public QuestObjective[] objectives;
    public bool allObjectivesRequired = true;

    // 奖励
    public QuestReward[] rewards;
    public int rewardGold;
    public int rewardAffection;      // 好感度变化

    // 运行时状态（非序列化）
    [System.NonSerialized] public QuestState state = QuestState.Locked;
    [System.NonSerialized] public int[] objectiveProgress;
}

/// <summary>单个任务的运行时追踪</summary>
[System.Serializable]
public class QuestProgress
{
    public string questId;
    public QuestState state;
    public int[] objectiveProgress;

    public QuestProgress(string id, QuestState s, int objCount)
    {
        questId = id;
        state = s;
        objectiveProgress = new int[objCount];
    }
}

/// <summary>存档用</summary>
[System.Serializable]
public class QuestSaveData
{
    public List<QuestProgress> quests;
}
