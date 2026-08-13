# Figma2Unity 使用说明

本目录包含两部分，配合使用可实现「用自然语言把 Figma 设计稿导入 Unity / 团结引擎」：

| 组成 | 路径 | 角色 |
|---|---|---|
| **工具（UPM 包）** | `Packages/cn.tuanjie.figma2unity/` | 真正执行导入的 Unity 编辑器插件 |
| **技能（Skill）** | `skills/figma2unity-import/` | 让 AI Agent 自动驱动上面工具的操作手册 |

---

## 零、安装

两部分各自独立安装：**工具（UPM 包）** 装进 Unity 工程，**技能（Skill）** 装进 AI Agent 的技能目录。

### 1. 安装工具（UPM 包）到 Unity / 团结引擎工程

工具是一个 UPM 嵌入包（embedded package），三选一：

**方式 A：嵌入包（推荐）** — 把整个包目录拷进目标工程的 `Packages/` 下：

```bash
cp -R Packages/cn.tuanjie.figma2unity <你的Unity工程>/Packages/
```

打开工程后，Package Manager 会自动识别为本地嵌入包并解析依赖（TextMeshPro、Newtonsoft.Json）。

**方式 B：本地路径引用** — 不移动文件，在目标工程的 `Packages/manifest.json` 的 `dependencies` 中加一行指向本包（路径按实际情况调整）：

```json
{
  "dependencies": {
    "cn.tuanjie.figma2unity": "file:/Users/chuxu/TestCodely/ta_skills/figma2unity/Packages/cn.tuanjie.figma2unity"
  }
}
```

**方式 C：Package Manager UI** — 菜单 `Window/Package Manager` → 左上 `+` → `Add package from disk...` → 选中本包的 `package.json`。

> **验证**：安装成功后菜单栏出现 `Window/Figma2Unity/Open Main Window`。
> **引擎要求**：Unity 2022.3+（兼容团结引擎 Tuanjie 1.5.x）。

### 2. 安装技能（Skill）到 AI Agent

把技能目录拷贝到 Agent 加载技能的目录（以 Codely CLI 为例，用户级技能目录为 `~/.codely-cli/skills/`）：

```bash
cp -R skills/figma2unity-import ~/.codely-cli/skills/
```

- 目录内必须保留 `SKILL.md` 与 `scripts/`（脚本模板为只读，勿改）。
- 安装后，当用户用自然语言表达「把 Figma 稿导入 Unity/团结引擎」时，Agent 会按 `SKILL.md` 的 `description` 自动匹配并加载该技能。

> 技能只负责**驱动**上面的 UPM 包，因此使用技能前请先完成第 1 步（否则 Preflight 会提示先安装包）。

---

## 一、工具简介：Figma2Unity

`cn.tuanjie.figma2unity` 是一个 **Figma → Unity (UGUI) 高保真导入工具**，以 UPM 嵌入包形式提供，导入空工程即可使用。

- **引擎要求**：Unity 2022.3+（兼容团结引擎 Tuanjie 1.5.x）
- **依赖**：TextMeshPro、Newtonsoft.Json（UPM 自动解析）

### 核心能力

- **高保真渲染**：纯色 / 渐变 / 多层 Fill / 描边 / 阴影 / 圆角 / 9-Slice / 旋转（Figmage 烘焙），矢量图走烘焙、失败兜底 Figma `/v1/images` PNG，默认开启 Sprite Atlas
- **布局还原**：25 种 Constraints → Anchor 映射、Auto Layout（H/V + Hug + Fill + Wrap/GRID）、真实 ScrollRect 滚动、`clipsContent` → RectMask2D
- **文本**：TMP + 富文本、LineHeight / LetterSpacing / TextAutoResize
- **交互组件**：Button / Toggle / InputField / ScrollRect / FlowButton
- **原型流**：Fade 过渡协程 + Section 多起点路由
- **字体**：Google Fonts 按需下载 + 中文兜底字体 `NotoSansSC`（预烘焙 SDF 随包自带）
- **增量同步**：节点级 Diff 重绘
- **离线**：一次性 Prefetch 后，`Use cache` 模式零网络重导

### 手动用法（不借助 Skill）

1. 菜单 `Window/Figma2Unity/Open Main Window`
2. 填 **Figma File Key**（或完整 URL）+ **Personal Access Token**（存 EditorPrefs，不入 git）
3. 可选：拖入 **F2UConfig**
4. **Mode** 选 `Full` 或 `Incremental`
5. 在线导入点 `Import`；离线则 `Prefetch` → 勾 `Use cache` → `Import`

> 更详细的功能矩阵与目录结构见 `Packages/cn.tuanjie.figma2unity/README.md`。

---

## 二、技能简介：figma2unity-import

`skills/figma2unity-import/` 是给 **AI Agent** 用的技能包。当用户用自然语言表达「把某个 Figma 设计稿导入 Unity / 团结引擎」这类意图时，Agent 会自动加载并执行该技能，替用户完成上面「手动用法」的全部步骤——**无需打开面板、无需手动点按钮**。

### 触发时机

用户说出类似以下内容时即可触发：

- “把这个 Figma 设计稿导入 Unity / 团结引擎”
- “跑一下 Figma2Unity”
- “用这个 File Key / Personal Access Token 导入设计稿”

### 技能内部流程（Agent 自动执行）

技能通过 `execute_csharp_script` 依序运行 `scripts/` 下的模板脚本：

1. **Preflight（`preflight.cs`）**：检查
   - 包是否安装（`package`）
   - File Key 是否已设置（`fileKey`）
   - Personal Access Token 是否已设置（`pat`）
   - 是否就绪（`ready`）
2. **补齐凭据（`set_credentials.cs`）**：仅当上一步缺值时，向用户索要 File Key / PAT，写入 Unity `EditorPrefs`（`Figma2Unity.FileKey` / `Figma2Unity.{ProductName}.PAT`）
3. **触发导入（`run_import.cs`）**：保存场景后，在隐藏窗口实例上调用 `F2UMainWindow.ImportAsync()` 触发导入（面板不弹出）

### 重要边界（技能设计约定）

- **只负责触发导入**：技能在第 3 步触发后即完成，**不会**等待、验证或报告导入结果。
- **脚本模板只读**：`scripts/` 下文件是只读模板，禁止编辑或填占位符；需要填入运行时值时会写到技能目录**之外**的临时文件（如 `/tmp/`），运行后立即删除。
- **凭据安全**：PAT 存于 `EditorPrefs`（不入 git），不会回显，也不会持久化到技能目录内。

---

## 三、两者如何协同（推荐用法）

```
用户（自然语言）
      │  "把这个 Figma 稿导入团结引擎，File Key 是 xxx，PAT 是 yyy"
      ▼
AI Agent ── 加载 skill: figma2unity-import
      │
      ├─ 1. preflight.cs      → 检查包 / File Key / PAT 是否就绪
      ├─ 2. set_credentials.cs → （缺失时）写入凭据到 EditorPrefs
      └─ 3. run_import.cs      → 调用 ImportAsync() 触发导入
      ▼
Packages/cn.tuanjie.figma2unity（工具）执行真正的 Figma → UGUI 导入
```

### 前置条件

1. Unity / 团结引擎工程已内嵌 `cn.tuanjie.figma2unity` 包（否则 Preflight 会提示先安装）。
2. 准备好 **Figma File Key**（或 URL）与 **Personal Access Token**。
3. Agent 运行环境已具备 `execute_csharp_script`、`unity_scene` 等 Unity 桥接能力。

### 典型对话示例

> **用户**：帮我把这个 Figma 设计稿导入团结引擎，File Key 是 `abc123`，PAT 是 `figd_xxx`。
>
> **Agent**：（自动）运行 Preflight → 检测到凭据缺失 → 写入 File Key/PAT → 触发导入 → 回复「导入已触发」。

一句话总结：**工具**负责“怎么把 Figma 变成 UGUI”，**技能**负责“让 AI 自动地把这件事做完”，用户只需用自然语言下达指令并提供凭据即可。
