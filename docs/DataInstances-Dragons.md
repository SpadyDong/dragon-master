# 龙种数据实例（21 种）— DragonData

> 规则（GDD 5.2.3 + 简化规则）：7 元素 × 每元素 3 种（2 普通 + 1 稀有）= 21 种
> 同种龙属性完全固定（无 IV/EV 随机），全部来自 DragonData
> `rarity`：`Common`=普通 / `Rare`=稀有
> `validSeasons`：-1=全年，0=春 1=夏 2=秋 3=冬
> `canBeDualElement`：默认 true（5% 概率双属性）

---

## 数值平衡基准

| 稀有度 | baseAttack | baseDefense | baseAgility | baseMaxHP | purchasePrice | captureDifficulty |
|---|---|---|---|---|---|---|
| 普通 Common | 18~24 | 12~18 | 10~15 | 100~130 | 800~1200 | 2~3 |
| 稀有 Rare | 28~36 | 20~26 | 16~22 | 150~190 | 2500~4000 | 4~6 |

> 通用字段（所有龙相同，可省略单独配置）：
> `juvenileStartDay=1`, `adolescentStartDay=6`, `adultStartDay=21`, `eggHatchDays=3`
> `eggVisualScale=0.35`, `juvenileVisualScale=0.55`, `adolescentVisualScale=0.78`, `adultVisualScale=1.0`

---

## 一、草（Dendro · 春）

### 1. 落叶龙 🌿（普通 · 代表种）
```json
{
  "speciesId": "leaf_fall_dragon",
  "speciesName": "落叶龙",
  "element": "Dendro",
  "rarity": "Common",
  "validSeasons": [0],
  "baseAttack": 20, "baseDefense": 14, "baseAgility": 13, "baseMaxHP": 120,
  "captureDifficulty": 2, "eggItemId": "egg_leaf_fall", "purchasePrice": 1000,
  "description": "栖息于森林深处的草属性幼龙，翠绿色鳞片如初春嫩叶。"
}
```

### 2. 藤蔓龙 🌱（普通）
```json
{
  "speciesId": "vine_dragon", "speciesName": "藤蔓龙", "element": "Dendro", "rarity": "Common",
  "validSeasons": [0], "baseAttack": 18, "baseDefense": 16, "baseAgility": 10, "baseMaxHP": 130,
  "captureDifficulty": 3, "eggItemId": "egg_vine", "purchasePrice": 900,
  "description": "背生藤蔓，缠绕树枝攀爬，防御略高于同类草龙。"
}
```

### 3. 森语龙 🌳（稀有）
```json
{
  "speciesId": "forest_whisper_dragon", "speciesName": "森语龙", "element": "Dendro", "rarity": "Rare",
  "validSeasons": [0], "baseAttack": 30, "baseDefense": 22, "baseAgility": 18, "baseMaxHP": 170,
  "captureDifficulty": 5, "eggItemId": "egg_forest_whisper", "purchasePrice": 3000,
  "description": "能听懂森林万物的稀有草龙，鳞片随季节变换深浅。"
}
```

---

## 二、火（Pyro · 夏）

### 4. 焰心龙 🔥（普通 · 代表种）
```json
{
  "speciesId": "flame_heart_dragon", "speciesName": "焰心龙", "element": "Pyro", "rarity": "Common",
  "validSeasons": [1], "baseAttack": 24, "baseDefense": 12, "baseAgility": 12, "baseMaxHP": 110,
  "captureDifficulty": 3, "eggItemId": "egg_flame_heart", "purchasePrice": 1100,
  "description": "火山外围的火属性龙，胸口有火焰状斑纹，攻击凌厉。"
}
```

### 5. 熔岩龙 🌋（普通）
```json
{
  "speciesId": "lava_dragon", "speciesName": "熔岩龙", "element": "Pyro", "rarity": "Common",
  "validSeasons": [1], "baseAttack": 22, "baseDefense": 15, "baseAgility": 10, "baseMaxHP": 125,
  "captureDifficulty": 3, "eggItemId": "egg_lava", "purchasePrice": 1000,
  "description": "熔岩穴中的火龙，鳞甲坚硬耐高温，行动略缓。"
}
```

### 6. 赤焰龙 🔥（稀有）
```json
{
  "speciesId": "crimson_flame_dragon", "speciesName": "赤焰龙", "element": "Pyro", "rarity": "Rare",
  "validSeasons": [1], "baseAttack": 36, "baseDefense": 20, "baseAgility": 16, "baseMaxHP": 160,
  "captureDifficulty": 6, "eggItemId": "egg_crimson_flame", "purchasePrice": 3500,
  "description": "通体赤红的稀有火龙，喷射的烈焰能熔化岩壁。"
}
```

---

## 三、水（Hydro · 夏）

### 7. 潮鸣龙 💧（普通 · 代表种）
```json
{
  "speciesId": "tide_song_dragon", "speciesName": "潮鸣龙", "element": "Hydro", "rarity": "Common",
  "validSeasons": [1, 2], "baseAttack": 21, "baseDefense": 15, "baseAgility": 14, "baseMaxHP": 118,
  "captureDifficulty": 2, "eggItemId": "egg_tide_song", "purchasePrice": 1000,
  "description": "海边深海洞窟的水龙，尾鳍如浪，叫声似潮汐。"
}
```

### 8. 溪流龙 🌊（普通）
```json
{
  "speciesId": "stream_dragon", "speciesName": "溪流龙", "element": "Hydro", "rarity": "Common",
  "validSeasons": [1, 2], "baseAttack": 19, "baseDefense": 13, "baseAgility": 15, "baseMaxHP": 112,
  "captureDifficulty": 2, "eggItemId": "egg_stream", "purchasePrice": 900,
  "description": "苍莽河上游的敏捷水龙，体态修长，行动迅捷。"
}
```

### 9. 深渊龙 🌌（稀有）
```json
{
  "speciesId": "abyss_dragon", "speciesName": "深渊龙", "element": "Hydro", "rarity": "Rare",
  "validSeasons": [1, 2], "baseAttack": 32, "baseDefense": 24, "baseAgility": 20, "baseMaxHP": 180,
  "captureDifficulty": 6, "eggItemId": "egg_abyss", "purchasePrice": 3800,
  "description": "海底遗迹深处的稀有水龙，能操纵深海暗流。"
}
```

---

## 四、岩（Geo · 全年）

### 10. 岩铠龙 🪨（普通 · 代表种）
```json
{
  "speciesId": "rock_armor_dragon", "speciesName": "岩铠龙", "element": "Geo", "rarity": "Common",
  "validSeasons": [-1], "baseAttack": 20, "baseDefense": 18, "baseAgility": 10, "baseMaxHP": 130,
  "captureDifficulty": 3, "eggItemId": "egg_rock_armor", "purchasePrice": 1200,
  "description": "地底深层与岩壁石林的岩龙，重甲防御，全年可遇。"
}
```

### 11. 石甲龙 🛡️（普通）
```json
{
  "speciesId": "stone_scale_dragon", "speciesName": "石甲龙", "element": "Geo", "rarity": "Common",
  "validSeasons": [-1], "baseAttack": 18, "baseDefense": 20, "baseAgility": 9, "baseMaxHP": 135,
  "captureDifficulty": 3, "eggItemId": "egg_stone_scale", "purchasePrice": 1100,
  "description": "以岩石为鳞的防御型岩龙，行动缓慢但坚不可摧。"
}
```

### 12. 山岳龙 ⛰️（稀有）
```json
{
  "speciesId": "mountain_dragon", "speciesName": "山岳龙", "element": "Geo", "rarity": "Rare",
  "validSeasons": [-1], "baseAttack": 30, "baseDefense": 26, "baseAgility": 16, "baseMaxHP": 190,
  "captureDifficulty": 5, "eggItemId": "egg_mountain", "purchasePrice": 4000,
  "description": "盘踞山巅的稀有岩龙，据传其脊背能撑起一座小山。"
}
```

---

## 五、冰（Cryo · 冬）

### 13. 霜翼龙 ❄️（普通 · 代表种）
```json
{
  "speciesId": "frost_wing_dragon", "speciesName": "霜翼龙", "element": "Cryo", "rarity": "Common",
  "validSeasons": [3], "baseAttack": 22, "baseDefense": 15, "baseAgility": 14, "baseMaxHP": 115,
  "captureDifficulty": 3, "eggItemId": "egg_frost_wing", "purchasePrice": 1100,
  "description": "雪山冰窟的冰龙，双翼覆霜，能在冰面滑翔。"
}
```

### 14. 冰晶龙 🧊（普通）
```json
{
  "speciesId": "ice_crystal_dragon", "speciesName": "冰晶龙", "element": "Cryo", "rarity": "Common",
  "validSeasons": [3], "baseAttack": 20, "baseDefense": 17, "baseAgility": 11, "baseMaxHP": 122,
  "captureDifficulty": 3, "eggItemId": "egg_ice_crystal", "purchasePrice": 1000,
  "description": "鳞片透明如冰晶的冰龙，能反射寒光迷惑猎物。"
}
```

### 15. 极冬龙 🌨️（稀有）
```json
{
  "speciesId": "deep_winter_dragon", "speciesName": "极冬龙", "element": "Cryo", "rarity": "Rare",
  "validSeasons": [3], "baseAttack": 33, "baseDefense": 25, "baseAgility": 19, "baseMaxHP": 185,
  "captureDifficulty": 6, "eggItemId": "egg_deep_winter", "purchasePrice": 3900,
  "description": "雪峰之巅的稀有冰龙，呼出的寒气能冻结整片湖面。"
}
```

---

## 六、雷（Electro · 秋）

### 16. 雷羽龙 ⚡（普通 · 代表种）
```json
{
  "speciesId": "thunder_feather_dragon", "speciesName": "雷羽龙", "element": "Electro", "rarity": "Common",
  "validSeasons": [2], "baseAttack": 23, "baseDefense": 13, "baseAgility": 15, "baseMaxHP": 110,
  "captureDifficulty": 3, "eggItemId": "egg_thunder_feather", "purchasePrice": 1100,
  "description": "风暴高原的雷龙，羽毛蓄满静电，快如闪电。"
}
```

### 17. 电鳞龙 ⚡（普通）
```json
{
  "speciesId": "volt_scale_dragon", "speciesName": "电鳞龙", "element": "Electro", "rarity": "Common",
  "validSeasons": [2], "baseAttack": 21, "baseDefense": 14, "baseAgility": 12, "baseMaxHP": 120,
  "captureDifficulty": 2, "eggItemId": "egg_volt_scale", "purchasePrice": 950,
  "description": "雷暴山顶的电龙，鳞片间跳动着微小电弧。"
}
```

### 18. 霆霄龙 🌩️（稀有）
```json
{
  "speciesId": "storm_heaven_dragon", "speciesName": "霆霄龙", "element": "Electro", "rarity": "Rare",
  "validSeasons": [2], "baseAttack": 35, "baseDefense": 21, "baseAgility": 22, "baseMaxHP": 165,
  "captureDifficulty": 6, "eggItemId": "egg_storm_heaven", "purchasePrice": 3600,
  "description": "御雷而行的稀有雷龙，仅在雷暴最烈时现身。"
}
```

---

## 七、风（Anemo · 春）

### 19. 风暴龙 🌪️（普通 · 代表种）
```json
{
  "speciesId": "storm_dragon", "speciesName": "风暴龙", "element": "Anemo", "rarity": "Common",
  "validSeasons": [0], "baseAttack": 22, "baseDefense": 13, "baseAgility": 15, "baseMaxHP": 112,
  "captureDifficulty": 2, "eggItemId": "egg_storm", "purchasePrice": 1000,
  "description": "山巅峡谷的风龙，翼展宽大，乘风翱翔。"
}
```

### 20. 流云龙 ☁️（普通）
```json
{
  "speciesId": "cloud_dragon", "speciesName": "流云龙", "element": "Anemo", "rarity": "Common",
  "validSeasons": [0], "baseAttack": 19, "baseDefense": 12, "baseAgility": 16, "baseMaxHP": 108,
  "captureDifficulty": 2, "eggItemId": "egg_cloud", "purchasePrice": 900,
  "description": "风蚀石林间游弋的风龙，身姿轻灵，来去如云。"
}
```

### 21. 苍穹龙 🐉（稀有）
```json
{
  "speciesId": "firmament_dragon", "speciesName": "苍穹龙", "element": "Anemo", "rarity": "Rare",
  "validSeasons": [0], "baseAttack": 31, "baseDefense": 22, "baseAgility": 21, "baseMaxHP": 175,
  "captureDifficulty": 5, "eggItemId": "egg_firmament", "purchasePrice": 3400,
  "description": "翱翔天际的稀有风龙，据传能看到大陆尽头的云海。"
}
```

---

## 补充：星陨龙 ✨（传说 · 剧情限定）

> 不参与常规 21 种，剧情最终奖励。双元素（随机主+副）。

```json
{
  "speciesId": "starfall_dragon", "speciesName": "星陨龙", "element": "Anemo", "rarity": "Rare",
  "validSeasons": [-1], "canBeDualElement": false,
  "baseAttack": 45, "baseDefense": 35, "baseAgility": 28, "baseMaxHP": 250,
  "captureDifficulty": 8, "eggItemId": "egg_starfall", "purchasePrice": 0,
  "description": "随灵流星坠落的传说龙种，蕴含两种元素之力。"
}
```

---

## 龙蛋物品（ItemData）配套清单

每只龙需对应 `eggItemId` 龙蛋物品，建议 type=`Material`（或专用龙蛋类），basePrice 对齐龙 purchasePrice × 0.5。共 22 个蛋物品 ID：

`egg_leaf_fall` / `egg_vine` / `egg_forest_whisper` / `egg_flame_heart` / `egg_lava` / `egg_crimson_flame` / `egg_tide_song` / `egg_stream` / `egg_abyss` / `egg_rock_armor` / `egg_stone_scale` / `egg_mountain` / `egg_frost_wing` / `egg_ice_crystal` / `egg_deep_winter` / `egg_thunder_feather` / `egg_volt_scale` / `egg_storm_heaven` / `egg_storm` / `egg_cloud` / `egg_firmament` / `egg_starfall`
