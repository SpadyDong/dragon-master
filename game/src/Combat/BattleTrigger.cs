using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 战斗触发器 — 挂在野怪/BOSS GameObject 上
/// 继承 Interactable，玩家按 F 触发战斗
/// </summary>
public class BattleTrigger : Interactable
{
    [Header("战斗数据")]
    [SerializeField] private WildAnimalData wildAnimalData;
    [SerializeField] private BossData bossData;

    [Header("战斗设置")]
    [Tooltip("触发战斗后是否销毁该触发器")]
    [SerializeField] private bool destroyAfterBattle = false;
    [Tooltip("是否需要击败后消失")]
    [SerializeField] private bool despawnOnDefeat = true;

    private bool _battleTriggered;
    private bool _isBoss => bossData != null;

    void Awake()
    {
        type = InteractType.Talk;
        promptText = $"按 F 与{(_isBoss ? bossData.bossName : wildAnimalData?.animalName ?? "野怪")}战斗";
    }

    public override void OnInteract(PlayerController player)
    {
        base.OnInteract(player);
        StartBattle();
    }

    /// <summary>开始战斗</summary>
    public void StartBattle()
    {
        if (_battleTriggered) return;
        _battleTriggered = true;

        var playerUnits = BuildPlayerUnits();
        var enemyUnits = BuildEnemyUnits();

        BattleType type = _isBoss ? BattleType.Boss : BattleType.WildEncounter;

        if (BattleManager.Instance == null)
        {
            Debug.LogError("BattleTrigger: 场景中缺少 BattleManager");
            return;
        }

        BattleManager.Instance.InitBattle(playerUnits, enemyUnits, type);

        // 监听战斗结束
        EventBus.Subscribe<string>(GameEvent.BattleEnded, OnBattleEnded);
    }

    /// <summary>构建玩家方单位</summary>
    List<CombatUnit> BuildPlayerUnits()
    {
        var units = new List<CombatUnit>();

        // 玩家
        if (PlayerStats.Instance != null)
            units.Add(CombatUnit.FromPlayer(PlayerStats.Instance));

        // 参战龙（已驯服且青年以上）
        if (DragonManager.Instance != null)
        {
            var tamedDragons = DragonManager.Instance.GetTamedDragons();
            foreach (var dragon in tamedDragons)
            {
                if (dragon.Stage == DragonStage.Adolescent || dragon.Stage == DragonStage.Adult)
                    units.Add(CombatUnit.FromDragon(dragon));
            }
        }

        return units;
    }

    /// <summary>构建敌方单位</summary>
    List<CombatUnit> BuildEnemyUnits()
    {
        var units = new List<CombatUnit>();

        if (_isBoss)
        {
            units.Add(bossData.BuildCombatUnit());
        }
        else if (wildAnimalData != null)
        {
            // 野怪可能成群出现（1-3只）
            int count = Random.Range(1, 4);
            for (int i = 0; i < count; i++)
                units.Add(wildAnimalData.BuildCombatUnit());
        }

        return units;
    }

    /// <summary>战斗结束回调</summary>
    void OnBattleEnded(string result)
    {
        EventBus.Unsubscribe<string>(GameEvent.BattleEnded, OnBattleEnded);

        if (result == "victory")
        {
            // 掉落
            if (wildAnimalData != null)
            {
                var drops = wildAnimalData.RollDrops();
                foreach (var itemId in drops)
                    InventoryManager.Instance?.AddItem(itemId, 1);
            }
            else if (bossData != null && bossData.rewardItemIds != null)
            {
                for (int i = 0; i < bossData.rewardItemIds.Length; i++)
                {
                    if (i < bossData.rewardItemChances.Length &&
                        Random.value < bossData.rewardItemChances[i])
                        InventoryManager.Instance?.AddItem(bossData.rewardItemIds[i], 1);
                }
            }

            if (despawnOnDefeat)
                Destroy(gameObject);
        }
        else
        {
            // 失败：重置触发器
            _battleTriggered = false;
        }

        if (destroyAfterBattle)
            Destroy(gameObject);
    }
}
