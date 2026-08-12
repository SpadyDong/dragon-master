using UnityEngine;

/// <summary>
/// 作物类别
/// </summary>
public enum CropCategory
{
    Vegetable,   // 蔬菜（白菜/萝卜/茄子/辣椒等）
    Grain,       // 谷物（稻/麦/粟/豆）
    Flower,      // 花卉
    Fruit        // 水果
}

/// <summary>
/// 作物品质分级
/// </summary>
public enum CropQuality
{
    Normal,   // 普通 ×1.0
    Silver,   // 银星 ×1.25
    Gold,     // 金星 ×1.5
    Purple    // 紫星 ×1.75
}

/// <summary>
/// 作物数据 — Unity 中创建为 ScriptableObject
/// 用法：Assets → Create → Farming → Crop Data
/// </summary>
[CreateAssetMenu(menuName = "Farming/Crop Data", fileName = "NewCrop")]
public class CropData : ScriptableObject
{
    [Header("基本信息")]
    public string cropId;
    public string cropName;
    [TextArea(1, 3)] public string description;
    public CropCategory category;
    public Sprite icon;

    [Header("季节")]
    [Tooltip("有效季节：0=春 1=夏 2=秋 3=冬。留空=四季皆可")]
    public int[] validSeasons = { 0, 1, 2, 3 };

    [Header("生长")]
    [Tooltip("从播种到成熟的总天数")]
    public int growthDaysTotal = 4;
    [Tooltip("生长阶段数（不含空地/锄地，含发芽和成熟）")]
    public int growthStages = 4;
    [Tooltip("各阶段外观精灵（长度=growthStages）")]
    public Sprite[] stageSprites;

    [Header("收获")]
    [Tooltip("收获产物物品 ID")]
    public string cropItemId;
    [Tooltip("种子物品 ID")]
    public string seedItemId;
    [Tooltip("最小收获数量")]
    public int yieldMin = 1;
    [Tooltip("最大收获数量")]
    public int yieldMax = 3;

    [Header("经济")]
    public int basePrice = 30;
    public int seedPrice = 10;

    [Header("品质与巨化")]
    [Tooltip("是否可能出现巨化作物")]
    public bool isGiantable = true;
    [Tooltip("巨化概率（连续晴天+金星品质时）")]
    [Range(0f, 1f)] public float giantChance = 0.005f;
    [Tooltip("巨化收获倍率")]
    public int giantYieldMultiplier = 5;

    /// <summary>品质倍率</summary>
    public static float GetQualityMultiplier(CropQuality quality)
    {
        return quality switch
        {
            CropQuality.Silver => 1.25f,
            CropQuality.Gold => 1.5f,
            CropQuality.Purple => 1.75f,
            _ => 1.0f
        };
    }

    /// <summary>品质中文名</summary>
    public static string GetQualityName(CropQuality quality)
    {
        return quality switch
        {
            CropQuality.Silver => "银星",
            CropQuality.Gold => "金星",
            CropQuality.Purple => "紫星",
            _ => "普通"
        };
    }

    /// <summary>根据品质计算售价</summary>
    public int GetSellPrice(CropQuality quality)
    {
        return Mathf.RoundToInt(basePrice * GetQualityMultiplier(quality));
    }

    /// <summary>该作物在指定季节是否可种植</summary>
    public bool IsValidSeason(int season)
    {
        if (validSeasons == null || validSeasons.Length == 0) return true;
        foreach (int s in validSeasons)
            if (s == season) return true;
        return false;
    }
}
