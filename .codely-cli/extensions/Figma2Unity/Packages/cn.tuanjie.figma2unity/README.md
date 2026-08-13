# Figma2Unity (cn.tuanjie.figma2unity)

Figma → Unity (UGUI) 导入工具。以 UPM 嵌入包形式提供，导入到空工程后即可使用。

## 依赖

包已在 `package.json` 声明以下依赖，UPM 会自动解析：

- `com.unity.textmeshpro` (TextMeshPro)
- `com.unity.nuget.newtonsoft-json` (Newtonsoft.Json)

Unity 版本要求：**2022.3+**（Tuanjie 1.5.x 兼容）。

## 功能矩阵

| 模块 | 支持范围 |
|---|---|
| **渲染** | 纯色（默认）；启用 `F2UConfig.EnableFigmageBaking` 后追加渐变 / 多层 Fill / 描边 / 阴影 / 圆角 / 9-Slice / 旋转 烘焙；VECTOR/BOOLEAN_OPERATION 走 Figmage 烘焙，失败兜底 Figma `/v1/images` PNG；Sprite Atlas 默认开启 |
| **布局** | 25 种 Constraints → Anchor 映射；Auto Layout（H/V + Hug + Fill + Wrap/GRID）；ScrollRect 真实滚动；FRAME `clipsContent` → RectMask2D |
| **文本** | TMP；`RichTextBuilder` 富文本；LineHeight / LetterSpacing / TextAutoResize |
| **圆角** | `F2UCornerRounder`（SDF shader `UI/Figma2Unity/CornerRounder`），运行时对 UGUI Image 做圆角/圆形遮罩，随包自带 shader |
| **交互** | Button / Toggle / InputField（含 PasswordField）/ ScrollRect / FlowButton |
| **原型流** | `PrototypeFlowController.TransitionToScreen` Fade 协程；`ISectionRoutingStrategy` 路由 |
| **字体** | Google Fonts CSS2 按需下载 + `TMP_FontAsset.CreateFontAsset`；中文兜底字体 `NotoSansSC`（预烘焙 SDF 随包自带） |
| **增量同步** | `DiffCalculator` / `SyncService.ApplyDiffAsync` 节点级重绘 |
| **离线** | `FigmaCacheStore` + `F2UPrefetchCommand` 一次性预下载，`Use cache` 模式零网络重导 |

## 用法

1. `Window/Figma2Unity/Open Main Window`
2. 填 **Figma File Key**（也接受完整 URL）+ **Personal Access Token**（存 EditorPrefs，不入 git）
3. 拖 **F2UConfig**（可选；不指定走默认值）
4. **Mode** 选 `Full` 或 `Incremental`
5. **在线导入**：点 `Import`
6. **离线流程**：点 `Prefetch` → 勾 `Use cache` → 点 `Import`

### 中文兜底字体

菜单 `Figma2Unity/Setup CJK Fallback Font` 会复用包内预烘焙的 `NotoSansSC_SDF.asset` 并注册到 TMP 全局 fallback 列表，使所有 TextMeshPro 组件都能渲染中文。

## 目录

- `Runtime/` — 运行时组件（`FlowButton` / `PrototypeFlowController` / `SyncHelper` / `SectionRouting` / `F2UCornerRounder`）、Figmage 渲染、Resources（shader/texture）、Shaders
- `Editor/` — Editor 工具 / Pipeline / Drawers / Sync / Fonts / Rendering / UI
- `Fonts/` — 中文兜底字体 `NotoSansSC.ttf` + 预烘焙 `NotoSansSC_SDF.asset`
- `Tests/` — Editor 测试（`UNITY_INCLUDE_TESTS`）

## 离线测试 (Layer A)

纯算法 / 数据 / 离线缓存 / Parser / Drawer 幂等 / Diff / Sync / 字体解析等，见工程根 `OfflineTests.Standalone/`：

```bash
cd OfflineTests.Standalone && dotnet test
```
