/// <summary>
/// 菜市场商品品类 — 对应 GDD 5.9.1 七大类
/// </summary>
public enum MarketCategory
{
    Vegetable,   // 蔬菜（白菜/萝卜/茄子/辣椒/葱姜蒜等）
    Fruit,       // 水果（桃/梨/樱桃/山楂/栗/核桃）
    Meat,        // 肉类（猪牛羊鸡鸭鹅兔）
    EggMilk,     // 蛋奶（鸡蛋/鸭蛋/鹅蛋/牛乳/羊乳）
    Fish,        // 鱼获（60类鱼）
    Seed,        // 种子
    Artisan      // 工匠品（咸蛋/豆腐/腊肉/酒/油/布/酱等）
}

/// <summary>
/// 商品品质（市场口径 · 4档）— 对应 GDD 5.2.4
/// </summary>
public enum MarketQuality
{
    Normal = 0,  // 普通 ×1.0
    Silver = 1,  // 银 ×1.25
    Gold = 2,    // 金 ×1.5
    Purple = 3   // 紫 ×1.75
}

/// <summary>
/// 菜市场工具类 — 品类名称/波动率/品质映射
/// </summary>
public static class MarketCategoryUtils
{
    /// <summary>品类中文名</summary>
    public static string GetCategoryName(MarketCategory category) => category switch
    {
        MarketCategory.Vegetable => "蔬菜",
        MarketCategory.Fruit     => "水果",
        MarketCategory.Meat      => "肉类",
        MarketCategory.EggMilk   => "蛋奶",
        MarketCategory.Fish      => "鱼获",
        MarketCategory.Seed      => "种子",
        MarketCategory.Artisan   => "工匠品",
        _ => "?"
    };

    /// <summary>各品类基准波动率（GDD 5.9.1，0-1 小数）</summary>
    public static float GetFluctuation(MarketCategory category) => category switch
    {
        MarketCategory.Vegetable => 0.20f,  // ±20%
        MarketCategory.Fruit     => 0.25f,  // ±25%
        MarketCategory.Meat      => 0.18f,  // ±18%
        MarketCategory.EggMilk   => 0.15f,  // ±15%
        MarketCategory.Fish      => 0.25f,  // ±25%
        MarketCategory.Seed      => 0.10f,  // +10%（当季）
        MarketCategory.Artisan   => 0.15f,  // ±15%
        _ => 0.15f
    };

    /// <summary>品质倍率（GDD 5.2.4）</summary>
    public static float GetQualityMultiplier(MarketQuality quality) => quality switch
    {
        MarketQuality.Silver => 1.25f,
        MarketQuality.Gold   => 1.5f,
        MarketQuality.Purple => 1.75f,
        _ => 1.0f
    };

    /// <summary>品质中文名</summary>
    public static string GetQualityName(MarketQuality quality) => quality switch
    {
        MarketQuality.Silver => "银",
        MarketQuality.Gold   => "金",
        MarketQuality.Purple => "紫",
        _ => "普通"
    };

    /// <summary>装备品质 → 市场品质映射（6档 → 4档）</summary>
    public static MarketQuality FromEquipmentQuality(EquipmentQuality quality) => quality switch
    {
        EquipmentQuality.White  => MarketQuality.Normal,
        EquipmentQuality.Green  => MarketQuality.Silver,
        EquipmentQuality.Blue   => MarketQuality.Gold,
        EquipmentQuality.Purple => MarketQuality.Purple,
        EquipmentQuality.Orange => MarketQuality.Purple,
        EquipmentQuality.Red    => MarketQuality.Purple,
        _ => MarketQuality.Normal
    };

    /// <summary>根据物品类型推断所属品类</summary>
    public static MarketCategory GetCategoryForItem(ItemData item)
    {
        if (item == null) return MarketCategory.Vegetable;

        // 通过物品名称推断（简易规则，正式数据表可覆盖）
        string name = item.itemName;
        if (name.Contains("种子") || name.Contains("苗")) return MarketCategory.Seed;
        if (name.Contains("鱼") || name.Contains("虾") || name.Contains("蟹")) return MarketCategory.Fish;
        if (name.Contains("肉") || name.Contains("排") || name.Contains("腿")) return MarketCategory.Meat;
        if (name.Contains("蛋") || name.Contains("乳") || name.Contains("奶")) return MarketCategory.EggMilk;
        if (name.Contains("果") || name.Contains("桃") || name.Contains("梨") || name.Contains("栗")) return MarketCategory.Fruit;

        // 工匠品（酒/油/酱/布/咸蛋等）
        if (item.type == ItemType.Material) return MarketCategory.Artisan;

        return MarketCategory.Vegetable;
    }
}
