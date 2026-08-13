# 工匠设备配方 + 社区收集包 + 成就数据实例

---

## 一、工匠设备配方（ArtisanDeviceData · 8 种，GDD 5.2.4）

### 1. 酱缸 🫙（deviceMultiplier=1.6）
| recipeId | 原料 | 产物 | processDays | 季节 |
|---|---|---|---|---|
| salted_egg | 鸡蛋×5 | 咸蛋×1 | 2 | 全年 |
| salted_duck_egg | 鸭蛋×5 | 咸鸭蛋×1 | 2 | 全年 |
| salted_goose_egg | 鹅蛋×5 | 咸鹅蛋×1 | 2 | 全年 |
| pickled_radish | 萝卜×1 | 酱萝卜×1 | 4 | 全年 |
| pickled_cabbage | 白菜×1 | 酱白菜×1 | 4 | 全年 |
| soy_sauce | 黄豆(煮熟)×1 | 酱油×1 | 14 | 夏加速+20% |

### 2. 豆腐坊 🏠（deviceMultiplier=1.7）
| recipeId | 原料 | 产物 | processDays |
|---|---|---|---|
| tofu | 黄豆×3+水 | 豆腐×4+豆浆×2 | 0.25（6小时） |
| fermented_tofu | 豆腐(发酵) | 腐乳×1 | 20（冬成功率高） |

### 3. 磨坊 ⚙️（deviceMultiplier=1.4）
| recipeId | 原料 | 产物 | processDays |
|---|---|---|---|
| flour | 小麦×5 | 面粉×1 | 0.08（2小时） |
| white_rice | 水稻×5 | 白米×1 | 0.08 |
| bean_powder | 黄豆×1 | 豆粉×1 | 0.08 |

### 4. 腊肉架 🥩（deviceMultiplier=1.8）
| recipeId | 原料 | 产物 | processDays | 季节 |
|---|---|---|---|---|
| cured_meat | 猪肉×2+盐+香料 | 腊肉×1 | 10 | 秋冬 |
| ham | 猪肉×2+盐 | 火腿×1 | 10 | 秋冬 |
| dried_beef | 牛肉×2+香料 | 牛肉干×1 | 3 | 全年 |

### 5. 糖坊 🍬（deviceMultiplier=2.0）
| recipeId | 原料 | 产物 | processDays |
|---|---|---|---|
| brown_sugar | 甘蔗×1 | 红糖×1 | 1 |
| white_sugar | 甜菜×1 | 白糖×1 | 1 |
| maltose | 麦芽×1 | 麦芽糖×1 | 1 |

### 6. 老式织机 🧵（deviceMultiplier=2.2）
| recipeId | 原料 | 产物 | processDays |
|---|---|---|---|
| wool_blanket | 羊毛×3 | 粗羊毛毯×1 | 1 |
| silk_cloth | 蚕丝×1 | 丝绸×1 | 1 |
| cotton_cloth | 棉纱×1 | 棉布×1 | 1 |

### 7. 酿酒桶 🍶（deviceMultiplier=2.5）
| recipeId | 原料 | 产物 | processDays |
|---|---|---|---|
| rice_wine | 水稻×5+酒曲 | 米酒×1 | 7 |
| fruit_wine | 水果×6+糖 | 果酒×1 | 7 |
| liquor | 高粱+酒曲+药材 | 烈酒×1 | 14 |

### 8. 榨油机 🫒（deviceMultiplier=1.9）
| recipeId | 原料 | 产物 | processDays |
|---|---|---|---|
| rapeseed_oil | 油菜籽×1 | 菜籽油×1 | 0.17（4小时） |
| peanut_oil | 花生×1 | 花生油×1 | 0.17 |
| sesame_oil | 芝麻×1 | 芝麻油×1 | 0.17 |

---

## 二、社区收集包（CommunityBundle · GDD 7.3）

| bundleId | bundleName | 所需物品 | 关联房间 roomId | 奖励 |
|---|---|---|---|---|
| spring_crops | 春耕包 | 芜菁×5, 白菜×5, 萝卜×5 | greenhouse | 温室解锁 |
| summer_crops | 夏作包 | 番茄×5, 辣椒×5, 玉米×5 | greenhouse | 温室解锁 |
| autumn_crops | 秋收包 | 南瓜×5, 胡萝卜×5, 红薯×5 | greenhouse | 温室解锁 |
| winter_crops | 冬储包 | 冬白菜×5, 白萝卜×5, 冬瓜×3 | greenhouse | 温室解锁 |
| fish_bundle | 渔获包 | 鲤鱼×3, 鲫鱼×3, 鲈鱼×3 | minecart | 矿车解锁 |
| ore_bundle | 矿石包 | 铜矿石×10, 铁矿石×10 | minecart | 矿车解锁 |
| animal_bundle | 畜牧包 | 鸡蛋×5, 牛乳×3, 羊毛×3 | golden_clock | 金色时钟 |
| artisan_bundle | 工匠包 | 咸蛋×3, 豆腐×3, 米酒×2 | golden_clock | 金色时钟 |

---

## 三、成就（AchievementData · GDD 7.6，80+ 示例）

> 字段：`achievementId`/`achievementName`/`category`/`collectionType`/`targetCount`/`rewardGold`/`rewardTitle`

### 种田类（Farming）
| achievementId | 名称 | targetCount | 说明 |
|---|---|---|---|
| ach_harvest_100 | 收获达人 | 100 | 累计收获100次 |
| ach_harvest_1000 | 丰收之王 | 1000 | 累计收获1000次 |
| ach_crops_all | 作物图鉴全 | 44 | 收集全部作物 |

### 钓鱼类（Fishing，collectionType=Fish）
| achievementId | 名称 | targetCount |
|---|---|---|
| ach_fish_10 | 钓鱼新手 | 10 |
| ach_fish_30 | 钓鱼行家 | 30 |
| ach_fish_60 | 鱼类图鉴全 | 60 |

### 驯龙类（Dragon，collectionType=Dragon）
| achievementId | 名称 | targetCount |
|---|---|---|
| ach_dragon_7 | 元素入门 | 7 |
| ach_dragon_21 | 龙种图鉴全 | 21 |

### 战斗类（Combat）
| achievementId | 名称 | targetCount |
|---|---|---|
| ach_kill_100 | 讨伐百兽 | 100 |
| ach_kill_1000 | 讨伐千兽 | 1000 |
| ach_boss_all | 全BOSS讨伐 | 10 |

### 社交类（Social）
| achievementId | 名称 | targetCount |
|---|---|---|
| ach_npc_40 | 全镇相识 | 40 |
| ach_married | 喜结良缘 | 1 |

### 里程碑（Milestone）
| achievementId | 名称 | targetCount |
|---|---|---|
| ach_final_boss | 击败最终BOSS | 1 |
| ach_all_relics | 集齐四圣物 | 4 |
| ach_notes_50 | 怪奇收藏家 | 50 |

---

## 汇总

| 类别 | 数量 |
|---|---|
| 工匠配方 | 30（8 设备） |
| 社区收集包 | 8 |
| 成就 | 25（示例，可扩展至 80+） |
