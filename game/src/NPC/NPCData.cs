using System;
using System.Collections.Generic;

/// <summary>
/// 礼物偏好等级
/// </summary>
public enum GiftTier { Hate = -30, Neutral = 0, Like = 40, Love = 80 }

[System.Serializable]
public class GiftPreference
{
    public string itemId;
    public GiftTier tier;
    public int AffectionChange => (int)tier;
}

[System.Serializable]
public class NPCScheduleEntry
{
    public int startHour, startMinute;
    public int endHour, endMinute;
    public string locationName;
    public float targetX, targetY;
    public string activity;
    public bool isMoving;
    public float[] patrolX;
    public float[] patrolY;
}

/// <summary>
/// NPC 心事件数据（每个可结婚对象有 4 个心事件 N1-N4）
/// </summary>
[System.Serializable]
public class NPCHeartEvent
{
    public int eventIndex;            // 1-4 (N1-N4)
    public int requiredAffection;     // 触发所需好感度
    public string eventId;            // 对话树 ID
    public string description;        // 事件描述（"N1·训练场切磋"）
    public string locationHint;       // 触发位置提示
    public int[] requiredHourRange;   // [8,18] 触发时段
    public string[] prerequisites;    // 前置事件 ID（如需要 N2 先触发 N3）
    public bool requiresItem;         // 是否需要特定物品
    public string requiredItemId;     // 所需物品 ID
    public string[] requiredNPCAffection; // 需要某NPC对主角好感 >= 某值
}

/// <summary>
/// NPC 关系描述（用于 NPC-NPC 关系网）
/// </summary>
[System.Serializable]
public class NPCRelationship
{
    public string targetNpcId;
    public string relationshipType;  // "兄弟"/"兄妹"/"父子"/"闺蜜"/"好友"/"酒友"等
    public string description;       // "阿岚与小石相依为命"
    public int baseAffection;        // 基础互有好感值
}

/// <summary>
/// NPC 完整数据容器
/// </summary>
[System.Serializable]
public class NPCDataContainer
{
    // 基本信息
    public string npcId;
    public string npcName;
    public int age;
    public string gender;             // "男" / "女"
    public bool isMarriageable;

    // 性格与背景
    [TextArea(2, 6)] public string personality;
    [TextArea(3, 10)] public string background;
    [TextArea(1, 4)] public string hobbies;

    // 属性
    public string element;            // "风"/"火"/"水"/"土"/"冰"/"雷"
    public string weaponType;         // "单手剑"/"双手剑"/"弓箭"
    public string weaponName;         // 武器名（"月岚"/"岩碎"等）

    // 生日
    public int birthMonth;            // 1-4 (春夏秋冬)
    public int birthDay;              // 1-28

    // 住所
    public string residence;

    // 礼物偏好
    public GiftPreference[] giftPreferences;

    // 每日作息
    public NPCScheduleEntry[] defaultSchedule;

    // 关系网
    public NPCRelationship[] relationships;

    // 心事件（仅可结婚对象）
    public NPCHeartEvent[] heartEvents;

    // ---- 便捷字段 ----
    public string lovedItems;
    public string likedItems;
    public string hatedItems;

    /// <summary>获取指定好感度的生日字符串</summary>
    public string GetBirthdayString() => $"{(birthMonth <= 2 ? (birthMonth == 1 ? "春" : "夏") : (birthMonth == 3 ? "秋" : "冬"))}{birthDay}日";

    /// <summary>获取指定好感度的心事件</summary>
    public NPCHeartEvent GetHeartEvent(int index)
    {
        if (heartEvents == null) return null;
        foreach (var e in heartEvents)
            if (e.eventIndex == index) return e;
        return null;
    }
}
