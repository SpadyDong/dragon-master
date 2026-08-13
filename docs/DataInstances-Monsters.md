# 野怪与 BOSS 数据实例 — WildAnimalData / BossData

> 规则（GDD 6.2）：普通野怪 / 精英怪 / 洞窟 BOSS / 主线 BOSS / 最终 BOSS
> 野怪 `WildAnimalData` 字段：`animalId`/`animalName`/`element`/`maxHP`/`attack`/`defense`/`agility`/`isFlying`/`captureItemId`/`dropTable[]`/`spawnScenes`
> BOSS `BossData` 字段：`bossId`/`bossName`/`element`/`weakElement`/`maxHP`/`attack`/`defense`/`agility`/`phase2*`/`rewardGold`/`rewardItemIds`

---

## 一、普通野怪（WildAnimalData · 20 种）

| animalId | animalName | element | maxHP | attack | defense | agility | isFlying | 刷新场景 |
|---|---|---|---|---|---|---|---|---|
| wolf | 野狼 | (null) | 40 | 10 | 6 | 12 | false | Forest |
| viper | 毒蛇 | Dendro | 35 | 9 | 4 | 13 | false | Forest |
| boar | 野猪 | (null) | 50 | 12 | 8 | 8 | false | Forest |
| giant_spider | 巨蜘蛛 | Dendro | 45 | 10 | 7 | 11 | false | Forest, Mine |
| piranha | 食人鱼 | Hydro | 30 | 11 | 4 | 14 | false | River |
| corrupted_bat | 腐化蝙蝠 | Electro | 35 | 9 | 5 | 15 | true | Forest, Mine |
| slime | 史莱姆 | Hydro | 30 | 8 | 6 | 6 | false | Forest |
| goblin | 哥布林 | (null) | 42 | 11 | 7 | 10 | false | Forest |
| thorn_wolf | 荆棘狼 | Dendro | 48 | 12 | 8 | 13 | false | Forest |
| mud_golem | 泥石傀儡 | Geo | 55 | 11 | 14 | 5 | false | Mine |
| fire_bat | 火蝙蝠 | Pyro | 38 | 11 | 5 | 14 | true | Volcano |
| snow_wolf | 雪狼 | Cryo | 48 | 12 | 7 | 12 | false | SnowMountain |
| ice_slime | 冰史莱姆 | Cryo | 40 | 9 | 8 | 6 | false | SnowMountain |
| rock_crab | 岩蟹 | Geo | 52 | 10 | 15 | 6 | false | Mine |
| lava_slime | 熔岩史莱姆 | Pyro | 45 | 11 | 8 | 6 | false | Volcano |
| sand_scorpion | 沙蝎 | Geo | 44 | 12 | 8 | 11 | false | Desert |
| desert_snake | 沙漠毒蛇 | Electro | 40 | 11 | 6 | 12 | false | Desert |
| snow_bat | 雪蝙蝠 | Cryo | 36 | 9 | 5 | 14 | true | SnowMountain |
| mushroom_monster | 蘑菇怪 | Dendro | 46 | 10 | 9 | 8 | false | Forest |
| rock_rhino | 石甲犀(幼) | Geo | 60 | 14 | 13 | 7 | false | Forest |

---

## 二、精英怪（WildAnimalData · 10 种）

| animalId | animalName | element | maxHP | attack | defense | agility | isFlying |
|---|---|---|---|---|---|---|---|
| raging_bear | 狂暴熊 | (null) | 90 | 20 | 15 | 9 | false |
| poison_fang_dragon | 毒牙龙 | Dendro | 85 | 19 | 13 | 14 | true |
| corrupted_beast | 腐化荒兽 | Electro | 95 | 21 | 14 | 11 | false |
| rock_golem | 岩石巨人 | Geo | 110 | 18 | 22 | 5 | false |
| frost_giant | 冰霜巨人 | Cryo | 105 | 20 | 18 | 7 | false |
| flame_elemental | 火焰元素 | Pyro | 90 | 24 | 12 | 12 | true |
| shadow_wolf | 暗影狼 | Electro | 88 | 20 | 12 | 15 | false |
| ancient_viper | 远古毒蟒 | Dendro | 100 | 21 | 14 | 13 | false |
| storm_hawk | 风暴鹰 | Anemo | 80 | 19 | 10 | 18 | true |
| abyss_crawler | 深渊爬行者 | Hydro | 92 | 20 | 13 | 13 | false |

---

## 三、洞窟 BOSS（BossData · 4 种）

| bossId | bossName | element | weakElement | maxHP | attack | defense | agility |
|---|---|---|---|---|---|---|---|
| cave_giant_worm | 地底巨虫 | Geo | Hydro | 200 | 30 | 25 | 12 |
| cave_stone_golem | 石巨人 | Geo | Pyro | 250 | 32 | 30 | 8 |
| cave_ice_golem | 冰魔像 | Cryo | Pyro | 220 | 30 | 26 | 10 |
| cave_flame_lord | 火焰领主 | Pyro | Hydro | 240 | 35 | 22 | 14 |

---

## 四、主线 BOSS（BossData · 4 守护龙，弱元素克制）

| bossId | bossName | element | weakElement | maxHP | attack | defense | agility |
|---|---|---|---|---|---|---|---|
| ancient_wind_dragon | 远古守护龙·风 | Anemo | (null) | 350 | 40 | 30 | 20 |
| ancient_flame_dragon | 远古守护龙·火 | Pyro | Hydro | 350 | 42 | 28 | 18 |
| ancient_water_dragon | 远古守护龙·水 | Hydro | Electro | 350 | 40 | 30 | 19 |
| ancient_ice_dragon | 远古守护龙·冰 | Cryo | Pyro | 350 | 40 | 30 | 18 |

> 对应四圣物：风息石/炎心石/潮涌石/霜魄石（GDD 2.2 第二幕）。

### 第二幕专属 BOSS（关联圣物）

| bossId | bossName | element | weakElement | maxHP | attack | defense | agility | 掉落圣物 |
|---|---|---|---|---|---|---|---|---|
| flame_heart_dragon | 炎心龙 | Pyro | Hydro | 300 | 38 | 26 | 16 | 风息石(WindStone) |
| sand_worm_king | 沙虫王 | Geo | Cryo | 400 | 42 | 32 | 10 | 炎心石(FlameStone) |
| abyss_guardian | 深渊守护者 | Hydro | Electro | 500 | 45 | 35 | 15 | 潮涌石(TideStone) |
| frost_guardian | 霜魄石守护 | Cryo | Pyro | 450 | 44 | 34 | 14 | 霜魄石(FrostStone) |

---

## 五、最终 BOSS（BossData · 双阶段）

```json
{
  "bossId": "dark_dragon_spirit_king",
  "bossName": "黑暗龙灵王",
  "element": "Anemo",
  "weakElement": null,
  "maxHP": 2000, "attack": 55, "defense": 40, "agility": 22,
  "phase2HPThreshold": 0.5,
  "phase2MaxHP": 3500, "phase2Attack": 70, "phase2Defense": 45,
  "rewardGold": 10000,
  "rewardItemIds": ["egg_starfall", "relic_final"]
}
```

> GDD 2.2 3-4：第一阶段 HP 2000 七元素轮换；第二阶段 HP 3500 持续召唤腐化龙。

---

## 六、教程 BOSS（BossData · M1 石甲犀）

```json
{
  "bossId": "stone_rhino",
  "bossName": "石甲犀",
  "element": "Geo",
  "weakElement": "Anemo",
  "maxHP": 80, "attack": 15, "defense": 12, "agility": 8,
  "rewardGold": 150, "rewardItemIds": []
}
```

---

## 掉落表（dropTable）建议

普通野怪掉落（示例）：
| 怪物 | 掉落 | 概率 |
|---|---|---|
| 野狼 | 生肉(meat_raw) | 0.6 |
| 野狼 | 狼皮(wolf_pelt) | 0.3 |
| 毒蛇 | 蛇胆(snake_gall) | 0.5 |
| 巨蜘蛛 | 蛛丝(spider_silk) | 0.5 |
| 岩蟹 | 蟹壳(crab_shell) | 0.4 |
| 泥石傀儡 | 铜矿石(copper_ore) | 0.5 |
| 火蝙蝠 | 火灵石(fire_spirit_stone) | 0.15 |
| 雪狼 | 狼皮(wolf_pelt) | 0.4 |

精英怪掉落（中概率蓝装/高级材料）：
| 怪物 | 掉落 | 概率 |
|---|---|---|
| 狂暴熊 | 熊皮(bear_pelt) | 0.5 |
| 毒牙龙 | 毒牙(poison_fang) | 0.4 |
| 岩石巨人 | 蓝装装备(随机) | 0.2 |

BOSS 掉落（紫装/圣物碎片/完整圣物）：
| BOSS | 掉落 |
|---|---|
| 洞窟 BOSS | 紫装 + 灵石圣物碎片 |
| 主线 BOSS | 橙装 + 完整灵石圣物 |
| 最终 BOSS | 结局道具 + 星陨龙蛋 |

---

## 汇总

| 类别 | 数量 |
|---|---|
| 普通野怪 | 20 |
| 精英怪 | 10 |
| 洞窟 BOSS | 4 |
| 主线守护龙 BOSS | 4 |
| 第二幕圣物 BOSS | 4 |
| 最终 BOSS | 1 |
| 教程 BOSS | 1 |
| **合计** | **44** |
