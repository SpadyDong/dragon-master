using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 熟练度管理器 — GDD 7.1
/// 六大熟练度统一成长框架 Lv1-100
/// 每10级获得一次专精分支（Lv10/Lv30 二选一），Lv50 解锁王牌称号
/// XP 通过订阅 EventBus 事件自动挂接，无需改动旧系统
/// </summary>
public class ProficiencyManager : MonoBehaviour
{
    public static ProficiencyManager Instance { get; private set; }

    public const int SKILL_COUNT = 6;
    public const int MAX_LEVEL = 100;
    public const int LEVELS_PER_BRANCH = 10;   // 每10级一个专精点

    /// <summary>每级所需 XP（线性：level * 100）</summary>
    public static int GetXpForLevel(int level) => level * 100;

    /// <summary>各熟练度当前 XP</summary>
    private int[] _xp = new int[SKILL_COUNT];

    /// <summary>各熟练度等级（1-100）</summary>
    private int[] _level = new int[SKILL_COUNT];

    /// <summary>专精分支选择 [skill, tier]（tier 0 = Lv10, 1 = Lv30）</summary>
    private ProficiencyBranch[,] _branches = new ProficiencyBranch[SKILL_COUNT, 2];

    /// <summary>Lv50 称号是否已解锁</summary>
    private bool[] _titleUnlocked = new bool[SKILL_COUNT];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < SKILL_COUNT; i++)
        {
            _level[i] = 1;
            _branches[i, 0] = ProficiencyBranch.None;
            _branches[i, 1] = ProficiencyBranch.None;
        }
    }

    void Start()
    {
        SubscribeXpSources();
    }

    void OnDestroy()
    {
        UnsubscribeXpSources();
    }

    // ==================== XP 自动挂接（订阅 EventBus） ====================

    void SubscribeXpSources()
    {
        // 种植
        EventBus.Subscribe<string>(GameEvent.CropPlanted, _ => AddXP(ProficiencySkill.Farming, 5));
        EventBus.Subscribe<string>(GameEvent.CropHarvested, _ => AddXP(ProficiencySkill.Farming, 10));
        EventBus.Subscribe<string>(GameEvent.FruitTreeHarvested, _ => AddXP(ProficiencySkill.Farming, 15));
        EventBus.Subscribe<string>(GameEvent.BeehiveHarvested, _ => AddXP(ProficiencySkill.Farming, 15));

        // 畜牧
        EventBus.Subscribe<string>(GameEvent.AnimalFed, _ => AddXP(ProficiencySkill.Husbandry, 3));
        EventBus.Subscribe<string>(GameEvent.AnimalPetted, _ => AddXP(ProficiencySkill.Husbandry, 2));
        EventBus.Subscribe<string>(GameEvent.AnimalProductCollected, _ => AddXP(ProficiencySkill.Husbandry, 10));
        EventBus.Subscribe<string>(GameEvent.DragonFed, _ => AddXP(ProficiencySkill.Husbandry, 5));
        EventBus.Subscribe<string>(GameEvent.DragonPetted, _ => AddXP(ProficiencySkill.Husbandry, 3));
        EventBus.Subscribe<string>(GameEvent.DragonEggLaid, _ => AddXP(ProficiencySkill.Husbandry, 30));

        // 钓鱼
        EventBus.Subscribe<string>(GameEvent.FishCaught, _ => AddXP(ProficiencySkill.Fishing, 10));

        // 战斗
        EventBus.Subscribe<string>(GameEvent.BattleEnded, result =>
        {
            if (result == "victory") AddXP(ProficiencySkill.Combat, 30);
        });
        EventBus.Subscribe<string>(GameEvent.ElementReactionTriggered, _ => AddXP(ProficiencySkill.Combat, 5));

        // 采集（矿洞系统事件）
        EventBus.Subscribe<string>(GameEvent.OreMined, _ => AddXP(ProficiencySkill.Gathering, 8));

        // 社交
        EventBus.Subscribe<int>(GameEvent.NPCAffectionChanged, change =>
        {
            if (change > 0) AddXP(ProficiencySkill.Social, Mathf.Abs(change));
        });
    }

    void UnsubscribeXpSources()
    {
        // 简化：不逐个反订阅（单例生命周期内有效）
    }

    // ==================== XP / 等级 ====================

    /// <summary>增加熟练度 XP</summary>
    public void AddXP(ProficiencySkill skill, int amount)
    {
        if (amount <= 0) return;
        int idx = (int)skill;
        if (idx < 0 || idx >= SKILL_COUNT) return;

        _xp[idx] += amount;
        EventBus.Publish(GameEvent.ProficiencyXPChanged, idx);

        // 连续升级检测
        while (_level[idx] < MAX_LEVEL && _xp[idx] >= GetXpForLevel(_level[idx]))
        {
            _xp[idx] -= GetXpForLevel(_level[idx]);
            _level[idx]++;
            OnLevelUp(skill, _level[idx]);
        }
    }

    void OnLevelUp(ProficiencySkill skill, int newLevel)
    {
        EventBus.Publish(GameEvent.ProficiencyLeveledUp, (int)skill);
        Debug.Log($"{ProficiencyUtils.GetSkillName(skill)} 熟练度提升至 Lv{newLevel}");

        if (newLevel >= 50 && !_titleUnlocked[(int)skill])
        {
            _titleUnlocked[(int)skill] = true;
            Debug.Log($"★ 解锁王牌称号: {ProficiencyUtils.GetTitle(skill)}");
        }
    }

    /// <summary>获取熟练度等级</summary>
    public int GetLevel(ProficiencySkill skill) => _level[(int)skill];

    /// <summary>获取熟练度 XP</summary>
    public int GetXP(ProficiencySkill skill) => _xp[(int)skill];

    /// <summary>获取升级所需 XP</summary>
    public int GetXpToNext(ProficiencySkill skill)
    {
        int lvl = _level[(int)skill];
        return lvl >= MAX_LEVEL ? 0 : GetXpForLevel(lvl);
    }

    /// <summary>获取升级进度（0.0-1.0）</summary>
    public float GetLevelProgress(ProficiencySkill skill)
    {
        int xpToNext = GetXpToNext(skill);
        return xpToNext <= 0 ? 1f : (float)_xp[(int)skill] / xpToNext;
    }

    // ==================== 专精分支 ====================

    /// <summary>选择专精分支（tier 1 = Lv10, tier 2 = Lv30）</summary>
    public bool ChooseBranch(ProficiencySkill skill, int tier, ProficiencyBranch branch)
    {
        int idx = (int)skill;
        if (tier != 1 && tier != 2) return false;
        if (branch == ProficiencyBranch.None) return false;

        int tierIdx = tier - 1;
        if (_branches[idx, tierIdx] != ProficiencyBranch.None) return false; // 已选择

        // 检查等级是否达标
        int requiredLevel = tier == 1 ? 10 : 30;
        if (_level[idx] < requiredLevel) return false;

        _branches[idx, tierIdx] = branch;
        Debug.Log($"已选择 {ProficiencyUtils.GetSkillName(skill)} {ProficiencyUtils.GetBranchName(skill, branch, tier)}");
        return true;
    }

    /// <summary>获取已选分支</summary>
    public ProficiencyBranch GetBranch(ProficiencySkill skill, int tier)
    {
        return _branches[(int)skill, tier - 1];
    }

    /// <summary>是否已解锁称号</summary>
    public bool HasTitle(ProficiencySkill skill) => _titleUnlocked[(int)skill];

    // ==================== 通用收益计算器 ====================

    /// <summary>作物生长速度加成（Farming × 0.5%）</summary>
    public float GetCropGrowthBonus() => _level[(int)ProficiencySkill.Farming] * 0.005f;

    /// <summary>作物品质提升概率（Farming × 0.3%）</summary>
    public float GetCropQualityBonus() => _level[(int)ProficiencySkill.Farming] * 0.003f;

    /// <summary>家畜心情上限加成（Husbandry × 0.5）</summary>
    public float GetAnimalMoodBonus() => _level[(int)ProficiencySkill.Husbandry] * 0.5f;

    /// <summary>钓鱼QTE稳定条加成（Fishing × 0.5%）</summary>
    public float GetFishQteBonus() => _level[(int)ProficiencySkill.Fishing] * 0.005f;

    /// <summary>稀有鱼出现概率加成（Fishing × 0.2%）</summary>
    public float GetRareFishBonus() => _level[(int)ProficiencySkill.Fishing] * 0.002f;

    /// <summary>战斗基础攻击加成（Combat × 0.3）</summary>
    public float GetCombatAttackBonus() => _level[(int)ProficiencySkill.Combat] * 0.3f;

    /// <summary>战斗暴击率加成（Combat × 0.05%）</summary>
    public float GetCombatCritBonus() => _level[(int)ProficiencySkill.Combat] * 0.0005f;

    /// <summary>采集掉落加成（Gathering × 0.3%）</summary>
    public float GetGatherDropBonus() => _level[(int)ProficiencySkill.Gathering] * 0.003f;

    /// <summary>社交礼物效果加成（Social × 0.5%）</summary>
    public float GetSocialGiftBonus() => _level[(int)ProficiencySkill.Social] * 0.005f;

    // ==================== 存档 ====================

    [System.Serializable]
    public class ProficiencySaveData
    {
        public int[] xp;
        public int[] level;
        public int[] branchTier1;   // 0=None, 1=A, 2=B
        public int[] branchTier2;
        public bool[] titleUnlocked;
    }

    public ProficiencySaveData GetSaveData()
    {
        var data = new ProficiencySaveData
        {
            xp = (int[])_xp.Clone(),
            level = (int[])_level.Clone(),
            branchTier1 = new int[SKILL_COUNT],
            branchTier2 = new int[SKILL_COUNT],
            titleUnlocked = (bool[])_titleUnlocked.Clone()
        };
        for (int i = 0; i < SKILL_COUNT; i++)
        {
            data.branchTier1[i] = (int)_branches[i, 0];
            data.branchTier2[i] = (int)_branches[i, 1];
        }
        return data;
    }

    public void LoadSaveData(ProficiencySaveData data)
    {
        if (data?.xp == null) return;
        for (int i = 0; i < SKILL_COUNT && i < data.xp.Length; i++)
        {
            _xp[i] = data.xp[i];
            _level[i] = data.level != null && i < data.level.Length ? data.level[i] : 1;
            if (data.branchTier1 != null && i < data.branchTier1.Length)
                _branches[i, 0] = (ProficiencyBranch)data.branchTier1[i];
            if (data.branchTier2 != null && i < data.branchTier2.Length)
                _branches[i, 1] = (ProficiencyBranch)data.branchTier2[i];
            if (data.titleUnlocked != null && i < data.titleUnlocked.Length)
                _titleUnlocked[i] = data.titleUnlocked[i];
        }
    }
}
