using UnityEngine;

/// <summary>
/// 鱼种稀有度 — 对应 GDD 5.7.2
/// </summary>
public enum FishRarity
{
    Common,     // 普通鱼
    Uncommon,   // 罕见鱼
    Rare,       // 稀有鱼
    Legendary   // 传说鱼（5条：四季各1+火山1）
}

/// <summary>
/// 钓点类型 — 对应 GDD 5.7.1
/// </summary>
public enum FishLocation
{
    Sea,          // 海边（沙滩岸、礁石、栈桥）
    Lake,         // 湖泊（镇中湖、森林湖）
    River,        // 小河（上游、中游、下游）
    SnowMountain, // 雪山（冰窟冬季冰面打洞）
    Volcano       // 火山（熔岩塘，需防火药水）
}

/// <summary>
/// 鱼种数据 — 对应 GDD 5.7
/// Unity 中创建为 ScriptableObject：Assets → Create → Fishing → Fish Data
/// </summary>
[CreateAssetMenu(menuName = "Fishing/Fish Data", fileName = "NewFish")]
public class FishData : ScriptableObject
{
    [Header("基本信息")]
    public string fishId;
    public string fishName;
    [TextArea(1, 3)] public string description;
    public FishRarity rarity;
    public FishLocation location;
    public Sprite icon;

    [Header("出现条件")]
    [Tooltip("出现季节（-1=全年, 0=春 1=夏 2=秋 3=冬）")]
    public int[] validSeasons = { -1 };
    [Tooltip("出现时段（0=清晨 1=上午 2=中午 3=下午 4=傍晚 5=夜晚 6=深夜, -1=全天）")]
    public int[] validTimeSlots = { -1 };
    [Tooltip("出现天气（-1=全天气, 0=晴 1=多云 2=小雨 3=大雨 4=雷暴 5=大风 6=大雪）")]
    public int[] validWeathers = { -1 };

    [Header("QTE 参数")]
    [Tooltip("QTE 持续秒数：普通3s/稀有6s/传说12s")]
    public float qteDuration = 3f;
    [Tooltip("咬钩速度修正（1.0=基准, >1=更快）")]
    public float biteSpeed = 1.0f;
    [Tooltip("QTE 难度 1-10（鱼窜动频率/幅度）")]
    [Range(1, 10)] public int difficulty = 3;

    [Header("经济")]
    public int basePrice = 30;
    [Tooltip("食用恢复体力")]
    public int energyRestore = 10;

    [Header("特殊")]
    public bool isLegendFish;
    [Tooltip("龙线索（如潮鸣龙/霜翼龙线索）")]
    [TextArea(1, 2)] public string questHint;

    // ==================== 工具方法 ====================

    /// <summary>该鱼种在指定季节是否出现</summary>
    public bool IsValidSeason(int season)
    {
        if (validSeasons == null || validSeasons.Length == 0) return true;
        foreach (int s in validSeasons)
            if (s == -1 || s == season) return true;
        return false;
    }

    /// <summary>该鱼种在指定时段是否出现</summary>
    public bool IsValidTimeSlot(int timeSlot)
    {
        if (validTimeSlots == null || validTimeSlots.Length == 0) return true;
        foreach (int t in validTimeSlots)
            if (t == -1 || t == timeSlot) return true;
        return false;
    }

    /// <summary>该鱼种在指定天气是否出现</summary>
    public bool IsValidWeather(int weatherIndex)
    {
        if (validWeathers == null || validWeathers.Length == 0) return true;
        foreach (int w in validWeathers)
            if (w == -1 || w == weatherIndex) return true;
        return false;
    }

    /// <summary>获取 QTE 默认持续时长（按稀有度）</summary>
    public static float GetDefaultQTEDuration(FishRarity rarity)
    {
        return rarity switch
        {
            FishRarity.Common   => 3f,
            FishRarity.Uncommon => 4f,
            FishRarity.Rare     => 6f,
            FishRarity.Legendary => 12f,
            _ => 3f
        };
    }
}
