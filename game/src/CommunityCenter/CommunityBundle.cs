using UnityEngine;

/// <summary>
/// 社区中心收集包所需物品
/// </summary>
[System.Serializable]
public struct BundleItem
{
    public string itemId;
    public int count;
}

/// <summary>
/// 社区中心收集包 — 对应 GDD 7.3
/// 收集各季节/技能的套装奖励来修复议事大厅
/// </summary>
[CreateAssetMenu(menuName = "Community/Community Bundle", fileName = "NewBundle")]
public class CommunityBundle : ScriptableObject
{
    [Header("基本信息")]
    public string bundleId;
    public string bundleName;
    [TextArea(2, 4)]
    public string description;

    [Header("所需物品")]
    public BundleItem[] requiredItems;

    [Header("关联房间")]
    [Tooltip("完成此收集包后修复的房间ID（如 greenhouse/minecart/golden_clock）")]
    public string roomId;

    [Header("奖励")]
    public int rewardGold;
    public string rewardItemId;
    [Tooltip("全体 NPC 好感加成")]
    public int rewardAffection = 200;
}
