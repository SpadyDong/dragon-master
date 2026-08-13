using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 结婚管理器 — GDD 5.5
/// 求婚校验（异性 + 好感1000+ + 完成所有心事件 + 龙心挂坠）
/// 配偶状态 + 每日战斗属性加成 + 配偶日常
/// </summary>
public class MarriageManager : MonoBehaviour
{
    public static MarriageManager Instance { get; private set; }

    /// <summary>求婚所需好感度</summary>
    public const int PROPOSAL_REQUIRED_AFFECTION = 1000;
    /// <summary>求婚道具：龙心挂坠</summary>
    public const string PROPOSAL_ITEM_ID = "dragon_heart_pendant";

    /// <summary>当前配偶 NPC ID（null=未婚）</summary>
    private string _spouseId;

    /// <summary>是否已婚</summary>
    public bool IsMarried => !string.IsNullOrEmpty(_spouseId);
    /// <summary>配偶 NPC ID</summary>
    public string SpouseId => _spouseId;

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
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    // ==================== 求婚 ====================

    /// <summary>检查是否可向某 NPC 求婚（GDD 5.5.1）</summary>
    public bool CanPropose(string npcId)
    {
        if (IsMarried) return false;  // 一人只能结婚一次

        if (!_npcData.TryGetValue(npcId, out var npc)) return false;
        if (!npc.isMarriageable) return false;

        // 仅允许异性结婚
        if (GameManager.Instance != null)
        {
            bool npcIsMale = npc.gender == "男";
            bool playerIsMale = GameManager.Instance.playerGender == PlayerGender.Male;
            if (npcIsMale == playerIsMale) return false;  // 同性
        }

        // 好感度 >= 1000（亲密阶段）
        if (PlayerAffectionManager.Instance == null) return false;
        if (PlayerAffectionManager.Instance.GetAffection(npcId) < PROPOSAL_REQUIRED_AFFECTION) return false;

        // 完成所有心事件
        if (!PlayerAffectionManager.Instance.HasCompletedAllHeartEvents(npcId)) return false;

        // 持有龙心挂坠
        if (InventoryManager.Instance == null) return false;
        if (!InventoryManager.Instance.HasItem(PROPOSAL_ITEM_ID, 1)) return false;

        return true;
    }

    /// <summary>求婚（返回是否成功）</summary>
    public bool Propose(string npcId)
    {
        if (!CanPropose(npcId)) return false;

        // 消耗龙心挂坠
        InventoryManager.Instance.RemoveItem(PROPOSAL_ITEM_ID, 1);

        _spouseId = npcId;
        EventBus.Publish(GameEvent.Married, npcId);

        var spouse = GetSpouse();
        Debug.Log($"求婚成功！与 {spouse?.npcName ?? npcId} 结为伴侣");
        return true;
    }

    /// <summary>获取求婚失败原因（供 UI 提示）</summary>
    public string GetProposalBlockReason(string npcId)
    {
        if (IsMarried) return "已经结婚了";
        if (!_npcData.TryGetValue(npcId, out var npc)) return "无效对象";
        if (!npc.isMarriageable) return "该NPC不可结婚";
        if (GameManager.Instance != null)
        {
            bool npcIsMale = npc.gender == "男";
            bool playerIsMale = GameManager.Instance.playerGender == PlayerGender.Male;
            if (npcIsMale == playerIsMale) return "仅允许异性结婚";
        }
        if (PlayerAffectionManager.Instance != null &&
            PlayerAffectionManager.Instance.GetAffection(npcId) < PROPOSAL_REQUIRED_AFFECTION)
            return "好感度不足（需≥1000）";
        if (PlayerAffectionManager.Instance != null &&
            !PlayerAffectionManager.Instance.HasCompletedAllHeartEvents(npcId))
            return "未完成所有心事件";
        if (InventoryManager.Instance != null &&
            !InventoryManager.Instance.HasItem(PROPOSAL_ITEM_ID, 1))
            return "缺少龙心挂坠";
        return "";
    }

    // ==================== 配偶 ====================

    /// <summary>获取配偶数据</summary>
    public NPCDataContainer GetSpouse()
    {
        if (!IsMarried) return null;
        return _npcData.TryGetValue(_spouseId, out var npc) ? npc : null;
    }

    /// <summary>获取配偶每日战斗属性加成类型（属性类型取决于配偶元素）</summary>
    public string GetSpouseBonusAttribute()
    {
        var spouse = GetSpouse();
        if (spouse == null) return "";

        return spouse.element switch
        {
            "风" => "敏捷",
            "雷" => "敏捷",
            "火" => "攻击",
            "水" => "灵力",
            "土" => "防御",
            "冰" => "防御",
            "草" => "生命",
            _ => "攻击"
        };
    }

    /// <summary>获取配偶每日战斗属性加成值（GDD 5.5.3：每日+5%）</summary>
    public float GetSpouseDailyBonus()
    {
        return IsMarried ? 0.05f : 0f;
    }

    /// <summary>配偶日常（做饭/浇水/喂龙，概率触发）</summary>
    void OnDayChanged(int day)
    {
        if (!IsMarried) return;
        var spouse = GetSpouse();
        if (spouse == null) return;

        // 概率提供日常 buff
        if (Random.value < 0.5f)
        {
            Debug.Log($"{spouse.npcName} 为你准备了爱心早餐");
            // 恢复体力（配偶做饭）
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.RestoreStamina(30);
        }
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class MarriageSaveData
    {
        public string spouseId;
    }

    public MarriageSaveData GetSaveData()
    {
        return new MarriageSaveData { spouseId = _spouseId ?? "" };
    }

    public void LoadSaveData(MarriageSaveData data)
    {
        if (data == null) return;
        _spouseId = string.IsNullOrEmpty(data.spouseId) ? null : data.spouseId;
    }
}
