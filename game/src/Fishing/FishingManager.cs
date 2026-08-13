using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局钓鱼管理器 — 对应 GDD 5.7
/// 维护鱼种数据库，天气修正（GDD 4.3.3），每日鱼种刷新，图鉴解锁，统计
/// </summary>
public class FishingManager : MonoBehaviour
{
    public static FishingManager Instance { get; private set; }

    [Header("鱼竿装备")]
    [Tooltip("当前装备的鱼竿（默认竹竿）")]
    [SerializeField] private FishingRodData currentRod;

    [Header("天气修正 — GDD 4.3.3")]
    [SerializeField] private float sunnyRareBonus = 0f;
    [SerializeField] private float lightRainRareBonus = 0.20f;
    [SerializeField] private float thunderRareBonus = 0.50f;
    [SerializeField] private float lightSnowRareBonus = 0.10f;
    [SerializeField] private float heavySnowRareBonus = -0.30f;
    [SerializeField] private float windyRareBonus = -0.10f;
    [SerializeField] private float foggyRareBonus = 0.30f;

    [SerializeField] private float sunnyQTEMod = 1.0f;
    [SerializeField] private float lightRainQTEMod = 0.80f;
    [SerializeField] private float thunderQTEMod = 1.50f;
    [SerializeField] private float lightSnowQTEMod = 1.10f;
    [SerializeField] private float heavySnowQTEMod = 1.30f;
    [SerializeField] private float windyQTEMod = 1.20f;
    [SerializeField] private float foggyQTEMod = 1.0f;

    // 鱼种数据库
    private FishData[] _allFishData;

    // 图鉴解锁状态
    private HashSet<string> _caughtFishIds = new();

    // 统计
    private int _totalCaught;
    private int _rareCaught;
    private int _legendaryCaught;

    // 所有钓点
    private List<FishingSpot> _allSpots = new();

    // 存档
    [System.Serializable]
    public struct FishingSaveData
    {
        public string currentRodId;
        public List<string> caughtFishIds;
        public int totalCaught;
        public int rareCaught;
        public int legendaryCaught;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);

        // 加载所有鱼种数据
        _allFishData = Resources.LoadAll<FishData>("Fishes");

        // 注册所有钓点
        _allSpots = new List<FishingSpot>(FindObjectsByType<FishingSpot>(FindObjectsSortMode.None));
        for (int i = 0; i < _allSpots.Count; i++)
        {
            _allSpots[i].SetSpotId(i);
            _allSpots[i].RefreshAvailableFish();
        }
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    // ==================== 跨天 ====================

    private void OnDayChanged(int day)
    {
        // 每日刷新所有钓点的可用鱼种
        foreach (var spot in _allSpots)
            spot.RefreshAvailableFish();
    }

    // ==================== 天气修正 — GDD 4.3.3 ====================

    /// <summary>获取天气对稀有鱼出现概率的加成</summary>
    public float GetRarityBonus(int weatherIndex)
    {
        return weatherIndex switch
        {
            0 => sunnyRareBonus,         // 晴：基准
            1 => 0f,                      // 多云
            2 => lightRainRareBonus,     // 小雨 +20%
            3 => lightRainRareBonus * 0.5f, // 大雨（折半）
            4 => thunderRareBonus,       // 雷暴 +50%
            5 => windyRareBonus,         // 大风 -10%
            6 => heavySnowRareBonus,     // 大雪 -30%
            _ => 0f
        };
    }

    /// <summary>获取天气对 QTE 难度的修正倍率</summary>
    public float GetQTEDifficultyModifier(int weatherIndex)
    {
        return weatherIndex switch
        {
            0 => sunnyQTEMod,            // 晴：标准
            1 => 1.0f,                    // 多云
            2 => lightRainQTEMod,        // 小雨 -20%
            3 => 1.1f,                    // 大雨略增
            4 => thunderQTEMod,         // 雷暴 +50%
            5 => windyQTEMod,            // 大风 +20%
            6 => heavySnowQTEMod,        // 大雪 +30%
            _ => 1.0f
        };
    }

    /// <summary>雾天稀有鱼加成</summary>
    public float GetFogRarityBonus()
    {
        // 雾天不在 WeatherNames 中单独列出，此处预留
        return foggyRareBonus;
    }

    // ==================== 鱼捕获 ====================

    /// <summary>鱼被钓到时记录</summary>
    public void OnFishCaught(FishData fish)
    {
        _totalCaught++;

        if (fish.rarity == FishRarity.Rare)
            _rareCaught++;
        else if (fish.rarity == FishRarity.Legendary)
            _legendaryCaught++;

        // 图鉴解锁
        bool isNew = _caughtFishIds.Add(fish.fishId);
        if (isNew)
        {
            Debug.Log($"新鱼种图鉴解锁: {fish.fishName} ({fish.rarity})");
        }
    }

    // ==================== 鱼竿 ====================

    /// <summary>当前装备的鱼竿</summary>
    public FishingRodData GetCurrentRod() => currentRod;

    /// <summary>切换鱼竿</summary>
    public void SetCurrentRod(FishingRodData rod)
    {
        currentRod = rod;
    }

    // ==================== 查询 ====================

    /// <summary>获取所有鱼种数据</summary>
    public FishData[] GetAllFishData() => _allFishData;

    /// <summary>获取已捕获的鱼种ID集合</summary>
    public HashSet<string> GetCaughtFishIds() => _caughtFishIds;

    /// <summary>是否已捕获指定鱼种</summary>
    public bool HasCaught(string fishId) => _caughtFishIds.Contains(fishId);

    /// <summary>图鉴完成度 (0-1)</summary>
    public float GetCompletionRate()
    {
        if (_allFishData == null || _allFishData.Length == 0) return 0f;
        return (float)_caughtFishIds.Count / _allFishData.Length;
    }

    /// <summary>获取所有钓点</summary>
    public List<FishingSpot> GetAllSpots() => _allSpots;

    /// <summary>获取指定类型的钓点</summary>
    public List<FishingSpot> GetSpotsByLocation(FishLocation location)
    {
        var result = new List<FishingSpot>();
        foreach (var spot in _allSpots)
            if (spot.Location == location)
                result.Add(spot);
        return result;
    }

    /// <summary>获取指定ID的鱼种数据</summary>
    public FishData GetFishData(string fishId)
    {
        if (_allFishData == null) return null;
        foreach (var fish in _allFishData)
            if (fish.fishId == fishId) return fish;
        return null;
    }

    // ==================== 统计 ====================

    public int TotalCaught => _totalCaught;
    public int RareCaught => _rareCaught;
    public int LegendaryCaught => _legendaryCaught;

    // ==================== 存档 ====================

    public FishingSaveData GetSaveData()
    {
        return new FishingSaveData
        {
            currentRodId = currentRod != null ? currentRod.rodId : "",
            caughtFishIds = new List<string>(_caughtFishIds),
            totalCaught = _totalCaught,
            rareCaught = _rareCaught,
            legendaryCaught = _legendaryCaught
        };
    }

    public void LoadSaveData(FishingSaveData data)
    {
        if (!string.IsNullOrEmpty(data.currentRodId))
            currentRod = Resources.Load<FishingRodData>($"Rods/{data.currentRodId}");

        _caughtFishIds = new HashSet<string>(data.caughtFishIds ?? new List<string>());
        _totalCaught = data.totalCaught;
        _rareCaught = data.rareCaught;
        _legendaryCaught = data.legendaryCaught;
    }
}
