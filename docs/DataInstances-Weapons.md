# 武器与装备数据实例 — ItemData（type=Weapon/Armor/Accessory）

> 规则（GDD 6.4）：3 种武器（单手剑/双手剑/弓箭）× 各元素；装备带元素属性标签
> 装备规则（GDD 6.4.2）：必须匹配人物主属性，匹配=100%，不匹配同类武器=50%，完全不符不可装备
> 品质（GDD 6.4.3）：白→绿→蓝→紫→橙→红（6 档）
> `QualityMultiplier`：白1.0 / 绿1.2 / 蓝1.5 / 紫1.8 / 橙2.2 / 红2.8

---

## 一、武器（Weapon · weaponSubType）

### 单手剑（Sword · 全属性通用）

| itemId | itemName | element | quality | attackBonus | basePrice |
|---|---|---|---|---|---|
| sword_iron_dendro | 铁剑·草 | Dendro | White | 8 | 100 |
| sword_iron_pyro | 铁剑·火 | Pyro | White | 8 | 100 |
| sword_iron_hydro | 铁剑·水 | Hydro | White | 8 | 100 |
| sword_iron_geo | 铁剑·岩 | Geo | White | 8 | 100 |
| sword_iron_cryo | 铁剑·冰 | Cryo | White | 8 | 100 |
| sword_iron_electro | 铁剑·雷 | Electro | White | 8 | 100 |
| sword_iron_anemo | 铁剑·风 | Anemo | White | 8 | 100 |
| sword_steel_dendro | 钢剑·草 | Dendro | Green | 14 | 250 |
| sword_steel_pyro | 钢剑·火 | Pyro | Green | 14 | 250 |
| sword_steel_hydro | 钢剑·水 | Hydro | Green | 14 | 250 |
| sword_steel_geo | 钢剑·岩 | Geo | Green | 14 | 250 |
| sword_steel_cryo | 钢剑·冰 | Cryo | Green | 14 | 250 |
| sword_steel_electro | 钢剑·雷 | Electro | Green | 14 | 250 |
| sword_steel_anemo | 钢剑·风 | Anemo | Green | 14 | 250 |
| sword_mithril_pyro | 秘银剑·火 | Pyro | Blue | 22 | 600 |
| sword_mithril_hydro | 秘银剑·水 | Hydro | Blue | 22 | 600 |
| sword_mithril_anemo | 秘银剑·风 | Anemo | Blue | 22 | 600 |
| sword_dragon_pyro | 龙牙剑·火 | Pyro | Purple | 32 | 1500 |
| sword_dragon_hydro | 龙牙剑·水 | Hydro | Purple | 32 | 1500 |

### 双手剑（Greatsword · 适合火/土）

| itemId | itemName | element | quality | attackBonus | basePrice |
|---|---|---|---|---|---|
| greatsword_iron_pyro | 铁大剑·火 | Pyro | White | 12 | 150 |
| greatsword_iron_geo | 铁大剑·岩 | Geo | White | 12 | 150 |
| greatsword_steel_pyro | 钢大剑·火 | Pyro | Green | 20 | 350 |
| greatsword_steel_geo | 钢大剑·岩 | Geo | Green | 20 | 350 |
| greatsword_mithril_pyro | 秘银大剑·火 | Pyro | Blue | 30 | 800 |
| greatsword_mithril_geo | 秘银大剑·岩 | Geo | Blue | 30 | 800 |
| greatsword_dragon_pyro | 龙脊大剑·火 | Pyro | Purple | 44 | 2000 |
| greatsword_dragon_geo | 龙脊大剑·岩 | Geo | Purple | 44 | 2000 |

### 弓箭（Bow · 适合风/水/冰）

| itemId | itemName | element | quality | attackBonus | basePrice |
|---|---|---|---|---|---|
| bow_wood_anemo | 木弓·风 | Anemo | White | 10 | 120 |
| bow_wood_hydro | 木弓·水 | Hydro | White | 10 | 120 |
| bow_wood_cryo | 木弓·冰 | Cryo | White | 10 | 120 |
| bow_hardwood_anemo | 硬木弓·风 | Anemo | Green | 17 | 300 |
| bow_hardwood_hydro | 硬木弓·水 | Hydro | Green | 17 | 300 |
| bow_hardwood_cryo | 硬木弓·冰 | Cryo | Green | 17 | 300 |
| bow_mithril_anemo | 秘银弓·风 | Anemo | Blue | 26 | 700 |
| bow_mithril_hydro | 秘银弓·水 | Hydro | Blue | 26 | 700 |
| bow_mithril_cryo | 秘银弓·冰 | Cryo | Blue | 26 | 700 |
| bow_dragon_anemo | 龙筋弓·风 | Anemo | Purple | 38 | 1800 |
| bow_dragon_hydro | 龙筋弓·水 | Hydro | Purple | 38 | 1800 |
| bow_dragon_cryo | 龙筋弓·冰 | Cryo | Purple | 38 | 1800 |

---

## 二、防具（Armor · 头盔/护甲/鞋子）

| itemId | itemName | 部位 | element | quality | defenseBonus | basePrice |
|---|---|---|---|---|---|---|
| armor_cloth | 布衣 | 护甲 | (null) | White | 3 | 50 |
| armor_leather | 皮甲 | 护甲 | (null) | Green | 6 | 150 |
| armor_iron | 铁甲 | 护甲 | (null) | Blue | 10 | 400 |
| armor_mithril | 秘银甲 | 护甲 | (null) | Purple | 15 | 1000 |
| armor_dragon | 龙鳞甲 | 护甲 | (null) | Orange | 22 | 3000 |
| helmet_cloth | 布帽 | 头盔 | (null) | White | 2 | 40 |
| helmet_leather | 皮盔 | 头盔 | (null) | Green | 4 | 120 |
| helmet_iron | 铁盔 | 头盔 | (null) | Blue | 7 | 300 |
| helmet_mithril | 秘银盔 | 头盔 | (null) | Purple | 11 | 800 |
| boots_cloth | 布靴 | 鞋子 | (null) | White | 2 | 40 |
| boots_leather | 皮靴 | 鞋子 | (null) | Green | 4 | 120 |
| boots_iron | 铁靴 | 鞋子 | (null) | Blue | 7 | 300 |
| boots_mithril | 秘银靴 | 鞋子 | (null) | Purple | 11 | 800 |

---

## 三、饰品（Accessory）

| itemId | itemName | element | quality | attackBonus | defenseBonus | magicBonus | basePrice |
|---|---|---|---|---|---|---|---|
| acc_stone | 灵石护符 | (null) | Green | 0 | 2 | 5 | 200 |
| acc_luck | 幸运符 | (null) | Blue | 2 | 2 | 2 | 500 |
| acc_dragon_scale | 龙鳞护符 | Pyro | Purple | 5 | 3 | 5 | 1500 |
| acc_wind_talisman | 风灵符 | Anemo | Blue | 3 | 1 | 5 | 600 |
| acc_fire_talisman | 火灵符 | Pyro | Blue | 3 | 1 | 5 | 600 |
| acc_water_talisman | 水灵符 | Hydro | Blue | 3 | 1 | 5 | 600 |
| acc_earth_talisman | 土灵符 | Geo | Blue | 3 | 1 | 5 | 600 |
| acc_ice_talisman | 冰灵符 | Cryo | Blue | 3 | 1 | 5 | 600 |
| acc_thunder_talisman | 雷灵符 | Electro | Blue | 3 | 1 | 5 | 600 |
| acc_grass_talisman | 草灵符 | Dendro | Blue | 3 | 1 | 5 | 600 |

---

## 四、龙装备（DragonEquipment）

| itemId | itemName | 子类型 | element | quality | attackBonus | defenseBonus | basePrice |
|---|---|---|---|---|---|---|---|
| saddle_leaf | 落叶龙鞍 | Saddle | Dendro | Green | 5 | 0 | 400 |
| saddle_flame | 焰心龙鞍 | Saddle | Pyro | Green | 5 | 0 | 400 |
| saddle_tide | 潮鸣龙鞍 | Saddle | Hydro | Green | 5 | 0 | 400 |
| saddle_rock | 岩铠龙鞍 | Saddle | Geo | Green | 5 | 0 | 400 |
| saddle_frost | 霜翼龙鞍 | Saddle | Cryo | Green | 5 | 0 | 400 |
| saddle_thunder | 雷羽龙鞍 | Saddle | Electro | Green | 5 | 0 | 400 |
| saddle_storm | 风暴龙鞍 | Saddle | Anemo | Green | 5 | 0 | 400 |
| darmor_leaf | 落叶龙鳞甲 | DragonArmor | Dendro | Green | 0 | 8 | 500 |
| darmor_flame | 焰心龙鳞甲 | DragonArmor | Pyro | Green | 0 | 8 | 500 |
| darmor_tide | 潮鸣龙鳞甲 | DragonArmor | Hydro | Green | 0 | 8 | 500 |
| darmor_rock | 岩铠龙鳞甲 | DragonArmor | Geo | Green | 0 | 8 | 500 |
| darmor_frost | 霜翼龙鳞甲 | DragonArmor | Cryo | Green | 0 | 8 | 500 |
| darmor_thunder | 雷羽龙鳞甲 | DragonArmor | Electro | Green | 0 | 8 | 500 |
| darmor_storm | 风暴龙鳞甲 | DragonArmor | Anemo | Green | 0 | 8 | 500 |
| dacc_leaf | 落叶龙饰 | DragonAccessory | Dendro | Blue | 3 | 2 | 600 |
| dacc_flame | 焰心龙饰 | DragonAccessory | Pyro | Blue | 3 | 2 | 600 |
| dacc_tide | 潮鸣龙饰 | DragonAccessory | Hydro | Blue | 3 | 2 | 600 |
| dacc_rock | 岩铠龙饰 | DragonAccessory | Geo | Blue | 3 | 2 | 600 |
| dacc_frost | 霜翼龙饰 | DragonAccessory | Cryo | Blue | 3 | 2 | 600 |
| dacc_thunder | 雷羽龙饰 | DragonAccessory | Electro | Blue | 3 | 2 | 600 |
| dacc_storm | 风暴龙饰 | DragonAccessory | Anemo | Blue | 3 | 2 | 600 |

---

## 汇总

| 类别 | 数量 |
|---|---|
| 单手剑 | 19 |
| 双手剑 | 8 |
| 弓箭 | 12 |
| 防具 | 13 |
| 饰品 | 10 |
| 龙装备 | 21 |
| **合计** | **83** |

> ⚠️ 说明：NPC 专属武器（月岚/岩碎/灵雀弓/潮汐/灶火刀/青囊/燎原/考古者/花间矢/缝针/夜莺/冰晶弓/裂地锤刀/糖霜/算盘剑/灶火）可另建 Orange/Red 品质的具名武器，此处未逐一列出。
