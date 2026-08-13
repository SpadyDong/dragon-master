using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 矿洞管理器 — GDD 7.4
/// 楼层推进、矿石掉落、电梯检查点、宝箱、遇怪
/// </summary>
public class MiningManager : MonoBehaviour
{
    public static MiningManager Instance { get; private set; }

    /// <summary>当前矿洞</summary>
    private MineData _currentMine;
    /// <summary>当前楼层（1-based）</summary>
    private int _currentFloor = 1;
    /// <summary>各矿洞到达的最大楼层</summary>
    private Dictionary<string, int> _maxReachedFloors = new();

    /// <summary>当前矿洞数据</summary>
    public MineData CurrentMine => _currentMine;
    /// <summary>当前楼层</summary>
    public int CurrentFloor => _currentFloor;
    /// <summary>是否在矿洞中</summary>
    public bool IsInMine => _currentMine != null;

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

    // ==================== 进入/退出 ====================

    /// <summary>进入矿洞</summary>
    public void EnterMine(MineData mine)
    {
        _currentMine = mine;
        _currentFloor = 1;

        if (!_maxReachedFloors.ContainsKey(mine.mineId))
            _maxReachedFloors[mine.mineId] = 1;

        Debug.Log($"进入 {mine.mineName}，当前第1层");
    }

    /// <summary>下到下一层</summary>
    public bool Descend()
    {
        if (_currentMine == null) return false;
        if (_currentFloor >= _currentMine.floorCount) return false;

        _currentFloor++;

        // 更新最大到达楼层
        if (_maxReachedFloors[_currentMine.mineId] < _currentFloor)
            _maxReachedFloors[_currentMine.mineId] = _currentFloor;

        EventBus.Publish(GameEvent.FloorDescended, _currentFloor);
        Debug.Log($"下降到第{_currentFloor}层");

        return true;
    }

    /// <summary>回到上一层</summary>
    public bool Ascend()
    {
        if (_currentMine == null || _currentFloor <= 1) return false;
        _currentFloor--;
        Debug.Log($"上升至第{_currentFloor}层");
        return true;
    }

    /// <summary>退出矿洞</summary>
    public void ExitMine()
    {
        _currentMine = null;
        _currentFloor = 1;
        Debug.Log("退出矿洞");
    }

    // ==================== 电梯 ====================

    /// <summary>获取当前矿洞的电梯检查点</summary>
    public int[] GetElevatorCheckpoints()
    {
        return _currentMine?.GetElevatorCheckpoints() ?? System.Array.Empty<int>();
    }

    /// <summary>乘坐电梯到指定楼层（需已到达过且是检查点）</summary>
    public bool UseElevator(int targetFloor)
    {
        if (_currentMine == null) return false;
        if (targetFloor < 1 || targetFloor > _currentMine.floorCount) return false;

        // 检查是否是检查点
        bool isCheckpoint = false;
        foreach (var cp in GetElevatorCheckpoints())
            if (cp == targetFloor) { isCheckpoint = true; break; }
        if (!isCheckpoint) return false;

        // 检查是否已到达过
        if (_maxReachedFloors[_currentMine.mineId] < targetFloor) return false;

        _currentFloor = targetFloor;
        Debug.Log($"乘坐电梯到第{targetFloor}层");
        return true;
    }

    // ==================== 挖矿 ====================

    /// <summary>挖矿（返回本次获得的矿石列表）</summary>
    public List<OreData> Mine()
    {
        var results = new List<OreData>();
        if (_currentMine == null || _currentMine.ores == null) return results;

        float gatherBonus = ProficiencyManager.Instance != null
            ? ProficiencyManager.Instance.GetGatherDropBonus()
            : 0f;

        foreach (var ore in _currentMine.ores)
        {
            if (!ore.CanAppear(_currentFloor)) continue;
            float chance = ore.dropChance * (1f + gatherBonus);
            if (Random.value < chance)
            {
                results.Add(ore);
                if (!string.IsNullOrEmpty(ore.itemId))
                    InventoryManager.Instance?.AddItem(ore.itemId, 1);

                if (ProficiencyManager.Instance != null)
                    ProficiencyManager.Instance.AddXP(ProficiencySkill.Gathering, ore.gatheringXP);

                EventBus.Publish(GameEvent.OreMined, ore.oreId);
            }
        }

        return results;
    }

    // ==================== 宝箱 ====================

    /// <summary>检查是否遇到宝箱</summary>
    public bool CheckTreasure()
    {
        if (_currentMine == null) return false;
        return Random.value < _currentMine.treasureChance;
    }

    /// <summary>开启宝箱（返回掉落物品）</summary>
    public string[] OpenTreasure()
    {
        if (_currentMine == null || _currentMine.treasureTable == null)
            return System.Array.Empty<string>();

        var drops = new List<string>();
        foreach (var entry in _currentMine.treasureTable)
        {
            if (Random.value < entry.chance)
            {
                int count = Random.Range(entry.minCount, entry.maxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    drops.Add(entry.itemId);
                    InventoryManager.Instance?.AddItem(entry.itemId, 1);
                }
            }
        }
        return drops.ToArray();
    }

    // ==================== 遇怪 ====================

    /// <summary>检查是否遇怪</summary>
    public bool CheckEncounter()
    {
        if (_currentMine == null || _currentMine.monsterData == null) return false;
        return Random.value < _currentMine.encounterChance;
    }

    /// <summary>触发遇怪战斗</summary>
    public void TriggerEncounter()
    {
        if (_currentMine?.monsterData == null) return;

        var playerUnits = new List<CombatUnit>();
        if (PlayerStats.Instance != null)
            playerUnits.Add(CombatUnit.FromPlayer(PlayerStats.Instance));

        if (DragonManager.Instance != null)
        {
            foreach (var dragon in DragonManager.Instance.GetTamedDragons())
            {
                if (dragon.Stage == DragonStage.Adolescent || dragon.Stage == DragonStage.Adult)
                    playerUnits.Add(CombatUnit.FromDragon(dragon));
            }
        }

        var enemyUnits = new List<CombatUnit>();
        int count = Random.Range(1, 3);
        for (int i = 0; i < count; i++)
            enemyUnits.Add(_currentMine.monsterData.BuildCombatUnit());

        BattleManager.Instance?.InitBattle(playerUnits, enemyUnits, BattleType.WildEncounter);
    }

    // ==================== 查询 ====================

    /// <summary>获取某矿洞最大到达楼层</summary>
    public int GetMaxReachedFloor(string mineId)
    {
        return _maxReachedFloors.TryGetValue(mineId, out var floor) ? floor : 0;
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class MiningSaveData
    {
        public List<string> mineIds;
        public List<int> maxFloors;
    }

    public MiningSaveData GetSaveData()
    {
        var data = new MiningSaveData
        {
            mineIds = new List<string>(),
            maxFloors = new List<int>()
        };
        foreach (var kv in _maxReachedFloors)
        {
            data.mineIds.Add(kv.Key);
            data.maxFloors.Add(kv.Value);
        }
        return data;
    }

    public void LoadSaveData(MiningSaveData data)
    {
        if (data?.mineIds == null) return;
        _maxReachedFloors.Clear();
        for (int i = 0; i < data.mineIds.Count && i < data.maxFloors.Count; i++)
        {
            _maxReachedFloors[data.mineIds[i]] = data.maxFloors[i];
        }
    }
}
