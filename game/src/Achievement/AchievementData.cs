using UnityEngine;

/// <summary>
/// 成就类别 — GDD 7.6
/// </summary>
public enum AchievementCategory
{
    Farming,     // 种田类（作物/收获）
    Fishing,     // 钓鱼类（鱼类图鉴）
    Dragon,      // 驯龙类（龙种图鉴）
    Combat,      // 战斗类（击杀/击败）
    Social,      // 社交类（NPC好感）
    Milestone    // 里程碑（图鉴收集/剧情进度）
}

/// <summary>
/// 图鉴类型 — GDD 7.6
/// </summary>
public enum CollectionType
{
    Crop,      // 作物图鉴
    Fish,      // 鱼类图鉴
    Dragon,    // 龙种图鉴
    WildAnimal,// 野怪图鉴
    Equipment, // 装备图鉴
    NPC        // NPC档案
}

/// <summary>
/// 成就数据 — GDD 7.6
/// </summary>
[CreateAssetMenu(menuName = "Achievement/Achievement Data", fileName = "NewAchievement")]
public class AchievementData : ScriptableObject
{
    [Header("基本信息")]
    public string achievementId;
    public string achievementName;
    [TextArea(2, 4)]
    public string description;

    [Header("类型")]
    public AchievementCategory category;
    [Tooltip("计数类型（关联的 CollectionType 或自定义计数）")]
    public CollectionType collectionType;

    [Header("目标")]
    [Tooltip("目标计数（达成即解锁）")]
    public int targetCount = 10;

    [Header("奖励")]
    public int rewardGold;
    public string rewardItemId;
    public string rewardTitle;  // 专属称号
}
