using System.Collections.Generic;

/// <summary>
/// 战斗指令类型 — GDD 6.5.1
/// </summary>
public enum BattleCommandType
{
    Attack,       // 攻击
    Skill,        // 技能
    Item,         // 道具
    Capture,      // 捕捉
    Defend,       // 防御
    SwitchDragon, // 换龙
    Flee          // 逃跑
}

/// <summary>
/// 战斗类型
/// </summary>
public enum BattleType
{
    WildEncounter,  // 野外遇敌
    Boss,           // BOSS战
    Tutorial        // 教程战（M1 BOSS 石甲犀）
}

/// <summary>
/// 战斗指令
/// </summary>
public struct BattleCommand
{
    public BattleCommandType type;
    public CombatUnit attacker;
    public CombatUnit target;
    public string skillId;
    public string itemId;
    public int dragonIndex;

    public static BattleCommand Attack(CombatUnit attacker, CombatUnit target)
        => new() { type = BattleCommandType.Attack, attacker = attacker, target = target };

    public static BattleCommand Skill(CombatUnit attacker, CombatUnit target, string skillId)
        => new() { type = BattleCommandType.Skill, attacker = attacker, target = target, skillId = skillId };

    public static BattleCommand UseItem(CombatUnit attacker, CombatUnit target, string itemId)
        => new() { type = BattleCommandType.Item, attacker = attacker, target = target, itemId = itemId };

    public static BattleCommand Capture(CombatUnit attacker, CombatUnit target, string itemId)
        => new() { type = BattleCommandType.Capture, attacker = attacker, target = target, itemId = itemId };

    public static BattleCommand Defend(CombatUnit unit)
        => new() { type = BattleCommandType.Defend, attacker = unit };

    public static BattleCommand SwitchDragon(CombatUnit unit, int index)
        => new() { type = BattleCommandType.SwitchDragon, attacker = unit, dragonIndex = index };

    public static BattleCommand Flee(CombatUnit unit)
        => new() { type = BattleCommandType.Flee, attacker = unit };
}

/// <summary>
/// 战斗阶段
/// </summary>
public enum BattlePhase
{
    Start,            // 战斗开始
    TurnStart,        // 回合开始（DOT结算等）
    SelectCommand,    // 选择指令
    SelectTarget,     // 选择目标
    ExecuteAction,    // 执行行动
    ApplyResult,      // 应用结果
    TurnEnd,          // 回合结束（状态清理）
    Victory,          // 胜利
    Defeat,           // 失败
    Fled              // 逃跑
}
