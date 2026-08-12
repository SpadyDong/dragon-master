using UnityEngine;

/// <summary>
/// 蜂蜜种类
/// </summary>
public enum HoneyType
{
    None,       // 无产出
    Normal,     // 普通蜜（相邻普通花）×2.0
    Rare        // 稀有花种蜜（相邻稀有花）×3.0–4.0
}

/// <summary>
/// 蜂箱系统
/// 建在果树/花园相邻3格内有效，普通花产普通蜜，稀有花种产稀有蜜
/// 每日自动推进，满后可收获
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Beehive : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private float detectRadius = 3f;     // 检测花/果树半径
    [SerializeField] private int productionDays = 3;      // 产蜜所需天数
    [SerializeField] private LayerMask flowerLayer = ~0;  // 花层

    [Header("状态")]
    [SerializeField] private bool isActive;              // 是否在花/果树附近
    [SerializeField] private int honeyProgress;           // 产蜜进度
    [SerializeField] private bool isReady;                // 蜂蜜可收获
    [SerializeField] private HoneyType honeyType = HoneyType.None;

    private SpriteRenderer _sr;
    private int _consecutiveDaysActive; // 连续激活天数（影响品质）

    // 存档
    [System.Serializable]
    public struct BeehiveSaveData
    {
        public bool isActive;
        public int honeyProgress;
        public bool isReady;
        public int honeyType;
        public int consecutiveDaysActive;
    }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        FarmingManager.Instance?.RegisterBeehive(this);
        CheckNearbyFlowers();
        UpdateVisual();
    }

    /// <summary>检查附近是否有花或果树</summary>
    public void CheckNearbyFlowers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius, flowerLayer);

        bool foundFlower = false;
        bool foundRareFlower = false;
        bool foundFruitTree = false;

        foreach (var hit in hits)
        {
            // 检查是否为果树
            if (hit.TryGetComponent<FruitTree>(out _))
                foundFruitTree = true;

            // 检查是否为花（通过 tag 或组件）
            if (hit.CompareTag("Flower"))
                foundFlower = true;
            if (hit.CompareTag("RareFlower"))
                foundRareFlower = true;
        }

        bool wasActive = isActive;
        isActive = foundFlower || foundFruitTree;

        // 确定蜂蜜类型
        if (foundRareFlower)
            honeyType = HoneyType.Rare;
        else if (foundFlower || foundFruitTree)
            honeyType = HoneyType.Normal;
        else
            honeyType = HoneyType.None;

        if (isActive && !wasActive)
            _consecutiveDaysActive = 0;
    }

    // ==================== 跨天推进 ====================

    /// <summary>跨天推进（由 FarmingManager 调用）</summary>
    public void AdvanceDay()
    {
        // 每日检查花源是否还在
        CheckNearbyFlowers();

        if (!isActive)
        {
            _consecutiveDaysActive = 0;
            return;
        }

        _consecutiveDaysActive++;

        if (!isReady)
        {
            honeyProgress++;

            // 大风天气效率降低
            if (GameManager.Instance != null && GameManager.Instance.weatherIndex == 5)
                honeyProgress--; // 大风天蜜蜂不出巢，抵消今日进度

            if (honeyProgress >= productionDays)
            {
                isReady = true;
                honeyProgress = 0;
                UpdateVisual();
            }
        }
    }

    // ==================== 收获 ====================

    /// <summary>收获蜂蜜</summary>
    public (string itemId, int count, HoneyType type) Harvest()
    {
        if (!isReady) return (null, 0, HoneyType.None);

        // 产量倍率
        float multiplier = honeyType switch
        {
            HoneyType.Normal => 2.0f,
            HoneyType.Rare => Mathf.Lerp(3.0f, 4.0f, Mathf.Clamp01(_consecutiveDaysActive / 30f)),
            _ => 0f
        };

        int count = Mathf.RoundToInt(multiplier);

        // 获取蜂蜜物品ID
        string honeyId = honeyType switch
        {
            HoneyType.Normal => "honey_normal",
            HoneyType.Rare => "honey_rare",
            _ => null
        };

        // 重置
        isReady = false;
        honeyProgress = 0;
        UpdateVisual();

        EventBus.Publish(GameEvent.BeehiveHarvested, honeyType.ToString());
        return (honeyId, count, honeyType);
    }

    // ==================== 视觉 ====================

    private void UpdateVisual()
    {
        if (_sr == null) return;

        // 简单颜色区分状态
        if (!isActive)
            _sr.color = new Color(0.6f, 0.4f, 0.2f, 0.5f);
        else if (isReady)
            _sr.color = new Color(1f, 0.85f, 0.3f, 1f);
        else
            _sr.color = new Color(0.8f, 0.6f, 0.3f, 0.9f);
    }

    // ==================== 属性 ====================

    public bool IsActive => isActive;
    public bool IsReady => isReady;
    public HoneyType CurrentHoneyType => honeyType;
    public float Progress => (float)honeyProgress / productionDays;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

    // ==================== 存档 ====================

    public BeehiveSaveData GetSaveData()
    {
        return new BeehiveSaveData
        {
            isActive = isActive,
            honeyProgress = honeyProgress,
            isReady = isReady,
            honeyType = (int)honeyType,
            consecutiveDaysActive = _consecutiveDaysActive
        };
    }

    public void LoadSaveData(BeehiveSaveData data)
    {
        isActive = data.isActive;
        honeyProgress = data.honeyProgress;
        isReady = data.isReady;
        honeyType = (HoneyType)data.honeyType;
        _consecutiveDaysActive = data.consecutiveDaysActive;
        UpdateVisual();
    }
}
