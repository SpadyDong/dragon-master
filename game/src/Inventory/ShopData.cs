using UnityEngine;
using System;

/// <summary>
/// 商店类型 — 对应 GDD 6.4.4
/// </summary>
public enum ShopType
{
    WeaponShop,   // 武器商店（阿岚管理）— 武器/防具/饰品 + 锻造升级
    DragonShop,   // 龙之商店（磐石管理）— 龙鞍/龙鳞甲/龙饰 + 龙装备锻造
    GeneralShop   // 通用商店（杂货铺，后续 M10 用）
}

/// <summary>
/// 商店商品条目
/// </summary>
[Serializable]
public struct ShopItemEntry
{
    public string itemId;
    [Tooltip("购买价（0=非卖品）")]
    public int buyPrice;
    [Tooltip("出售价（0=不收购）")]
    public int sellPrice;
    [Tooltip("最低品质要求")]
    public EquipmentQuality minQuality;
    [Tooltip("是否有货上限（0=无限）")]
    public int stockLimit;
}

/// <summary>
/// 锻造配方 — GDD 6.4.4 获取与强化
/// </summary>
[Serializable]
public struct ForgeRecipe
{
    [Tooltip("产物物品ID")]
    public string resultItemId;
    [Tooltip("被升级/消耗的原料装备ID")]
    public string materialItemId;
    [Tooltip("额外材料ID列表")]
    public string[] extraMaterials;
    [Tooltip("额外材料数量列表（与extraMaterials一一对应）")]
    public int[] extraMaterialCounts;
    [Tooltip("锻造费（金币）")]
    public int forgeCost;
    [Tooltip("产物品质")]
    public EquipmentQuality resultQuality;
    [Tooltip("锻造配方名称")]
    public string recipeName;
}

/// <summary>
/// 商店数据 — ScriptableObject
/// 用法：Assets → Create → Shop → Shop Data
/// </summary>
[CreateAssetMenu(menuName = "Shop/Shop Data", fileName = "NewShop")]
public class ShopData : ScriptableObject
{
    [Header("基本信息")]
    public string shopId;
    public ShopType shopType;
    public string shopName;
    [TextArea(1, 3)]
    public string description;

    [Header("商品列表")]
    public ShopItemEntry[] items;

    [Header("锻造配方")]
    public ForgeRecipe[] forgeRecipes;

    /// <summary>根据 itemId 查找商品条目</summary>
    public ShopItemEntry? FindEntry(string itemId)
    {
        if (items == null) return null;
        foreach (var entry in items)
        {
            if (entry.itemId == itemId) return entry;
        }
        return null;
    }

    /// <summary>获取商品最终购买价（含品质倍率）</summary>
    public static int GetFinalBuyPrice(ShopItemEntry entry, ItemData item)
    {
        if (item == null || entry.buyPrice <= 0) return 0;
        return Mathf.RoundToInt(entry.buyPrice * item.QualityMultiplier);
    }

    /// <summary>获取物品出售价（基准价的60% × 品质倍率）</summary>
    public static int GetFinalSellPrice(ItemData item)
    {
        if (item == null) return 0;
        return Mathf.RoundToInt(item.basePrice * 0.6f * item.QualityMultiplier);
    }
}
