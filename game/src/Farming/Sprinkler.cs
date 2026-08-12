using UnityEngine;

/// <summary>
/// 洒水器等级
/// 对应 GDD 5.1.2：铜制（十字4格）/ 铁制（3×3=8格）/ 金制（5×5=24格）
/// </summary>
public enum SprinklerTier
{
    Copper,   // 铜制：耕种 Lv2 解锁，十字4格（上/下/左/右）
    Iron,     // 铁制：耕种 Lv5 解锁，3×3共8格
    Gold      // 金制：耕种 Lv8 解锁，5×5共24格
}

/// <summary>
/// 洒水器 — 每日早晨自动浇水覆盖范围内的地块
/// 大风天气效率 ×0.6（水被吹散，覆盖范围缩小）
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Sprinkler : MonoBehaviour
{
    [Header("洒水器设置")]
    [SerializeField] private SprinklerTier tier = SprinklerTier.Copper;
    [SerializeField] private float tileSize = 1f;

    [Header("灵石充能")]
    [Tooltip("是否需要灵石充能（铁制及以上）")]
    [SerializeField] private bool requiresCharging = false;
    [SerializeField] private int chargesRemaining = 0;

    private static readonly Vector2Int[] CopperOffsets = {
        new(0, 0),   // 自身
        new(0, 1),   // 上
        new(0, -1),  // 下
        new(-1, 0),  // 左
        new(1, 0)    // 右
    };

    private static readonly Vector2Int[] IronOffsets = new Vector2Int[9];
    private static readonly Vector2Int[] GoldOffsets = new Vector2Int[25];

    static Sprinkler()
    {
        // 铁制 3×3
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                IronOffsets[(x + 1) * 3 + (y + 1)] = new Vector2Int(x, y);

        // 金制 5×5
        for (int x = -2; x <= 2; x++)
            for (int y = -2; y <= 2; y++)
                GoldOffsets[(x + 2) * 5 + (y + 2)] = new Vector2Int(x, y);
    }

    void Start()
    {
        FarmingManager.Instance?.RegisterSprinkler(this);
    }

    /// <summary>激活洒水 — 浇水覆盖范围内的所有地块</summary>
    public void ActivateWatering()
    {
        // 灵石充能检查
        if (requiresCharging && chargesRemaining <= 0)
            return;

        Vector2Int[] offsets = tier switch
        {
            SprinklerTier.Copper => CopperOffsets,
            SprinklerTier.Iron => IronOffsets,
            SprinklerTier.Gold => GoldOffsets,
            _ => CopperOffsets
        };

        // 大风天气效率 ×0.6（减少覆盖范围）
        if (WeatherSystem.Instance != null && GameManager.Instance != null)
        {
            if (GameManager.Instance.weatherIndex == 5) // 大风
            {
                // 大风时只浇一半格子（偶数索引）
                for (int i = 0; i < offsets.Length; i += 2)
                    WaterTileAt(offsets[i]);
                EventBus.Publish(GameEvent.SprinklerActivated, (int)tier);
                return;
            }
        }

        foreach (var offset in offsets)
            WaterTileAt(offset);

        if (requiresCharging)
            chargesRemaining--;

        EventBus.Publish(GameEvent.SprinklerActivated, (int)tier);
    }

    private void WaterTileAt(Vector2Int offset)
    {
        Vector3 targetPos = transform.position + new Vector3(offset.x * tileSize, offset.y * tileSize, 0);

        // 查找附近的地块
        FarmTile tile = FarmingManager.Instance?.GetTileNearPosition(targetPos, tileSize * 0.6f);
        if (tile != null)
            tile.AutoWater();
    }

    /// <summary>充能（灵石）</summary>
    public void Charge(int amount)
    {
        chargesRemaining += amount;
    }

    /// <summary>获取等级</summary>
    public SprinklerTier Tier => tier;

    /// <summary>获取覆盖范围偏移（供 UI 预览用）</summary>
    public Vector2Int[] GetCoverageOffsets()
    {
        return tier switch
        {
            SprinklerTier.Copper => CopperOffsets,
            SprinklerTier.Iron => IronOffsets,
            SprinklerTier.Gold => GoldOffsets,
            _ => CopperOffsets
        };
    }
}
