using UnityEngine;

/// <summary>
/// 龙装备子类型 — 对应 GDD 6.4.2
/// 龙鞍/龙鳞甲/龙饰必须匹配龙的主属性
/// </summary>
public enum DragonEquipmentSubType
{
    None,             // 非龙装备
    Saddle,           // 龙鞍 — 影响骑乘速度/跨越地形
    DragonArmor,      // 龙鳞甲 — 影响防御
    DragonAccessory   // 龙饰 — 附灵附加元素效果
}

/// <summary>
/// 龙装备工具类 — 元素匹配规则 GDD 6.4.2
/// </summary>
public static class DragonEquipmentUtils
{
    /// <summary>龙装备槽位数量</summary>
    public const int DRAGON_SLOT_COUNT = 3;

    /// <summary>龙装备槽位索引</summary>
    public static readonly DragonEquipmentSubType[] DragonSlots =
    {
        DragonEquipmentSubType.Saddle,
        DragonEquipmentSubType.DragonArmor,
        DragonEquipmentSubType.DragonAccessory
    };

    /// <summary>
    /// 检查龙装备是否与龙元素匹配 — GDD 6.4.2
    /// 匹配：可装备，元素技能正常
    /// 不匹配：龙心情下降，元素技能被禁用
    /// </summary>
    public static EquipmentMatchResult CheckElementMatch(ItemData item, DragonElement dragonElement)
    {
        if (item.type != ItemType.DragonEquipment) return EquipmentMatchResult.Incompatible;

        // 无元素装备 → 可装备
        if (!item.element.HasValue) return EquipmentMatchResult.Match;

        // 元素匹配
        if (item.element.Value == dragonElement) return EquipmentMatchResult.Match;

        // 不匹配但可装备（会有惩罚）
        return EquipmentMatchResult.WeaponMismatch;
    }

    /// <summary>验证物品是否为指定龙装备槽位</summary>
    public static bool IsValidForSlot(DragonEquipmentSubType slot, ItemData item)
    {
        if (item.type != ItemType.DragonEquipment) return false;
        return item.dragonEquipmentSubType == slot;
    }

    /// <summary>龙装备槽位中文名</summary>
    public static string GetSlotName(DragonEquipmentSubType slot) => slot switch
    {
        DragonEquipmentSubType.Saddle          => "龙鞍",
        DragonEquipmentSubType.DragonArmor      => "龙鳞甲",
        DragonEquipmentSubType.DragonAccessory  => "龙饰",
        _ => "未知"
    };

    /// <summary>根据龙装备子类型获取槽位索引</summary>
    public static int GetSlotIndex(DragonEquipmentSubType subType) => subType switch
    {
        DragonEquipmentSubType.Saddle          => 0,
        DragonEquipmentSubType.DragonArmor      => 1,
        DragonEquipmentSubType.DragonAccessory  => 2,
        _ => -1
    };
}
