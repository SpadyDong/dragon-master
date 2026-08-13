using UnityEngine;

/// <summary>
/// 野怪数据 — 对应 GDD 6.2
/// 普通野怪（野狼/毒蛇/野猪等）
/// </summary>
[CreateAssetMenu(menuName = "Combat/Wild Animal Data", fileName = "NewWildAnimal")]
public class WildAnimalData : ScriptableObject
{
    [Header("基本信息")]
    public string animalId;
    public string animalName;
    [TextArea(2, 4)]
    public string description;

    [Header("元素")]
    public DragonElement element;

    [Header("属性")]
    public int maxHP = 50;
    public int attack = 12;
    public int defense = 8;
    public int agility = 10;
    public bool isFlying;

    [Header("捕捉")]
    public bool isCapturable = true;
    public string captureItemId = "rope_net";
    [Tooltip("捕捉难度（0-10，越高越难）")]
    [Range(0, 10)] public int captureDifficulty = 3;

    [Header("奖励")]
    public int rewardGold = 30;
    public int rewardExp = 15;
    [Tooltip("掉落物品")]
    public DropEntry[] dropTable;

    [Header("刷新")]
    [Tooltip("刷新场景（Town/Forest/SnowMountain）")]
    public string[] spawnScenes;
    [Tooltip("是否夜晚刷新")]
    public bool spawnAtNight;

    [System.Serializable]
    public struct DropEntry
    {
        public string itemId;
        [Range(0f, 1f)] public float dropChance;
        public int minCount;
        public int maxCount;
    }

    /// <summary>构建野怪战斗单位</summary>
    public CombatUnit BuildCombatUnit()
    {
        var unit = new CombatUnit(animalId, animalName, CombatUnitType.WildAnimal)
        {
            Element = element,
            MaxHP = maxHP,
            Attack = attack,
            Defense = defense,
            Agility = agility,
            IsFlying = isFlying,
            IsCapturable = isCapturable,
            CaptureItemId = captureItemId
        };
        unit.FullHeal();
        return unit;
    }

    /// <summary>随机掉落</summary>
    public string[] RollDrops()
    {
        if (dropTable == null || dropTable.Length == 0) return System.Array.Empty<string>();

        var drops = new System.Collections.Generic.List<string>();
        foreach (var entry in dropTable)
        {
            if (Random.value < entry.dropChance)
            {
                int count = Random.Range(entry.minCount, entry.maxCount + 1);
                for (int i = 0; i < count; i++)
                    drops.Add(entry.itemId);
            }
        }
        return drops.ToArray();
    }
}
