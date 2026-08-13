using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 战斗单位类型 — GDD 6.5
/// </summary>
public enum CombatUnitType
{
    Player,      // 玩家
    Dragon,      // 龙（玩家方）
    WildAnimal,  // 野怪
    Boss,        // BOSS
    NPCAlly      // NPC友方（剧情战）
}

/// <summary>
/// 战斗异常状态
/// </summary>
[System.Flags]
public enum CombatStatus
{
    None      = 0,
    Frozen    = 1 << 0,  // 冻结：跳过1回合
    Stunned   = 1 << 1,  // 眩晕：跳过1回合
    Burning   = 1 << 2,  // 燃烧DOT
    Electro   = 1 << 3,  // 感电DOT
    DefDown   = 1 << 4,  // 防御降低（超导）
    Shielded  = 1 << 5,  // 元素护盾（结晶）
    Defending = 1 << 6   // 防御指令中（减伤50%）
}

/// <summary>
/// DOT 持续伤害效果
/// </summary>
[System.Serializable]
public struct DotEffect
{
    public DragonElement element;
    public int damagePerTurn;
    public int remainingTurns;
}

/// <summary>
/// 战斗单位 — 统一封装玩家/龙/野怪/BOSS的战斗属性
/// 对应 GDD 6.5 回合制战斗系统
/// </summary>
public class CombatUnit
{
    // ==================== 身份 ====================
    public string UnitId { get; set; }
    public string DisplayName { get; set; }
    public CombatUnitType UnitType { get; set; }
    public Sprite Icon { get; set; }

    // ==================== 元素 ====================
    public DragonElement Element { get; set; }
    public DragonElement? SecondaryElement { get; set; }

    // 防御方当前附着元素（用于元素反应判定）
    public DragonElement? AttachedElement { get; set; }

    // ==================== 属性 ====================
    public int MaxHP { get; set; }
    public int CurrentHP { get; private set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Agility { get; set; }

    // 玩家专属
    public int MaxMP { get; set; }
    public int CurrentMP { get; private set; }
    public WeaponSubType WeaponType { get; set; } = WeaponSubType.None;

    // BOSS 专属
    public DragonElement? WeakElement { get; set; }
    public bool IsFlying { get; set; }

    // 捕捉
    public bool IsCapturable { get; set; }
    public string CaptureItemId { get; set; }

    // ==================== 状态 ====================
    public CombatStatus Status { get; set; }
    public List<DotEffect> DotEffects { get; private set; } = new();
    public int DefReductionTurns { get; set; }
    public bool IsAlive => CurrentHP > 0;

    // 关联的运行时对象（DragonController 等，用于捕捉后联动）
    public object SourceRef { get; set; }

    // ==================== 初始化 ====================

    public CombatUnit(string id, string name, CombatUnitType type)
    {
        UnitId = id;
        DisplayName = name;
        UnitType = type;
    }

    /// <summary>从 PlayerStats 构建玩家战斗单位</summary>
    public static CombatUnit FromPlayer(PlayerStats ps)
    {
        var eq = EquipmentManager.Instance;
        int weaponAtk = eq != null ? eq.GetCurrentWeaponAttack() : 0;
        int equipAtk = eq != null ? eq.GetAttackBonus() : 0;
        int equipDef = eq != null ? eq.GetDefenseBonus() : 0;

        var unit = new CombatUnit("player", "主角", CombatUnitType.Player)
        {
            Element = ps.PlayerElement ?? DragonElement.Dendro,
            MaxHP = ps.MaxHP,
            Attack = ps.Strength * 2 + weaponAtk + equipAtk,
            Defense = ps.Vitality + equipDef,
            Agility = ps.Agility,
            MaxMP = ps.MaxMP,
            CurrentMP = ps.CurrentMP,
            WeaponType = eq != null ? eq.GetCurrentWeaponData()?.weaponSubType ?? WeaponSubType.None : WeaponSubType.None
        };
        unit.CurrentHP = unit.MaxHP;
        return unit;
    }

    /// <summary>从 DragonController 构建龙战斗单位</summary>
    public static CombatUnit FromDragon(DragonController dragon)
    {
        var unit = new CombatUnit(dragon.Uid, dragon.Data?.speciesName ?? "龙", CombatUnitType.Dragon)
        {
            Element = dragon.PrimaryElement,
            SecondaryElement = dragon.SecondaryElement,
            MaxHP = dragon.MaxHP,
            Attack = dragon.GetEquippedAttack(),
            Defense = dragon.GetEquippedDefense(),
            Agility = dragon.Agility,
            IsCapturable = dragon.IsWild,
            CaptureItemId = "rope_net",
            SourceRef = dragon
        };
        unit.CurrentHP = dragon.CurrentHP > 0 ? dragon.CurrentHP : unit.MaxHP;
        return unit;
    }

    // ==================== 伤害/治疗 ====================

    /// <summary>受到伤害（返回实际扣血量）</summary>
    public int TakeDamage(int amount)
    {
        int actual = Mathf.Min(amount, CurrentHP);
        CurrentHP -= actual;
        return actual;
    }

    /// <summary>治疗</summary>
    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    /// <summary>回满 HP（初始化/复活用）</summary>
    public void FullHeal()
    {
        CurrentHP = MaxHP;
    }

    /// <summary>消耗MP</summary>
    public bool ConsumeMP(int amount)
    {
        if (CurrentMP < amount) return false;
        CurrentMP -= amount;
        return true;
    }

    // ==================== 状态管理 ====================

    /// <summary>添加状态</summary>
    public void AddStatus(CombatStatus status) => Status |= status;

    /// <summary>移除状态</summary>
    public void RemoveStatus(CombatStatus status) => Status &= ~status;

    /// <summary>是否有某状态</summary>
    public bool HasStatus(CombatStatus status) => (Status & status) != 0;

    /// <summary>是否无法行动（冻结/眩晕）</summary>
    public bool CannotAct => HasStatus(CombatStatus.Frozen) || HasStatus(CombatStatus.Stunned);

    /// <summary>是否防御中（减伤50%）</summary>
    public bool IsDefending => HasStatus(CombatStatus.Defending);

    /// <summary>防御减伤倍率</summary>
    public float DamageReductionMultiplier => IsDefending ? 0.5f : 1.0f;

    /// <summary>添加DOT效果</summary>
    public void AddDot(DragonElement element, int damagePerTurn, int duration)
    {
        DotEffects.Add(new DotEffect { element = element, damagePerTurn = damagePerTurn, remainingTurns = duration });
        if (element == DragonElement.Pyro) AddStatus(CombatStatus.Burning);
        else if (element == DragonElement.Electro) AddStatus(CombatStatus.Electro);
    }

    /// <summary>每回合结算DOT（返回总伤害）</summary>
    public int ProcessDots()
    {
        int totalDamage = 0;
        for (int i = DotEffects.Count - 1; i >= 0; i--)
        {
            var dot = DotEffects[i];
            totalDamage += dot.damagePerTurn;
            dot.remainingTurns--;
            if (dot.remainingTurns <= 0)
            {
                if (dot.element == DragonElement.Pyro) RemoveStatus(CombatStatus.Burning);
                else if (dot.element == DragonElement.Electro) RemoveStatus(CombatStatus.Electro);
                DotEffects.RemoveAt(i);
            }
            else
            {
                DotEffects[i] = dot;
            }
        }
        if (totalDamage > 0) TakeDamage(totalDamage);
        return totalDamage;
    }

    /// <summary>每回合结束清理过期状态</summary>
    public void EndTurnCleanup()
    {
        // 冻结/眩晕只持续1回合
        if (HasStatus(CombatStatus.Frozen)) RemoveStatus(CombatStatus.Frozen);
        if (HasStatus(CombatStatus.Stunned)) RemoveStatus(CombatStatus.Stunned);

        // 防御指令只持续当回合
        if (HasStatus(CombatStatus.Defending)) RemoveStatus(CombatStatus.Defending);

        // 减防倒计时
        if (DefReductionTurns > 0)
        {
            DefReductionTurns--;
            if (DefReductionTurns <= 0) RemoveStatus(CombatStatus.DefDown);
        }
    }

    /// <summary>获取有效防御力（含减防状态）</summary>
    public int EffectiveDefense => HasStatus(CombatStatus.DefDown) ? Mathf.RoundToInt(Defense * 0.8f) : Defense;

    /// <summary>HP 百分比（0.0~1.0）</summary>
    public float HPPercent => (float)CurrentHP / MaxHP;

    /// <summary>MP 百分比</summary>
    public float MPPercent => MaxMP > 0 ? (float)CurrentMP / MaxMP : 0f;
}
