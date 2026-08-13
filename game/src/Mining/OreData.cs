using UnityEngine;

/// <summary>
/// 矿石数据 — 对应 GDD 7.4 矿洞系统
/// </summary>
[CreateAssetMenu(menuName = "Mining/Ore Data", fileName = "NewOre")]
public class OreData : ScriptableObject
{
    [Header("基本信息")]
    public string oreId;
    public string oreName;
    public Sprite icon;
    [TextArea(1, 2)]
    public string description;

    [Header("分布")]
    [Tooltip("最小出现楼层")]
    public int minFloor = 1;
    [Tooltip("最大出现楼层")]
    public int maxFloor = 120;
    [Tooltip("每次挖矿掉落概率（0-1）")]
    [Range(0f, 1f)] public float dropChance = 0.5f;

    [Header("价值")]
    public int sellPrice = 10;
    [Tooltip("掉落对应的物品ID")]
    public string itemId;
    [Tooltip("采集熟练度 XP")]
    public int gatheringXP = 8;

    /// <summary>是否在指定楼层出现</summary>
    public bool CanAppear(int floor) => floor >= minFloor && floor <= maxFloor;
}
