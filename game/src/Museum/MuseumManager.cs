using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 博物馆管理器 — GDD 7.9
/// 捐赠物品 + 里程碑奖励 + 图书馆阅读
/// 复用 AchievementManager 的图鉴集合
/// </summary>
public class MuseumManager : MonoBehaviour
{
    public static MuseumManager Instance { get; private set; }

    /// <summary>博物馆捐赠记录（collectionType → itemId 集合）</summary>
    private Dictionary<CollectionType, HashSet<string>> _donations = new();
    /// <summary>已阅读的书籍ID</summary>
    private HashSet<string> _readBooks = new();

    /// <summary>里程碑阈值（捐赠达到 N 件发奖励）</summary>
    private static readonly int[] MILESTONES = { 5, 10, 20, 40, 60, 80, 100 };
    /// <summary>已领取的里程碑</summary>
    private HashSet<int> _claimedMilestones = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (CollectionType type in System.Enum.GetValues(typeof(CollectionType)))
            _donations[type] = new HashSet<string>();
    }

    // ==================== 捐赠 ====================

    /// <summary>捐赠物品（移除背包 + 记入博物馆 + 检查里程碑）</summary>
    public bool Donate(CollectionType type, string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        if (!_donations.TryGetValue(type, out var set)) return false;
        if (set.Contains(itemId)) return false; // 已捐赠

        // 移除背包物品
        if (InventoryManager.Instance == null || !InventoryManager.Instance.RemoveItem(itemId, 1))
            return false;

        set.Add(itemId);
        EventBus.Publish(GameEvent.MuseumDonated, itemId);
        Debug.Log($"博物馆捐赠: {itemId}");

        // 检查里程碑
        CheckMilestones();
        return true;
    }

    /// <summary>是否已捐赠</summary>
    public bool HasDonated(CollectionType type, string itemId)
        => _donations.TryGetValue(type, out var set) && set.Contains(itemId);

    /// <summary>获取某类型捐赠数量</summary>
    public int GetDonatedCount(CollectionType type)
        => _donations.TryGetValue(type, out var set) ? set.Count : 0;

    /// <summary>获取总捐赠数量</summary>
    public int GetTotalDonatedCount()
    {
        int total = 0;
        foreach (var kv in _donations)
            total += kv.Value.Count;
        return total;
    }

    // ==================== 里程碑 ====================

    void CheckMilestones()
    {
        int total = GetTotalDonatedCount();
        foreach (var threshold in MILESTONES)
        {
            if (total >= threshold && !_claimedMilestones.Contains(threshold))
            {
                _claimedMilestones.Add(threshold);
                GrantMilestoneReward(threshold);
            }
        }
    }

    void GrantMilestoneReward(int threshold)
    {
        int gold = threshold * 100;
        GameManager.Instance?.AddGold(gold);
        Debug.Log($"博物馆里程碑达成: 捐赠{threshold}件，奖励{gold}文");
    }

    // ==================== 图书馆 ====================

    /// <summary>阅读书籍（记录 + 返回是否首次阅读）</summary>
    public bool ReadBook(string bookId)
    {
        if (string.IsNullOrEmpty(bookId)) return false;
        if (_readBooks.Contains(bookId)) return false;
        _readBooks.Add(bookId);
        Debug.Log($"阅读书籍: {bookId}");
        return true;
    }

    /// <summary>是否已阅读</summary>
    public bool HasReadBook(string bookId) => _readBooks.Contains(bookId);

    /// <summary>已读书籍数</summary>
    public int ReadBookCount => _readBooks.Count;

    // ==================== 存档 ====================

    [System.Serializable]
    public class MuseumSaveData
    {
        public List<int> donationTypeKeys;
        public List<string> donationItemIds;   // 扁平：type|itemId 组合
        public List<string> readBooks;
        public List<int> claimedMilestones;
    }

    public MuseumSaveData GetSaveData()
    {
        var data = new MuseumSaveData
        {
            donationItemIds = new List<string>(),
            readBooks = new List<string>(_readBooks),
            claimedMilestones = new List<int>(_claimedMilestones)
        };

        foreach (var kv in _donations)
        {
            foreach (var itemId in kv.Value)
                data.donationItemIds.Add($"{(int)kv.Key}|{itemId}");
        }
        return data;
    }

    public void LoadSaveData(MuseumSaveData data)
    {
        if (data == null) return;

        foreach (CollectionType type in System.Enum.GetValues(typeof(CollectionType)))
            _donations[type] = new HashSet<string>();

        if (data.donationItemIds != null)
        {
            foreach (var entry in data.donationItemIds)
            {
                var parts = entry.Split('|');
                if (parts.Length == 2 && int.TryParse(parts[0], out int typeKey))
                    _donations[(CollectionType)typeKey].Add(parts[1]);
            }
        }

        _readBooks = new HashSet<string>(data.readBooks ?? new List<string>());
        _claimedMilestones = new HashSet<int>(data.claimedMilestones ?? new List<int>());
    }
}
