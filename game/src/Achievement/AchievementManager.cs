using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 成就管理器 — GDD 7.6
/// 成就解锁 + 图鉴收集 + 奖励发放
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    /// <summary>成就进度字典</summary>
    private Dictionary<string, int> _progress = new();
    /// <summary>已解锁成就</summary>
    private HashSet<string> _unlocked = new();

    /// <summary>图鉴收集集合（按类型）</summary>
    private Dictionary<CollectionType, HashSet<string>> _collections = new();

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
            _collections[type] = new HashSet<string>();
    }

    void Start()
    {
        SubscribeCollectionSources();
    }

    void OnDestroy()
    {
        UnsubscribeCollectionSources();
    }

    // ==================== 图鉴收集挂接 ====================

    void SubscribeCollectionSources()
    {
        // 鱼类图鉴
        EventBus.Subscribe<string>(GameEvent.FishCaught, fishId => RegisterDiscovery(CollectionType.Fish, fishId));
        // 龙种图鉴
        EventBus.Subscribe<string>(GameEvent.DragonCaptured, speciesId => RegisterDiscovery(CollectionType.Dragon, speciesId));
        // 作物图鉴
        EventBus.Subscribe<string>(GameEvent.CropHarvested, cropId => RegisterDiscovery(CollectionType.Crop, cropId));
        // 装备图鉴
        EventBus.Subscribe<string>(GameEvent.EquipmentForged, itemId => RegisterDiscovery(CollectionType.Equipment, itemId));
    }

    void UnsubscribeCollectionSources()
    {
        // 单例生命周期内有效，简化处理
    }

    // ==================== 图鉴收集 ====================

    /// <summary>记录新发现</summary>
    public bool RegisterDiscovery(CollectionType type, string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        var set = _collections[type];
        if (set.Contains(id)) return false;

        set.Add(id);
        Debug.Log($"图鉴新发现 [{type}]: {id}");

        // 触发相关成就进度
        ReportProgress(type, 1);
        return true;
    }

    /// <summary>是否已收集</summary>
    public bool HasDiscovered(CollectionType type, string id)
        => _collections.TryGetValue(type, out var set) && set.Contains(id);

    /// <summary>获取某类型图鉴收集数量</summary>
    public int GetCollectionCount(CollectionType type)
        => _collections.TryGetValue(type, out var set) ? set.Count : 0;

    /// <summary>获取某类型图鉴全部ID</summary>
    public HashSet<string> GetCollection(CollectionType type)
        => _collections.TryGetValue(type, out var set) ? set : new HashSet<string>();

    // ==================== 成就进度 ====================

    /// <summary>报告成就进度（按图鉴类型）</summary>
    public void ReportProgress(CollectionType type, int amount)
    {
        foreach (var achievement in LoadAllAchievements())
        {
            if (achievement.collectionType != type) continue;
            AddAchievementProgress(achievement, amount);
        }
    }

    /// <summary>报告自定义计数成就进度</summary>
    public void ReportProgressById(string achievementId, int amount)
    {
        var achievement = Resources.Load<AchievementData>($"Achievements/{achievementId}");
        if (achievement != null)
            AddAchievementProgress(achievement, amount);
    }

    void AddAchievementProgress(AchievementData achievement, int amount)
    {
        if (_unlocked.Contains(achievement.achievementId)) return;

        if (!_progress.TryGetValue(achievement.achievementId, out int current))
            current = 0;

        current += amount;
        _progress[achievement.achievementId] = current;

        if (current >= achievement.targetCount)
            UnlockAchievement(achievement);
    }

    /// <summary>解锁成就</summary>
    void UnlockAchievement(AchievementData achievement)
    {
        if (_unlocked.Contains(achievement.achievementId)) return;
        _unlocked.Add(achievement.achievementId);

        // 发放奖励
        if (achievement.rewardGold > 0)
            GameManager.Instance?.AddGold(achievement.rewardGold);
        if (!string.IsNullOrEmpty(achievement.rewardItemId))
            InventoryManager.Instance?.AddItem(achievement.rewardItemId, 1);

        EventBus.Publish(GameEvent.AchievementUnlocked, achievement.achievementId);
        Debug.Log($"★ 解锁成就: {achievement.achievementName}");

        if (!string.IsNullOrEmpty(achievement.rewardTitle))
            Debug.Log($"  获得称号: {achievement.rewardTitle}");
    }

    /// <summary>加载所有成就数据</summary>
    AchievementData[] LoadAllAchievements()
    {
        return Resources.LoadAll<AchievementData>("Achievements");
    }

    // ==================== 查询 ====================

    /// <summary>是否已解锁</summary>
    public bool IsUnlocked(string achievementId) => _unlocked.Contains(achievementId);

    /// <summary>获取成就进度</summary>
    public int GetProgress(string achievementId) => _progress.TryGetValue(achievementId, out var p) ? p : 0;

    /// <summary>已解锁成就数</summary>
    public int UnlockedCount => _unlocked.Count;

    // ==================== 存档 ====================

    [System.Serializable]
    public class AchievementSaveData
    {
        public List<string> progressIds;
        public List<int> progressCounts;
        public List<string> unlockedIds;
        public List<string> collectionFish;
        public List<string> collectionDragon;
        public List<string> collectionCrop;
        public List<string> collectionEquipment;
    }

    public AchievementSaveData GetSaveData()
    {
        var data = new AchievementSaveData
        {
            progressIds = new List<string>(),
            progressCounts = new List<int>(),
            unlockedIds = new List<string>(_unlocked),
            collectionFish = new List<string>(_collections[CollectionType.Fish]),
            collectionDragon = new List<string>(_collections[CollectionType.Dragon]),
            collectionCrop = new List<string>(_collections[CollectionType.Crop]),
            collectionEquipment = new List<string>(_collections[CollectionType.Equipment])
        };
        foreach (var kv in _progress)
        {
            data.progressIds.Add(kv.Key);
            data.progressCounts.Add(kv.Value);
        }
        return data;
    }

    public void LoadSaveData(AchievementSaveData data)
    {
        if (data == null) return;

        _unlocked = new HashSet<string>(data.unlockedIds ?? new List<string>());

        _progress.Clear();
        if (data.progressIds != null)
        {
            for (int i = 0; i < data.progressIds.Count && i < data.progressCounts.Count; i++)
                _progress[data.progressIds[i]] = data.progressCounts[i];
        }

        _collections[CollectionType.Fish] = new HashSet<string>(data.collectionFish ?? new List<string>());
        _collections[CollectionType.Dragon] = new HashSet<string>(data.collectionDragon ?? new List<string>());
        _collections[CollectionType.Crop] = new HashSet<string>(data.collectionCrop ?? new List<string>());
        _collections[CollectionType.Equipment] = new HashSet<string>(data.collectionEquipment ?? new List<string>());
    }
}
