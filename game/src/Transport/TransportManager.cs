using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 传送系统 — GDD 7.8
/// 矿车传送 / 回程法杖 / 传送方尖碑 / 金色时钟
/// </summary>
public class TransportManager : MonoBehaviour
{
    public static TransportManager Instance { get; private set; }

    /// <summary>已解锁的矿车传送点</summary>
    private HashSet<string> _unlockedMinecarts = new();
    /// <summary>是否拥有回程法杖</summary>
    private bool _hasReturnWand;
    /// <summary>已建造的方尖碑位置</summary>
    private HashSet<string> _builtObelisks = new();
    /// <summary>是否拥有金色时钟</summary>
    private bool _hasGoldenClock;

    /// <summary>回程法杖冷却时间（现实秒）</summary>
    private const float RETURN_WAND_COOLDOWN = 1800f; // 30分钟
    private float _returnWandCooldownRemaining;

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

    void Update()
    {
        if (_returnWandCooldownRemaining > 0)
            _returnWandCooldownRemaining -= Time.deltaTime;
    }

    // ==================== 矿车 ====================

    /// <summary>解锁矿车传送点</summary>
    public void UnlockMinecart(string pointId)
    {
        _unlockedMinecarts.Add(pointId);
        Debug.Log($"解锁矿车传送点: {pointId}");
    }

    /// <summary>是否已解锁矿车传送点</summary>
    public bool IsMinecartUnlocked(string pointId) => _unlockedMinecarts.Contains(pointId);

    /// <summary>矿车传送（需已解锁）</summary>
    public bool TeleportToMinecart(string pointId)
    {
        if (!IsMinecartUnlocked(pointId)) return false;
        Debug.Log($"矿车传送: {pointId}");
        return true;
    }

    // ==================== 回程法杖 ====================

    /// <summary>是否拥有回程法杖</summary>
    public bool HasReturnWand => _hasReturnWand;

    /// <summary>获得回程法杖</summary>
    public void ObtainReturnWand()
    {
        _hasReturnWand = true;
    }

    /// <summary>回程法杖是否在冷却中</summary>
    public bool IsReturnWandOnCooldown => _returnWandCooldownRemaining > 0;

    /// <summary>使用回程法杖（回家）</summary>
    public bool UseReturnWand()
    {
        if (!_hasReturnWand) return false;
        if (_returnWandCooldownRemaining > 0) return false;

        _returnWandCooldownRemaining = RETURN_WAND_COOLDOWN;
        Debug.Log("使用回程法杖，传送回家");
        return true;
    }

    // ==================== 方尖碑 ====================

    /// <summary>建造方尖碑（需铱矿）</summary>
    public bool BuildObelisk(string locationId)
    {
        if (_builtObelisks.Contains(locationId)) return false;

        // 需铱矿（简化：检查材料）
        if (InventoryManager.Instance == null) return false;
        if (!InventoryManager.Instance.RemoveItem("iridium_ore", 10)) return false;

        _builtObelisks.Add(locationId);
        Debug.Log($"建造传送方尖碑: {locationId}");
        return true;
    }

    /// <summary>是否已建造方尖碑</summary>
    public bool HasObelisk(string locationId) => _builtObelisks.Contains(locationId);

    // ==================== 金色时钟 ====================

    /// <summary>是否拥有金色时钟（停止杂草生长、保护家园）</summary>
    public bool HasGoldenClock => _hasGoldenClock;

    /// <summary>建造金色时钟</summary>
    public bool BuildGoldenClock()
    {
        if (_hasGoldenClock) return false;
        if (GameManager.Instance == null) return false;
        if (!GameManager.Instance.SpendGold(10000000)) return false; // 1000万金币

        _hasGoldenClock = true;
        Debug.Log("建造金色时钟");
        return true;
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class TransportSaveData
    {
        public List<string> unlockedMinecarts;
        public bool hasReturnWand;
        public List<string> builtObelisks;
        public bool hasGoldenClock;
    }

    public TransportSaveData GetSaveData()
    {
        return new TransportSaveData
        {
            unlockedMinecarts = new List<string>(_unlockedMinecarts),
            hasReturnWand = _hasReturnWand,
            builtObelisks = new List<string>(_builtObelisks),
            hasGoldenClock = _hasGoldenClock
        };
    }

    public void LoadSaveData(TransportSaveData data)
    {
        if (data == null) return;
        _unlockedMinecarts = new HashSet<string>(data.unlockedMinecarts ?? new List<string>());
        _hasReturnWand = data.hasReturnWand;
        _builtObelisks = new HashSet<string>(data.builtObelisks ?? new List<string>());
        _hasGoldenClock = data.hasGoldenClock;
    }
}
