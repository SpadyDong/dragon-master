using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 菜市场管理器 — GDD 5.9
/// 每日价格公告板 + 七类商品波动 + 供需下跌 + 经营者折扣
/// </summary>
public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    /// <summary>每日价格刷新时刻</summary>
    private const int PRICE_REFRESH_HOUR = 6;

    /// <summary>供需下跌阈值（当日抛售超过此数量开始下跌）</summary>
    private const int SUPPLY_DROP_THRESHOLD = 50;
    /// <summary>每超过阈值多少份跌一次</summary>
    private const int SUPPLY_DROP_STEP = 10;
    /// <summary>每次下跌幅度</summary>
    private const float SUPPLY_DROP_PERCENT = 0.03f;
    /// <summary>最低跌至基准价的百分比</summary>
    private const float MIN_PRICE_PERCENT = 0.6f;

    /// <summary>当日各品类价格倍率（基准 1.0）</summary>
    private Dictionary<MarketCategory, float> _categoryMultiplier = new();
    /// <summary>当日各商品抛售数量（供需下跌）</summary>
    private Dictionary<string, int> _dailySoldCount = new();

    /// <summary>经营者（沈大叔/沈大娘）对主角的好感度 — 玩家好感系统（M6）实现后注入</summary>
    private int _keeperAffection = 0;

    /// <summary>设置经营者好感度（由玩家好感系统调用）</summary>
    public void SetKeeperAffection(int affection) => _keeperAffection = affection;

    /// <summary>上次刷新价格的游戏日</summary>
    private int _lastRefreshDay = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.HourChanged, OnHourChanged);
        // 初始化当日价格
        RefreshDailyPrices();
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.HourChanged, OnHourChanged);
    }

    // ==================== 每日价格刷新 ====================

    void OnHourChanged(int hour)
    {
        // 跨天检测 + 06:00 刷新
        int currentDay = GameManager.Instance != null ? GameManager.Instance.day : 0;
        if (hour == PRICE_REFRESH_HOUR && currentDay != _lastRefreshDay)
        {
            RefreshDailyPrices();
        }
    }

    /// <summary>刷新当日价格（06:00 调用）</summary>
    public void RefreshDailyPrices()
    {
        _categoryMultiplier.Clear();
        _dailySoldCount.Clear();

        foreach (MarketCategory category in System.Enum.GetValues(typeof(MarketCategory)))
        {
            float fluctuation = MarketCategoryUtils.GetFluctuation(category);
            // 波动范围：1-fluctuation ~ 1+fluctuation
            float multiplier = 1f + Random.Range(-fluctuation, fluctuation);
            _categoryMultiplier[category] = multiplier;
        }

        _lastRefreshDay = GameManager.Instance != null ? GameManager.Instance.day : 0;

        EventBus.Publish(GameEvent.MarketPriceUpdated);
        Debug.Log("菜市场价格已刷新（06:00）");
    }

    // ==================== 价格查询 ====================

    /// <summary>获取商品品类</summary>
    public MarketCategory GetCategory(string itemId)
    {
        var item = Resources.Load<ItemData>($"Items/{itemId}");
        return MarketCategoryUtils.GetCategoryForItem(item);
    }

    /// <summary>获取商品基准价（来自 ItemData.basePrice）</summary>
    public int GetBasePrice(string itemId)
    {
        var item = Resources.Load<ItemData>($"Items/{itemId}");
        return item != null ? item.basePrice : 10;
    }

    /// <summary>获取商品当日价格（含波动 + 供需修正）</summary>
    public int GetPrice(string itemId, MarketQuality quality = MarketQuality.Normal)
    {
        int basePrice = GetBasePrice(itemId);
        var category = GetCategory(itemId);

        float categoryMult = _categoryMultiplier.TryGetValue(category, out var m) ? m : 1.0f;
        float qualityMult = MarketCategoryUtils.GetQualityMultiplier(quality);
        float supplyMult = GetSupplyDemandMultiplier(itemId);

        int price = Mathf.RoundToInt(basePrice * categoryMult * qualityMult * supplyMult);
        return Mathf.Max(1, price);
    }

    /// <summary>供需下跌倍率（GDD 5.9.1）</summary>
    float GetSupplyDemandMultiplier(string itemId)
    {
        if (!_dailySoldCount.TryGetValue(itemId, out int sold)) return 1.0f;
        if (sold <= SUPPLY_DROP_THRESHOLD) return 1.0f;

        // 超过阈值后每 +10 份跌 3%
        int overThreshold = sold - SUPPLY_DROP_THRESHOLD;
        int steps = overThreshold / SUPPLY_DROP_STEP;
        float multiplier = Mathf.Max(MIN_PRICE_PERCENT, 1.0f - steps * SUPPLY_DROP_PERCENT);
        return multiplier;
    }

    /// <summary>获取当日价格快照（供 UI 面板）</summary>
    public Dictionary<MarketCategory, float> GetDailyBoard()
    {
        return new Dictionary<MarketCategory, float>(_categoryMultiplier);
    }

    /// <summary>获取指定品类当日波动倍率</summary>
    public float GetCategoryMultiplier(MarketCategory category)
    {
        return _categoryMultiplier.TryGetValue(category, out var m) ? m : 1.0f;
    }

    // ==================== 买卖 ====================

    /// <summary>经营者好感折扣（GDD 5.9.3）</summary>
    public float GetKeeperDiscount()
    {
        int affection = _keeperAffection;
        if (PlayerAffectionManager.Instance != null)
        {
            affection = Mathf.Max(
                affection,
                PlayerAffectionManager.Instance.GetAffection("shen_daniang"),
                PlayerAffectionManager.Instance.GetAffection("shen_dashu")
            );
        }

        if (affection >= 3000) return 0.15f;   // 挚爱：卖价+15%/买价-15%
        if (affection >= 1500) return 0.10f;   // +10%/-10%
        if (affection >= 500) return 0.05f;    // +5%/-5%
        return 0f;
    }

    /// <summary>出售物品（返回获得的金币）</summary>
    public int Sell(string itemId, int count = 1, MarketQuality quality = MarketQuality.Normal)
    {
        if (InventoryManager.Instance == null) return 0;
        if (!InventoryManager.Instance.RemoveItem(itemId, count)) return 0;

        int unitPrice = GetPrice(itemId, quality);
        float discount = GetKeeperDiscount();
        int total = Mathf.RoundToInt(unitPrice * count * (1f + discount));

        // 记录抛售（供需下跌）
        if (!_dailySoldCount.TryGetValue(itemId, out int sold))
            sold = 0;
        _dailySoldCount[itemId] = sold + count;

        GameManager.Instance?.AddGold(total);
        EventBus.Publish(GameEvent.ItemSold, itemId);
        return total;
    }

    /// <summary>购买种子（返回是否成功）</summary>
    public bool BuySeed(string itemId, int count = 1)
    {
        var item = Resources.Load<ItemData>($"Items/{itemId}");
        if (item == null) return false;

        int unitPrice = GetPrice(itemId);
        float discount = GetKeeperDiscount();
        int total = Mathf.RoundToInt(unitPrice * count * (1f - discount));

        if (GameManager.Instance != null && !GameManager.Instance.SpendGold(total)) return false;
        InventoryManager.Instance?.AddItem(itemId, count);

        EventBus.Publish(GameEvent.ItemBought, itemId);
        return true;
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class MarketSaveData
    {
        public int[] categoryKeys;    // (int)MarketCategory
        public float[] categoryMults;
        public List<string> soldItemIds;
        public List<int> soldCounts;
        public int lastRefreshDay;
    }

    public MarketSaveData GetSaveData()
    {
        var data = new MarketSaveData
        {
            categoryKeys = new int[_categoryMultiplier.Count],
            categoryMults = new float[_categoryMultiplier.Count],
            soldItemIds = new List<string>(),
            soldCounts = new List<int>(),
            lastRefreshDay = _lastRefreshDay
        };

        int i = 0;
        foreach (var kv in _categoryMultiplier)
        {
            data.categoryKeys[i] = (int)kv.Key;
            data.categoryMults[i] = kv.Value;
            i++;
        }
        foreach (var kv in _dailySoldCount)
        {
            data.soldItemIds.Add(kv.Key);
            data.soldCounts.Add(kv.Value);
        }
        return data;
    }

    public void LoadSaveData(MarketSaveData data)
    {
        if (data == null) return;

        _categoryMultiplier.Clear();
        if (data.categoryKeys != null)
        {
            for (int i = 0; i < data.categoryKeys.Length && i < data.categoryMults.Length; i++)
                _categoryMultiplier[(MarketCategory)data.categoryKeys[i]] = data.categoryMults[i];
        }

        _dailySoldCount.Clear();
        if (data.soldItemIds != null)
        {
            for (int i = 0; i < data.soldItemIds.Count && i < data.soldCounts.Count; i++)
                _dailySoldCount[data.soldItemIds[i]] = data.soldCounts[i];
        }

        _lastRefreshDay = data.lastRefreshDay;
    }
}
