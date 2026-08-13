# 矿石与矿洞数据实例 — OreData / MineData

> 规则（GDD 7.4）：4 个矿洞：阿布铜矿洞(120层) / 骷髅深渊(100+) / 火山矿洞(50层) / 冰窟矿洞(40层)
> OreData 字段：`oreId`/`oreName`/`minFloor`/`maxFloor`/`dropChance`/`sellPrice`/`itemId`/`gatheringXP`
> MineData 字段：`mineId`/`mineName`/`floorCount`/`elevatorInterval`/`ores[]`/`monsterData`/`encounterChance`/`treasureChance`/`treasureTable[]`

---

## 一、矿石（OreData · 8 种）

| oreId | oreName | minFloor | maxFloor | dropChance | sellPrice | itemId | gatheringXP |
|---|---|---|---|---|---|---|---|
| copper | 铜矿石 | 1 | 120 | 0.5 | 10 | copper_ore | 8 |
| iron | 铁矿石 | 20 | 120 | 0.45 | 25 | iron_ore | 10 |
| gold | 金矿石 | 60 | 120 | 0.35 | 60 | gold_ore | 15 |
| iridium | 铱矿石 | 100 | 120 | 0.15 | 150 | iridium_ore | 25 |
| fire_stone | 火灵石 | 1 | 50 | 0.25 | 80 | fire_spirit_stone | 12 |
| ice_crystal | 冰晶 | 1 | 40 | 0.25 | 80 | ice_crystal | 12 |
| gem | 宝石 | 30 | 120 | 0.10 | 200 | gem | 30 |
| star_ore | 星陨矿 | 110 | 120 | 0.05 | 500 | star_ore | 50 |

---

## 二、矿洞（MineData · 4 个）

### 1. 阿布铜矿洞（120 层）
```json
{
  "mineId": "abu_copper_mine",
  "mineName": "阿布铜矿洞",
  "floorCount": 120,
  "elevatorInterval": 10,
  "ores": ["copper", "iron", "gold", "iridium", "gem"],
  "monsterData": "cave_giant_worm",  // 引用 WildAnimalData
  "encounterChance": 0.2,
  "treasureChance": 0.1
}
```

### 2. 骷髅深渊（无底，已开放 100+）
```json
{
  "mineId": "skeleton_abyss",
  "mineName": "骷髅深渊",
  "floorCount": 100,
  "elevatorInterval": 10,
  "ores": ["iron", "gold", "iridium", "star_ore"],
  "monsterData": "corrupted_beast",
  "encounterChance": 0.3,
  "treasureChance": 0.15
}
```

### 3. 火山矿洞（50 层 · 夏）
```json
{
  "mineId": "volcano_mine",
  "mineName": "火山矿洞",
  "floorCount": 50,
  "elevatorInterval": 10,
  "ores": ["fire_stone", "gold", "gem"],
  "monsterData": "flame_elemental",
  "encounterChance": 0.25,
  "treasureChance": 0.12
}
```

### 4. 冰窟矿洞（40 层 · 冬）
```json
{
  "mineId": "ice_cave_mine",
  "mineName": "冰窟矿洞",
  "floorCount": 40,
  "elevatorInterval": 10,
  "ores": ["ice_crystal", "iron", "gem"],
  "monsterData": "ice_slime",
  "encounterChance": 0.25,
  "treasureChance": 0.12
}
```

---

## 三、宝箱掉落表（treasureTable 建议）

| itemId | 概率 | 数量范围 |
|---|---|---|
| copper_ore | 0.4 | 3-8 |
| iron_ore | 0.3 | 2-5 |
| gold_ore | 0.15 | 1-3 |
| gem | 0.08 | 1-2 |
| 灵石碎片 | 0.05 | 1-1 |
| 蓝装(随机) | 0.02 | 1-1 |
