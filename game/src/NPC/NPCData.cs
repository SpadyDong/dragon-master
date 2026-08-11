using System;

/// <summary>
/// 礼物偏好等级
/// </summary>
public enum GiftTier { Hate = -30, Neutral = 0, Like = 40, Love = 80 }

/// <summary>
/// NPC ScriptableObject 配置数据
/// 在 Unity 中：右键 → Create → NPC → NPC Data
/// </summary>
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
    public string activity;      // "工作"/"闲逛"/"回家"/"就餐"/"睡觉"
    public bool isMoving;
    public float[] patrolX;
    public float[] patrolY;
}

// NPCData.cs — 在 Unity 中创建为 ScriptableObject
// 用法：Assets → Create → NPC → NPC Data
// 由于纯代码无法直接创建 SO，这里提供数据结构类供 NPCController 序列化使用

/// <summary>
/// NPC 数据容器（在 NPCController 上直接序列化，无需 SO）
/// </summary>
[System.Serializable]
public class NPCDataContainer
{
    public string npcId;
    public string npcName;
    public bool isMarriageable;
    public int birthMonth;        // 1-4 (春夏秋冬)
    public int birthDay;          // 1-28
    public GiftPreference[] giftPreferences;
    public NPCScheduleEntry[] defaultSchedule;
}
