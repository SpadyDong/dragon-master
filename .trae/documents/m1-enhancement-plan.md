# M1 增强计划：交互、移动、NPC、对话系统

## 当前状态

| 已有 | 缺失 |
|---|---|
| 8方向移动 + 奔跑 | 方向跟踪（不知道朝哪）、空闲动画 |
| 相机跟随 | 无 |
| Y-sort 深度排序 | 无 |
| HUD 时间/金币/天气 | 无 |
| M 键地图面板 | 无 |
| - | **按 F 无任何反应** |
| - | **没有任何 NPC** |
| - | **没有对话系统** |
| - | **没有交互检测** |

---

## 新增文件清单（10 个 C# + 3 个精灵）

```
game/src/
├── Core/
│   ├── InteractionController.cs   ← 新增：F键交互检测 + 交互类型分发
│   └── Interactable.cs            ← 新增：交互组件基类（挂到可交互物体上）
├── Player/
│   ├── PlayerController.cs        ← 修改：增加方向跟踪 (facingDirection)
│   └── PlayerAnimation.cs         ← 新增：4/8方向动画状态机
├── NPC/
│   ├── NPCController.cs           ← 新增：NPC基类（巡逻/站岗/寻路）
│   ├── NPCSchedule.cs             ← 新增：NPC每日作息数据
│   ├── NPCData.cs                 ← 新增：NPC配置 ScriptableObject
│   └── SimplePathfinding.cs       ← 新增：简易A*寻路（Tilemap网格）
├── Dialogue/
│   ├── DialogueData.cs            ← 新增：对话数据结构
│   ├── DialogueManager.cs         ← 新增：对话状态机 + 分支 + 事件
│   └── DialogueUI.cs              ← 新增：底部对话框UI + 头像 + 打字机
├── UI/
│   └── InteractionPrompt.cs       ← 新增："按F交谈"提示文字
```

---

## 详细设计

### 1. 交互系统

**Interactable.cs** — 可交互组件基类

```csharp
public enum InteractType { Talk, Pickup, Door, Craft, Shop, Sign }

public class Interactable : MonoBehaviour
{
    public InteractType type = InteractType.Talk;
    public string promptText = "按 F 交谈";      // 提示文字
    public float interactRange = 1.5f;            // 交互距离
    public bool requireFacing = true;             // 是否需要面向目标

    // 子类重写
    public virtual void OnInteract(PlayerController player) { }
    public virtual bool CanInteract(PlayerController player) => true;
}
```

**InteractionController.cs** — 挂到 Player 上

```csharp
// Update() 中:
// 1. 检测范围内的所有 Interactable
// 2. 取最近的、在玩家面前的
// 3. 显示 InteractionPrompt
// 4. F键按下 → 调用 nearestInteractable.OnInteract()
// 5. 冲突检测：地图打开时不触发交互、对话中不触发(由DialogueManager控制)
```

**InteractionPrompt.cs** — 浮动提示 UI

```
Player 头顶或屏幕底部显示半透明提示："[F] 交谈" / "[F] 拾取" / "[F] 进入"
有淡入淡出动画
距离过远或转向后自动消失
```

---

### 2. 移动完善

**PlayerController.cs 修改**：

```
新增:
  - Direction facingDirection (枚举: Up/Down/Left/Right/UpLeft/UpRight/DownLeft/DownRight)
  - Idle 状态时保持最后朝向
  - 被对话/菜单锁定输入时调用 LockMovement() / UnlockMovement()
```

**PlayerAnimation.cs** — 独立动画控制器

```csharp
// 根据 PlayerController.facingDirection 设置 Animator 参数
// 支持 4 方向（上下左右）或 8 方向动画
// 参数: AnimX, AnimY, IsMoving, IsRunning
// 先用 4 方向色块精灵占位，后续替换美术资源
```

**移动锁定机制**：
- 对话打开 → `LockMovement()`（输入归零，停止动画）
- 地图打开 → 不锁移动（关闭地图恢复）
- 菜单打开 → `LockMovement()`

---

### 3. NPC 系统

**NPCData.cs** — ScriptableObject 配置

```csharp
[CreateAssetMenu(menuName = "NPC/NPC Data")]
public class NPCData : ScriptableObject
{
    public string npcName;           // "卯师傅"
    public string npcId;             // "mao_shifu"
    public Sprite portrait;          // 对话头像
    public Sprite sprite;            // 世界精灵
    public bool isMarriageable;      // 可结婚
    public string birthday;          // "冬21"
    public GiftPref[] giftPrefs;     // 礼物偏好
    public NPCScheduleEntry[] dailySchedule;  // 每日作息
}

[System.Serializable]
public struct NPCScheduleEntry
{
    public int startHour, startMinute;
    public int endHour, endMinute;
    public string locationScene;      // 场景名
    public Vector2 targetPosition;    // 目标位置（世界坐标）
    public string activity;           // "工作" / "闲逛" / "回家" / "就餐"
}

[System.Serializable]
public struct GiftPref
{
    public string itemId;
    public int affectionChange;  // +80超爱 / +40喜欢 / +15普通 / -30讨厌
}
```

**NPCController.cs** — NPC 运行时

```csharp
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class NPCController : MonoBehaviour, Interactable  // 实现交互
{
    public NPCData data;
    
    // 当前状态
    Vector2 targetPosition;
    bool isMoving;
    float moveSpeed = 2f;
    
    // 巡逻模式（M1 简易版：站岗 + 简单巡逻）
    // 后续 M4 加入完整作息系统
    
    void Update()
    {
        // 检查是否到达当前作息目标位置
        // 若未到达 → 寻路移动
        // 若到达 → 等待/闲逛动画
    }
    
    // Interactable 实现
    public override void OnInteract(PlayerController player)
    {
        DialogueManager.Instance.StartDialogue(data.npcId, player);
    }
}
```

**SimplePathfinding.cs** — 简易寻路

```csharp
// M1 版本：直线移动 + 简单障碍检测
// 不实现完整A*（等 M6 再上Grid+Heap A*）
// 当前实现：
//   1. 向目标方向移动
//   2. 碰到 Tilemap Collider → 尝试左右闪避
//   3. 到达目标 → 停止

// 后续升级为 A* 时替换此类即可，接口保持不变
```

**M1 先做 3 个 NPC 测试**：
| NPC | 行为 | 位置 |
|---|---|---|
| 卯师傅 | 饭馆门口站岗，8-18点 | 村落饭馆前 |
| 阿岚 | 广场闲逛巡逻 | 广场区域 |
| 磐石 | 铁匠铺门口站岗 | 铁匠铺前 |

---

### 4. 对话系统

**DialogueData.cs** — 数据结构

```csharp
[System.Serializable]
public class DialogueLine
{
    public string speakerName;        // "卯师傅"
    public Sprite speakerPortrait;    // 头像
    public string text;               // 对话内容
    public float typingSpeed = 0.05f; // 打字机速度
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;         // "来一份烤鱼"
    public string nextDialogueId;     // 跳转对话ID
    public int affectionChange;       // 好感变化
    public UnityEvent onChosen;       // 选择后事件（给任务/给物品等）
}

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
    public List<DialogueLine> lines;         // 此节点的对话行
    public List<DialogueChoice> choices;     // 选项（空=自动下一节点）
    public string nextNodeId;                // 无选项时的下一节点
    public List<DialogueCondition> conditions;  // 触发条件
    public List<DialogueEffect> effects;        // 对话效果
}

// 条件：好感度>=X、季节、天气、时间、已有物品、已完成任务...
// 效果：好感+X、给物品、标记任务、解锁对话...
```

**DialogueManager.cs** — 状态机

```csharp
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    
    // 状态
    public bool IsDialogueActive { get; private set; }
    DialogueTree currentTree;
    DialogueNode currentNode;
    int currentLineIndex;
    
    // 事件
    public event System.Action OnDialogueStart;
    public event System.Action OnDialogueEnd;
    public event System.Action<string> OnNodeComplete;
    
    public void StartDialogue(string npcId, PlayerController player)
    {
        // 1. 根据 npcId + 条件选择 DialogueTree
        // 2. 锁定玩家移动
        // 3. 显示 DialogueUI
        // 4. 开始第一行
    }
    
    public void AdvanceDialogue()
    {
        // 空格/点击 → 下一行
        // 当前行打字机未播完 → 立即完成
        // 当前行已完成 → 下一行
        // 节点结束有选项 → 显示选项
    }
    
    public void SelectChoice(int index)
    {
        // 应用 affectionChange
        // 触发 onChosen
        // 跳转下一节点
    }
    
    public void EndDialogue()
    {
        // 隐藏 UI
        // 解锁移动
        // 触发 OnDialogueEnd
    }
}
```

**DialogueUI.cs** — 底部对话框

```
┌──────────────────────────────────────────┐
│ ┌────┐                                   │
│ │头像│  卯师傅：今天有新鲜的烤鱼哦～       │
│ │    │  要不要来一份？                    │
│ └────┘                          ▼ (继续) │
├──────────────────────────────────────────┤
│  > 来一份烤鱼 (50文)                      │
│    今天不想吃                             │
│    有什么推荐的？                         │
└──────────────────────────────────────────┘

特性：
- 打字机效果（逐字显示 + 可选音效）
- 头像框 + 名字 + 正文
- 空格/点击加速（立即完成当前行）
- 选项显示时高亮当前项
- ↑↓ 选择 + 空格/Enter 确认
- 名字颜色区分 NPC / 玩家
```

---

### 5. 精灵资源新增

用 Python + Pillow 生成占位 NPC 精灵（24×32 色块 + 简单造型）：

| 精灵 | 描述 |
|---|---|
| npc_chef.png | 卯师傅（白色围裙 + 头巾） |
| npc_male_a.png | 阿岚（蓝色衣服男生） |
| npc_male_smith.png | 磐石（棕衣铁匠） |

---

### 6. 修改的文件

| 文件 | 修改内容 |
|---|---|
| PlayerController.cs | 新增 facingDirection、LockMovement/UnlockMovement、InteractionController 引用 |
| InputManager.cs | 新增 IsInputLocked 属性（对话/菜单时屏蔽移动） |

---

## 文件整理后的目录结构

```
game/src/
├── Core/
│   ├── GameManager.cs
│   ├── InputManager.cs           ← 修改
│   ├── CameraFollow.cs
│   ├── PlayerInputActions.cs
│   └── InteractionController.cs  ← 新增
├── Player/
│   ├── PlayerController.cs       ← 修改
│   └── PlayerAnimation.cs        ← 新增
├── NPC/
│   ├── NPCController.cs          ← 新增
│   ├── NPCSchedule.cs            ← 新增
│   ├── NPCData.cs                ← 新增
│   ├── SimplePathfinding.cs      ← 新增
│   └── Interactable.cs           ← 新增
├── Dialogue/
│   ├── DialogueData.cs           ← 新增
│   ├── DialogueManager.cs        ← 新增
│   └── DialogueUI.cs             ← 新增
├── World/
│   └── YSortOrder.cs
├── UI/
│   ├── HUD.cs
│   ├── MapPanel.cs
│   └── InteractionPrompt.cs      ← 新增
└── Settings/
    └── PlayerInputActions.inputactions

game/sprites/
├── NPC/                          ← 新增
│   ├── npc_chef.png
│   ├── npc_male_a.png
│   └── npc_male_smith.png
├── Player/
│   ├── player.png (已有的)
│   ├── player_idle_down.png      ← 新增4方向
│   ├── player_idle_up.png
│   ├── player_idle_left.png
│   └── player_idle_right.png
├── Tiles/ (已有的14个)
└── UI/ (已有的3个)
```

---

## 实现顺序

1. **Interactable.cs** — 交互基类（无依赖）
2. **InteractionController.cs** — 交互检测（依赖 Interactable + InputManager）
3. **InteractionPrompt.cs** — 浮动提示 UI
4. **PlayerController.cs 修改** — 加 facingDirection + LockMovement
5. **PlayerAnimation.cs** — 方向动画
6. **NPCData.cs** — NPC 配置 SO（无依赖）
7. **NPCController.cs** — NPC 运行时（依赖 NPCData + Interactable）
8. **SimplePathfinding.cs** — 简易寻路
9. **DialogueData.cs** — 对话数据结构（无依赖）
10. **DialogueManager.cs** — 对话状态机（依赖 DialogueData）
11. **DialogueUI.cs** — 对话框 UI
12. NPC 精灵生成
13. InputManager 加 IsInputLocked

---

## 预期效果

实现后在 Unity 中：
- 玩家按 WASD 8 方向移动，精灵会根据朝向切换
- 靠近卯师傅时头顶出现 "[F] 交谈" 提示
- 按 F → 底部弹对话框，卯师傅头像 + 打字机文字
- 空格推进对话，出现选项时 ↑↓ 选择
- 对话期间玩家无法移动
- 阿岚在广场巡逻走动，靠近可对话
- 磐石在铁匠铺站岗
