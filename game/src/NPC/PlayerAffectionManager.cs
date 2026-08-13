using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 好感度阶段 — GDD 5.3.5
/// </summary>
public enum AffectionLevel
{
    Stranger,   // 陌生 0-199
    Familiar,   // 熟悉 200-499
    Friendly,   // 友好 500-999
    Intimate,   // 亲密 1000-2999
    Beloved     // 挚爱 3000+
}

/// <summary>
/// 玩家→NPC 好感度管理器 — GDD 5.3.5 / 5.3.6
/// 管理玩家对每名 NPC 的好感度、送礼、心事件
/// 接线：Dialogue 系统（好感条件/效果）、RelationshipPanel、MarketManager 经营者折扣
/// </summary>
public class PlayerAffectionManager : MonoBehaviour
{
    public static PlayerAffectionManager Instance { get; private set; }

    /// <summary>好感度上限</summary>
    public const int MAX_AFFECTION = 9999;

    /// <summary>玩家 → NPC 好感度</summary>
    private Dictionary<string, int> _affections = new();

    /// <summary>连续送礼记录（npcId → 连续天数）</summary>
    private Dictionary<string, int> _consecutiveGiftDays = new();
    /// <summary>上次送礼日期（npcId → 上次送礼的游戏日）</summary>
    private Dictionary<string, int> _lastGiftDay = new();

    /// <summary>已触发心事件（npcId → 已触发的事件索引集合）</summary>
    private Dictionary<string, HashSet<int>> _triggeredHeartEvents = new();

    /// <summary>NPC 数据缓存</summary>
    private Dictionary<string, NPCDataContainer> _npcData;

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

    void Start()
    {
        _npcData = NPCDatabase.BuildAll();
    }

    // ==================== 好感度查询/修改 ====================

    /// <summary>获取玩家对某 NPC 的好感度</summary>
    public int GetAffection(string npcId)
    {
        return _affections.TryGetValue(npcId, out int val) ? val : 0;
    }

    /// <summary>修改好感度（返回修改后的值）</summary>
    public int ChangeAffection(string npcId, int amount)
    {
        if (string.IsNullOrEmpty(npcId)) return 0;

        int current = GetAffection(npcId);
        int newVal = Mathf.Clamp(current + amount, 0, MAX_AFFECTION);
        _affections[npcId] = newVal;

        // 全局通知（供 Proficiency/QuestManager 等）
        if (amount != 0)
            EventBus.Publish(GameEvent.NPCAffectionChanged, amount);

        return newVal;
    }

    /// <summary>获取好感度阶段</summary>
    public AffectionLevel GetAffectionLevel(string npcId)
    {
        int aff = GetAffection(npcId);
        if (aff >= 3000) return AffectionLevel.Beloved;
        if (aff >= 1000) return AffectionLevel.Intimate;
        if (aff >= 500) return AffectionLevel.Friendly;
        if (aff >= 200) return AffectionLevel.Familiar;
        return AffectionLevel.Stranger;
    }

    /// <summary>好感度阶段中文名</summary>
    public static string GetAffectionLevelName(AffectionLevel level) => level switch
    {
        AffectionLevel.Stranger => "陌生",
        AffectionLevel.Familiar => "熟悉",
        AffectionLevel.Friendly => "友好",
        AffectionLevel.Intimate => "亲密",
        AffectionLevel.Beloved  => "挚爱",
        _ => "?"
    };

    /// <summary>根据好感度数值判定阶段（静态，供 UI 复用，避免阈值漂移）</summary>
    public static AffectionLevel GetAffectionLevelFromValue(int affection)
    {
        if (affection >= 3000) return AffectionLevel.Beloved;
        if (affection >= 1000) return AffectionLevel.Intimate;
        if (affection >= 500) return AffectionLevel.Friendly;
        if (affection >= 200) return AffectionLevel.Familiar;
        return AffectionLevel.Stranger;
    }

    /// <summary>好感度阶段图标（♥ 数量）</summary>
    public static string GetAffectionLevelIcon(AffectionLevel level) => level switch
    {
        AffectionLevel.Stranger => "♥",
        AffectionLevel.Familiar => "♥♥",
        AffectionLevel.Friendly => "♥♥♥",
        AffectionLevel.Intimate => "♥♥♥♥",
        AffectionLevel.Beloved  => "♥♥♥♥♥",
        _ => ""
    };

    // ==================== 送礼 ====================

    /// <summary>送礼（返回实际好感变化）</summary>
    public int GiveGift(string npcId, string itemId)
    {
        if (!_npcData.TryGetValue(npcId, out var npc)) return 0;
        if (InventoryManager.Instance == null) return 0;

        // 消耗物品
        if (!InventoryManager.Instance.RemoveItem(itemId, 1)) return 0;

        // 解析礼物档位
        GiftTier tier = ResolveGiftTier(npc, itemId);
        int baseChange = (int)tier;

        // 生日×2
        if (IsBirthday(npc))
            baseChange *= 2;

        // 连续送礼累计（+5/+10/+15）
        int consecutiveBonus = GetConsecutiveGiftBonus(npcId);
        baseChange += consecutiveBonus;

        // 应用社交熟练度收益（礼物效果加成）
        if (ProficiencyManager.Instance != null)
            baseChange = Mathf.RoundToInt(baseChange * (1f + ProficiencyManager.Instance.GetSocialGiftBonus()));

        ChangeAffection(npcId, baseChange);
        RecordGift(npcId);

        Debug.Log($"送礼 {itemId} → {npc.npcName}：{baseChange}（档位:{tier}）");
        return baseChange;
    }

    /// <summary>解析礼物档位</summary>
    GiftTier ResolveGiftTier(NPCDataContainer npc, string itemId)
    {
        // 1. 精确匹配 giftPreferences[] 数组
        if (npc.giftPreferences != null)
        {
            foreach (var pref in npc.giftPreferences)
            {
                if (pref.itemId == itemId) return pref.tier;
            }
        }

        // 2. 回退字符串包含匹配（按 itemId 或 itemName）
        var item = Resources.Load<ItemData>($"Items/{itemId}");
        string itemName = item != null ? item.itemName : itemId;

        if (ContainsItem(npc.lovedItems, itemId, itemName)) return GiftTier.Love;
        if (ContainsItem(npc.likedItems, itemId, itemName)) return GiftTier.Like;
        if (ContainsItem(npc.hatedItems, itemId, itemName)) return GiftTier.Hate;

        return GiftTier.Neutral;
    }

    bool ContainsItem(string csvList, string itemId, string itemName)
    {
        if (string.IsNullOrEmpty(csvList)) return false;
        var parts = csvList.Split(',');
        foreach (var p in parts)
        {
            string token = p.Trim();
            if (string.IsNullOrEmpty(token)) continue;
            if (token == itemId || token == itemName) return true;
        }
        return false;
    }

    /// <summary>是否 NPC 生日</summary>
    bool IsBirthday(NPCDataContainer npc)
    {
        if (GameManager.Instance == null) return false;
        return npc.birthMonth == GameManager.Instance.season + 1 &&
               npc.birthDay == GameManager.Instance.day;
    }

    /// <summary>连续送礼累计加成</summary>
    int GetConsecutiveGiftBonus(string npcId)
    {
        if (!_consecutiveGiftDays.TryGetValue(npcId, out int days)) return 0;
        return days switch
        {
            >= 3 => 15,
            >= 2 => 10,
            >= 1 => 5,
            _ => 0
        };
    }

    /// <summary>记录送礼（连续天数 +1，跨天断裂重置）</summary>
    void RecordGift(string npcId)
    {
        int currentDay = GameManager.Instance != null ? GameManager.Instance.day : 0;

        if (_lastGiftDay.TryGetValue(npcId, out int lastDay) && lastDay == currentDay - 1)
        {
            _consecutiveGiftDays[npcId] = _consecutiveGiftDays.TryGetValue(npcId, out int d) ? d + 1 : 1;
        }
        else if (lastDay == currentDay)
        {
            // 同日重复送礼不增加连续天数
        }
        else
        {
            _consecutiveGiftDays[npcId] = 1;
        }
        _lastGiftDay[npcId] = currentDay;
    }

    // ==================== 心事件 ====================

    /// <summary>获取当前可用（好感达标且未触发）的心事件</summary>
    public NPCHeartEvent GetAvailableHeartEvent(string npcId)
    {
        if (!_npcData.TryGetValue(npcId, out var npc)) return null;
        if (npc.heartEvents == null || npc.heartEvents.Length == 0) return null;

        int affection = GetAffection(npcId);
        if (!_triggeredHeartEvents.TryGetValue(npcId, out var triggered))
            triggered = new HashSet<int>();

        foreach (var evt in npc.heartEvents)
        {
            if (triggered.Contains(evt.eventIndex)) continue;
            if (affection >= evt.requiredAffection) return evt;
        }
        return null;
    }

    /// <summary>标记心事件已触发</summary>
    public void MarkHeartEventTriggered(string npcId, int eventIndex)
    {
        if (!_triggeredHeartEvents.TryGetValue(npcId, out var set))
        {
            set = new HashSet<int>();
            _triggeredHeartEvents[npcId] = set;
        }
        set.Add(eventIndex);
    }

    /// <summary>是否已触发某心事件</summary>
    public bool HasTriggeredHeartEvent(string npcId, int eventIndex)
    {
        return _triggeredHeartEvents.TryGetValue(npcId, out var set) && set.Contains(eventIndex);
    }

    /// <summary>是否已完成所有心事件（结婚前提，GDD 5.5.1）</summary>
    public bool HasCompletedAllHeartEvents(string npcId)
    {
        if (!_npcData.TryGetValue(npcId, out var npc)) return false;
        if (npc.heartEvents == null || npc.heartEvents.Length == 0) return true; // 无可结婚心事件 → 视为满足

        if (!_triggeredHeartEvents.TryGetValue(npcId, out var triggered))
            return false;

        foreach (var evt in npc.heartEvents)
        {
            if (!triggered.Contains(evt.eventIndex)) return false;
        }
        return true;
    }

    /// <summary>检查心事件是否满足触发条件（基础版：前置事件 + 好感 + 时段）</summary>
    public bool CanTriggerHeartEvent(string npcId, NPCHeartEvent evt)
    {
        if (evt == null) return false;

        // 好感阈值
        if (GetAffection(npcId) < evt.requiredAffection) return false;

        // 前置事件
        if (evt.prerequisites != null)
        {
            foreach (var pre in evt.prerequisites)
            {
                // 前置为心事件索引（字符串形式的数字）时检查已触发
                if (int.TryParse(pre, out int preIndex) && !HasTriggeredHeartEvent(npcId, preIndex))
                    return false;
            }
        }

        // 时段
        if (evt.requiredHourRange != null && evt.requiredHourRange.Length >= 2)
        {
            int hour = GameManager.Instance != null ? GameManager.Instance.hour : 0;
            if (hour < evt.requiredHourRange[0] || hour > evt.requiredHourRange[1])
                return false;
        }

        // 所需物品
        if (evt.requiresItem && !string.IsNullOrEmpty(evt.requiredItemId))
        {
            if (InventoryManager.Instance == null || !InventoryManager.Instance.HasItem(evt.requiredItemId, 1))
                return false;
        }

        return true;
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class PlayerAffectionSaveData
    {
        public List<string> npcIds;
        public List<int> affections;
        public List<string> giftNpcIds;
        public List<int> consecutiveDays;
        public List<int> lastGiftDays;
        public List<string> heartEventNpcIds;
        public List<string> heartEventIndexes;  // 逗号分隔的索引
    }

    public PlayerAffectionSaveData GetSaveData()
    {
        var data = new PlayerAffectionSaveData
        {
            npcIds = new List<string>(),
            affections = new List<int>(),
            giftNpcIds = new List<string>(),
            consecutiveDays = new List<int>(),
            lastGiftDays = new List<int>(),
            heartEventNpcIds = new List<string>(),
            heartEventIndexes = new List<string>()
        };

        foreach (var kv in _affections)
        {
            data.npcIds.Add(kv.Key);
            data.affections.Add(kv.Value);
        }
        foreach (var kv in _consecutiveGiftDays)
        {
            data.giftNpcIds.Add(kv.Key);
            data.consecutiveDays.Add(kv.Value);
            data.lastGiftDays.Add(_lastGiftDay.TryGetValue(kv.Key, out int d) ? d : 0);
        }
        foreach (var kv in _triggeredHeartEvents)
        {
            data.heartEventNpcIds.Add(kv.Key);
            data.heartEventIndexes.Add(string.Join(",", kv.Value));
        }
        return data;
    }

    public void LoadSaveData(PlayerAffectionSaveData data)
    {
        if (data == null) return;

        _affections.Clear();
        if (data.npcIds != null)
        {
            for (int i = 0; i < data.npcIds.Count && i < data.affections.Count; i++)
                _affections[data.npcIds[i]] = data.affections[i];
        }

        _consecutiveGiftDays.Clear();
        _lastGiftDay.Clear();
        if (data.giftNpcIds != null)
        {
            for (int i = 0; i < data.giftNpcIds.Count; i++)
            {
                _consecutiveGiftDays[data.giftNpcIds[i]] = i < data.consecutiveDays.Count ? data.consecutiveDays[i] : 0;
                _lastGiftDay[data.giftNpcIds[i]] = i < data.lastGiftDays.Count ? data.lastGiftDays[i] : 0;
            }
        }

        _triggeredHeartEvents.Clear();
        if (data.heartEventNpcIds != null)
        {
            for (int i = 0; i < data.heartEventNpcIds.Count; i++)
            {
                var set = new HashSet<int>();
                if (i < data.heartEventIndexes.Count && !string.IsNullOrEmpty(data.heartEventIndexes[i]))
                {
                    foreach (var s in data.heartEventIndexes[i].Split(','))
                        if (int.TryParse(s, out int idx)) set.Add(idx);
                }
                _triggeredHeartEvents[data.heartEventNpcIds[i]] = set;
            }
        }
    }
}
