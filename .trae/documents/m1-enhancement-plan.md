# M1 增强计划：交互 / NPC / 对话 / 基础系统补全

## 一、当前代码与 GDD 差距

| 状态 | 内容 |
|---|---|
| ✅ 已有 | 8方向移动+奔跑、相机跟随、Y-sort、HUD、M键地图、Input System 6个绑定 |
| ❌ 缺失 | 交互系统、NPC系统、对话系统、事件总线、时间推进、天气变化、玩家属性(HP/体力)、背包物品、暂停菜单、存档/读档、音频管理、GameManager状态机 |

---

## 二、新增文件清单（21 个 C# + 5 个精灵）

```
game/src/
├── Core/
│   ├── EventBus.cs              ← 新增：事件总线（跨系统解耦通信）
│   ├── GameState.cs             ← 新增：Playing/Paused/Dialogue/Menu 状态枚举
│   ├── GameManager.cs           ← 修改：接入 GameState + 事件发布
│   ├── InputManager.cs          ← 修改：增加 IsInputLocked + Q/R/Space/C 绑定
│   ├── PlayerInputActions.cs    ← 修改：补充 Q/R/Space/1-5/B/C 输入绑定
│   ├── CameraFollow.cs
│   └── TimeManager.cs           ← 新增：时间推进引擎（分钟→天→季节→年）
├── Player/
│   ├── PlayerController.cs      ← 修改：方向跟踪 + LockMovement + 体力消耗
│   ├── PlayerAnimation.cs       ← 新增：4方向动画状态机
│   └── PlayerStats.cs           ← 新增：HP/体力/饥饿/灵力 + 四维属性
├── Interaction/
│   ├── Interactable.cs          ← 新增：交互组件基类
│   ├── InteractionController.cs ← 新增：F键检测+最近目标+分发
│   └── InteractionPrompt.cs     ← 新增：浮动 "[F] 交谈" 提示UI
├── NPC/
│   ├── NPCData.cs               ← 新增：NPC ScriptableObject（姓名/头像/生日/礼好/作息）
│   ├── NPCController.cs         ← 新增：NPC运行时（巡逻/站岗/对话触发）
│   ├── NPCSchedule.cs           ← 新增：每日作息数据 + 时段查询
│   └── SimplePathfinding.cs     ← 新增：Tilemap网格寻路
├── Dialogue/
│   ├── DialogueData.cs          ← 新增：对话树 ScriptableObject
│   ├── DialogueManager.cs       ← 新增：对话状态机 + 分支 + 选项
│   └── DialogueUI.cs            ← 新增：底部对话框UI（头像+打字机+选项）
├── Inventory/
│   ├── ItemData.cs              ← 新增：物品 ScriptableObject
│   ├── InventoryManager.cs      ← 新增：背包逻辑（添加/移除/堆叠）
│   └── InventoryUI.cs           ← 新增：E键背包面板
├── UI/
│   ├── HUD.cs
│   ├── MapPanel.cs
│   ├── PauseMenu.cs             ← 新增：Esc暂停菜单（继续/存档/设置/退出）
│   └── DayTransitionUI.cs       ← 新增：过天结算画面
├── Audio/
│   └── AudioManager.cs          ← 新增：BGM+SFX管理（先骨架，音效后补）
├── SaveLoad/
│   └── SaveManager.cs           ← 新增：JSON存档框架（5+1槽位）
└── World/
    └── YSortOrder.cs
```

---

## 三、详细设计

### 3.1 事件总线（EventBus.cs）

**为什么第一个做**：所有新系统都通过事件通信，避免互相引用。

```csharp
// 全局事件类型（枚举 + 带参数事件）
public enum GameEvent
{
    // 时间
    MinuteChanged, HourChanged, DayChanged, SeasonChanged, YearChanged,
    // 天气
    WeatherChanged,
    // 玩家
    PlayerMoved, PlayerInteracted, PlayerStatsChanged, PlayerGoldChanged,
    // NPC
    NPCDialogueStarted, NPCDialogueEnded, NPCAffectionChanged,
    // 物品
    ItemPickedUp, ItemUsed, InventoryChanged,
    // 游戏状态
    GamePaused, GameResumed, GameStateChanged
}

public static class EventBus
{
    static Dictionary<GameEvent, Delegate> _events = new();

    public static void Subscribe<T>(GameEvent e, Action<T> handler);  // 有参数
    public static void Subscribe(GameEvent e, Action handler);         // 无参数
    public static void Publish<T>(GameEvent e, T arg);
    public static void Publish(GameEvent e);
    public static void Unsubscribe<T>(GameEvent e, Action<T> handler);
}
```

**解耦效果示例**：
- TimeManager 发 `DayChanged` → HUD 自动刷新日期 → NPC 查询新作息 → 农场检查作物生长
- 各系统只订阅自己关心的事件，互不知晓对方存在

---

### 3.2 时间推进引擎（TimeManager.cs）

```csharp
public class TimeManager : MonoBehaviour
{
    [SerializeField] float realSecondsPerGameMinute = 0.7f; // 1游戏分钟=0.7现实秒 → 1天≈16.8分钟
    [SerializeField] int minutesPerDay = 1440;               // 24h×60min

    // 实时推进
    void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        
        _accumulator += Time.deltaTime;
        while (_accumulator >= realSecondsPerGameMinute)
        {
            _accumulator -= realSecondsPerGameMinute;
            AdvanceOneMinute();
        }
    }

    void AdvanceOneMinute()
    {
        GameManager.Instance.minute += 10;  // 每10分钟一跳
        EventBus.Publish(GameEvent.MinuteChanged, GameManager.Instance.minute);

        if (minute >= 60) { AdvanceHour(); }
    }

    void AdvanceHour()   → 触发 HourChanged
    void AdvanceDay()    → 触发 DayChanged（28天/季 → 切换季节 → SeasonChanged）
    void AdvanceSeason() → 触发 SeasonChanged
    void AdvanceYear()   → 触发 YearChanged

    // 时间跳转（睡觉用）
    public void SkipToTime(int targetHour, int targetMinute);
    public void SkipToNextDay(int wakeHour = 6);
}
```

**昼夜状态**：
| 时段 | 时间范围 | 特效 |
|---|---|---|
| 清晨 | 06:00-08:00 | 暖黄滤镜 |
| 上午 | 08:00-12:00 | 正常 |
| 中午 | 12:00-14:00 | 高亮 |
| 下午 | 14:00-18:00 | 正常 |
| 傍晚 | 18:00-20:00 | 橙黄滤镜 |
| 夜晚 | 20:00-24:00 | 暗蓝滤镜 |
| 深夜 | 00:00-06:00 | 极暗 + 体力惩罚 |

---

### 3.3 玩家属性系统（PlayerStats.cs）

```csharp
public class PlayerStats : MonoBehaviour
{
    // 四维属性（开局加点，影响衍生值）
    [SerializeField] int strength = 5;    // STR
    [SerializeField] int agility = 5;     // AGI
    [SerializeField] int vitality = 5;    // VIT
    [SerializeField] int intelligence = 5; // INT

    // 生存四要素（实时变化）
    public int CurrentHP { get; private set; }
    public int MaxHP => 100 + vitality * 10;
    public int CurrentStamina { get; private set; }
    public int MaxStamina => 100 + vitality * 5;
    public int CurrentHunger { get; private set; }
    public int MaxHunger => 100;
    public int CurrentMP { get; private set; }
    public int MaxMP => 50 + intelligence * 10;

    // 消耗
    public bool ConsumeStamina(int amount);   // 返回是否足够
    public void TakeDamage(int amount);
    public void Heal(int amount);
    public void Eat(int hungerRestore, int staminaRestore);

    // 奔跑消耗（在 PlayerController.Update 中调用）
    public void OnRunningTick() => ConsumeStamina(1); // 每秒扣1体力

    // 深夜惩罚（TimeManager 触发）
    void OnDeepNight() => ConsumeStamina(2); // 每10分钟扣2体力
}
```

---

### 3.4 交互系统（3 个文件）

**Interactable.cs**：
```csharp
public enum InteractType { Talk, Pickup, Door, Craft, Shop, Sign, Chest }

public class Interactable : MonoBehaviour
{
    public InteractType type;
    public string promptText = "按 F 交谈";
    public float interactRange = 1.5f;
    public bool requireFacing = true;  // 是否需要面向NPC

    public virtual void OnInteract(PlayerController player) { }
    public virtual bool CanInteract(PlayerController player) => true;
}
```

**InteractionController.cs** — 挂在 Player 上：
```
Update():
  1. 用 Physics2D.OverlapCircle 检测范围内的 Interactable
  2. 过滤：距离最近 + (requireFacing → 在玩家面前) + CanInteract
  3. 有目标 → 显示 InteractionPrompt
  4. 无目标 → 隐藏 InteractionPrompt
  5. F键 + 有目标 → OnInteract() → 发 EventBus.Publish(Interacted)
  6. 对话中/暂停中 → 不检测交互
```

**InteractionPrompt.cs** — Canvas 浮动文字：
```
半透明背景 + "[F] 交谈" 文字
挂在目标头顶或固定在屏幕下方
有淡入淡出动画（CanvasGroup alpha）
```

---

### 3.5 NPC 系统（4 个文件）

**NPCData.cs** — ScriptableObject：
```
字段：npcId, npcName, portraitSprite, worldSprite, isMarriageable
      birthday(月/日), giftPreferences(Dictionary)
      defaultSchedule(NPCScheduleEntry[])
```

**NPCSchedule.cs** — 作息数据：
```csharp
[System.Serializable]
public struct NPCScheduleEntry
{
    public int startHour, startMinute;
    public int endHour, endMinute;
    public string locationName;       // "饭馆" / "广场" / "家"
    public Vector2 targetPosition;    // 世界坐标
    public string activity;           // "工作" / "闲逛" / "回家" / "就餐" / "睡觉"
    public bool isMoving;             // 该时段是否在移动巡逻
    public Vector2[] patrolPoints;    // 巡逻路径点
}

// NPCSchedule 提供：
// GetCurrentActivity(GameTime time) → NPCScheduleEntry
```

**NPCController.cs**：
```csharp
[RequireComponent(typeof(Rigidbody2D))]
public class NPCController : MonoBehaviour
{
    public NPCData data;
    NPCScheduleEntry _currentActivity;
    Vector2 _targetPos;
    int _patrolIndex;

    void Start()
    {
        // 订阅 HourChanged 事件 → 重新查询当前时段作息
        EventBus.Subscribe(GameEvent.HourChanged, OnHourChanged);
    }

    void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        UpdateMovement();
        UpdateAnimation();
    }

    void UpdateMovement()
    {
        // 站岗：不动
        // 巡逻：沿 patrolPoints 移动（SimplePathfinding）
        // 在家/睡觉：隐藏或移动到屋内
    }

    // Interactable 接口（在 NPCController 所在的 GameObject 上挂 Interactable 组件）
    public void OnInteract()
    {
        DialogueManager.Instance.StartDialogue(data.npcId);
    }
}
```

**SimplePathfinding.cs**：
```csharp
// M1 版本：向目标直线移动 + TilemapCollider2D 碰撞回避
// 后续 M4 升级为 A*
public class SimplePathfinding : MonoBehaviour
{
    TilemapCollider2D _collider;
    float _avoidAngle = 45f;  // 碰到障碍时的回避角度

    public Vector2 GetMovementDirection(Vector2 currentPos, Vector2 targetPos);
}
```

**M1 先做 3 个测试 NPC**：

| NPC | 作息 | 位置 |
|---|---|---|
| 卯师傅 | 6-8点广场买菜 → 8-18点饭馆站岗 → 18-20点酒馆 → 20点回家 | 饭馆/广场/酒馆 |
| 阿岚 | 8-12点铁匠铺 → 12-14点饭馆就餐 → 14-18点铁匠铺 → 18点后广场闲逛 | 铁匠铺/广场 |
| 磐石 | 全天铁匠铺站岗（只在12点去饭馆） | 铁匠铺 |

---

### 3.6 对话系统（3 个文件）

**DialogueData.cs**：
```csharp
[CreateAssetMenu(menuName = "Dialogue/Dialogue Tree")]
public class DialogueTree : ScriptableObject
{
    public string dialogueId;
    public List<DialogueNode> nodes;
}

[System.Serializable]
public class DialogueNode
{
    public string nodeId;
    public string speakerName;
    public Sprite speakerPortrait;
    public List<string> lines;                    // 此节点对话行
    public List<DialogueChoice> choices;           // 选项
    public string nextNodeId;                      // 无选项时的下一节点
    public DialogueCondition[] conditions;         // 触发条件
    public DialogueEffect[] effects;               // 效果
}

[System.Serializable]
public class DialogueChoice
{
    public string text;
    public string nextNodeId;
    public int affectionChange;
}

// 条件类型：好感度 >=、季节 ==、天气 ==、时间范围、持有物品、任务状态
// 效果类型：好感度变化、给予物品、移除物品、标记任务进度、解锁对话
```

**DialogueManager.cs**：
```csharp
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public bool IsActive { get; private set; }

    DialogueTree _currentTree;
    DialogueNode _currentNode;
    int _currentLineIndex;
    string _currentNpcId;

    // 开始对话
    public void StartDialogue(string npcId)
    {
        IsActive = true;
        _currentNpcId = npcId;
        // 根据 npcId + 条件选择最匹配的 DialogueTree
        _currentTree = SelectDialogueTree(npcId);
        _currentNode = _currentTree.nodes[0];
        _currentLineIndex = 0;

        GameManager.Instance.SetState(GameState.Dialogue);
        EventBus.Publish(GameEvent.NPCDialogueStarted, npcId);
        DialogueUI.Instance.ShowLine(_currentNode.speakerName,
                                     _currentNode.speakerPortrait,
                                     _currentNode.lines[0]);
    }

    // 推进对话（空格/点击）
    public void Advance()
    {
        // 打字机未播完 → 立即完成当前行
        // 当前行完成 + 还有更多行 → 下一行
        // 当前行完成 + 节点结束 + 有选项 → 显示选项
        // 当前行完成 + 节点结束 + 无选项 → 跳转 nextNodeId 或结束
    }

    // 选择选项
    public void SelectChoice(int index)
    {
        DialogueChoice choice = _currentNode.choices[index];
        // 应用好感变化
        // 跳转下一节点
        // 触发展开效果
    }

    public void EndDialogue()
    {
        IsActive = false;
        GameManager.Instance.SetState(GameState.Playing);
        EventBus.Publish(GameEvent.NPCDialogueEnded, _currentNpcId);
    }
}
```

**DialogueUI.cs**：
```
底部对话框布局：
┌───────────────────────────────────────────┐
│ ┌────┐                                    │
│ │头像│ 卯师傅：今天有新鲜的烤鱼～          │
│ │    │ 要不要来一份？          ▼          │
│ └────┘                                    │
├───────────────────────────────────────────┤
│  > 来一份烤鱼                              │
│    今天不想吃                              │
│    有什么推荐的？                          │
└───────────────────────────────────────────┘

特性：
- 打字机效果（逐字 + 音效触发点）
- 空格/点击：打字中→跳过/打字完成→下一行
- 选项 ↑↓ 选择 + 空格确认
- 名字颜色区分 NPC/玩家
- 条件过滤选项（不满足条件的不显示）
```

**M1 内置 3 段测试对话**：
1. 卯师傅（好感0，春季早上）：介绍饭馆 → 选项"看看菜单"/"下次再来"
2. 阿岚（好感0）：简单自我介绍 "我是铁匠铺的阿岚"
3. 磐石（好感0）：单行 "武器找我，龙装备去隔壁"

---

### 3.7 背包系统（3 个文件）

**ItemData.cs** — ScriptableObject：
```csharp
[CreateAssetMenu(menuName = "Item/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public string description;
    public Sprite icon;
    public ItemType type;  // Consumable/Weapon/Armor/Seed/Tool/Material/Currency
    public int maxStack = 99;
    public int basePrice = 10;  // 基准售价（菜市场波动前）
    public bool isSellable = true;
}
```

**InventoryManager.cs**：
```csharp
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public int maxSlots = 36;
    List<InventorySlot> _slots;

    public bool AddItem(ItemData item, int count = 1);
    public bool RemoveItem(string itemId, int count = 1);
    public int GetItemCount(string itemId);
    public bool HasItem(string itemId, int count = 1);

    [System.Serializable]
    public struct InventorySlot
    {
        public ItemData item;
        public int count;
    }
}
```

**InventoryUI.cs** — E 键面板：
```
36 格网格 + 物品图标 + 数量
选中物品 → 显示名称/描述/操作按钮（使用/丢弃）
拖拽交换
```

---

### 3.8 暂停菜单（PauseMenu.cs）

```
Esc 键打开：
┌──────────────┐
│  继续游戏     │
│  存档        │
│  读档        │
│  ──────────  │
│  设置        │  → 音量 / 分辨率 / 全屏
│  ──────────  │
│  返回主菜单   │
│  退出游戏     │
└──────────────┘

打开时 Time.timeScale = 0
关闭时 Time.timeScale = 1
```

---

### 3.9 存档系统骨架（SaveManager.cs）

```csharp
public class SaveManager : MonoBehaviour
{
    const int MANUAL_SLOTS = 5;
    const float AUTO_SAVE_INTERVAL = 600f; // 10分钟

    [System.Serializable]
    public class SaveData
    {
        public int year, season, day, hour, minute;
        public string weather;
        public int gold;
        public PlayerSaveData player;
        public InventorySaveData inventory;
        public Dictionary<string, NPCSaveData> npcs;
        // ... 后续扩展
    }

    public void SaveToSlot(int slot);
    public void LoadFromSlot(int slot);
    public void AutoSave();
    public SaveData[] ListSaves();  // 返回所有槽位信息
}
```

---

### 3.10 音频骨架（AudioManager.cs）

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;

    public void PlayBGM(AudioClip clip, float fadeIn = 1f);
    public void StopBGM(float fadeOut = 1f);
    public void PlaySFX(AudioClip clip, float volume = 1f);
    public void SetBGMVolume(float volume);
    public void SetSFXVolume(float volume);
}
```

---

### 3.11 GameState 状态机（GameState.cs）

```csharp
public enum GameState { Playing, Paused, Dialogue, Menu, Transition, Combat }

// GameManager 中：
public GameState CurrentState { get; private set; } = GameState.Playing;

public void SetState(GameState newState)
{
    if (CurrentState == newState) return;
    CurrentState = newState;
    EventBus.Publish(GameEvent.GameStateChanged, newState);
    // Playing → 恢复时间
    // Paused/Dialogue/Menu → 暂停时间
}
```

---

### 3.12 精灵资源新增

| 精灵 | 用途 |
|---|---|
| npc_chef.png | 卯师傅（白围兜 + 头巾，女性） |
| npc_male_a.png | 阿岚（蓝色衣服，青年男性） |
| npc_male_smith.png | 磐石（棕色皮围裙，中年男性铁匠） |
| player_idle_down/up/left/right.png | 玩家4方向站姿 |

---

## 四、修改的文件

| 文件 | 改动 |
|---|---|
| `Core/GameManager.cs` | +GameState枚举 + SetState() + 事件发布 |
| `Core/InputManager.cs` | +IsInputLocked + Q/R/Space/C输入绑定（占位） |
| `Core/PlayerInputActions.cs` | +Q/R/Space/1-5/B/C 输入绑定 JSON |
| `Player/PlayerController.cs` | +facingDirection + LockMovement() + 奔跑扣体力 |

---

## 五、实现顺序

| 轮次 | 文件 | 依赖 |
|---|---|---|
| 1 | EventBus.cs + GameState.cs | 无 |
| 2 | GameManager.cs（改） | EventBus + GameState |
| 3 | TimeManager.cs | GameManager + EventBus |
| 4 | PlayerStats.cs | EventBus |
| 5 | PlayerInputActions.cs（改）+ InputManager.cs（改） | 无 |
| 6 | PlayerController.cs（改）+ PlayerAnimation.cs | InputManager + PlayerStats |
| 7 | Interactable.cs + InteractionController.cs + InteractionPrompt.cs | InputManager + PlayerController |
| 8 | NPCData.cs + NPCSchedule.cs | 无 |
| 9 | SimplePathfinding.cs | 无 |
| 10 | NPCController.cs | NPCData + NPCSchedule + SimplePathfinding + EventBus |
| 11 | DialogueData.cs | 无 |
| 12 | DialogueManager.cs + DialogueUI.cs | DialogueData + EventBus + GameState |
| 13 | ItemData.cs + InventoryManager.cs + InventoryUI.cs | EventBus |
| 14 | PauseMenu.cs | GameState + EventBus |
| 15 | SaveManager.cs | GameManager + PlayerStats + InventoryManager |
| 16 | AudioManager.cs | 无 |
| 17 | NPC精灵生成 + 玩家4方向精灵 |

---

## 六、交付后效果

进入 Unity 运行后：
- **时间自动推进**：白天→傍晚夜→深夜（自然过渡）
- **玩家**：8方向移动+方向动画，Shift奔跑扣体力，深夜扣体力
- **靠近卯师傅** → 头顶浮现 "[F] 交谈"
- **按 F** → 底部弹对话：头像+打字机文字 "今天有新鲜的烤鱼～"
- **空格推进** → 出选项 "来一份"/"下次再来" → 选择后对话结束
- **阿岚** 在铁匠铺和广场间按作息自动巡逻
- **按 E** → 打开 36 格背包面板
- **按 Esc** → 暂停菜单（继续/存档/设定/退出）
- **按 M** → 打开大地图（已有）
- **HUD** 实时更新：时间流逝 + 季节切换 + 金币 + 天气变化
