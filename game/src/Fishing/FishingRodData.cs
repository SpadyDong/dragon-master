using UnityEngine;

/// <summary>
/// 鱼竿等级 — 对应 GDD 5.7.3
/// 竹→铁→金→龙脊，每级降低难度
/// </summary>
public enum RodTier
{
    Bamboo,       // 竹竿（初始）
    Iron,         // 铁竿
    Gold,         // 金竿
    DragonSpine   // 龙脊竿（终极）
}

/// <summary>
/// 鱼竿数据 — 对应 GDD 5.7.3
/// Unity 中创建为 ScriptableObject：Assets → Create → Fishing → Fishing Rod Data
/// </summary>
[CreateAssetMenu(menuName = "Fishing/Fishing Rod Data", fileName = "NewRod")]
public class FishingRodData : ScriptableObject
{
    [Header("基本信息")]
    public string rodId;
    public string rodName;
    public RodTier tier;
    [TextArea(1, 2)] public string description;
    public Sprite icon;

    [Header("QTE 修正")]
    [Tooltip("绿条大小加成（0.0=无加成, 0.5=绿条扩大50%）")]
    [Range(0f, 0.5f)] public float qteBarSizeBonus = 0f;
    [Tooltip("拉线速度加成（1.0=基准, >1=更快）")]
    public float reelingSpeed = 1.0f;
    [Tooltip("可捕获最大难度（超过此难度的鱼无法捕获）")]
    public int maxCatchDifficulty = 3;

    [Header("经济")]
    public int purchasePrice = 100;

    /// <summary>竿等级中文名</summary>
    public static string GetTierName(RodTier tier)
    {
        return tier switch
        {
            RodTier.Bamboo      => "竹竿",
            RodTier.Iron         => "铁竿",
            RodTier.Gold         => "金竿",
            RodTier.DragonSpine  => "龙脊竿",
            _ => "未知"
        };
    }
}
