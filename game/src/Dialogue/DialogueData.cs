using System;
using System.Collections.Generic;

/// <summary>
/// 对话条件类型
/// </summary>
public enum DialogueConditionType
{
    None,
    AffectionAtLeast,      // 好感度 >=
    SeasonIs,              // 季节 ==
    WeatherIs,             // 天气 ==
    HourBetween,           // 小时范围
    HasItem,               // 持有物品
    QuestProgress,         // 任务进度
    HasPerk                // 拥有技能/专精
}

/// <summary>
/// 对话效果类型
/// </summary>
public enum DialogueEffectType
{
    None,
    ChangeAffection,       // 好感度变化
    GiveItem,              // 给予物品
    RemoveItem,            // 移除物品
    SetQuestProgress,      // 设置任务进度
    UnlockDialogue,        // 解锁新对话
    TriggerEvent           // 触发事件
}

[System.Serializable]
public class DialogueCondition
{
    public DialogueConditionType type;
    public string param;    // 条件参数（物品ID/任务ID等）
    public int value;       // 条件数值
    public int value2;      // 辅助数值

    public bool IsMet()
    {
        // 条件检查由 DialogueManager 统一处理
        return DialogueConditionChecker.Check(type, param, value, value2);
    }
}

[System.Serializable]
public class DialogueEffect
{
    public DialogueEffectType type;
    public string param;
    public int value;

    public void Apply()
    {
        DialogueEffectApplier.Apply(type, param, value);
    }
}

[System.Serializable]
public class DialogueChoice
{
    public string text;
    public string nextNodeId;
    public int affectionChange;
    public DialogueCondition[] conditions;  // 显示此选项需要满足的条件
    public DialogueEffect[] effects;        // 选择后的效果
}

[System.Serializable]
public class DialogueNode
{
    public string nodeId;
    public string speakerName;
    public List<string> lines;           // 此节点对话行
    public List<DialogueChoice> choices; // 选项（空=自动跳转）
    public string nextNodeId;            // 无选项时的下一节点
    public DialogueCondition[] conditions;
    public DialogueEffect[] effects;
}

/// <summary>
/// 对话树 — Unity 中创建为 ScriptableObject
/// </summary>
[System.Serializable]
public class DialogueTreeData
{
    public string dialogueId;
    public string npcId;
    public List<DialogueNode> nodes;
}

/// <summary>
/// 对话条件检查器（静态方法）
/// </summary>
public static class DialogueConditionChecker
{
    public static bool Check(DialogueConditionType type, string param, int value, int value2)
    {
        if (type == DialogueConditionType.None) return true;

        switch (type)
        {
            case DialogueConditionType.HourBetween:
                return GameManager.Instance.hour >= value && GameManager.Instance.hour < value2;
            case DialogueConditionType.SeasonIs:
                return GameManager.Instance.season == value;
            case DialogueConditionType.WeatherIs:
                return GameManager.Instance.weatherIndex == value;
            case DialogueConditionType.AffectionAtLeast:
                // param = npcId, value = 阈值
                return PlayerAffectionManager.Instance != null &&
                       PlayerAffectionManager.Instance.GetAffection(param) >= value;
            default:
                return true; // 临时通过所有未实现条件
        }
    }
}

/// <summary>
/// 对话效果应用器（静态方法）
/// </summary>
public static class DialogueEffectApplier
{
    public static void Apply(DialogueEffectType type, string param, int value)
    {
        switch (type)
        {
            case DialogueEffectType.None:
                break;
            case DialogueEffectType.ChangeAffection:
                // param = npcId, value = 变化量
                if (PlayerAffectionManager.Instance != null)
                    PlayerAffectionManager.Instance.ChangeAffection(param, value);
                else
                    EventBus.Publish(GameEvent.NPCAffectionChanged, value);
                break;
            case DialogueEffectType.TriggerEvent:
                EventBus.Publish(GameEvent.PlayerInteracted, param);
                break;
        }
    }
}
