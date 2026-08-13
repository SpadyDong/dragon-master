using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 社区中心管理器 — GDD 7.3
/// 两路线二选一：社区中心路线（收集修复）vs 龙腾集团路线（花钱解锁）
/// </summary>
public class CommunityCenterManager : MonoBehaviour
{
    public static CommunityCenterManager Instance { get; private set; }

    /// <summary>已完成的收集包</summary>
    private HashSet<string> _completedBundles = new();
    /// <summary>已修复的房间</summary>
    private HashSet<string> _repairedRooms = new();

    /// <summary>是否已加入龙腾集团（反派路线）</summary>
    private bool _joinedDragonTeng;

    /// <summary>是否已加入龙腾集团</summary>
    public bool JoinedDragonTeng => _joinedDragonTeng;

    /// <summary>龙腾集团签约费用</summary>
    public const int DRAGON_TENG_COST = 50000;

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

    // ==================== 收集包 ====================

    /// <summary>检查是否能完成收集包（材料足够）</summary>
    public bool CanCompleteBundle(CommunityBundle bundle)
    {
        if (bundle == null || bundle.requiredItems == null) return false;
        if (InventoryManager.Instance == null) return false;

        foreach (var item in bundle.requiredItems)
        {
            if (!InventoryManager.Instance.HasItem(item.itemId, item.count)) return false;
        }
        return true;
    }

    /// <summary>完成收集包（消耗材料 + 发奖励 + 修复房间）</summary>
    public bool CompleteBundle(CommunityBundle bundle)
    {
        if (bundle == null) return false;
        if (_completedBundles.Contains(bundle.bundleId)) return false;
        if (!CanCompleteBundle(bundle)) return false;

        // 消耗材料
        foreach (var item in bundle.requiredItems)
            InventoryManager.Instance.RemoveItem(item.itemId, item.count);

        _completedBundles.Add(bundle.bundleId);

        // 发奖励
        if (bundle.rewardGold > 0)
            GameManager.Instance?.AddGold(bundle.rewardGold);
        if (!string.IsNullOrEmpty(bundle.rewardItemId))
            InventoryManager.Instance?.AddItem(bundle.rewardItemId, 1);

        // 修复关联房间
        if (!string.IsNullOrEmpty(bundle.roomId))
            _repairedRooms.Add(bundle.roomId);

        // 全体 NPC 好感
        if (bundle.rewardAffection > 0)
            EventBus.Publish(GameEvent.NPCAffectionChanged, bundle.rewardAffection);

        EventBus.Publish(GameEvent.BundleCompleted, bundle.bundleId);
        Debug.Log($"完成收集包: {bundle.bundleName}");
        return true;
    }

    /// <summary>是否已完成某收集包</summary>
    public bool HasCompletedBundle(string bundleId) => _completedBundles.Contains(bundleId);

    // ==================== 房间 ====================

    /// <summary>房间是否已修复</summary>
    public bool IsRoomRepaired(string roomId) => _repairedRooms.Contains(roomId);

    /// <summary>获取所有已修复房间</summary>
    public HashSet<string> GetRepairedRooms() => _repairedRooms;

    // ==================== 龙腾集团路线 ====================

    /// <summary>加入龙腾集团（花钱直接解锁所有设施）</summary>
    public bool JoinDragonTengGroup()
    {
        if (_joinedDragonTeng) return false;
        if (GameManager.Instance == null) return false;
        if (!GameManager.Instance.SpendGold(DRAGON_TENG_COST)) return false;

        _joinedDragonTeng = true;
        Debug.Log("加入龙腾集团，所有设施已解锁（NPC 好感受影响）");
        return true;
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class CommunityCenterSaveData
    {
        public List<string> completedBundles;
        public List<string> repairedRooms;
        public bool joinedDragonTeng;
    }

    public CommunityCenterSaveData GetSaveData()
    {
        return new CommunityCenterSaveData
        {
            completedBundles = new List<string>(_completedBundles),
            repairedRooms = new List<string>(_repairedRooms),
            joinedDragonTeng = _joinedDragonTeng
        };
    }

    public void LoadSaveData(CommunityCenterSaveData data)
    {
        if (data == null) return;
        _completedBundles = new HashSet<string>(data.completedBundles ?? new List<string>());
        _repairedRooms = new HashSet<string>(data.repairedRooms ?? new List<string>());
        _joinedDragonTeng = data.joinedDragonTeng;
    }
}
