# 鱼种数据实例（60 种）— FishData

> 规则（GDD 5.7）：60+ 鱼种，含 5 条传说鱼（四季各 1 + 火山 1）
> `rarity`：Common=普通 / Uncommon=罕见 / Rare=稀有 / Legendary=传说
> `location`：Sea=海边 / Lake=湖泊 / River=小河 / SnowMountain=雪山 / Volcano=火山
> `validSeasons`：-1=全年，0=春 1=夏 2=秋 3=冬
> `validTimeSlots`：0=清晨 1=上午 2=中午 3=下午 4=傍晚 5=夜晚 6=深夜，-1=全天
> `validWeathers`：0=晴 1=多云 2=小雨 3=大雨 4=雷暴 5=大风 6=大雪，-1=全天气
> `qteDuration`：普通3s / 罕见4s / 稀有6s / 传说12s

---

## 一、海边（Sea · 15 种）

| fishId | fishName | rarity | 季节 | 时段 | 天气 | 难度 | basePrice | 体力 |
|---|---|---|---|---|---|---|---|---|
| sea_bass | 海鲈 | Common | 全年 | 全天 | 晴 | 2 | 30 | 10 |
| sea_bream | 海鲷 | Common | 全年 | 清晨 | 晴 | 2 | 35 | 10 |
| mackerel | 鲭鱼 | Common | 春夏 | 上午 | 晴 | 2 | 28 | 10 |
| sardine | 沙丁鱼 | Common | 全年 | 上午 | 多云 | 1 | 20 | 8 |
| herring | 鲱鱼 | Common | 秋 | 下午 | 小雨 | 1 | 22 | 8 |
| flounder | 比目鱼 | Uncommon | 全年 | 夜晚 | 晴 | 3 | 55 | 14 |
| octopus | 章鱼 | Uncommon | 夏 | 夜晚 | 晴 | 4 | 65 | 16 |
| squid | 鱿鱼 | Uncommon | 全年 | 深夜 | 晴 | 3 | 50 | 13 |
| pufferfish | 河豚 | Rare | 夏 | 中午 | 晴 | 6 | 120 | 5 |
| lobster | 龙虾 | Uncommon | 夏秋 | 上午 | 晴 | 3 | 60 | 15 |
| sea_urchin | 海胆 | Uncommon | 春夏 | 清晨 | 晴 | 4 | 70 | 12 |
| tuna | 金枪鱼 | Rare | 全年 | 上午 | 晴 | 6 | 150 | 25 |
| golden_tuna | 金色帝王鱼 | Rare | 全年 | 清晨 | 晴 | 7 | 200 | 30 |
| abalone | 鲍鱼 | Rare | 秋冬 | 上午 | 晴 | 6 | 140 | 20 |
| legendary_kraken | 深渊巨鱿 | Legendary | 全年 | 深夜 | 雷暴 | 10 | 2000 | 50 |

---

## 二、湖泊（Lake · 12 种）

| fishId | fishName | rarity | 季节 | 时段 | 天气 | 难度 | basePrice | 体力 |
|---|---|---|---|---|---|---|---|---|
| carp | 鲤鱼 | Common | 全年 | 全天 | 晴 | 2 | 25 | 9 |
| crucian | 鲫鱼 | Common | 全年 | 上午 | 晴 | 1 | 22 | 9 |
| grass_carp | 草鱼 | Common | 春夏 | 下午 | 多云 | 2 | 30 | 11 |
| bighead_carp | 鳙鱼 | Common | 全年 | 中午 | 晴 | 2 | 32 | 12 |
| lake_bass | 湖鲈 | Uncommon | 夏 | 清晨 | 晴 | 3 | 50 | 14 |
| eel | 鳗鱼 | Uncommon | 春夏 | 夜晚 | 小雨 | 4 | 60 | 16 |
| catfish | 鲶鱼 | Common | 全年 | 夜晚 | 晴 | 2 | 28 | 10 |
| loach | 泥鳅 | Common | 春夏 | 清晨 | 小雨 | 1 | 18 | 7 |
| crab | 河蟹 | Uncommon | 秋 | 傍晚 | 晴 | 3 | 55 | 13 |
| turtle | 甲鱼 | Rare | 全年 | 中午 | 晴 | 6 | 130 | 22 |
| freshwater_clam | 河蚌 | Common | 全年 | 上午 | 晴 | 1 | 20 | 6 |
| legendary_loong | 湖怪龙鱼 | Legendary | 秋 | 深夜 | 雾天 | 10 | 2200 | 55 |

---

## 三、小河（River · 12 种）

| fishId | fishName | rarity | 季节 | 时段 | 天气 | 难度 | basePrice | 体力 |
|---|---|---|---|---|---|---|---|---|
| trout | 鳟鱼 | Common | 全年 | 上午 | 晴 | 2 | 30 | 10 |
| salmon | 鲑鱼 | Uncommon | 秋 | 上午 | 晴 | 3 | 65 | 18 |
| dace | 鲮鱼 | Common | 全年 | 下午 | 多云 | 2 | 26 | 9 |
| gudgeon | 麦穗鱼 | Common | 春夏 | 清晨 | 晴 | 1 | 15 | 6 |
| minnow | 鳑鲏 | Common | 春 | 全天 | 晴 | 1 | 14 | 5 |
| shad | 鲥鱼 | Rare | 春 | 上午 | 小雨 | 5 | 110 | 20 |
| eel_river | 河鳗 | Uncommon | 春夏 | 夜晚 | 小雨 | 4 | 62 | 16 |
| mandarin_fish | 鳜鱼 | Rare | 春夏 | 清晨 | 晴 | 5 | 100 | 20 |
| snakehead | 黑鱼 | Uncommon | 夏 | 中午 | 晴 | 3 | 55 | 15 |
| ricefish | 稻田鱼 | Common | 春夏 | 全天 | 晴 | 1 | 18 | 7 |
| shrimp | 河虾 | Common | 全年 | 清晨 | 晴 | 1 | 16 | 5 |
| legendary_dragon_carp | 龙鳞鲷 | Legendary | 春 | 傍晚 | 大风 | 9 | 2100 | 50 |

---

## 四、雪山（SnowMountain · 12 种）

| fishId | fishName | rarity | 季节 | 时段 | 天气 | 难度 | basePrice | 体力 |
|---|---|---|---|---|---|---|---|---|
| ice_fish | 冰鱼 | Common | 冬 | 全天 | 小雪 | 2 | 35 | 12 |
| snow_trout | 雪鳟 | Common | 冬 | 上午 | 晴 | 2 | 40 | 13 |
| frost_carp | 霜鲤 | Common | 冬 | 中午 | 晴 | 2 | 38 | 12 |
| glacier_salmon | 冰川鲑 | Uncommon | 冬 | 清晨 | 大雪 | 4 | 70 | 20 |
| ice_cod | 冰鳕 | Uncommon | 冬 | 下午 | 小雪 | 3 | 60 | 18 |
| snow_crab | 雪蟹 | Uncommon | 冬 | 上午 | 晴 | 3 | 65 | 17 |
| crystal_fish | 冰晶鱼 | Rare | 冬 | 深夜 | 大雪 | 6 | 140 | 25 |
| aurora_fish | 极光鱼 | Rare | 冬 | 夜晚 | 晴 | 6 | 150 | 26 |
| frost_clam | 霜蚌 | Common | 冬 | 上午 | 晴 | 1 | 22 | 7 |
| glacier_eel | 冰鳗 | Uncommon | 冬 | 夜晚 | 大雪 | 4 | 68 | 19 |
| snow_loach | 雪泥鳅 | Common | 冬 | 清晨 | 小雪 | 1 | 20 | 7 |
| legendary_frost_dragon | 霜翼龙鱼 | Legendary | 冬 | 深夜 | 大雪 | 10 | 2300 | 55 |

---

## 五、火山（Volcano · 9 种）

| fishId | fishName | rarity | 季节 | 时段 | 天气 | 难度 | basePrice | 体力 |
|---|---|---|---|---|---|---|---|---|
| lava_carp | 熔岩鲤 | Common | 夏 | 全天 | 晴 | 3 | 45 | 15 |
| ember_fish | 火苗鱼 | Common | 夏 | 上午 | 晴 | 2 | 42 | 14 |
| magma_eel | 岩浆鳗 | Uncommon | 夏 | 夜晚 | 晴 | 4 | 70 | 20 |
| cinder_bass | 灰烬鲈 | Common | 夏 | 下午 | 晴 | 2 | 40 | 13 |
| flame_clam | 焰贝 | Uncommon | 夏 | 清晨 | 晴 | 3 | 65 | 18 |
| sulfur_fish | 硫磺鱼 | Common | 夏 | 中午 | 晴 | 3 | 48 | 16 |
| obsidian_fish | 黑曜鱼 | Rare | 夏 | 夜晚 | 晴 | 6 | 130 | 24 |
| phoenix_fish | 凤凰鱼 | Rare | 夏 | 清晨 | 晴 | 6 | 145 | 26 |
| legendary_flame_heart | 焰心龙鱼 | Legendary | 夏 | 深夜 | 雷暴 | 10 | 2400 | 60 |

---

## 汇总统计

| 地点 | Common | Uncommon | Rare | Legendary | 小计 |
|---|---|---|---|---|---|
| 海边 Sea | 6 | 5 | 3 | 1 | 15 |
| 湖泊 Lake | 7 | 3 | 1 | 1 | 12 |
| 小河 River | 7 | 3 | 1 | 1 | 12 |
| 雪山 SnowMountain | 6 | 4 | 1 | 1 | 12 |
| 火山 Volcano | 5 | 2 | 1 | 1 | 9 |
| **合计** | **31** | **17** | **7** | **5** | **60** |

> 传说鱼（5 条）：深渊巨鱿（海·全年·雷暴）、湖怪龙鱼（湖·秋·雾）、龙鳞鲷（河·春·大风）、霜翼龙鱼（雪山·冬·大雪）、焰心龙鱼（火山·夏·雷暴）。
> 传说鱼 `isLegendFish=true`，可设置 `questHint` 指向对应龙种线索。
