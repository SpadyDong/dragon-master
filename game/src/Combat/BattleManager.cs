using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 回合制战斗管理器 — GDD 6.5
/// 管理战斗全流程：初始化 → 行动队列 → 指令执行 → 胜负判定
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("战斗状态")]
    public BattlePhase CurrentPhase = BattlePhase.Start;
    public BattleType BattleType = BattleType.WildEncounter;
    public int TurnCount { get; private set; }

    /// <summary>玩家方单位</summary>
    public List<CombatUnit> PlayerUnits { get; private set; } = new();
    /// <summary>敌方单位</summary>
    public List<CombatUnit> EnemyUnits { get; private set; } = new();
    /// <summary>行动顺序队列</summary>
    public List<CombatUnit> ActionQueue { get; private set; } = new();
    /// <summary>当前行动单位索引</summary>
    public int CurrentUnitIndex { get; private set; }
    /// <summary>当前行动单位</summary>
    public CombatUnit CurrentUnit => (CurrentUnitIndex >= 0 && CurrentUnitIndex < ActionQueue.Count)
        ? ActionQueue[CurrentUnitIndex] : null;

    /// <summary>战斗日志</summary>
    public List<string> BattleLog { get; private set; } = new();
    /// <summary>上一回合的合击配对（用于检测合击）</summary>
    private CombatUnit _lastComboPartner;
    private CombatUnit _lastComboTarget;

    void Awake()
    {
        Instance = this;
    }

    // ==================== 初始化 ====================

    /// <summary>初始化战斗</summary>
    public void InitBattle(List<CombatUnit> playerUnits, List<CombatUnit> enemyUnits, BattleType type)
    {
        PlayerUnits = playerUnits;
        EnemyUnits = enemyUnits;
        BattleType = type;
        TurnCount = 0;
        BattleLog.Clear();

        GameManager.Instance.SetState(GameState.Combat);
        EventBus.Publish(GameEvent.BattleStarted, type.ToString());

        BuildActionQueue();
        CurrentPhase = BattlePhase.TurnStart;
        StartNextTurn();

        Log("=== 战斗开始 ===");
        foreach (var u in enemyUnits)
            Log($"遭遇: {u.DisplayName} (HP:{u.MaxHP} 元素:{DragonElementUtils.GetElementName(u.Element)})");
    }

    /// <summary>构建敏捷排序的行动队列</summary>
    void BuildActionQueue()
    {
        ActionQueue.Clear();

        // 弓箭先手
        foreach (var u in PlayerUnits)
            if (u.IsAlive && WeaponTypeStats.IsFirstStrike(u.WeaponType))
                ActionQueue.Add(u);

        // 合并所有存活单位
        var all = new List<CombatUnit>();
        all.AddRange(PlayerUnits);
        all.AddRange(EnemyUnits);

        // 按敏捷降序排序（排除已加入的弓箭先手单位）
        all.Sort((a, b) => b.Agility.CompareTo(a.Agility));
        foreach (var u in all)
        {
            if (u.IsAlive && !ActionQueue.Contains(u))
                ActionQueue.Add(u);
        }

        CurrentUnitIndex = 0;
    }

    // ==================== 回合管理 ====================

    /// <summary>开始下一回合</summary>
    void StartNextTurn()
    {
        TurnCount++;
        CurrentPhase = BattlePhase.TurnStart;

        // DOT 结算
        foreach (var unit in ActionQueue)
        {
            if (!unit.IsAlive) continue;
            int dotDmg = unit.ProcessDots();
            if (dotDmg > 0)
                Log($"{unit.DisplayName} 受到持续伤害 {dotDmg}");
            if (!unit.IsAlive)
                OnUnitDefeated(unit);
        }

        // 检查胜负
        if (CheckBattleEnd()) return;

        // 找到下一个可行动的单位
        AdvanceToNextActor();
    }

    /// <summary>推进到下一个可行动的单位</summary>
    void AdvanceToNextActor()
    {
        while (CurrentUnitIndex < ActionQueue.Count)
        {
            var unit = ActionQueue[CurrentUnitIndex];
            if (!unit.IsAlive)
            {
                CurrentUnitIndex++;
                continue;
            }

            // 冻结/眩晕跳过
            if (unit.CannotAct)
            {
                Log($"{unit.DisplayName} 被冻结/眩晕，无法行动");
                unit.EndTurnCleanup();
                CurrentUnitIndex++;
                continue;
            }

            // 到达可行动单位
            CurrentPhase = BattlePhase.SelectCommand;
            EventBus.Publish(GameEvent.TurnChanged, TurnCount);
            Log($"--- 第{TurnCount}回合 — {unit.DisplayName}行动 ---");
            return;
        }

        // 一轮结束，重新排序
        if (!CheckBattleEnd())
        {
            BuildActionQueue();
            StartNextTurn();
        }
    }

    // ==================== 指令执行 ====================

    /// <summary>执行战斗指令</summary>
    public void ExecuteCommand(BattleCommand command)
    {
        CurrentPhase = BattlePhase.ExecuteAction;
        int weather = GameManager.Instance != null ? GameManager.Instance.weatherIndex : 0;

        switch (command.type)
        {
            case BattleCommandType.Attack:
                ExecuteAttack(command.attacker, command.target, weather);
                break;
            case BattleCommandType.Skill:
                ExecuteSkill(command.attacker, command.target, command.skillId, weather);
                break;
            case BattleCommandType.Item:
                ExecuteItem(command.attacker, command.target, command.itemId);
                break;
            case BattleCommandType.Capture:
                ExecuteCapture(command.attacker, command.target, command.itemId);
                break;
            case BattleCommandType.Defend:
                ExecuteDefend(command.attacker);
                break;
            case BattleCommandType.SwitchDragon:
                ExecuteSwitchDragon(command.attacker, command.dragonIndex);
                break;
            case BattleCommandType.Flee:
                ExecuteFlee(command.attacker);
                return;
        }

        EndAction();
    }

    /// <summary>攻击指令</summary>
    void ExecuteAttack(CombatUnit attacker, CombatUnit target, int weather)
    {
        // 合击检测：相邻人龙攻击同一目标
        bool isCombo = CheckComboAttack(attacker, target);

        DamageResult result;
        if (isCombo)
        {
            var partner = GetComboPartner(attacker);
            result = DamageCalculator.CalculateCombo(attacker, partner, target, weather);
            Log($"★ 合击! {attacker.DisplayName} + {partner.DisplayName} → {target.DisplayName}");
            _lastComboPartner = attacker;
            _lastComboTarget = target;
        }
        else
        {
            result = DamageCalculator.Calculate(attacker, target, weather);
        }

        target.TakeDamage(result.finalDamage);
        Log(result.summary);

        // 应用反应附加效果
        ApplyReactionEffects(attacker, target, result);

        // BOSS 二阶段检测
        CheckBossPhaseTransition(target);

        if (!target.IsAlive)
            OnUnitDefeated(target);
    }

    /// <summary>技能指令（简化版：消耗MP + 元素伤害）</summary>
    void ExecuteSkill(CombatUnit attacker, CombatUnit target, string skillId, int weather)
    {
        int mpCost = 20;
        if (!attacker.ConsumeMP(mpCost))
        {
            Log($"{attacker.DisplayName} 灵力不足，技能失败");
            return;
        }

        // 技能伤害 = 基础攻击×1.5
        var originalAtk = attacker.Attack;
        attacker.Attack = Mathf.RoundToInt(originalAtk * 1.5f);
        var result = DamageCalculator.Calculate(attacker, target, weather);
        attacker.Attack = originalAtk;

        target.TakeDamage(result.finalDamage);
        Log($"{attacker.DisplayName} 释放技能 → {result.summary}");

        ApplyReactionEffects(attacker, target, result);

        CheckBossPhaseTransition(target);

        if (!target.IsAlive)
            OnUnitDefeated(target);
    }

    /// <summary>道具指令</summary>
    void ExecuteItem(CombatUnit user, CombatUnit target, string itemId)
    {
        var item = Resources.Load<ItemData>($"Items/{itemId}");
        if (item == null) return;

        if (InventoryManager.Instance == null || !InventoryManager.Instance.RemoveItem(itemId, 1))
        {
            Log("道具不足");
            return;
        }

        if (item.hpRestore > 0)
        {
            target.Heal(item.hpRestore);
            Log($"{target.DisplayName} 恢复了 {item.hpRestore} HP");
        }
        if (item.staminaRestore > 0 && user.UnitType == CombatUnitType.Player)
        {
            PlayerStats.Instance?.RestoreStamina(item.staminaRestore);
        }

        EventBus.Publish(GameEvent.ItemUsed, itemId);
    }

    /// <summary>捕捉指令 — GDD 6.5.3</summary>
    void ExecuteCapture(CombatUnit attacker, CombatUnit target, string itemId)
    {
        if (!target.IsCapturable)
        {
            Log($"{target.DisplayName} 无法被捕捉");
            return;
        }

        if (InventoryManager.Instance == null || !InventoryManager.Instance.RemoveItem(itemId, 1))
        {
            Log("捕捉道具不足");
            return;
        }

        EventBus.Publish(GameEvent.CaptureAttempted, target.UnitId);

        // HP越低成功率越高
        float hpPercent = target.HPPercent;
        float baseRate = Mathf.Lerp(0.05f, 0.80f, 1f - hpPercent);
        float itemBonus = itemId switch
        {
            "fresh_meat" => 0.10f,
            "rope_net" => 0.15f,
            "anesthetic" => 0.25f,
            _ => 0f
        };
        float totalRate = Mathf.Clamp01(baseRate + itemBonus);

        if (Random.value < totalRate)
        {
            Log($"★ 捕捉成功! {target.DisplayName} 加入了你的队伍");
            EventBus.Publish(GameEvent.CaptureSuccess, target.UnitId);

            // 将龙加入玩家方
            if (target.SourceRef is DragonController dragon)
            {
                DragonManager.Instance?.OnDragonCaptured(dragon);
                dragon.SetInDen(true);
            }

            // 从敌方移除
            EnemyUnits.Remove(target);
            target.TakeDamage(target.CurrentHP); // 标记为"被捕获"

            if (CheckBattleEnd()) return;
        }
        else
        {
            Log($"捕捉失败... {target.DisplayName} 挣脱了");
        }
    }

    /// <summary>防御指令</summary>
    void ExecuteDefend(CombatUnit unit)
    {
        unit.AddStatus(CombatStatus.Defending);
        Log($"{unit.DisplayName} 进入防御姿态，受到伤害减半");
    }

    /// <summary>换龙指令</summary>
    void ExecuteSwitchDragon(CombatUnit current, int dragonIndex)
    {
        if (dragonIndex < 0 || dragonIndex >= PlayerUnits.Count) return;
        var newDragon = PlayerUnits[dragonIndex];
        if (!newDragon.IsAlive || newDragon == current) return;

        Log($"{current.DisplayName} 换上了 {newDragon.DisplayName}");

        // 在行动队列中替换
        int idx = ActionQueue.IndexOf(current);
        if (idx >= 0) ActionQueue[idx] = newDragon;
    }

    /// <summary>逃跑指令</summary>
    void ExecuteFlee(CombatUnit unit)
    {
        // 逃跑成功率基于敏捷对比
        int enemyAvgAgi = 0;
        int aliveEnemies = 0;
        foreach (var e in EnemyUnits)
        {
            if (e.IsAlive) { enemyAvgAgi += e.Agility; aliveEnemies++; }
        }
        if (aliveEnemies > 0) enemyAvgAgi /= aliveEnemies;

        float fleeRate = Mathf.Clamp01(0.3f + (unit.Agility - enemyAvgAgi) * 0.02f);

        if (Random.value < fleeRate)
        {
            Log("逃跑成功!");
            CurrentPhase = BattlePhase.Fled;
            EndBattle(false);
        }
        else
        {
            Log("逃跑失败...");
            EndAction();
        }
    }

    // ==================== 合击系统 ====================

    /// <summary>检测是否满足合击条件 — GDD 6.5.3</summary>
    bool CheckComboAttack(CombatUnit attacker, CombatUnit target)
    {
        // 上一回合人和龙攻击同一目标 → 触发合击
        if (_lastComboTarget == target && _lastComboPartner != null && _lastComboPartner != attacker)
        {
            // 一个是人类一个是龙
            bool attackerIsHuman = attacker.UnitType == CombatUnitType.Player || attacker.UnitType == CombatUnitType.NPCAlly;
            bool partnerIsDragon = _lastComboPartner.UnitType == CombatUnitType.Dragon;
            bool partnerIsHuman = _lastComboPartner.UnitType == CombatUnitType.Player || _lastComboPartner.UnitType == CombatUnitType.NPCAlly;
            bool attackerIsDragon = attacker.UnitType == CombatUnitType.Dragon;

            return (attackerIsHuman && partnerIsDragon) || (attackerIsDragon && partnerIsHuman);
        }
        return false;
    }

    /// <summary>获取合击搭档</summary>
    CombatUnit GetComboPartner(CombatUnit attacker)
    {
        return _lastComboPartner;
    }

    // ==================== 反应效果应用 ====================

    void ApplyReactionEffects(CombatUnit attacker, CombatUnit target, DamageResult result)
    {
        if (result.reactionType == ElementReactionType.None) return;

        // 冻结
        if (result.freezeApplied)
        {
            target.AddStatus(CombatStatus.Frozen);
            Log($"{target.DisplayName} 被冻结了!");
        }

        // DOT
        if (result.dotApplied)
        {
            var reaction = ElementReactionSystem.GetReaction(attacker.Element, target.AttachedElement ?? target.Element);
            int dotDamage = Mathf.RoundToInt(attacker.Attack * reaction.dotMultiplier);
            target.AddDot(attacker.Element, dotDamage, reaction.dotDuration);
            Log($"{target.DisplayName} 受到持续伤害效果 ({reaction.dotDuration}回合)");
        }

        // 减防
        if (result.defReductionTurns > 0)
        {
            target.AddStatus(CombatStatus.DefDown);
            target.DefReductionTurns = result.defReductionTurns;
            Log($"{target.DisplayName} 防御降低 ({result.defReductionTurns}回合)");
        }

        // 护盾
        if (result.shieldGenerated)
        {
            attacker.AddStatus(CombatStatus.Shielded);
            Log($"{attacker.DisplayName} 获得元素护盾");
        }

        EventBus.Publish(GameEvent.ElementReactionTriggered, result.reactionType.ToString());
    }

    // ==================== 回合结束 ====================

    /// <summary>单个单位行动结束</summary>
    void EndAction()
    {
        CurrentPhase = BattlePhase.TurnEnd;

        // 当前单位清理状态
        CurrentUnit?.EndTurnCleanup();

        // 重置合击记录（每个单位行动只检测一次）
        _lastComboPartner = null;
        _lastComboTarget = null;

        // 推进到下一个单位
        CurrentUnitIndex++;
        AdvanceToNextActor();
    }

    // ==================== 胜负判定 ====================

    /// <summary>单位被击败</summary>
    void OnUnitDefeated(CombatUnit unit)
    {
        Log($"{unit.DisplayName} 被击败了!");
        EventBus.Publish(GameEvent.UnitDefeated, unit.UnitId);
    }

    /// <summary>BOSS 二阶段检测 — GDD 6.2</summary>
    void CheckBossPhaseTransition(CombatUnit unit)
    {
        if (unit.UnitType != CombatUnitType.Boss) return;
        if (unit.SourceRef is BossData boss)
        {
            boss.CheckPhaseTransition(unit);
            if (boss.HasPhase2)
                Log($"{unit.DisplayName} 进入第二阶段!");
        }
    }

    /// <summary>检查战斗是否结束</summary>
    bool CheckBattleEnd()
    {
        bool allPlayerDead = true;
        foreach (var u in PlayerUnits)
            if (u.IsAlive) { allPlayerDead = false; break; }

        bool allEnemyDead = true;
        foreach (var u in EnemyUnits)
            if (u.IsAlive) { allEnemyDead = false; break; }

        if (allEnemyDead)
        {
            CurrentPhase = BattlePhase.Victory;
            EndBattle(true);
            return true;
        }
        if (allPlayerDead)
        {
            CurrentPhase = BattlePhase.Defeat;
            EndBattle(false);
            return true;
        }
        return false;
    }

    /// <summary>结束战斗</summary>
    void EndBattle(bool victory)
    {
        EventBus.Publish(GameEvent.BattleEnded, victory ? "victory" : "defeat");

        if (victory)
        {
            Log("=== 胜利! ===");
            // 奖励
            int totalGold = 0;
            foreach (var enemy in EnemyUnits)
            {
                if (enemy.UnitType == CombatUnitType.Boss)
                    totalGold += 500;
                else
                    totalGold += 30;
            }
            GameManager.Instance?.AddGold(totalGold);
            Log($"获得 {totalGold} 文");
        }
        else
        {
            Log("=== 失败... ===");
            Log("被送回了龙脊镇");
        }

        // 恢复游戏状态
        GameManager.Instance.SetState(GameState.Playing);
        InputManager.Instance.IsInputLocked = false;
    }

    // ==================== 日志 ====================

    void Log(string message)
    {
        BattleLog.Add(message);
        EventBus.Publish(GameEvent.UnitDamaged, message);
        Debug.Log($"[Battle] {message}");
    }

    // ==================== 查询 ====================

    /// <summary>获取所有存活单位</summary>
    public List<CombatUnit> GetAliveEnemies() => EnemyUnits.FindAll(u => u.IsAlive);

    /// <summary>获取玩家方存活单位</summary>
    public List<CombatUnit> GetAlivePlayers() => PlayerUnits.FindAll(u => u.IsAlive);
}
