using UnityEngine;

/// <summary>
/// 伤害计算结果
/// </summary>
public struct DamageResult
{
    public int finalDamage;
    public bool isCrit;
    public ElementReactionType reactionType;
    public bool freezeApplied;
    public bool dotApplied;
    public bool shieldGenerated;
    public bool aoeSplash;
    public int defReductionTurns;
    public string summary;  // 日志文本
}

/// <summary>
/// 伤害计算器 — 六步计算流程
/// 1) 基础伤害 = 攻击力
/// 2) 减去防御 = max(1, base - def)
/// 3) × 元素抗性 (同元素×0.5, 双属性取较高抗性)
/// 4) × 天气修正 (GDD 4.3.5)
/// 5) × 元素反应倍率 (ElementReactionSystem)
/// 6) × 暴击倍率 (CritSystem)
/// 对应 GDD 6.3 + 6.5
/// </summary>
public static class DamageCalculator
{
    /// <summary>计算最终伤害</summary>
    public static DamageResult Calculate(CombatUnit attacker, CombatUnit defender, int weatherIndex)
    {
        var result = new DamageResult { reactionType = ElementReactionType.None };

        // Step 1: 基础伤害
        int baseDamage = attacker.Attack;

        // Step 2: 减防御
        int defense = defender.EffectiveDefense;
        int afterDef = Mathf.Max(1, baseDamage - defense);

        // Step 3: 元素抗性
        float elementMultiplier = GetElementResistanceMultiplier(attacker.Element, defender);
        int afterElement = Mathf.RoundToInt(afterDef * elementMultiplier);

        // Step 4: 天气修正
        float weatherMult = GetWeatherModifier(attacker.Element, weatherIndex);
        int afterWeather = Mathf.RoundToInt(afterElement * weatherMult);

        // Step 5: 元素反应
        DragonElement? defElement = defender.AttachedElement ?? defender.Element;
        var reaction = ElementReactionSystem.GetReaction(attacker.Element, defElement.Value);
        int afterReaction = Mathf.RoundToInt(afterWeather * reaction.damageMultiplier);
        result.reactionType = reaction.type;

        // 弱点元素加成（BOSS）
        if (defender.WeakElement.HasValue && attacker.Element == defender.WeakElement.Value)
            afterReaction = Mathf.RoundToInt(afterReaction * 1.5f);

        // Step 6: 暴击
        bool isCrit = CritSystem.RollCrit(attacker, defender);
        if (isCrit)
        {
            float critMult = CritSystem.GetCritDamageMultiplier(attacker);
            afterReaction = Mathf.RoundToInt(afterReaction * critMult);
        }
        result.isCrit = isCrit;

        // 防御指令减伤
        float finalMult = defender.DamageReductionMultiplier;
        result.finalDamage = Mathf.RoundToInt(afterReaction * finalMult);

        // 反应附加效果
        result.freezeApplied = reaction.freezeTarget;
        result.dotApplied = reaction.dotDuration > 0;
        result.shieldGenerated = reaction.generateShield;
        result.aoeSplash = reaction.aoeSplash;
        result.defReductionTurns = reaction.defReductionTurns;

        // 生成日志
        result.summary = BuildSummary(attacker, defender, result);

        // 设置防御方附着元素
        defender.AttachedElement = attacker.Element;

        return result;
    }

    /// <summary>获取元素抗性倍率</summary>
    static float GetElementResistanceMultiplier(DragonElement atkElement, CombatUnit defender)
    {
        // 同元素抗性 ×0.5
        bool primaryMatch = defender.Element == atkElement;
        bool secondaryMatch = defender.SecondaryElement.HasValue && defender.SecondaryElement.Value == atkElement;

        // 双属性个体：按两种抗性取较高者（即取较低的减伤倍率）
        if (primaryMatch || secondaryMatch) return 0.5f;

        return 1.0f;
    }

    /// <summary>天气对战斗的影响 — GDD 4.3.5</summary>
    static float GetWeatherModifier(DragonElement element, int weatherIndex)
    {
        // weatherIndex: 0=晴, 1=多云, 2=小雨, 3=大雨, 4=雷暴, 5=大风, 6=大雪
        return weatherIndex switch
        {
            2 => element == DragonElement.Pyro ? 0.7f : element == DragonElement.Hydro ? 1.2f : 1.0f,  // 小雨
            3 => element == DragonElement.Pyro ? 0.7f : element == DragonElement.Hydro ? 1.2f : 1.0f,  // 大雨
            4 => element == DragonElement.Electro ? 1.4f : 1.0f,  // 雷暴
            5 => element == DragonElement.Anemo ? 1.3f : 0.95f,   // 大风
            6 => element == DragonElement.Cryo ? 1.3f : element == DragonElement.Pyro ? 0.9f : 1.0f,  // 大雪
            _ => 1.0f  // 晴/多云
        };
    }

    /// <summary>构建伤害日志</summary>
    static string BuildSummary(CombatUnit attacker, CombatUnit defender, DamageResult result)
    {
        string s = $"{attacker.DisplayName} → {defender.DisplayName} 造成 {result.finalDamage} 伤害";
        if (result.reactionType != ElementReactionType.None)
            s += $" [{ElementReactionSystem.GetReactionName(result.reactionType)}]";
        if (result.isCrit) s += " [暴击!]";
        if (result.freezeApplied) s += " [冻结]";
        if (result.dotApplied) s += " [持续伤害]";
        return s;
    }

    /// <summary>计算合击伤害倍率 — GDD 6.5.3</summary>
    public const float COMBO_ATTACK_MULTIPLIER = 1.3f;

    /// <summary>计算合击伤害</summary>
    public static DamageResult CalculateCombo(CombatUnit attacker1, CombatUnit attacker2, CombatUnit defender, int weatherIndex)
    {
        // 取两个攻击者中较高的攻击力作为基础
        int comboBase = Mathf.Max(attacker1.Attack, attacker2.Attack);
        var tempAttacker = new CombatUnit("combo", "合击", CombatUnitType.Player)
        {
            Element = attacker1.Element,
            Attack = Mathf.RoundToInt(comboBase * COMBO_ATTACK_MULTIPLIER),
            Agility = Mathf.Max(attacker1.Agility, attacker2.Agility),
            WeaponType = attacker1.WeaponType
        };
        return Calculate(tempAttacker, defender, weatherIndex);
    }
}
