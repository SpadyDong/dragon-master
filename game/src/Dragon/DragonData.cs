using UnityEngine;

/// <summary>
/// 龙种稀有度
/// </summary>
public enum DragonRarity
{
    Common,   // 普通
    Rare      // 稀有
}

/// <summary>
/// 龙种数据 — 简化版
/// 同种龙属性固定，无 IV/EV 随机
/// 每种元素 3 种龙：2 普通 + 1 稀有
/// </summary>
[CreateAssetMenu(menuName = "Dragon/Dragon Data", fileName = "NewDragon")]
public class DragonData : ScriptableObject
{
    [Header("基本信息")]
    public string speciesId;
    public string speciesName;
    [TextArea(1, 3)] public string description;
    public DragonElement element;
    public DragonRarity rarity;

    [Tooltip("该龙种出现的有效季节（-1=全年）")]
    public int[] validSeasons = { -1 };

    [Tooltip("是否可能出现双属性个体（5%概率）")]
    public bool canBeDualElement = true;

    [Header("固定属性 — 同种龙完全相同")]
    public int baseAttack = 20;
    public int baseDefense = 15;
    public int baseAgility = 12;
    public int baseMaxHP = 120;

    [Header("成长天数阈值")]
    public int juvenileStartDay = 1;
    public int adolescentStartDay = 6;
    public int adultStartDay = 21;
    public int eggHatchDays = 3;

    [Header("阶段视觉缩放")]
    [Range(0.1f, 1.5f)] public float eggVisualScale = 0.35f;
    [Range(0.1f, 1.5f)] public float juvenileVisualScale = 0.55f;
    [Range(0.1f, 1.5f)] public float adolescentVisualScale = 0.78f;
    [Range(0.1f, 1.5f)] public float adultVisualScale = 1.0f;

    [Header("外观精灵")]
    public Sprite eggSprite;
    public Sprite juvenileSprite;
    public Sprite adolescentSprite;
    public Sprite adultSprite;

    [Header("捕捉")]
    [Range(1, 10)] public int captureDifficulty = 3;
    public string eggItemId;

    [Header("经济")]
    public int purchasePrice = 1000;

    // ==================== 工具方法 ====================

    public bool IsValidSeason(int season)
    {
        if (validSeasons == null || validSeasons.Length == 0) return true;
        foreach (int s in validSeasons)
            if (s == -1 || s == season) return true;
        return false;
    }

    public float GetVisualScale(DragonStage stage) => stage switch
    {
        DragonStage.Egg        => eggVisualScale,
        DragonStage.Juvenile   => juvenileVisualScale,
        DragonStage.Adolescent => adolescentVisualScale,
        DragonStage.Adult      => adultVisualScale,
        _ => 1.0f
    };

    public Sprite GetStageSprite(DragonStage stage) => stage switch
    {
        DragonStage.Egg        => eggSprite,
        DragonStage.Juvenile   => juvenileSprite,
        DragonStage.Adolescent => adolescentSprite,
        DragonStage.Adult      => adultSprite,
        _ => eggSprite
    };

    public DragonStage GetStageForGrowthDay(int growthDay)
    {
        if (growthDay < juvenileStartDay) return DragonStage.Egg;
        if (growthDay < adolescentStartDay) return DragonStage.Juvenile;
        if (growthDay < adultStartDay) return DragonStage.Adolescent;
        return DragonStage.Adult;
    }
}
