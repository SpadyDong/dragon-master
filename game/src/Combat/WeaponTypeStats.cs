/// <summary>
/// 武器类型战斗属性 — 对应 GDD 6.4.1
/// 单手剑：近战1格，攻速快，暴击率高
/// 双手剑：近战2格横扫，攻速慢，高攻击群伤
/// 弓箭：远程3-5格，先手，可打飞行
/// </summary>
public static class WeaponTypeStats
{
    /// <summary>攻击范围（格）</summary>
    public static int GetAttackRange(WeaponSubType type) => type switch
    {
        WeaponSubType.Sword       => 1,
        WeaponSubType.Greatsword  => 2,
        WeaponSubType.Bow         => 3,
        _ => 1
    };

    /// <summary>弓箭最大攻击距离</summary>
    public const int BOW_MAX_RANGE = 5;

    /// <summary>攻速倍率（1.0=基准）</summary>
    public static float GetAttackSpeed(WeaponSubType type) => type switch
    {
        WeaponSubType.Sword       => 1.2f,
        WeaponSubType.Greatsword  => 0.7f,
        WeaponSubType.Bow         => 1.0f,
        _ => 1.0f
    };

    /// <summary>暴击率加成（GDD: 单手剑+5%）</summary>
    public static float GetCritRateBonus(WeaponSubType type) => type switch
    {
        WeaponSubType.Sword       => 0.05f,
        WeaponSubType.Greatsword  => 0.02f,
        WeaponSubType.Bow         => 0.03f,
        _ => 0f
    };

    /// <summary>是否可打飞行目标</summary>
    public static bool CanHitFlying(WeaponSubType type) => type == WeaponSubType.Bow;

    /// <summary>是否横扫群伤</summary>
    public static bool IsCleave(WeaponSubType type) => type == WeaponSubType.Greatsword;

    /// <summary>是否先手（弓箭先手）</summary>
    public static bool IsFirstStrike(WeaponSubType type) => type == WeaponSubType.Bow;

    /// <summary>武器类型中文名</summary>
    public static string GetWeaponTypeName(WeaponSubType type) => type switch
    {
        WeaponSubType.Sword       => "单手剑",
        WeaponSubType.Greatsword  => "双手剑",
        WeaponSubType.Bow         => "弓箭",
        WeaponSubType.None        => "无",
        _ => "?"
    };

    /// <summary>武器类型描述</summary>
    public static string GetWeaponTypeDescription(WeaponSubType type) => type switch
    {
        WeaponSubType.Sword       => "近战1格 · 攻速快 · 暴击率高 · 全属性通用",
        WeaponSubType.Greatsword  => "近战2格横扫 · 攻速慢 · 高攻击群伤 · 适合火/土",
        WeaponSubType.Bow         => "远程3-5格 · 先手 · 可打飞行 · 适合风/水/冰",
        _ => ""
    };

    /// <summary>推荐元素（GDD 6.4.1）</summary>
    public static DragonElement[] GetRecommendedElements(WeaponSubType type)
    {
        return type switch
        {
            WeaponSubType.Sword       => new[] { DragonElement.Dendro, DragonElement.Pyro, DragonElement.Hydro, DragonElement.Geo, DragonElement.Cryo, DragonElement.Electro, DragonElement.Anemo },
            WeaponSubType.Greatsword  => new[] { DragonElement.Pyro, DragonElement.Geo },
            WeaponSubType.Bow         => new[] { DragonElement.Anemo, DragonElement.Hydro, DragonElement.Cryo },
            _ => System.Array.Empty<DragonElement>()
        };
    }
}
