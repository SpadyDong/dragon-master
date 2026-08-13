using UnityEngine;

/// <summary>
/// 物品类型
/// </summary>
public enum ItemType
{
    Consumable,        // 消耗品（食物、药水）
    Weapon,            // 武器
    Armor,             // 防具
    Accessory,         // 饰品
    Seed,              // 种子
    Tool,              // 工具（锄头、洒水器等）
    Material,          // 材料（矿石、木材等）
    Currency,          // 货币
    Gift,              // 礼物
    DragonEquipment    // 龙装备（龙鞍/龙鳞甲/龙饰）
}

/// <summary>
/// 武器子类型 — 决定装备到哪个武器槽
/// </summary>
public enum WeaponSubType
{
    None,        // 非武器
    Sword,       // 单手剑
    Greatsword,  // 大剑/双手剑
    Bow          // 弓箭
}

/// <summary>
/// 装备品质 — GDD 6.4.3 六档
/// </summary>
public enum EquipmentQuality
{
    White,    // 白（普通）
    Green,    // 绿（良好）
    Blue,     // 蓝（稀有）
    Purple,   // 紫（史诗）
    Orange,   // 橙（传说）
    Red       // 红（神话）
}

/// <summary>
/// 物品数据 — Unity 中创建为 ScriptableObject
/// 用法：Assets → Create → Item → Item Data
/// </summary>
[CreateAssetMenu(menuName = "Item/Item Data", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    [Header("基本信息")]
    public string itemId;
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public ItemType type;
    public int maxStack = 99;
    public int basePrice = 10;
    public bool isSellable = true;

    [Header("消耗品")]
    public int staminaRestore;
    public int hpRestore;
    public int hungerRestore;

    [Header("装备属性")]
    public int attackBonus;
    public int defenseBonus;
    public int speedBonus;
    public int magicBonus;

    [Header("武器")]
    public WeaponSubType weaponSubType = WeaponSubType.None;

    [Header("龙装备 — GDD 6.4.2")]
    [Tooltip("龙装备子类型（龙鞍/龙鳞甲/龙饰）")]
    public DragonEquipmentSubType dragonEquipmentSubType = DragonEquipmentSubType.None;

    [Header("元素与品质 — GDD 6.4.2 / 6.4.3")]
    [Tooltip("装备的元素属性（null=无元素，非武器/防具可留空）")]
    public DragonElement? element;
    [Tooltip("装备品质（仅武器/防具/饰品有效）")]
    public EquipmentQuality quality = EquipmentQuality.White;

    /// <summary>品质属性加成倍率（白1.0 → 红2.0）</summary>
    public float QualityMultiplier => quality switch
    {
        EquipmentQuality.White  => 1.0f,
        EquipmentQuality.Green  => 1.2f,
        EquipmentQuality.Blue   => 1.5f,
        EquipmentQuality.Purple => 1.8f,
        EquipmentQuality.Orange => 2.2f,
        EquipmentQuality.Red    => 2.8f,
        _ => 1.0f
    };

    /// <summary>品质中文名</summary>
    public static string GetQualityName(EquipmentQuality q) => q switch
    {
        EquipmentQuality.White  => "白",
        EquipmentQuality.Green  => "绿",
        EquipmentQuality.Blue   => "蓝",
        EquipmentQuality.Purple => "紫",
        EquipmentQuality.Orange => "橙",
        EquipmentQuality.Red    => "红",
        _ => "?"
    };

    /// <summary>品质颜色</summary>
    public static Color GetQualityColor(EquipmentQuality q) => q switch
    {
        EquipmentQuality.White  => new Color(0.8f, 0.8f, 0.8f),
        EquipmentQuality.Green  => new Color(0.3f, 0.8f, 0.3f),
        EquipmentQuality.Blue   => new Color(0.3f, 0.5f, 1.0f),
        EquipmentQuality.Purple => new Color(0.7f, 0.3f, 1.0f),
        EquipmentQuality.Orange => new Color(1.0f, 0.6f, 0.1f),
        EquipmentQuality.Red    => new Color(1.0f, 0.2f, 0.2f),
        _ => Color.white
    };

    /// <summary>含品质加成的实际攻击力</summary>
    public int EffectiveAttack => Mathf.RoundToInt(attackBonus * QualityMultiplier);

    /// <summary>含品质加成的实际防御力</summary>
    public int EffectiveDefense => Mathf.RoundToInt(defenseBonus * QualityMultiplier);
}
