# 笼子谜题关卡设置指南

本指南说明如何设置最终关卡的笼子谜题，玩家需要按顺序将时间轴移动到特定的结点区域来降低笼子的栏杆。

## 概述

笼子谜题使用以下组件：
- **TimableCage**：具有顺序结点区域的时间操控对象，触发栏杆下降
- **TimableKnotUI**：在时间轴上显示当前活动结点区域的 UI（基于 Shader 的进度条）
- **InteractionManager**：针对笼子对象使用 TimableKnotUI1

## 游戏流程

1. 玩家激活笼子的时间操控
2. UI 显示时间轴上高亮的**第一个结点区域**
3. 玩家将时间轴移动到高亮区域并**保持所需时间** → 栏杆下降，结点完成
4. 短暂延迟后，UI 过渡显示**第二个结点区域**
5. 玩家重复操作直到所有结点完成
6. 当所有结点完成时：
   - 笼子栏杆完全降下
   - 笼子无法再被选中（标签变为 "Untagged"）
   - 时间操控自动退出
   - 关卡完成！

## 设置步骤

### 1. 创建笼子对象

1. 创建一个名为 "Cage" 的空 GameObject
2. 添加笼子结构的子对象（墙壁、顶部等）
3. 创建一个名为 "Bars" 的子对象，用于向下移动
4. 在根 "Cage" 对象上添加 `TimableCage` 组件
5. 确保 "Cage" 有 **"Timable"** 标签

### 2. 配置 TimableCage 组件

在 TimableCage 的 Inspector 中：

**笼子设置 (Cage Settings)：**
- **Bars Transform**：拖入 "Bars" 子对象
- **Lower Distance Per Knot**：每个激活结点降低的距离（如 0.5）
- **Lower Speed**：栏杆下降动画速度（如 2）
- **Lower Direction**：通常为 (0, -1, 0) 表示向下

**保持设置 (Hold Settings)：**
- **Knot Hold Duration**：玩家需要在结点区域保持的时间（秒）（如 3.0）

**过渡设置 (Transition Settings)：**
- **Knot Transition Delay**：显示下一个结点 UI 前的延迟时间（秒）（如 1.0）

**结点区域 (Knot Regions)（顺序）：**
- 点击 + 按钮按**解决顺序**添加结点区域
- 为每个结点设置 **Min Time Value** 和 **Max Time Value**
- 结点按数组顺序解决（索引 0，然后 1，然后 2 等）

**音效（可选）：**
- **Knot Activated Sound**：每个结点解决时播放
- **All Knots Completed Sound**：笼子完全打开时播放

### 3. 在场景中创建 TimableKnotUI1

1. 创建一个新的 Canvas（World Space）命名为 "TimableKnotUI1"
2. 在 Canvas 上添加 `TimableKnotUI` 组件
3. 创建以下 UI 元素作为子对象：

**UI 结构：**
```
TimableKnotUI1 (Canvas + TimableKnotUI)
├── TimelineGraphic (Image，带进度 Shader 材质)
├── KnotContainer (空 RectTransform) - 用于结点指示器叠加
├── HoldProgressBar (Image, Fill 类型) - 显示保持进度
├── TimeValueText (Text - Legacy)
├── StatusText (Text - Legacy)
└── HoldProgressText (Text - Legacy, 可选)
```

4. 创建**进度 Shader 材质**：
   - 创建一个新材质，使用具有 `_Progress` float 属性的 Shader
   - Shader 应可视化从 0（左/过去）到 1（右/未来）的进度
   - 0.5 表示中心（现在，时间值 0）
   - 将此材质分配给 TimelineGraphic 的 Image 组件

5. 配置 TimableKnotUI 组件：
   - **Timeline Graphic**：拖入 TimelineGraphic (Image)
   - **Progress Property Name**："_Progress"（默认，匹配 Shader 属性）
   - **Time Value Text**：拖入 TimeValueText
   - **Status Text**：拖入 StatusText
   - **Knot Container**：拖入 KnotContainer
   - **Hold Progress Bar**：拖入 HoldProgressBar
   - **Hold Progress Text**：拖入 HoldProgressText（可选）
   - 根据需要调整颜色

### 4. 配置 InteractionManager

在场景中找到 InteractionManager：

1. 在 Inspector 中找到 **Timeline UI** 部分
2. 设置 **Timable Knot UI 1**：拖入 TimableKnotUI1 Canvas

## 进度条 Shader 要求

时间轴可视化使用基于 Shader 的方法实现平滑进度显示：

| 属性 | 类型 | 范围 | 描述 |
|------|------|------|------|
| `_Progress` | Float | 0 到 1 | 时间轴位置（0=过去，0.5=现在，1=未来）|

时间值映射公式：`progressValue = (timeValue + 1) * 0.5`
- 时间值 -1（远过去）→ 进度 0（左边缘）
- 时间值 0（现在）→ 进度 0.5（中心）
- 时间值 +1（远未来）→ 进度 1（右边缘）

## 工作原理

1. 玩家瞄准笼子并激活时间操控（扳机）
2. TimableKnotUI1 出现，显示**仅当前活动结点**高亮
3. 玩家移动时间轴（摇杆）到高亮区域
4. 当时间轴进入结点时：
   - 保持进度条开始填充
   - 玩家必须稳定保持所需时间
5. 保持完成时 → 结点激活，栏杆下降
6. 过渡延迟后，时间轴重置到中心（0）
7. UI 更新显示**下一个结点区域**
8. 重复直到所有结点完成
9. 笼子标签变为 "Untagged" → 无法再被选中
10. 时间操控在 1.5 秒后自动退出

## 测试

- 使用 TimableCage 的 `ResetKnots()` 方法在测试期间重置进度
- 检查控制台调试消息：
  - "TimableCage: Knot X activated!"
  - "TimableCage: Transitioning to knot X"
  - "TimableCage: All knots completed!"
- 选中笼子时 Gizmos 显示目标栏杆位置

## 示例结点配置（3 个结点）

| 顺序 | 最小值 | 最大值 | 描述 |
|------|--------|--------|------|
| 0    | -0.8   | -0.4   | 远过去 |
| 1    | 0.2    | 0.5    | 近未来 |
| 2    | -0.3   | 0.1    | 接近现在 |

玩家必须按顺序解决：先是过去区域，然后未来，最后现在。
保持时间：每个结点 3.0 秒（可配置）。

---

## 宝珠通关序列设置

当笼子的所有结点解开后，宝珠会执行一系列通关动画，然后显示制作人员名单，最后返回主菜单。

### 1. 创建宝珠对象

1. 在密室2区域创建3D球体（右键 → `3D Object > Sphere`）
2. 命名为 "TreasureOrb"
3. 调整大小（建议：0.3, 0.3, 0.3）
4. 添加发光材质（建议使用自发光 Shader）
5. 添加 `OrbVictorySequence` 组件

### 2. 创建淡出 Canvas

1. 创建新的 Canvas（Screen Space - Camera 或 World Space）
2. 命名为 "FadeCanvas"
3. 添加全屏黑色 Image 子对象
4. 在 Canvas 上添加 `CanvasGroup` 组件
5. 设置初始 Alpha = 0

**如果使用 World Space Canvas：**
- 将 Canvas 放置在玩家摄像机子对象下
- 设置合适的距离（如 0.5 米前方）
- 确保 Canvas 跟随玩家视角

### 3. 创建结语 Canvas

1. 创建另一个 Canvas
2. 命名为 "CreditsCanvas"
3. 添加 `CanvasGroup` 组件
4. 添加 TextMeshPro 文本组件（或 Text - Legacy）
5. 设置初始 Alpha = 0
6. 配置文本样式：
   - 字体大小：适当大小
   - 对齐方式：居中
   - 颜色：白色

**结语内容（脚本会自动设置）：**
```
感谢游玩

本游戏仍在开发阶段
不代表最终成品呈现效果

SJTU Design 作品

作者
Ma Jiale    Zhang Qi

鸣谢
张安东 老师
```

### 4. 配置 OrbVictorySequence 组件

在宝珠的 Inspector 中配置：

| 字段 | 说明 | 建议值 |
|------|------|--------|
| Target Cage | 目标笼子引用 | 拖入场景中的 TimableCage 对象 |
| Orbit Duration | 绕圈动画时长 | 3 秒 |
| Orbit Loops | 绕圈次数 | 3 圈 |
| Orbit Radius | 绕圈半径 | 1 米 |
| Orbit Height | 绕圈中心高度 | 2 米 |
| Fly To Player Duration | 飞向玩家时长 | 2 秒 |
| Distance From Player | 玩家面前距离 | 0.5 米 |
| Orb Fade Out Duration | 宝珠渐隐时长 | 2 秒 |
| Screen Fade Duration | 屏幕淡出时长 | 2 秒 |
| Credits Display Time | 结语显示时长 | 8 秒 |
| Credits Fade In Duration | 结语淡入时长 | 1 秒 |
| Fade Canvas Group | 淡出 Canvas | 拖入 FadeCanvas 的 CanvasGroup |
| Credits Canvas Group | 结语 Canvas | 拖入 CreditsCanvas 的 CanvasGroup |

### 5. 通关序列流程

1. 玩家解开笼子的所有结点
2. `TimableCage` 触发 `OnCageCompleted` 事件
3. `OrbVictorySequence` 开始执行：
   - **阶段1**：宝珠在笼子上方绕圈飞行
   - **阶段2**：宝珠飞向玩家面前
   - **阶段3**：宝珠逐渐变透明并消失
   - **阶段4**：屏幕逐渐变黑
   - **阶段5**：显示结语文本
   - **阶段6**：返回 Home 场景，恢复主菜单状态

### 6. 测试

- 使用宝珠 Inspector 中的右键菜单 → "Trigger Victory Sequence" 可以手动触发通关序列
- 检查控制台消息：
  - "OrbVictorySequence: Cage completed! Starting victory sequence."
  - "OrbVictorySequence: Starting orbit animation."
  - "OrbVictorySequence: Flying to player."
  - "OrbVictorySequence: Fading out orb."
  - "OrbVictorySequence: Fading screen to black."
  - "OrbVictorySequence: Showing credits."
  - "OrbVictorySequence: Returning to home scene."
