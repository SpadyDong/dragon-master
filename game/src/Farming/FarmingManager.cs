using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局种植管理器
/// 维护所有 FarmTile，监听跨天事件推进生长，处理天气联动
/// </summary>
public class FarmingManager : MonoBehaviour
{
    public static FarmingManager Instance { get; private set; }

    private List<FarmTile> _allTiles = new();
    private List<Sprinkler> _sprinklers = new();
    private List<FruitTree> _fruitTrees = new();
    private List<Beehive> _beehives = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Subscribe<int>(GameEvent.SeasonChanged, OnSeasonChanged);

        // 注册所有已放置的地块
        FarmTile[] tiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].SetTileIndex(i);
            _allTiles.Add(tiles[i]);
        }

        // 注册洒水器
        _sprinklers = new List<Sprinkler>(FindObjectsByType<Sprinkler>(FindObjectsSortMode.None));

        // 注册果树
        _fruitTrees = new List<FruitTree>(FindObjectsByType<FruitTree>(FindObjectsSortMode.None));

        // 注册蜂箱
        _beehives = new List<Beehive>(FindObjectsByType<Beehive>(FindObjectsSortMode.None));
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Unsubscribe<int>(GameEvent.SeasonChanged, OnSeasonChanged);
    }

    /// <summary>注册新放置的地块</summary>
    public void RegisterTile(FarmTile tile)
    {
        if (!_allTiles.Contains(tile))
        {
            tile.SetTileIndex(_allTiles.Count);
            _allTiles.Add(tile);
        }
    }

    /// <summary>取消注册地块</summary>
    public void UnregisterTile(FarmTile tile)
    {
        _allTiles.Remove(tile);
    }

    /// <summary>注册洒水器</summary>
    public void RegisterSprinkler(Sprinkler sprinkler)
    {
        if (!_sprinklers.Contains(sprinkler))
            _sprinklers.Add(sprinkler);
    }

    /// <summary>注册果树</summary>
    public void RegisterFruitTree(FruitTree tree)
    {
        if (!_fruitTrees.Contains(tree))
            _fruitTrees.Add(tree);
    }

    /// <summary>注册蜂箱</summary>
    public void RegisterBeehive(Beehive hive)
    {
        if (!_beehives.Contains(hive))
            _beehives.Add(hive);
    }

    /// <summary>获取所有地块</summary>
    public List<FarmTile> GetAllTiles() => _allTiles;

    // ==================== 跨天推进 ====================

    private void OnDayChanged(int day)
    {
        // 1. 洒水器自动浇水（早晨6:00）
        foreach (var sprinkler in _sprinklers)
            sprinkler.ActivateWatering();

        // 2. 天气联动：雨天自动浇水
        ApplyWeatherWatering();

        // 3. 天气联动：雷暴冲毁 / 大雪冻伤
        ApplyWeatherDamage();

        // 4. 所有地块推进生长
        foreach (var tile in _allTiles)
            tile.AdvanceDay();

        // 5. 果树推进
        foreach (var tree in _fruitTrees)
            tree.AdvanceDay();

        // 6. 蜂箱推进
        foreach (var hive in _beehives)
            hive.AdvanceDay();
    }

    private void OnSeasonChanged(int season)
    {
        // 季节变化时的特殊处理由各 tile 的 AdvanceDay 中的过季检查覆盖
    }

    // ==================== 天气联动 ====================

    /// <summary>雨天自动浇水</summary>
    private void ApplyWeatherWatering()
    {
        if (GameManager.Instance == null) return;
        if (WeatherSystem.Instance == null) return;

        // 雨天：不需要人工浇水
        if (!WeatherSystem.Instance.NeedsWatering)
        {
            foreach (var tile in _allTiles)
                tile.AutoWater();
        }
    }

    /// <summary>雷暴冲毁低洼地块 / 大雪冻伤</summary>
    private void ApplyWeatherDamage()
    {
        if (GameManager.Instance == null) return;
        int weather = GameManager.Instance.weatherIndex;

        // 天气索引：5=雷暴
        if (weather == 4) // 雷暴
        {
            foreach (var tile in _allTiles)
            {
                if (tile.HasCrop && Random.value < 0.05f) // 5% 概率被冲毁
                {
                    tile.ForceWither();
                }
            }
        }
        // 天气索引：6=大雪
        else if (weather == 6) // 大雪
        {
            foreach (var tile in _allTiles)
            {
                if (tile.HasCrop)
                {
                    // 30% 概率冻伤（品质降1档）
                    if (Random.value < 0.3f)
                        tile.ApplyFrostDamage();
                }
            }
        }
    }

    // ==================== 查询 ====================

    /// <summary>获取指定位置附近的地块</summary>
    public FarmTile GetTileNearPosition(Vector3 worldPos, float maxDistance = 1f)
    {
        FarmTile nearest = null;
        float minDist = maxDistance;

        foreach (var tile in _allTiles)
        {
            float dist = Vector3.Distance(tile.transform.position, worldPos);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = tile;
            }
        }

        return nearest;
    }

    /// <summary>获取所有成熟的地块</summary>
    public List<FarmTile> GetMatureTiles()
    {
        var result = new List<FarmTile>();
        foreach (var tile in _allTiles)
            if (tile.State == FarmTileState.Mature)
                result.Add(tile);
        return result;
    }

    /// <summary>获取所有需要浇水的地块</summary>
    public List<FarmTile> GetUnwateredTiles()
    {
        var result = new List<FarmTile>();
        foreach (var tile in _allTiles)
            if (tile.HasCrop && !tile.IsWatered)
                result.Add(tile);
        return result;
    }
}
