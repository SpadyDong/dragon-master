

## Codely Structured Memories

### User

### Feedback
- [2026-08-12 15:17:00] 代码风格规范：PascalCase 属性/方法，_camelCase 私有字段，[Header]+[SerializeField] 模式，单例用 Instance 属性 + DontDestroyOnLoad，跨系统通信用 EventBus.Subscribe/Publish，ScriptableObject 用 [CreateAssetMenu]，存档用 JsonUtility 兼容的 [Serializable] 结构。
- [2026-08-13 11:03:52] 龙系统简化规则：龙与普通动物仅两点不同——有元素属性 + 可参加战斗。去掉骑乘系统（DragonRiding.cs 已删除）、心情系统、IV/EV 随机属性、繁育 IV 遗传。同种龙属性完全固定（全部来自 DragonData）。每种元素 3 种龙（2 普通 + 1 稀有），龙种不宜过多，大部分生物还是鸡鸭猫狗等普通动物。天气仅保留雷暴受惊/逃出龙舍。自动喂食不影响心情。TerrainType 枚举已从 DragonRiding.cs 迁移到 TerrainTile.cs。

### Project
- [2026-08-13 16:45:11] 项目：驯龙高手（Dragon Tamer Chronicles）— Unity + C# 2.5D 模拟经营 RPG，GDD 约1900行。代码在 game/src/ 下，按模块分目录。无 Unity 工程文件，仅纯 .cs 源码。开发按 GDD 里程碑推进：M1-M12 全部完成（代码层），存档0.12.0。UI面板接线完成（UIManager扩展至8标签页+10独立面板）。已生成docs/全套文档：Architecture.md、DataAssets.md、DataInstances-*.md(龙/鱼/食谱/武器/怪物/作物/物品/矿石/杂项)。代码审查已修复：①SaveManager补DontDestroyOnLoad(防场景切换丢失) ②FoodBuff语义统一(attackBonus/defenseBonus为百分比整数5=+5%，CookingManager加GetAttackBuffPercent等消费方法) ③ArtisanPanel接线缺口(UIManager加OpenArtisan(ArtisanDeviceData)重载传入设备数据，ArtisanStation调用带参版本，面板补全配方列表+开始加工按钮) ④CommunityPanel从Resources.LoadAll改为Inspector引用bundles[]字段(与文档约定一致)。














### Reference

