using UnityEngine;

/// <summary>
/// BOSS 数据 — 对应 GDD 6.2
/// 支持双阶段（HP 低于阈值时进入第二阶段）
/// </summary>
[CreateAssetMenu(menuName = "Combat/Boss Data", fileName = "NewBoss")]
public class BossData : ScriptableObject
{
    [Header("基本信息")]
    public string bossId;
    public string bossName;
    [TextArea(2, 4)]
    public string description;

    [Header("元素")]
    public DragonElement element;
    public DragonElement? secondaryElement;
    [Tooltip("弱点元素 — 受该元素攻击额外+50%伤害")]
    public DragonElement? weakElement;

    [Header("第一/唯一阶段属性")]
    public int maxHP = 100;
    public int attack = 20;
    public int defense = 15;
    public int agility = 12;
    public bool isFlying;

    [Header("第二阶段（可选）")]
    [Tooltip("第二阶段HP阈值（当前HP/最大HP ≤ 此值时触发，0=无第二阶段）")]
    [Range(0f, 1f)] public float phase2HPThreshold;
    public int phase2MaxHP;
    public int phase2Attack;
    public int phase2Defense;

    [Header("捕捉")]
    [Tooltip("是否可捕捉（null=不可）")]
    public bool isCapturable;
    public string captureItemId = "rope_net";

    [Header("奖励")]
    public int rewardGold = 500;
    public int rewardExp = 100;
    [Tooltip("掉落物品ID")]
    public string[] rewardItemIds;
    [Tooltip("掉落物品概率（0-1，与rewardItemIds一一对应）")]
    public float[] rewardItemChances;

    /// <summary>是否有多阶段</summary>
    public bool HasPhase2 => phase2HPThreshold > 0f;

    /// <summary>构建 BOSS 战斗单位</summary>
    public CombatUnit BuildCombatUnit()
    {
        var unit = new CombatUnit(bossId, bossName, CombatUnitType.Boss)
        {
            Element = element,
            SecondaryElement = secondaryElement,
            WeakElement = weakElement,
            MaxHP = maxHP,
            Attack = attack,
            Defense = defense,
            Agility = agility,
            IsFlying = isFlying,
            IsCapturable = isCapturable,
            CaptureItemId = captureItemId,
            SourceRef = this
        };
        unit.FullHeal();
        return unit;
    }

    /// <summary>检查是否进入第二阶段，并切换属性</summary>
    public void CheckPhaseTransition(CombatUnit unit)
    {
        if (!HasPhase2 || unit.CurrentHP <= 0) return;
        if (unit.HPPercent > phase2HPThreshold) return;

        // 切换到第二阶段
        unit.MaxHP = phase2MaxHP;
        unit.Attack = phase2Attack;
        unit.Defense = phase2Defense;
        unit.CurrentHP = unit.MaxHP;

        Debug.Log($"{bossName} 进入第二阶段!");
    }
}
