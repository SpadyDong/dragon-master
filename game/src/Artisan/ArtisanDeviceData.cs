using UnityEngine;

/// <summary>
/// 工匠设备加工配方
/// </summary>
[System.Serializable]
public struct ArtisanRecipe
{
    [Tooltip("配方ID")]
    public string recipeId;
    [Tooltip("配方名称")]
    public string recipeName;

    [Header("投入")]
    [Tooltip("主要原料物品ID")]
    public string inputItemId;
    [Tooltip("主要原料数量")]
    public int inputCount;
    [Tooltip("额外材料（可选）")]
    public ArtisanMaterial[] extraMaterials;

    [Header("产出")]
    [Tooltip("产物物品ID")]
    public string outputItemId;
    [Tooltip("产物数量")]
    public int outputCount;

    [Header("加工")]
    [Tooltip("加工耗时（游戏天数）")]
    public int processDays;
    [Tooltip("限制季节（-1=全年, 0=春 1=夏 2=秋 3=冬）")]
    public int requiredSeason;
}

/// <summary>
/// 工匠设备额外材料
/// </summary>
[System.Serializable]
public struct ArtisanMaterial
{
    public string itemId;
    public int count;
}

/// <summary>
/// 工匠设备数据 — 对应 GDD 5.2.4
/// 8 种设备：酱缸/豆腐坊/磨坊/腊肉架/糖坊/老式织机/酿酒桶/榨油机
/// </summary>
[CreateAssetMenu(menuName = "Artisan/Device Data", fileName = "NewArtisanDevice")]
public class ArtisanDeviceData : ScriptableObject
{
    [Header("基本信息")]
    public string deviceId;
    public string deviceName;
    [TextArea(2, 4)]
    public string description;

    [Header("设备参数")]
    [Tooltip("设备倍率（GDD 5.2.4：酱缸×1.6-2.5 / 豆腐坊×1.7 / 磨坊×1.4 / 腊肉架×1.8 / 糖坊×2.0 / 织机×2.2 / 酿酒桶×2.5-3.0 / 榨油机×1.9）")]
    public float deviceMultiplier = 1.5f;

    [Header("产物品类")]
    [Tooltip("产物归属的菜市场品类")]
    public MarketCategory outputCategory = MarketCategory.Artisan;

    [Header("配方列表")]
    public ArtisanRecipe[] recipes;
}
