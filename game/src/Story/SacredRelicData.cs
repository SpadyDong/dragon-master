using UnityEngine;

/// <summary>
/// 四圣物数据 — 对应 GDD 2.2 第二幕
/// </summary>
[CreateAssetMenu(menuName = "Story/Sacred Relic Data", fileName = "NewSacredRelic")]
public class SacredRelicData : ScriptableObject
{
    [Header("基本信息")]
    public string relicId;
    public string relicName;
    [TextArea(2, 4)]
    public string description;

    [Header("圣物类型")]
    public SacredRelic relicType;

    [Header("所属幕")]
    public StoryAct storyAct = StoryAct.Act2;

    [Header("守护BOSS")]
    [Tooltip("守护此圣物的 BOSS ID（炎心龙/沙虫王/深渊守护者等）")]
    public string requiredBossId;

    [Header("解锁条件")]
    [TextArea(1, 2)]
    public string unlockCondition;

    [Header("奖励")]
    [Tooltip("圣物对应的物品ID（可镶嵌/装备）")]
    public string rewardItemId;
}
