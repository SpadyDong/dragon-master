using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局地形管理器 — 对应 GDD 6.6
/// 通行检查、季节联动（冬季湖面结冰可通行）、天气联动（大雨破坏桥梁）、桥梁修复
/// </summary>
public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance { get; private set; }

    private List<TerrainTile> _allTerrainTiles = new();

    [Header("季节联动")]
    [Tooltip("冬季湖面结冰可通行")]
    [SerializeField] private bool winterFreezeWater = true;

    [Header("天气联动")]
    [Tooltip("大雨天气桥梁破坏概率")]
    [Range(0f, 1f)] [SerializeField] private float bridgeBreakChance = 0.05f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Subscribe<int>(GameEvent.SeasonChanged, OnSeasonChanged);

        // 注册所有地形格子
        _allTerrainTiles = new List<TerrainTile>(FindObjectsByType<TerrainTile>(FindObjectsSortMode.None));
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Unsubscribe<int>(GameEvent.SeasonChanged, OnSeasonChanged);
    }

    // ==================== 跨天 ====================

    private void OnDayChanged(int day)
    {
        // 天气联动：大雨可能破坏桥梁
        CheckWeatherBridgeDamage();
    }

    private void OnSeasonChanged(int season)
    {
        // 季节变化时通知地形更新
        // 冰面通行性由 TerrainTile.IsSeasonalPassable() 自动处理
        EventBus.Publish(GameEvent.TerrainChanged, season);
    }

    // ==================== 天气联动 ====================

    /// <summary>大雨天气可能破坏桥梁</summary>
    private void CheckWeatherBridgeDamage()
    {
        if (GameManager.Instance == null) return;
        int weather = GameManager.Instance.weatherIndex;

        // 大雨(3)可能破坏桥梁
        if (weather == 3)
        {
            foreach (var tile in _allTerrainTiles)
            {
                if (tile.IsBreakableBridge && !tile.IsBridgeBroken)
                {
                    if (Random.value < bridgeBreakChance)
                    {
                        tile.BreakBridge();
                        Debug.LogWarning($"桥梁被洪水破坏！位置: {tile.transform.position}");
                    }
                }
            }
        }
    }

    // ==================== 通行检查 ====================

    /// <summary>指定位置是否可通行</summary>
    public bool CanPass(Vector2 worldPos)
    {
        var tile = GetTerrainAt(worldPos);
        return tile == null || tile.IsPassable;
    }

    /// <summary>获取指定位置的移动消耗</summary>
    public float GetMovementCost(Vector2 worldPos)
    {
        var tile = GetTerrainAt(worldPos);
        return tile == null ? 1.0f : tile.MovementCost;
    }

    /// <summary>获取指定位置的 TerrainTile（通过 Collider2D 重叠检测）</summary>
    private TerrainTile GetTerrainAt(Vector2 worldPos)
    {
        foreach (var tile in _allTerrainTiles)
        {
            var col = tile.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldPos))
                return tile;
        }
        return null;
    }

    // ==================== 桥梁管理 ====================

    /// <summary>获取所有已损坏的桥梁</summary>
    public List<TerrainTile> GetBrokenBridges()
    {
        var result = new List<TerrainTile>();
        foreach (var tile in _allTerrainTiles)
            if (tile.IsBridgeBroken)
                result.Add(tile);
        return result;
    }

    /// <summary>修复指定位置的桥梁</summary>
    public bool RepairBridgeAt(Vector2 worldPos)
    {
        var tile = GetTerrainAt(worldPos);
        if (tile == null) return false;
        return tile.RepairBridge();
    }

    // ==================== 注册 ====================

    /// <summary>注册新地形格子</summary>
    public void RegisterTile(TerrainTile tile)
    {
        if (!_allTerrainTiles.Contains(tile))
            _allTerrainTiles.Add(tile);
    }

    /// <summary>注销地形格子</summary>
    public void UnregisterTile(TerrainTile tile)
    {
        _allTerrainTiles.Remove(tile);
    }

    // ==================== 季节通行查询 ====================

    /// <summary>当前是否冬季（湖面结冰可通行）</summary>
    public bool IsWinterFrozen()
    {
        if (!winterFreezeWater) return false;
        return GameManager.Instance != null && GameManager.Instance.season == 3;
    }

    // ==================== 存档 ====================

    public List<TerrainTile.TerrainSaveData> GetAllSaveData()
    {
        var result = new List<TerrainTile.TerrainSaveData>();
        foreach (var tile in _allTerrainTiles)
            result.Add(tile.GetSaveData());
        return result;
    }

    public void LoadAllSaveData(List<TerrainTile.TerrainSaveData> data)
    {
        if (data == null) return;
        foreach (var saveData in data)
        {
            // 通过位置匹配
            var tile = _allTerrainTiles.Find(t =>
                Mathf.Approximately(t.transform.position.x, saveData.posX) &&
                Mathf.Approximately(t.transform.position.y, saveData.posY));
            tile?.LoadSaveData(saveData);
        }
    }
}
