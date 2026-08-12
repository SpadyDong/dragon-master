

## Codely Structured Memories

### User

### Feedback
- [2026-08-12 15:17:00] 代码风格规范：PascalCase 属性/方法，_camelCase 私有字段，[Header]+[SerializeField] 模式，单例用 Instance 属性 + DontDestroyOnLoad，跨系统通信用 EventBus.Subscribe/Publish，ScriptableObject 用 [CreateAssetMenu]，存档用 JsonUtility 兼容的 [Serializable] 结构。

### Project
- [2026-08-12 15:16:51] 项目：驯龙高手（Dragon Tamer Chronicles）— Unity + C# 2.5D 模拟经营 RPG，GDD 约1000行。代码在 game/src/ 下，按模块分目录（Core/Player/NPC/Farming/Livestock/Building/UI/Inventory/Dialogue/Interaction/SaveLoad/Audio/World）。无 Unity 工程文件，仅纯 .cs 源码 + 两张地图原画。开发按 GDD 里程碑推进：M1(原型/移动/Tilemap/UI/NPC/对话/任务/存档)已完成，M2(种植/畜牧/洒水器/果树/蜂箱/储物箱)已完成，下一步 M5(驯龙系统)。
### Reference

