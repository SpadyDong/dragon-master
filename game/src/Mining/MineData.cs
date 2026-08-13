using UnityEngine;

/// <summary>
/// 矿洞数据 — 对应 GDD 7.4
/// 4 个矿洞：阿布铜矿洞(120层) / 骷髅深渊(100+) / 火山矿洞(50层) / 冰窟矿洞(40层)
/// </summary>
[CreateAssetMenu(menuName = "Mining/Mine Data", fileName = "NewMine")]
public class MineData : ScriptableObject
{
    [Header("基本信息")]
    public string mineId;
    public string mineName;
    [TextArea(2, 4)]
    public string description;

    [Header("楼层")]
    public int floorCount = 120;
    [Tooltip("电梯检查点间隔（每10层一个）")]
    public int elevatorInterval = 10;

    [Header("矿石")]
    public OreData[] ores;

    [Header("怪物")]
    [Tooltip("遇怪时生成的野怪数据")]
    public WildAnimalData monsterData;
    [Tooltip("每层遇怪概率（0-1）")]
    [Range(0f, 1f)] public float encounterChance = 0.2f;

    [Header("宝箱")]
    [Tooltip("每层宝箱概率（0-1）")]
    [Range(0f, 1f)] public float treasureChance = 0.1f;
    public TreasureEntry[] treasureTable;

    [System.Serializable]
    public struct TreasureEntry
    {
        public string itemId;
        [Range(0f, 1f)] public float chance;
        public int minCount;
        public int maxCount;
    }

    /// <summary>获取电梯检查点楼层列表</summary>
    public int[] GetElevatorCheckpoints()
    {
        if (elevatorInterval <= 0) return System.Array.Empty<int>();
        var checkpoints = new System.Collections.Generic.List<int>();
        for (int floor = elevatorInterval; floor < floorCount; floor += elevatorInterval)
            checkpoints.Add(floor);
        return checkpoints.ToArray();
    }
}
