using UnityEngine;

/// <summary>
/// 暴击系统 — 对应 GDD 6.5.2
/// 基础暴击率: 人物5% / 龙3%
/// 敏捷加成: 每10敏捷+1%
/// 武器加成: 单手剑+5% / 双手剑+2% / 弓箭+3%
/// 敏捷差>50时高方+10%
/// 冻结/眩晕/石化下被暴击+20%
/// 暴击伤害: 基础×1.5
/// </summary>
public static class CritSystem
{
    /// <summary>基础暴击率</summary>
    public static float GetBaseCritRate(CombatUnit unit)
    {
        return unit.UnitType switch
        {
            CombatUnitType.Player => 0.05f,
            CombatUnitType.Dragon => 0.03f,
            CombatUnitType.NPCAlly => 0.05f,
            CombatUnitType.WildAnimal => 0.02f,
            CombatUnitType.Boss => 0.05f,
            _ => 0.02f
        };
    }

    /// <summary>敏捷暴击加成（每10敏捷+1%）</summary>
    public static float GetAgilityCritBonus(int agility)
    {
        return agility / 10 * 0.01f;
    }

    /// <summary>武器暴击加成</summary>
    public static float GetWeaponCritBonus(WeaponSubType weaponType)
    {
        return WeaponTypeStats.GetCritRateBonus(weaponType);
    }

    /// <summary>敏捷差碾压（差>50时高方+10%）</summary>
    public static float GetAgilityCrushBonus(int atkAgility, int defAgility)
    {
        int diff = atkAgility - defAgility;
        return diff > 50 ? 0.10f : 0f;
    }

    /// <summary>异常目标暴击加成（冻结/眩晕/石化下+20%）</summary>
    public static float GetStatusCritBonus(CombatUnit target)
    {
        if (target.HasStatus(CombatStatus.Frozen) ||
            target.HasStatus(CombatStatus.Stunned))
            return 0.20f;
        return 0f;
    }

    /// <summary>计算总暴击率</summary>
    public static float GetTotalCritRate(CombatUnit attacker, CombatUnit target)
    {
        float rate = GetBaseCritRate(attacker);
        rate += GetAgilityCritBonus(attacker.Agility);
        rate += GetWeaponCritBonus(attacker.WeaponType);
        rate += GetAgilityCrushBonus(attacker.Agility, target.Agility);
        rate += GetStatusCritBonus(target);
        return Mathf.Clamp01(rate);
    }

    /// <summary>暴击伤害倍率（基础1.5×）</summary>
    public static float GetCritDamageMultiplier(CombatUnit unit)
    {
        return 1.5f;
    }

    /// <summary>判定是否暴击</summary>
    public static bool RollCrit(CombatUnit attacker, CombatUnit target)
    {
        float rate = GetTotalCritRate(attacker, target);
        return Random.value < rate;
    }

    /// <summary>获取暴击率显示文本</summary>
    public static string GetCritRateText(CombatUnit unit, CombatUnit target)
    {
        float rate = GetTotalCritRate(unit, target);
        return $"{rate * 100:F1}%";
    }
}
