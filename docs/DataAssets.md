# 《驯龙高手》数据资产清单（ScriptableObject）

> 用途：指导在 Unity 中批量创建数据资产（Assets → Create → 对应菜单）
> 加载方式分为两类：**Resources.Load（ID 字符串）** 和 **Inspector 拖拽引用**
> 所有 Resources 资产必须放在 `Assets/Resources/` 对应子目录下

---

## 一、Resources 目录结构（必须严格遵循）

```
Assets/
└── Resources/
    ├── Items/          → ItemData（物品/武器/防具/饰品/龙装备/种子/材料/礼物）
    ├── Dragons/        → DragonData（龙种）
    ├── Crops/          → CropData（作物）
    ├── Fishes/         → FishData（鱼种）
    ├── Rods/           → FishingRodData（鱼竿）
    ├── Animals/        → AnimalData（家畜）
    ├── Recipes/        → RecipeData（食谱）
    ├── Achievements/   → AchievementData（成就，LoadAll）
    └── Festivals/      → FestivalData（节日，LoadAll）
```

> 说明：以下类型**通过 Inspector 拖拽引用**（非 Resources），可放在任意目录，但建议统一放 `Assets/Data/` 下分类管理。

---

## 二、17 种 ScriptableObject 资产类型总览

| # | 类型 | 创建菜单 | 加载方式 | 目录 |
|---|---|---|---|---|
| 1 | ItemData | `Item/Item Data` | Resources | `Resources/Items/` |
| 2 | CropData | `Farming/Crop Data` | Resources | `Resources/Crops/` |
| 3 | AnimalData | `Livestock/Animal Data` | Resources | `Resources/Animals/` |
| 4 | DragonData | `Dragon/Dragon Data` | Resources | `Resources/Dragons/` |
| 5 | FishData | `Fishing/Fish Data` | Resources | `Resources/Fishes/` |
| 6 | FishingRodData | `Fishing/Fishing Rod Data` | Resources | `Resources/Rods/` |
| 7 | RecipeData | `Cooking/Recipe Data` | Resources | `Resources/Recipes/` |
| 8 | AchievementData | `Achievement/Achievement Data` | Resources (LoadAll) | `Resources/Achievements/` |
| 9 | FestivalData | `Festival/Festival Data` | Resources (LoadAll) | `Resources/Festivals/` |
| 10 | ShopData | `Shop/Shop Data` | Inspector | `Assets/Data/Shops/` |
| 11 | BossData | `Combat/Boss Data` | Inspector | `Assets/Data/Combat/` |
| 12 | WildAnimalData | `Combat/Wild Animal Data` | Inspector | `Assets/Data/Combat/` |
| 13 | OreData | `Mining/Ore Data` | Inspector | `Assets/Data/Mining/` |
| 14 | MineData | `Mining/Mine Data` | Inspector | `Assets/Data/Mining/` |
| 15 | CommunityBundle | `Community/Community Bundle` | Inspector | `Assets/Data/Community/` |
| 16 | ArtisanDeviceData | `Artisan/Device Data` | Inspector | `Assets/Data/Artisan/` |
| 17 | SacredRelicData | `Story/Sacred Relic Data` | Inspector | `Assets/Data/Story/` |

---

## 三、各类型字段与实例清单

### 1. ItemData（物品） — `Resources/Items/`

**关键字段**：`itemId`（唯一，Resources 路径用）、`itemName`、`type`（ItemType）、`weaponSubType`、`element`、`quality`、`dragonEquipmentSubType`、`basePrice`、`attackBonus/defenseBonus/speedBonus/magicBonus`、`staminaRestore/hpRestore/hungerRestore`

**GDD 建议实例**（约 200+）：

| 子类 | 数量 | 说明 |
|---|---|---|
| 种子 | 40 | 春/夏/秋/冬作物种子（芜菁/白菜/萝卜/稻/麦/粟/豆等） |
| 作物产物 | 60 | 蔬菜/水果/谷物（对应 CropData.cropItemId） |
| 工具 | 15 | 锄头（锈/铜/铁/金）、洒水壶、鱼竿等 |
| 武器 | 54 | 单手剑/双手剑/弓箭 × 6品质 × 各元素 |
| 防具/饰品 | 30 | 头盔/护甲/鞋子/饰品 |
| 龙装备 | 9+ | 龙鞍/龙鳞甲/龙饰 × 各元素 |
| 材料 | 30 | 矿石/木材/灵石/灵晶等 |
| 料理 | 50 | 对应 RecipeData.resultItemId |
| 鱼 | 60 | 对应 FishData（可直售） |
| 肉蛋奶 | 40 | 鸡蛋/鸭蛋/牛乳/生肉等 |
| 礼物/特殊 | 40 | 龙心挂坠、古币、灵石粉末等 |
| 工匠品 | 30 | 咸蛋/豆腐/腊肉/酒/油/酱等 |

> ⚠️ 关键 ID 约定（代码中硬编码引用）：
> - `dragon_heart_pendant` — 龙心挂坠（求婚道具）
> - `rope_net` / `fresh_meat` / `anesthetic` — 捕捉道具
> - `rusty_hoe` / `watering_can` / `turnip_seed` / `copper_ingot` / `cloth_armor` / `dragon_food` — 教程奖励
> - `iridium_ore` — 铱矿（方尖碑建造材料）
> - `heal_potion` — 回血药（战斗道具）

---

### 2. CropData（作物） — `Resources/Crops/`

**关键字段**：`cropId`、`cropName`、`category`、`validSeasons`、`growthDaysTotal`、`growthStages`、`cropItemId`、`seedItemId`、`yieldMin/Max`、`basePrice`、`seedPrice`、`isGiantable`

**GDD 建议实例**：每季节 10+ 种中式作物（稻/麦/粟/豆/白菜/萝卜/茄子/辣椒/姜/葱/蒜等），共 **40+ 种**。

---

### 3. AnimalData（家畜） — `Resources/Animals/`

**关键字段**：`animalId`、`animalName`、`type`（AnimalType 枚举）、`primaryProduct`、`secondaryProduct`、`productionInterval`、`purchasePrice`、`feedCost`、`canGraze`

**GDD 建议实例**：8 种家畜（鸡/鸭/鹅/猪/牛/羊/山羊/家兔），每种 1 个 = **8 个**。

---

### 4. DragonData（龙种） — `Resources/Dragons/`

**关键字段**：`speciesId`、`speciesName`、`element`、`rarity`、`validSeasons`、`baseAttack/Defense/Agility/MaxHP`、`juvenileStartDay/adolescentStartDay/adultStartDay/eggHatchDays`、`captureDifficulty`、`eggItemId`、`purchasePrice`

**GDD 建议实例**（简化后 7 元素 × 3 种 = **21 种**）：

| 元素 | 普通×2 | 稀有×1 |
|---|---|---|
| 草 | 落叶龙 | 稀有草龙 |
| 火 | 焰心龙 | 稀有火龙 |
| 水 | 潮鸣龙 | 稀有水龙 |
| 岩 | 岩铠龙 | 稀有岩龙 |
| 冰 | 霜翼龙 | 稀有冰龙 |
| 雷 | 雷羽龙 | 稀有雷龙 |
| 风 | 风暴龙 | 稀有风龙 |

> 另：星陨龙（双元素传说，剧情限定）可选。

---

### 5. FishData（鱼种） — `Resources/Fishes/`

**关键字段**：`fishId`、`fishName`、`rarity`、`location`、`validSeasons/TimeSlots/Weathers`、`qteDuration`、`biteSpeed`、`difficulty`、`basePrice`、`energyRestore`、`isLegendFish`

**GDD 建议实例**：60+ 鱼种，含 5 条传说鱼（四季各 1 + 火山 1）。

---

### 6. FishingRodData（鱼竿） — `Resources/Rods/`

**关键字段**：`rodId`、`rodName`、`tier`（Bamboo/Iron/Gold/DragonSpine）、`qteBarSizeBonus`、`reelingSpeed`、`maxCatchDifficulty`、`purchasePrice`

**GDD 建议实例**：4 种鱼竿（竹/铁/金/龙脊）= **4 个**。

---

### 7. RecipeData（食谱） — `Resources/Recipes/`

**关键字段**：`recipeId`、`recipeName`、`ingredients[]`、`resultItemId`、`resultCount`、`cookTime`、`unlockSource`、`buff`

**GDD 建议实例**：100+ 食谱（12 固定家常 + 12 季节特供 + 8 隐藏私房 + 其余）。

---

### 8. AchievementData（成就） — `Resources/Achievements/`

**关键字段**：`achievementId`、`achievementName`、`category`、`collectionType`、`targetCount`、`rewardGold`、`rewardItemId`、`rewardTitle`

**GDD 建议实例**：80+ 成就（种田1000/全鱼类/全龙种/全NPC/击败最终BOSS等）。

---

### 9. FestivalData（节日） — `Resources/Festivals/`

**关键字段**：`festivalId`、`festivalName`、`season`、`day`、`activities`、`rewardGold`、`rewardItemId`、`rewardAffection`

> ⚠️ 内置兜底：`FestivalManager` 已内置 9 个节日默认表（春日祭/驯龙大典/夏之海祭/龙脊大集市/秋日丰收祭/灵石祭奠/冬雪祭/星陨除夕/新年）。**若在 Resources/Festivals/ 创建资产，会覆盖内置表**（LoadAll 优先）。建议：如用自定义节日，需完整覆盖 9 个；否则留空依赖内置表。

---

### 10. ShopData（商店） — `Assets/Data/Shops/`

**关键字段**：`shopId`、`shopName`、`shopType`（WeaponShop/DragonShop/GeneralShop）、`items[]`（ShopItemEntry）、`forgeRecipes[]`（ForgeRecipe）

**GDD 建议实例**：2-3 个（武器商店/龙之商店/通用商店）。

---

### 11. BossData（BOSS） — `Assets/Data/Combat/`

**关键字段**：`bossId`、`bossName`、`element`、`weakElement`、`maxHP/attack/defense/agility`、`isFlying`、`phase2HPThreshold/phase2MaxHP/phase2Attack`、`rewardGold/rewardItemIds`

**GDD 建议实例**：主线 BOSS ×4（风/火/水/冰守护龙）+ 洞窟 BOSS ×4 + 最终 BOSS 黑暗龙灵王（双阶段 HP2000→3500）+ 教程 BOSS 石甲犀 = **10 个**。

---

### 12. WildAnimalData（野怪） — `Assets/Data/Combat/`

**关键字段**：`animalId`、`animalName`、`element`、`maxHP/attack/defense/agility`、`isFlying`、`captureItemId`、`dropTable[]`、`spawnScenes`

**GDD 建议实例**：普通野怪 20 + 精英怪 10 = **30 个**。

---

### 13. OreData（矿石） — `Assets/Data/Mining/`

**关键字段**：`oreId`、`oreName`、`minFloor/maxFloor`、`dropChance`、`sellPrice`、`itemId`、`gatheringXP`

**GDD 建议实例**：铜/铁/金/铱/火灵石/冰灵石 等 **6-10 个**。

---

### 14. MineData（矿洞） — `Assets/Data/Mining/`

**关键字段**：`mineId`、`mineName`、`floorCount`、`elevatorInterval`、`ores[]`、`monsterData`、`encounterChance`、`treasureChance`、`treasureTable[]`

**GDD 建议实例**：4 个（阿布铜矿洞120层/骷髅深渊100+/火山矿洞50层/冰窟矿洞40层）。

---

### 15. CommunityBundle（社区收集包） — `Assets/Data/Community/`

**关键字段**：`bundleId`、`bundleName`、`requiredItems[]`、`roomId`、`rewardGold`、`rewardItemId`、`rewardAffection`

**GDD 建议实例**：按季节/技能套装，**6-10 个收集包**（对应温室/矿车/金色时钟等房间）。

---

### 16. ArtisanDeviceData（工匠设备） — `Assets/Data/Artisan/`

**关键字段**：`deviceId`、`deviceName`、`deviceMultiplier`、`outputCategory`、`recipes[]`（ArtisanRecipe）

**GDD 建议实例**：8 种设备（酱缸/豆腐坊/磨坊/腊肉架/糖坊/老式织机/酿酒桶/榨油机）= **8 个**。

---

### 17. SacredRelicData（四圣物） — `Assets/Data/Story/`

**关键字段**：`relicId`、`relicName`、`relicType`（SacredRelic）、`storyAct`、`requiredBossId`、`unlockCondition`、`rewardItemId`

**GDD 建议实例**：4 个（风息石/炎心石/潮涌石/霜魄石）。

---

## 四、创建优先级建议

### P0 — 原型可运行（最少集）
1. **ItemData**：教程奖励物品（rusty_hoe/watering_can/turnip_seed/copper_ingot/cloth_armor/dragon_food）+ 捕捉道具（rope_net/fresh_meat/anesthetic）+ 龙心挂坠（dragon_heart_pendant）+ 基础材料
2. **CropData**：1-2 种春季作物（芜菁/白菜）
3. **AnimalData**：鸡/牛/羊 3 种
4. **DragonData**：落叶龙（草）完整四阶段
5. **FishData**：5-10 种基础鱼
6. **FishingRodData**：竹竿
7. **RecipeData**：2-3 个基础食谱

### P1 — 核心玩法
8. 全部 21 种 DragonData
9. 全部 8 种 AnimalData
10. 40+ CropData
11. 4 种 FishingRodData
12. 6-10 OreData + 4 MineData
13. WildAnimalData（20普通+10精英）
14. BossData（教程BOSS + 主线4）

### P2 — 经济/社交/剧情
15. ShopData（2-3）+ ArtisanDeviceData（8）
16. CommunityBundle（6-10）
17. AchievementData（80+）
18. SacredRelicData（4）

### P3 — 完整
19. 全部 RecipeData（100+）
20. 全部 ItemData（200+）
21. FestivalData（可选，有内置兜底）
