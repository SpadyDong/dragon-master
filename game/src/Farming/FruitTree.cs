using UnityEngine;

/// <summary>
/// 果树种类
/// 对应 GDD 5.1.1：桃/樱桃/梨/山楂/栗子/核桃
/// </summary>
public enum FruitTreeType
{
    Peach,       // 桃树：春种初夏果，首果第2年，寿命30年
    Cherry,      // 樱桃：春种夏果，首果第2年，寿命25年
    Pear,        // 梨树：春种秋果，首果第3年，寿命40年
    Hawthorn,    // 山楂：秋种秋果，首果第3年，寿命35年
    Chestnut,    // 栗子：春种深秋果，首果第4年，寿命50年
    Walnut       // 核桃：春种晚秋果，首果第5年，寿命80年
}

/// <summary>
/// 果树系统
/// 2×2格占位，树龄逐年递增产果量，品质随树龄上升
/// 大雪20%果实震落，大风15%断枝次年减产
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FruitTree : MonoBehaviour
{
    [Header("树种")]
    [SerializeField] private FruitTreeType treeType = FruitTreeType.Peach;

    [Header("状态")]
    [SerializeField] private int treeAge = 0;          // 树龄（年）
    [SerializeField] private int daysToNextFruit;       // 距下次结果天数
    [SerializeField] private bool isHarvestable;       // 当前可收获
    [SerializeField] private CropQuality currentQuality = CropQuality.Normal;

    [Header("配置")]
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Sprite seedlingSprite;
    [SerializeField] private Sprite youngSprite;
    [SerializeField] private Sprite matureSprite;
    [SerializeField] private Sprite fruitingSprite;

    // 树种配置
    private static readonly TreeConfig[] Configs = {
        new() { type = FruitTreeType.Peach,    firstFruitYear = 2,  lifespan = 30, springSeason = 0, harvestSeason = 1, baseYield = 4 },
        new() { type = FruitTreeType.Cherry,   firstFruitYear = 2,  lifespan = 25, springSeason = 0, harvestSeason = 1, baseYield = 3 },
        new() { type = FruitTreeType.Pear,     firstFruitYear = 3,  lifespan = 40, springSeason = 0, harvestSeason = 2, baseYield = 6 },
        new() { type = FruitTreeType.Hawthorn, firstFruitYear = 3,  lifespan = 35, springSeason = 2, harvestSeason = 2, baseYield = 5 },
        new() { type = FruitTreeType.Chestnut, firstFruitYear = 4,  lifespan = 50, springSeason = 0, harvestSeason = 2, baseYield = 10 },
        new() { type = FruitTreeType.Walnut,   firstFruitYear = 5,  lifespan = 80, springSeason = 0, harvestSeason = 2, baseYield = 12 },
    };

    [System.Serializable]
    private struct TreeConfig
    {
        public FruitTreeType type;
        public int firstFruitYear;    // 首果年龄
        public int lifespan;         // 寿命
        public int springSeason;      // 种植季节
        public int harvestSeason;     // 收获季节
        public int baseYield;         // 第一年基础产量（后续递增）
    }

    private SpriteRenderer _sr;
    private bool _wasDamagedByWind;   // 前一年大风断枝标记 → 次年减产一季

    // 存档
    [System.Serializable]
    public struct FruitTreeSaveData
    {
        public int treeType;
        public int treeAge;
        public int daysToNextFruit;
        public bool isHarvestable;
        public int quality;
        public bool wasDamagedByWind;
    }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        FarmingManager.Instance?.RegisterFruitTree(this);
        UpdateVisual();
    }

    /// <summary>获取树种配置</summary>
    private TreeConfig GetConfig()
    {
        foreach (var cfg in Configs)
            if (cfg.type == treeType) return cfg;
        return Configs[0];
    }

    // ==================== 跨天推进 ====================

    /// <summary>跨天推进（由 FarmingManager 调用）</summary>
    public void AdvanceDay()
    {
        if (GameManager.Instance == null) return;

        var cfg = GetConfig();
        int currentSeason = GameManager.Instance.season;

        // 检查是否到了结果季节
        if (treeAge >= cfg.firstFruitYear && currentSeason == cfg.harvestSeason && !isHarvestable)
        {
            daysToNextFruit--;
            if (daysToNextFruit <= 0)
            {
                isHarvestable = true;
                EvaluateQuality(cfg);
                UpdateVisual();
            }
        }

        // 天气影响
        ApplyWeatherEffects();
    }

    /// <summary>跨年推进（由 SeasonChanged 触发，春季第一天 = 树龄+1）</summary>
    public void OnSpringStart()
    {
        treeAge++;
        var cfg = GetConfig();

        if (treeAge > cfg.lifespan)
        {
            // 树木死亡 — 清除
            Die();
            return;
        }

        // 重置当年结果状态
        isHarvestable = false;
        daysToNextFruit = 28; // 当季28天内结果

        // 清除大风断枝标记（减产一季后恢复）
        _wasDamagedByWind = false;

        UpdateVisual();
    }

    // ==================== 天气 ====================

    private void ApplyWeatherEffects()
    {
        if (GameManager.Instance == null) return;
        int weather = GameManager.Instance.weatherIndex;

        // 大雪：20% 果实震落损失
        if (weather == 6 && isHarvestable)
        {
            if (Random.value < 0.2f)
            {
                isHarvestable = false;
                Debug.Log($"{treeType} 果实被大雪震落");
            }
        }

        // 大风：15% 断枝 → 次年减产一季
        if (weather == 5)
        {
            if (Random.value < 0.15f)
            {
                _wasDamagedByWind = true;
                Debug.Log($"{treeType} 被大风断枝，次年减产");
            }
        }
    }

    // ==================== 收获 ====================

    /// <summary>收获果实</summary>
    public (string itemId, int count, CropQuality quality) Harvest()
    {
        if (!isHarvestable) return (null, 0, CropQuality.Normal);

        var cfg = GetConfig();

        // 计算产量：基础产量 × 树龄递增系数
        int ageMultiplier = Mathf.Clamp(treeAge / 5, 1, 4); // 第5年×2, 第10年×3, 第15年×4
        int yield = cfg.baseYield * ageMultiplier;

        // 大风断枝减产
        if (_wasDamagedByWind)
            yield = Mathf.Max(1, yield / 2);

        // 品质
        CropQuality quality = currentQuality;

        // 重置
        isHarvestable = false;
        UpdateVisual();

        // 获取果实物品ID
        string fruitId = GetFruitItemId();

        EventBus.Publish(GameEvent.FruitTreeHarvested, treeType.ToString());
        return (fruitId, yield, quality);
    }

    /// <summary>获取果实物品ID</summary>
    private string GetFruitItemId()
    {
        return treeType switch
        {
            FruitTreeType.Peach => "peach",
            FruitTreeType.Cherry => "cherry",
            FruitTreeType.Pear => "pear",
            FruitTreeType.Hawthorn => "hawthorn",
            FruitTreeType.Chestnut => "chestnut",
            FruitTreeType.Walnut => "walnut",
            _ => "unknown_fruit"
        };
    }

    // ==================== 品质 ====================

    private void EvaluateQuality(TreeConfig cfg)
    {
        // 第10年起稳定金星
        if (treeAge >= 10)
            currentQuality = CropQuality.Gold;
        // 第20年起概率出紫星
        else if (treeAge >= 20 && Random.value < 0.2f)
            currentQuality = CropQuality.Purple;
        else
            currentQuality = Random.value < 0.5f ? CropQuality.Silver : CropQuality.Normal;
    }

    // ==================== 视觉 ====================

    private void UpdateVisual()
    {
        if (_sr == null) return;

        var cfg = GetConfig();

        if (treeAge == 0)
            _sr.sprite = seedlingSprite;
        else if (treeAge < cfg.firstFruitYear)
            _sr.sprite = youngSprite;
        else if (isHarvestable && fruitingSprite != null)
            _sr.sprite = fruitingSprite;
        else
            _sr.sprite = matureSprite;
    }

    private void Die()
    {
        Debug.Log($"{treeType} 树龄已达 {treeAge}，寿命已尽");
        // 树木死亡 — 视觉变为枯木
        _sr.color = new Color(0.4f, 0.3f, 0.2f, 0.5f);
        isHarvestable = false;
    }

    // ==================== 属性 ====================

    public FruitTreeType TreeType => treeType;
    public int TreeAge => treeAge;
    public bool IsHarvestable => isHarvestable;
    public CropQuality CurrentQuality => currentQuality;

    /// <summary>获取 2×2 占位范围（供碰撞/建造检查用）</summary>
    public static Vector2Int[] GetOccupiedOffsets()
    {
        return new Vector2Int[] {
            new(0, 0), new(1, 0),
            new(0, 1), new(1, 1)
        };
    }

    /// <summary>获取禁止建造的遮挡范围（树坑周围1格）</summary>
    public static Vector2Int[] GetExclusionOffsets()
    {
        // 2×2 + 周围1格 = 4×4 去掉 2×2
        var list = new System.Collections.Generic.List<Vector2Int>();
        for (int x = -1; x <= 2; x++)
            for (int y = -1; y <= 2; y++)
            {
                if (x >= 0 && x <= 1 && y >= 0 && y <= 1) continue;
                list.Add(new Vector2Int(x, y));
            }
        return list.ToArray();
    }

    // ==================== 存档 ====================

    public FruitTreeSaveData GetSaveData()
    {
        return new FruitTreeSaveData
        {
            treeType = (int)treeType,
            treeAge = treeAge,
            daysToNextFruit = daysToNextFruit,
            isHarvestable = isHarvestable,
            quality = (int)currentQuality,
            wasDamagedByWind = _wasDamagedByWind
        };
    }

    public void LoadSaveData(FruitTreeSaveData data)
    {
        treeType = (FruitTreeType)data.treeType;
        treeAge = data.treeAge;
        daysToNextFruit = data.daysToNextFruit;
        isHarvestable = data.isHarvestable;
        currentQuality = (CropQuality)data.quality;
        _wasDamagedByWind = data.wasDamagedByWind;
        UpdateVisual();
    }
}
