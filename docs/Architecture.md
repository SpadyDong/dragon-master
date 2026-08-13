# 《驯龙高手》架构文档

> 更新日期：2026-08-13
> 存档版本：**0.12.0**
> 引擎：Unity + C#（纯 .cs 源码，无 Unity 工程文件）

---

## 一、技术栈与约定

| 项 | 约定 |
|---|---|
| 命名 | PascalCase 属性/方法，`_camelCase` 私有字段 |
| 序列化 | `[Header]` + `[SerializeField]` 模式 |
| 单例 | `Instance` 静态属性；跨场景用 `DontDestroyOnLoad` |
| 跨系统通信 | `EventBus.Subscribe/Publish`（GameEvent 枚举） |
| 数据资产 | ScriptableObject + `[CreateAssetMenu]` |
| 存档 | JsonUtility 兼容的 `[Serializable]` 结构 |
| 数据加载 | `Resources.Load<T>("路径/{id}")` 或 Inspector 拖拽 |

---

## 二、模块目录结构（`game/src/`）

| 目录 | 职责 | 关键类 |
|---|---|---|
| `Core/` | 全局核心 | GameManager / TimeManager / WeatherSystem / InputManager / EventBus / EquipmentManager / QuestManager / SceneTransitionManager / SettingsManager / TutorialManager |
| `Player/` | 玩家 | PlayerController / PlayerStats / PlayerAnimation |
| `NPC/` | NPC | NPCController / NPCData / NPCDatabase / NPCRelationshipManager / PlayerAffectionManager / MarriageManager / NPCQuests |
| `Dragon/` | 驯龙 | DragonData / DragonElement / DragonController / DragonManager / DragonBreeding / DragonEquipment |
| `Farming/` | 种植 | FarmingManager / FarmTile / CropData / FruitTree / Beehive / Sprinkler |
| `Livestock/` | 畜牧 | LivestockManager / AnimalController / AnimalData |
| `Fishing/` | 钓鱼 | FishingManager / FishData / FishingRodData / FishingQTE / FishingSpot / FishPond |
| `Combat/` | 战斗 | BattleManager / CombatUnit / CritSystem / DamageCalculator / ElementReactionSystem / WeaponTypeStats / BossData / WildAnimalData / BattleTrigger / BattleUI |
| `Inventory/` | 背包/商店 | InventoryManager / ItemData / InventoryUI / ShopData / ShopController |
| `Building/` | 建造 | StorageChest |
| `Dialogue/` | 对话 | DialogueManager / DialogueData / DialogueUI / NPCDialogueTrees |
| `Interaction/` | 交互 | Interactable / InteractionController / InteractionPrompt |
| `Terrain/` | 地形 | TerrainManager / TerrainTile |
| `UI/` | 界面 | UIManager / HUD / InventoryPanel / EquipmentPanel / MapPanel / QuestPanel / RelationshipPanel / SettingsPanel / QuickSlotBar / PauseMenu / ShopPanel / ProficiencyPanel / AchievementPanel / MarketPanel / ArtisanPanel / CookingPanel / MiningPanel / MuseumPanel / TransportPanel / CommunityPanel / SecretNotePanel |
| `SaveLoad/` | 存档 | SaveManager |
| `Audio/` | 音频 | AudioManager |
| `World/` | 世界 | YSortOrder / HotSpring |
| `Proficiency/` | 熟练度 | ProficiencyManager / ProficiencySkill |
| `Cooking/` | 烹饪 | CookingManager / RecipeData |
| `Mining/` | 矿洞 | MiningManager / OreData / MineData |
| `Achievement/` | 成就 | AchievementManager / AchievementData |
| `Market/` | 菜市场 | MarketManager / MarketCategory / MarketStall |
| `Artisan/` | 工匠设备 | ArtisanManager / ArtisanDeviceData / ArtisanStation |
| `Festival/` | 节日 | FestivalManager / FestivalData |
| `CommunityCenter/` | 社区中心 | CommunityCenterManager / CommunityBundle |
| `Museum/` | 博物馆 | MuseumManager |
| `Transport/` | 传送 | TransportManager |
| `Secrets/` | 秘密纸条 | SecretNoteManager |
| `Story/` | 主线剧情 | StoryManager / StoryProgress / SacredRelicData / StoryEndingResolver / StoryQuests |

---

## 三、单例清单（38 个）

### 3.1 跨场景常驻（`DontDestroyOnLoad`，21 个）

| 单例 | 文件 | 职责 |
|---|---|---|
| GameManager | Core/GameManager.cs | 时间/天气/金币/游戏状态/玩家性别 |
| InputManager | Core/InputManager.cs | 输入管理 + 输入锁定 |
| QuestManager | Core/QuestManager.cs | 任务状态机 |
| SceneTransitionManager | Core/SceneTransitionManager.cs | 场景切换 |
| SettingsManager | Core/SettingsManager.cs | 设置持久化（PlayerPrefs） |
| AudioManager | Audio/AudioManager.cs | 音频 |
| NPCRelationshipManager | NPC/NPCRelationshipManager.cs | NPC-NPC 关系网 |
| PlayerAffectionManager | NPC/PlayerAffectionManager.cs | 玩家→NPC 好感度 + 送礼 + 心事件 |
| MarriageManager | NPC/MarriageManager.cs | 结婚 + 配偶 |
| ProficiencyManager | Proficiency/ProficiencyManager.cs | 6 大熟练度 |
| CookingManager | Cooking/CookingManager.cs | 食谱/烹饪/Buff |
| MiningManager | Mining/MiningManager.cs | 矿洞 |
| AchievementManager | Achievement/AchievementManager.cs | 成就/图鉴 |
| MarketManager | Market/MarketManager.cs | 菜市场价格 |
| ArtisanManager | Artisan/ArtisanManager.cs | 工匠加工 |
| FestivalManager | Festival/FestivalManager.cs | 节日 |
| CommunityCenterManager | CommunityCenter/CommunityCenterManager.cs | 社区中心 |
| MuseumManager | Museum/MuseumManager.cs | 博物馆捐赠 |
| TransportManager | Transport/TransportManager.cs | 传送 |
| SecretNoteManager | Secrets/SecretNoteManager.cs | 秘密纸条 |
| StoryManager | Story/StoryManager.cs | 主线剧情 |

### 3.2 场景内单例（17 个，随场景重建）

| 单例 | 文件 | 职责 |
|---|---|---|
| WeatherSystem | Core/WeatherSystem.cs | 天气系统 |
| EquipmentManager | Core/EquipmentManager.cs | 装备管理（含龙装备） |
| TutorialManager | Core/TutorialManager.cs | 七日引导 |
| DialogueManager | Dialogue/DialogueManager.cs | 对话 |
| DialogueUI | Dialogue/DialogueUI.cs | 对话 UI |
| DragonManager | Dragon/DragonManager.cs | 驯龙 |
| FarmingManager | Farming/FarmingManager.cs | 种植 |
| FishingManager | Fishing/FishingManager.cs | 钓鱼 |
| InventoryManager | Inventory/InventoryManager.cs | 背包 |
| LivestockManager | Livestock/LivestockManager.cs | 畜牧 |
| BattleManager | Combat/BattleManager.cs | 战斗 |
| PlayerController | Player/PlayerController.cs | 玩家移动 |
| PlayerStats | Player/PlayerStats.cs | 玩家属性 |
| SaveManager | SaveLoad/SaveManager.cs | 存档 |
| TerrainManager | Terrain/TerrainManager.cs | 地形 |
| QuickSlotBar | UI/QuickSlotBar.cs | 快捷栏 |
| UIManager | UI/UIManager.cs | UI 面板管理 |

> ⚠️ **注意**：`SaveManager` 未设置 `DontDestroyOnLoad`，需确保其 GameObject 在初始场景且跨场景保留，或手动添加。

---

## 四、全局事件总线（GameEvent，约 76 个）

`EventBus.Subscribe<T>(GameEvent, Action<T>)` / `EventBus.Publish<T>(GameEvent, T)`。参数类型见下表。

### 时间（5）
`MinuteChanged`(int) · `HourChanged`(int) · `DayChanged`(int) · `SeasonChanged`(int) · `YearChanged`(int)

### 天气（1）
`WeatherChanged`(int)

### 玩家（7）
`PlayerMoved` · `PlayerInteracted`(string) · `PlayerStatsChanged` · `PlayerGoldChanged`(int) · `PlayerStaminaChanged`(int) · `PlayerHPChanged`(int) · `PlayerHungerChanged`(int) · `PlayerElementChanged`(int)

### NPC / 好感 / 结婚（5）
`NPCDialogueStarted` · `NPCDialogueEnded` · `NPCAffectionChanged`(int) · `MarriageProposed`(string) · `Married`(string)

### 物品 / 装备 / 商店（10）
`ItemPickedUp`(string) · `ItemUsed`(string) · `InventoryChanged`(string) · `EquipmentChanged` · `WeaponSwitched`(int) · `ShopOpened`(string) · `ShopClosed`(string) · `ItemBought`(string) · `ItemSold`(string) · `EquipmentForged`(string)

### 游戏状态（3）
`GamePaused` · `GameResumed` · `GameStateChanged`(GameState)

### 种植（6）
`FarmTileStateChanged`(int) · `CropPlanted`(string) · `CropHarvested`(string) · `SprinklerActivated`(int) · `FruitTreeHarvested`(string) · `BeehiveHarvested`(string)

### 畜牧（4）
`AnimalFed`(string) · `AnimalPetted`(string) · `AnimalProductCollected`(string) · `AnimalMoodChanged`(string)

### 驯龙（6）
`DragonStageChanged`(string) · `DragonFed`(string) · `DragonPetted`(string) · `DragonEggLaid`(string) · `DragonHatched`(string) · `DragonCaptured`(string)

### 钓鱼（4）
`FishCaught`(string) · `FishingStarted`(int) · `FishingEnded`(bool) · `FishPondHarvested`(string)

### 地形 / 元素（2）
`TerrainChanged`(int) · `ElementReactionTriggered`(string)

### 战斗（7）
`BattleStarted`(string) · `BattleEnded`(string) · `TurnChanged`(int) · `UnitDamaged`(string) · `UnitDefeated`(string) · `CaptureAttempted`(string) · `CaptureSuccess`(string)

### M10+ 扩展（11）
`ProficiencyLeveledUp`(int) · `ProficiencyXPChanged`(int) · `RecipeLearned`(string) · `DishCooked`(string) · `OreMined`(string) · `FloorDescended`(int) · `AchievementUnlocked`(string) · `HotSpringUsed` · `MarketPriceUpdated` · `ArtisanJobCompleted`(string) · `FestivalStarted`(string) · `BundleCompleted`(string) · `MuseumDonated`(string) · `NoteFound`(int)

### 主线剧情（4）
`StoryNodeCompleted`(string) · `RelicCollected`(string) · `StoryActChanged`(int) · `EndingResolved`(string)

---

## 五、存档结构（SaveData · 版本 0.12.0）

`SaveManager.SaveData` 字段，JSON 格式（`JsonUtility.ToJson`）：

| 字段 | 类型 | 说明 |
|---|---|---|
| year/season/day/hour/minute | int | 时间 |
| weather/weatherIndex | string/int | 天气 |
| gold | int | 金币 |
| sceneName / playerPosX/Y | string/float | 场景与位置 |
| currentHP/Stamina/Hunger/MP | int | 玩家当前状态 |
| strength/agility/vitality/intelligence | int | 四维属性 |
| playerElement | int | 主元素（-1=null） |
| playerGender | int | 性别（0=男 1=女） |
| inventory | InventorySaveData | 背包 |
| equipment | EquipmentSaveData | 装备（含龙装备） |
| quests | QuestSaveData | 任务 |
| farmTiles / fruitTrees / beehives | List | 种植 |
| animals | List | 畜牧 |
| storageChests | List | 储物箱 |
| dragons | List | 驯龙 |
| fishing / fishPonds | ... | 钓鱼 |
| terrainTiles | List | 地形 |
| proficiency | ProficiencySaveData | 熟练度 |
| cooking | CookingSaveData | 烹饪 |
| mining | MiningSaveData | 矿洞 |
| achievements | AchievementSaveData | 成就 |
| market | MarketSaveData | 菜市场 |
| artisan | ArtisanSaveData | 工匠 |
| playerAffection | PlayerAffectionSaveData | 好感度 |
| marriage | MarriageSaveData | 结婚 |
| festival | FestivalSaveData | 节日 |
| communityCenter | CommunityCenterSaveData | 社区中心 |
| museum | MuseumSaveData | 博物馆 |
| transport | TransportSaveData | 传送 |
| secretNotes | SecretNoteSaveData | 秘密纸条 |
| story | StorySaveData | 主线 |

### 版本兼容策略
- 旧存档加载时，新字段为 null → 对应 Manager 跳过加载（保持默认）
- `playerElement` 用 `IsVersionAtLeast(data.version, "0.8.0")` 判断（数值比较，修复过字典序 bug）

---

## 六、跨模块依赖关系（关键）

| 依赖方 | 依赖 | 用途 |
|---|---|---|
| ProficiencyManager | 各系统 GameEvent | 订阅 XP 自动挂接（CropPlanted/FishCaught/BattleEnded/NPCAffectionChanged 等） |
| AchievementManager | FishCaught/DragonCaptured/CropHarvested/EquipmentForged | 图鉴收集 |
| MarketManager | PlayerAffectionManager | 经营者好感折扣 |
| ArtisanManager | MarketManager | 定价公式（原料公告价） |
| MarriageManager | PlayerAffectionManager + InventoryManager | 求婚校验（好感 + 龙心挂坠） |
| StoryManager | MarriageManager + DayChanged/SeasonChanged | 结局判定 + 幕推进 |
| CookingManager | ProficiencyManager + InventoryManager | 烹饪 + 社交 XP |
| MiningManager | ProficiencyManager + BattleManager | 挖矿 XP + 遇怪战斗 |
| CombatUnit | PlayerStats / DragonController / EquipmentManager | 构建战斗单位 |
| DialogueConditionChecker | PlayerAffectionManager | AffectionAtLeast 条件 |
| SaveManager | 所有 Manager | 聚合存档 |

---

## 七、ScriptableObject 资产（17 种）

详见 `docs/DataAssets.md`。Resources 加载路径：`Items/`、`Dragons/`、`Crops/`、`Fishes/`、`Rods/`、`Animals/`、`Recipes/`、`Achievements/`（LoadAll）、`Festivals/`（LoadAll）。

---

## 八、开发里程碑状态

| 里程碑 | 状态 |
|---|---|
| M1 原型 | ✅ |
| M2 经营基础 | ✅ |
| M5 驯龙系统 | ✅（简化） |
| M6 NPC+好感+结婚 | ✅ |
| M7 钓鱼+地形 | ✅ |
| M8 七元素+武器装备 | ✅ |
| M9 回合制战斗 | ✅ |
| M10 星露谷补全（核心+经济+剩余） | ✅ |
| M11 主线剧情（逻辑层） | ✅ |
| M12 打磨收尾 | ✅ |

> 全部里程碑代码层完成。待 Unity 导入后：编译验证 + 创建数据资产 + 挂载 Manager 到场景。
