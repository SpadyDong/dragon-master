using UnityEngine;

/// <summary>
/// 七元素枚举
/// </summary>
public enum DragonElement
{
    Dendro,   // 草
    Pyro,     // 火
    Hydro,    // 水
    Geo,      // 岩
    Cryo,     // 冰
    Electro,  // 雷
    Anemo     // 风
}

/// <summary>
/// 龙成长阶段
/// </summary>
public enum DragonStage
{
    Egg,         // 龙蛋
    Juvenile,    // 幼年龙
    Adolescent,  // 青年龙（可出战）
    Adult        // 成年龙（可繁育）
}

/// <summary>
/// 龙性别
/// </summary>
public enum DragonGender
{
    Male,
    Female
}

/// <summary>
/// 元素工具类 — 克制/季节加成
/// </summary>
public static class DragonElementUtils
{
    /// <summary>同属性抗性 ×0.5</summary>
    public static float GetResistanceMultiplier(DragonElement atk, DragonElement def)
        => atk == def ? 0.5f : 1.0f;

    /// <summary>
    /// 季节属性加成
    /// 春:草+20%/风+15%  夏:火+25%/水+10%  秋:雷+20%/岩+10%  冬:冰+25%/火-10%
    /// </summary>
    public static float GetSeasonalBonus(DragonElement element, int season) => season switch
    {
        0 => element switch { DragonElement.Dendro => 0.20f, DragonElement.Anemo => 0.15f, _ => 0f },
        1 => element switch { DragonElement.Pyro => 0.25f, DragonElement.Hydro => 0.10f, _ => 0f },
        2 => element switch { DragonElement.Electro => 0.20f, DragonElement.Geo => 0.10f, _ => 0f },
        3 => element switch { DragonElement.Cryo => 0.25f, DragonElement.Pyro => -0.10f, _ => 0f },
        _ => 0f
    };

    public static string GetElementName(DragonElement e) => e switch
    {
        DragonElement.Dendro  => "草",
        DragonElement.Pyro    => "火",
        DragonElement.Hydro   => "水",
        DragonElement.Geo     => "岩",
        DragonElement.Cryo    => "冰",
        DragonElement.Electro => "雷",
        DragonElement.Anemo   => "风",
        _ => "?"
    };
}
