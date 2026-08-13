using UnityEngine;

/// <summary>
/// 地形类型 — 对应 GDD 6.6
/// </summary>
public enum TerrainType
{
    Flat, Slope, Cliff, Beach, ShallowWater, DeepWater,
    Ice, Bridge, SnowMountain, Volcano, Lava
}

/// <summary>
/// 地形格子 — 对应 GDD 6.6
/// 管理通行性、移动消耗、特殊效果（悬崖掉血/雪山严寒/火山炎热/桥梁可破坏）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TerrainTile : MonoBehaviour
{
    [Header("地形类型")]
    [SerializeField] private TerrainType terrainType = TerrainType.Flat;

    [Header("通行性")]
    [SerializeField] private bool isPassable = true;
    [Tooltip("移动消耗倍率（1.0=基准, 1.3=上下坡, 0=不可通行）")]
    [SerializeField] private float movementCost = 1.0f;

    [Header("季节联动")]
    [Tooltip("是否受季节影响（如冰面仅冬季可通行）")]
    [SerializeField] private bool isSeasonal;
    [Tooltip("可通行的季节（0=春 1=夏 2=秋 3=冬, -1=全季）")]
    [SerializeField] private int[] passableSeasons = { -1 };

    [Header("特殊效果")]
    [Tooltip("悬崖坠落掉血量")]
    [SerializeField] private int cliffFallDamage = 30;
    [Tooltip("雪山严寒每分钟扣体力")]
    [SerializeField] private int snowMountainStaminaCost = 5;
    [Tooltip("火山炎热每分钟扣体力")]
    [SerializeField] private int volcanoStaminaCost = 5;
    [Tooltip("熔岩格扣血量")]
    [SerializeField] private int lavaDamage = 20;

    [Header("桥梁")]
    [Tooltip("是否为可破坏桥梁")]
    [SerializeField] private bool isBreakableBridge;
    [SerializeField] private bool isBridgeBroken;

    private float _effectTickTimer;
    private const float EFFECT_TICK_INTERVAL = 60f; // 每分钟扣一次

    // 存档
    [System.Serializable]
    public struct TerrainSaveData
    {
        public int terrainType;
        public bool isPassable;
        public bool isBridgeBroken;
        public float posX, posY;
    }

    /// <summary>地形类型</summary>
    public TerrainType TerrainType => terrainType;
    /// <summary>是否可通行</summary>
    public bool IsPassable => isPassable && !isBridgeBroken && IsSeasonalPassable();
    /// <summary>移动消耗</summary>
    public float MovementCost => IsPassable ? movementCost : 0f;
    /// <summary>是否为可破坏桥梁</summary>
    public bool IsBreakableBridge => isBreakableBridge;
    /// <summary>桥梁是否已损坏</summary>
    public bool IsBridgeBroken => isBridgeBroken;

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        // 检查玩家是否在此地形上
        ApplyTerrainEffects();
    }

    /// <summary>季节通行检查</summary>
    private bool IsSeasonalPassable()
    {
        if (!isSeasonal) return true;

        int season = GameManager.Instance != null ? GameManager.Instance.season : 0;
        if (passableSeasons == null || passableSeasons.Length == 0) return true;

        foreach (int s in passableSeasons)
            if (s == -1 || s == season) return true;
        return false;
    }

    /// <summary>应用地形特殊效果（当玩家在此地形上时）</summary>
    private void ApplyTerrainEffects()
    {
        if (PlayerController.Instance == null) return;

        // 简化版：检查玩家是否在此触发器内
        // 实际通过 OnTriggerEnter2D/OnTriggerExit2D 管理
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (PlayerStats.Instance == null) return;

        switch (terrainType)
        {
            case TerrainType.Cliff:
                // 悬崖坠落掉血
                PlayerStats.Instance.TakeDamage(cliffFallDamage);
                break;
            case TerrainType.Lava:
                // 熔岩扣血
                PlayerStats.Instance.TakeDamage(lavaDamage);
                break;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (PlayerStats.Instance == null) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        _effectTickTimer += Time.deltaTime;

        if (_effectTickTimer >= EFFECT_TICK_INTERVAL)
        {
            _effectTickTimer = 0f;

            switch (terrainType)
            {
                case TerrainType.SnowMountain:
                    // 严寒扣体力，如果有厚衣服减免（预留检查）
                    PlayerStats.Instance.ConsumeStamina(snowMountainStaminaCost);
                    break;
                case TerrainType.Volcano:
                    // 炎热扣体力，如果有防火药水减免（预留检查）
                    PlayerStats.Instance.ConsumeStamina(volcanoStaminaCost);
                    break;
            }
        }
    }

    // ==================== 桥梁修复 ====================

    /// <summary>破坏桥梁</summary>
    public void BreakBridge()
    {
        if (!isBreakableBridge) return;
        isBridgeBroken = true;
        EventBus.Publish(GameEvent.TerrainChanged, (int)terrainType);
    }

    /// <summary>修复桥梁 — 需消耗木材+石材</summary>
    public bool RepairBridge()
    {
        if (!isBridgeBroken) return false;

        // 检查材料（木材+石材）
        if (InventoryManager.Instance == null) return false;
        if (!InventoryManager.Instance.HasItem("wood", 5)) return false;
        if (!InventoryManager.Instance.HasItem("stone", 5)) return false;

        InventoryManager.Instance.RemoveItem("wood", 5);
        InventoryManager.Instance.RemoveItem("stone", 5);

        isBridgeBroken = false;
        EventBus.Publish(GameEvent.TerrainChanged, (int)terrainType);
        return true;
    }

    // ==================== 存档 ====================

    public TerrainSaveData GetSaveData()
    {
        return new TerrainSaveData
        {
            terrainType = (int)terrainType,
            isPassable = isPassable,
            isBridgeBroken = isBridgeBroken,
            posX = transform.position.x,
            posY = transform.position.y
        };
    }

    public void LoadSaveData(TerrainSaveData data)
    {
        terrainType = (TerrainType)data.terrainType;
        isPassable = data.isPassable;
        isBridgeBroken = data.isBridgeBroken;
    }
}
