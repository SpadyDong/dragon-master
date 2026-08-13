# 食谱数据实例（32+ 道）— RecipeData

> 来源（GDD 5.8 卯时饭馆 + GDD 7.5 烹饪系统）
> 三类：① 固定家常（12） ② 季节特供（12，每季3） ③ 隐藏私房（8）
> 字段：`recipeId` / `recipeName` / `ingredients[]` / `resultItemId` / `resultCount` / `cookTime` / `unlockSource` / `buff`

---

## ① 固定家常（12 道 · 全年）

| recipeId | recipeName | 食材 | resultItemId | 售价 | 体力 | Buff |
|---|---|---|---|---|---|---|
| rice_greens | 白米饭+青菜 | 大米×1, 青菜×1 | dish_rice_greens | 20 | +40 | — |
| tomato_egg_noodle | 番茄鸡蛋面 | 番茄×1, 鸡蛋×1, 面粉×1 | dish_tomato_egg_noodle | 35 | +80 | 心情+5 |
| braised_pork_rice | 红烧肉盖饭 | 猪肉×1, 大米×1, 酱油×1 | dish_braised_pork | 80 | +160 HP+30 | — |
| farmhouse_bowl | 农家一碗香 | 猪肉×1, 鸡蛋×1, 辣椒×1 | dish_farmhouse | 100 | +180 | 攻击+5%(1日) |
| potato_beef | 土豆炖牛肉 | 土豆×2, 牛肉×1 | dish_potato_beef | 120 | +200 HP+60 | 防御+8%(1日) |
| grilled_fish | 烤鱼 | 任意鱼×1 | dish_grilled_fish | 130 | +170 MP+30 | 钓鱼稀有度+5%(1日) |
| tofu_casserole | 豆腐煲 | 豆腐×1, 白菜×1 | dish_tofu_casserole | 60 | +100 MP+50 | 治疗吸收+10% |
| mutton_paomo | 羊肉泡馍 | 羊肉×1, 面粉×1 | dish_mutton_paomo | 140 | +220 | 寒冷抗性+50% |
| stir_fry_greens | 清炒时蔬 | 任意蔬菜×1 | dish_stir_fry | 40 | +60 | 品质+1级(1日作物) |
| three_fresh_soup | 三鲜汤 | 虾×1, 蛋×1, 青菜×1 | dish_three_fresh | 90 | +120 MP+80 | 暴击+3%(1日) |
| meat_buns | 酱肉包(3只) | 猪肉×1, 面粉×1 | dish_meat_buns | 50 | +90 | — |
| wonton | 卯氏云吞 | 猪肉×1, 面粉×1 | dish_wonton | 70 | +130 | 速度+10%(战斗1场) |

---

## ② 季节特供（12 道 · 每季 3）

### 春（season=0）
| recipeId | recipeName | 食材 | Buff |
|---|---|---|---|
| yanduxian | 腌笃鲜 | 春笋×1, 腊肉×1 | 体力+250，种植生长+10%(1日) |
| toona_egg | 香椿芽炒蛋 | 香椿芽×1, 鸡蛋×1 | 心情+20，暴击+4% |
| shepherd_wonton | 荠菜馄饨 | 荠菜×1, 面粉×1 | 鱼类稀有度+10%(当日) |

### 夏（season=1）
| recipeId | recipeName | 食材 | Buff |
|---|---|---|---|
| cold_noodle | 凉拌鸡丝凉面 | 鸡丝×1, 黄瓜×1, 芝麻×1 | 抗暑，速度+5% |
| wintermelon_meatball | 冬瓜丸子汤 | 冬瓜×1, 猪肉×1 | HP+80，水抗+10% |
| garlic_crayfish | 蒜蓉小龙虾 | 河虾×2, 蒜×1 | 体力+300，掉落+5%(当日) |

### 秋（season=2）
| recipeId | recipeName | 食材 | Buff |
|---|---|---|---|
| chestnut_chicken | 板栗烧鸡 | 栗子×1, 鸡肉×1 | 攻击+10%，防御+5%(1日) |
| osmanthus_lotus | 桂花糯米藕 | 莲藕×1, 桂花×1, 糯米×1 | 灵力+100，好感礼物+5% |
| crab_tofu | 蟹粉豆腐 | 螃蟹×1, 豆腐×1 | HP+100 MP+100，幸运+10% |

### 冬（season=3）
| recipeId | recipeName | 食材 | Buff |
|---|---|---|---|
| cloud_boil_pot | 云腾八珍锅(招牌) | 松露×1, 兽骨×1, 八珍×1 | 体力+500 全属性+5%(2日) |
| sauerkraut_pork | 白肉酸菜火锅 | 酸菜×1, 白肉×1 | 抗寒，心情+20 |
| laba_porridge | 腊八粥(腊八限定) | 8种豆+米 | 好感+3%/全属性+2%(1季) |

---

## ③ 隐藏私房（8 道 · 送稀有食材解锁）

| # | recipeId | recipeName | 解锁条件（送卯师傅） | 效果 |
|---|---|---|---|---|
| H1 | crab_lionhead | 蟹粉狮子头 | 金秋螃蟹×3 | 全队HP/MP回满 |
| H2 | fish_bite_sheep | 传说鱼咬羊 | 传说鱼×1+羊肉×1 | 全属性+12%(3日) |
| H3 | truffle_chicken | 松露炖鸡 | 松露×2+土鸡×1 | 驯龙捕捉+10%(1日) |
| H4 | beggar_chicken | 秘制叫花鸡 | 完整鸡×1+荷叶+黄泥(好感≥800) | 所有NPC当日+5好感 |
| H5 | walnut_eight | 核桃八宝鸡 | 核桃+板栗+莲子+香菇…8种 | 经验+20%(1日) |
| H6 | fermented_rice | 酒酿圆子 | 米酒+桂花+糯米(好感≥1200) | 告白成功率+15% |
| H7 | love_bento | 爱心便当 | 婚后专属 | 全属性+8%+暴击+8% |
| H8 | longevity_pot | 养母长寿锅 | 婚后卯大娘剧情 | 永久体质+10(1次) |

---

## 通用 Buff 字段映射（FoodBuff）

| Buff 项 | 对应字段 | 示例值 |
|---|---|---|
| 攻击+5% | attackBonus | 5（按百分比由烹饪系统应用，或存百分比） |
| 防御+8% | defenseBonus | 8 |
| 暴击+4% | critBonus | 0.04f |
| HP上限+30 | hpMaxBonus | 30 |
| MP上限+50 | mpMaxBonus | 50 |
| 持续时间 | durationMinutes | 1440（1日） |

> ⚠️ 说明：现有 `FoodBuff` 结构的 `attackBonus/defenseBonus` 是整数绝对值，若需百分比加成，需在 `CookingManager.ApplyBuff` 中约定为「百分比整数」或扩展字段。建议：攻击/防御字段存百分比整数（如 5 表示 +5%），HP/MP 存绝对值。
