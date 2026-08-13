using UnityEngine;

/// <summary>
/// 元素反应类型 — 对应 GDD 6.3.2
/// </summary>
public enum ElementReactionType
{
    None,       // 无反应
    Vaporize,   // 蒸发：火↔水
    Melt,       // 融化：火↔冰
    Overload,   // 超载：火↔雷
    ElectroCharge, // 感电：雷↔水
    Superconduct,  // 超导：雷↔冰
    Burning,    // 燃烧：火↔草
    Bloom,      // 绽放：水↔草
    Aggravate,  // 激化：雷↔草
    Freeze,     // 冻结：水↔冰
    Crystallize, // 结晶：岩+火/水/冰/雷
    Swirl       // 扩散：风+火/水/冰/雷
}

/// <summary>
/// 元素反应结果
/// </summary>
public struct ElementReactionResult
{
    public ElementReactionType type;
    public float damageMultiplier;   // 伤害倍率
    public int dotDuration;          // DOT 持续回合（0=无DOT）
    public float dotMultiplier;      // DOT 每回合倍率
    public bool freezeTarget;        // 是否冻结目标
    public bool generateShield;      // 是否生成护盾
    public bool aoeSplash;           // 是否范围溅射
    public int defReductionTurns;    // 减防持续回合
}

/// <summary>
/// 元素反应系统 — 对应 GDD 6.3.2
/// 11 种元素反应表 + 伤害计算
/// </summary>
public static class ElementReactionSystem
{
    /// <summary>
    /// 获取两个元素之间的反应
    /// atk = 攻击方元素, def = 防御方已有元素
    /// </summary>
    public static ElementReactionResult GetReaction(DragonElement atk, DragonElement def)
    {
        // 火 ↔ 水：蒸发
        if (IsPair(atk, def, DragonElement.Pyro, DragonElement.Hydro))
        {
            // 火打水 ×1.5，水打火 ×2.0
            float mult = atk == DragonElement.Pyro ? 1.5f : 2.0f;
            return new ElementReactionResult
            {
                type = ElementReactionType.Vaporize,
                damageMultiplier = mult
            };
        }

        // 火 ↔ 冰：融化
        if (IsPair(atk, def, DragonElement.Pyro, DragonElement.Cryo))
        {
            // 火打冰 ×2.0，冰打火 ×1.5
            float mult = atk == DragonElement.Pyro ? 2.0f : 1.5f;
            return new ElementReactionResult
            {
                type = ElementReactionType.Melt,
                damageMultiplier = mult
            };
        }

        // 火 ↔ 雷：超载
        if (IsPair(atk, def, DragonElement.Pyro, DragonElement.Electro))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.Overload,
                damageMultiplier = 1.2f,
                aoeSplash = true
            };
        }

        // 雷 ↔ 水：感电
        if (IsPair(atk, def, DragonElement.Electro, DragonElement.Hydro))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.ElectroCharge,
                damageMultiplier = 0.8f,
                dotDuration = 3,
                dotMultiplier = 0.4f
            };
        }

        // 雷 ↔ 冰：超导
        if (IsPair(atk, def, DragonElement.Electro, DragonElement.Cryo))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.Superconduct,
                damageMultiplier = 0.7f,
                defReductionTurns = 2
            };
        }

        // 火 ↔ 草：燃烧
        if (IsPair(atk, def, DragonElement.Pyro, DragonElement.Dendro))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.Burning,
                damageMultiplier = 0.6f,
                dotDuration = 4,
                dotMultiplier = 0.5f
            };
        }

        // 水 ↔ 草：绽放
        if (IsPair(atk, def, DragonElement.Hydro, DragonElement.Dendro))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.Bloom,
                damageMultiplier = 1.0f
            };
        }

        // 雷 ↔ 草：激化
        if (IsPair(atk, def, DragonElement.Electro, DragonElement.Dendro))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.Aggravate,
                damageMultiplier = 1.15f
            };
        }

        // 水 ↔ 冰：冻结
        if (IsPair(atk, def, DragonElement.Hydro, DragonElement.Cryo))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.Freeze,
                damageMultiplier = 0.5f,
                freezeTarget = true
            };
        }

        // 岩 + 火/水/冰/雷：结晶
        if (atk == DragonElement.Geo && IsOneOf(def, DragonElement.Pyro, DragonElement.Hydro, DragonElement.Cryo, DragonElement.Electro))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.Crystallize,
                damageMultiplier = 1.0f,
                generateShield = true
            };
        }

        // 风 + 火/水/冰/雷：扩散
        if (atk == DragonElement.Anemo && IsOneOf(def, DragonElement.Pyro, DragonElement.Hydro, DragonElement.Cryo, DragonElement.Electro))
        {
            return new ElementReactionResult
            {
                type = ElementReactionType.Swirl,
                damageMultiplier = 0.8f,
                aoeSplash = true
            };
        }

        // 无反应
        return new ElementReactionResult
        {
            type = ElementReactionType.None,
            damageMultiplier = 1.0f
        };
    }

    /// <summary>
    /// 计算元素反应后的最终伤害
    /// </summary>
    public static int CalcReactionDamage(int baseDamage, DragonElement atkElement, DragonElement defElement)
    {
        var reaction = GetReaction(atkElement, defElement);
        if (reaction.type == ElementReactionType.None) return baseDamage;

        int result = Mathf.RoundToInt(baseDamage * reaction.damageMultiplier);

        // 同属性抗性 ×0.5
        if (atkElement == defElement)
            result = Mathf.RoundToInt(result * 0.5f);

        EventBus.Publish(GameEvent.ElementReactionTriggered, reaction.type.ToString());

        return result;
    }

    /// <summary>反应中文名</summary>
    public static string GetReactionName(ElementReactionType type) => type switch
    {
        ElementReactionType.Vaporize       => "蒸发",
        ElementReactionType.Melt           => "融化",
        ElementReactionType.Overload       => "超载",
        ElementReactionType.ElectroCharge  => "感电",
        ElementReactionType.Superconduct   => "超导",
        ElementReactionType.Burning        => "燃烧",
        ElementReactionType.Bloom          => "绽放",
        ElementReactionType.Aggravate      => "激化",
        ElementReactionType.Freeze         => "冻结",
        ElementReactionType.Crystallize    => "结晶",
        ElementReactionType.Swirl          => "扩散",
        _ => "无"
    };

    // ==================== 辅助 ====================

    private static bool IsPair(DragonElement a, DragonElement b, DragonElement e1, DragonElement e2)
        => (a == e1 && b == e2) || (a == e2 && b == e1);

    private static bool IsOneOf(DragonElement e, params DragonElement[] targets)
    {
        foreach (var t in targets)
            if (e == t) return true;
        return false;
    }
}
