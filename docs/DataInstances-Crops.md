# 作物与种子数据实例 — CropData + 种子 ItemData

> 规则（GDD 5.1）：春/夏/秋/冬专属作物（每种 10+，中式作物）
> CropData 字段：`cropId`/`cropName`/`category`/`validSeasons`/`growthDaysTotal`/`cropItemId`/`seedItemId`/`yieldMin/Max`/`basePrice`/`seedPrice`
> 品质：普通×1.0 / 银×1.25 / 金×1.5 / 紫×1.75
> 种子 ItemData：type=Seed，itemName 后缀"种子"

---

## 一、春（season=0 · 10 种）

| cropId | cropName | 生长天数 | 产量 | basePrice | seedItemId | seedPrice |
|---|---|---|---|---|---|---|
| turnip | 芜菁 | 4 | 1-2 | 30 | turnip_seed | 15 |
| cabbage | 白菜 | 5 | 1-2 | 35 | cabbage_seed | 18 |
| radish | 萝卜 | 5 | 1-2 | 40 | radish_seed | 20 |
| potato | 土豆 | 6 | 1-3 | 45 | potato_seed | 22 |
| wheat | 小麦 | 5 | 2-4 | 28 | wheat_seed | 14 |
| rice | 水稻 | 6 | 2-4 | 32 | rice_seed | 16 |
| scallion | 葱 | 4 | 1-2 | 25 | scallion_seed | 12 |
| garlic | 蒜 | 5 | 1-2 | 30 | garlic_seed | 15 |
| tea | 茶 | 8 | 1-2 | 50 | tea_seed | 25 |
| strawberry | 草莓 | 7 | 2-3 | 60 | strawberry_seed | 30 |

---

## 二、夏（season=1 · 10 种）

| cropId | cropName | 生长天数 | 产量 | basePrice | seedItemId | seedPrice |
|---|---|---|---|---|---|---|
| tomato | 番茄 | 6 | 2-3 | 45 | tomato_seed | 22 |
| cucumber | 黄瓜 | 5 | 1-2 | 38 | cucumber_seed | 19 |
| eggplant | 茄子 | 5 | 1-2 | 42 | eggplant_seed | 21 |
| pepper | 辣椒 | 6 | 2-3 | 48 | pepper_seed | 24 |
| corn | 玉米 | 7 | 2-4 | 40 | corn_seed | 20 |
| watermelon | 西瓜 | 10 | 1-2 | 80 | watermelon_seed | 40 |
| millet | 粟米 | 5 | 2-4 | 30 | millet_seed | 15 |
| soy | 黄豆 | 6 | 2-4 | 34 | soy_seed | 17 |
| ginger | 姜 | 6 | 1-2 | 40 | ginger_seed | 20 |
| melon | 甜瓜 | 8 | 1-2 | 70 | melon_seed | 35 |

---

## 三、秋（season=2 · 10 种）

| cropId | cropName | 生长天数 | 产量 | basePrice | seedItemId | seedPrice |
|---|---|---|---|---|---|---|
| pumpkin | 南瓜 | 8 | 1-2 | 65 | pumpkin_seed | 32 |
| carrot | 胡萝卜 | 5 | 1-2 | 35 | carrot_seed | 18 |
| sweet_potato | 红薯 | 6 | 1-3 | 45 | sweet_potato_seed | 22 |
| onion | 洋葱 | 5 | 1-2 | 32 | onion_seed | 16 |
| leek | 韭菜 | 4 | 1-2 | 28 | leek_seed | 14 |
| taro | 芋头 | 7 | 1-3 | 50 | taro_seed | 25 |
| lotus_root | 莲藕 | 7 | 1-2 | 55 | lotus_root_seed | 28 |
| buckwheat | 荞麦 | 5 | 2-4 | 30 | buckwheat_seed | 15 |
| sunflower | 向日葵 | 6 | 1-2 | 40 | sunflower_seed | 20 |
| chestnut | 板栗 | 9 | 2-4 | 60 | chestnut_seed | 30 |

---

## 四、冬（season=3 · 8 种，需温室或耐寒）

| cropId | cropName | 生长天数 | 产量 | basePrice | seedItemId | seedPrice |
|---|---|---|---|---|---|---|
| winter_cabbage | 冬白菜 | 7 | 1-2 | 45 | winter_cabbage_seed | 22 |
| daikon | 白萝卜 | 6 | 1-2 | 40 | daikon_seed | 20 |
| winter_wheat | 冬小麦 | 7 | 2-4 | 32 | winter_wheat_seed | 16 |
| snow_pea | 雪豆 | 5 | 1-2 | 38 | snow_pea_seed | 19 |
| winter_melon | 冬瓜 | 9 | 1-2 | 70 | winter_melon_seed | 35 |
| turnip_green | 冬菜 | 6 | 1-2 | 35 | turnip_green_seed | 18 |
| horseradish | 辣根 | 7 | 1-2 | 42 | horseradish_seed | 21 |
| winter_berry | 冬莓 | 8 | 2-3 | 75 | winter_berry_seed | 38 |

---

## 五、果树（FruitTree · 6 种，GDD 5.1.1）

| treeId | treeName | 种植季 | 结果季 | 首果期 | 寿命 | 果实 basePrice |
|---|---|---|---|---|---|---|
| peach | 桃树 | 春 | 夏 | 第2年 | 30年 | 30 |
| cherry | 樱桃 | 春 | 夏 | 第2年 | 25年 | 60 |
| pear | 梨树 | 春 | 秋 | 第3年 | 40年 | 40 |
| hawthorn | 山楂 | 秋 | 秋 | 第3年 | 35年 | 45 |
| chestnut_tree | 栗子树 | 春 | 深秋 | 第4年 | 50年 | 35 |
| walnut | 核桃 | 春 | 晚秋 | 第5年 | 80年 | 55 |

---

## 汇总

| 类别 | 数量 |
|---|---|
| 春季作物 | 10 |
| 夏季作物 | 10 |
| 秋季作物 | 10 |
| 冬季作物 | 8 |
| 果树 | 6 |
| **合计** | **44** |

> 每种作物对应 1 个 CropData + 1 个种子 ItemData + 1 个产物 ItemData（type=Material 或专用蔬菜类）。
