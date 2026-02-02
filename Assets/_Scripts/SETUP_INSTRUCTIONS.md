# VR游戏设置说明
## Unity VR游戏完整设置指南（使用XR Interaction Toolkit）

---

## 目录
1. [概述](#概述)
2. [场景设置](#场景设置)
3. [玩家设置](#玩家设置)
4. [UI设置](#ui设置)
5. [传送锚点设置](#传送锚点设置)
6. [宝珠拾取设置](#宝珠拾取设置)
7. [构建设置](#构建设置)
8. [测试指南](#测试指南)

---

## 概述

**场景结构：**
- **Home场景** - 主菜单和出生点
- **Island场景** - 包含岛屿区域 + 密室1 + 密室2（全部在一个场景中）

此VR游戏系统包含6个主要脚本：
- **GameManager.cs** - 控制整体游戏状态和流程
- **UIManager.cs** - 管理菜单UI（胜利UI可选）
- **SceneTransitionManager.cs** - 处理场景加载和玩家出生
- **TeleportAnchor.cs** - 创建传送点
- **OrbPickup.cs** - 处理宝珠收集
- **VRPlayerMovement.cs** - 控制VR玩家移动

---

## 场景设置

### 步骤1：创建场景

1. 在Unity中创建两个场景：
   - **Home** (主菜单场景)
   - **Island** (包含岛屿区域、密室1和密室2)

2. 将两个场景添加到构建设置：
   - 前往 `File > Build Settings`
   - 将两个场景拖入"Scenes in Build"面板
   - 确保"Home"在索引0（第一个场景）

### 步骤2：创建管理器对象（仅在Home场景）

在 **Home** 场景中，创建持久化的管理器对象：

1. **创建GameManager对象：**
   - 在Hierarchy中右键 → `Create Empty`
   - 命名为"GameManager"
   - 添加 `GameManager.cs` 脚本
   - 此对象将自动在所有场景中持久化

2. **创建UIManager对象：**
   - 在Hierarchy中右键 → `Create Empty`
   - 命名为"UIManager"
   - 添加 `UIManager.cs` 脚本
   - 此对象将自动在所有场景中持久化

3. **创建SceneTransitionManager对象：**
   - 在Hierarchy中右键 → `Create Empty`
   - 命名为"SceneTransitionManager"
   - 添加 `SceneTransitionManager.cs` 脚本
   - 此对象将自动在所有场景中持久化

**重要提示：** 这三个管理器对象应该只存在于Home场景中。它们会通过 `DontDestroyOnLoad()` 自动持久化到其他场景。

---

## 玩家设置

### 步骤1：使用XR Interaction Toolkit创建XR Origin

1. **安装XR Interaction Toolkit：**
   - 打开Package Manager (`Window > Package Manager`)
   - 安装"XR Interaction Toolkit"
   - 安装"XR Plugin Management"
   - 为你的VR设备配置XR Plugin（Oculus、OpenXR等）

2. **创建XR Origin (XR Rig)：**
   - 在Hierarchy中，右键 → `XR > XR Origin (Action-based)` 或 `XR > XR Origin (Device-based)`
   - 这会创建一个完整的VR设备，包含：
     - XR Origin (根对象)
     - Camera Offset
     - Main Camera (VR头显视图)
     - Left Controller (左手柄)
     - Right Controller (右手柄)

3. **添加必需组件：**
   - 选择 **XR Origin** 根对象
   - 添加 `CharacterController` 组件
   - 调整CharacterController设置：
     - Height: 1.8 (典型人类身高)
     - Radius: 0.3
     - Center: (0, 0.9, 0)

4. **添加VRPlayerMovement脚本：**
   - 保持XR Origin选中状态，添加 `VRPlayerMovement.cs`
   - 配置脚本：
     - Move Speed: 3
     - Gravity: 9.8
     - VR Camera: 将 **Main Camera**（Camera Offset的子对象）拖入此字段
     - Use Primary Controller: 勾选此项（使用左手柄进行移动）

5. **标记玩家：**
   - 选择 **XR Origin** 根对象
   - 在Inspector中，将Tag设置为"Player"
   - 如果"Player"标签不存在，创建它：`Edit > Project Settings > Tags and Layers`

### 步骤2：将玩家分配给GameManager

1. 在 **Home** 场景中，选择GameManager对象
2. 在Inspector中，找到GameManager脚本
3. 将你的XR Origin（玩家）拖入"Player"字段

---

## UI设置

### 步骤1：创建主菜单UI（Home场景）

1. **创建Canvas：**
   - 在Hierarchy中右键 → `UI > Canvas`
   - 命名为"MainMenuCanvas"
   - 将Canvas设置为"World Space"模式（用于VR兼容）
   - 配置Canvas：
     - Pos X, Y, Z: 放置在出生点前方（例如：0, 1.5, 2）
     - Width: 1920
     - Height: 1080
     - Scale: 0.001, 0.001, 0.001

2. **创建主菜单面板：**
   - 在Canvas上右键 → `UI > Panel`
   - 命名为"MainMenuPanel"
   - 添加UI元素：
     - **标题文本：** "Painted Time VR"
     - **开始按钮：** 创建按钮，文本："Start Game"
     - **退出按钮：** 创建按钮，文本："Quit Game"

3. **创建胜利屏幕面板（可选 - 暂时跳过）：**
   - 如果以后想要胜利UI，可以创建另一个面板
   - 现在可以跳过此步骤
   - 宝珠仍会将玩家传送回Home，只是没有胜利消息

### 步骤2：配置UIManager

1. 选择UIManager对象
2. 在Inspector中，配置UIManager脚本：
   - **Main Menu Panel:** 将"MainMenuPanel"拖到这里
   - **Victory Panel:** 暂时留空（可选）
   - **Start Game Button:** 将"Start Game"按钮拖到这里
   - **Quit Button:** 将"Quit Game"按钮拖到这里
   - **Restart Button:** 留空（仅在有胜利面板时需要）
   - **Quit From Victory Button:** 留空（仅在有胜利面板时需要）

### 步骤3：设置场景渐变（使用PICO平台）

用于平滑的场景过渡，使用PICO设备原生支持：

**环境要求：**
- SDK 版本：1.1.0 及以上
- PICO 设备型号：PICO Neo3 系列、PICO 4 系列、PICO 4 Ultra 系列
- PICO 设备系统版本：5.7.0 及以上

**设置步骤：**

1. 在Hierarchy中选中 **XR Origin** 下的 **Main Camera** 对象
2. 在Inspector窗口底部点击 **Add Component** 按钮
3. 搜索并添加 **PICO Screen Fade** 脚本
4. 配置场景渐变参数：
   - **Gradient Time:** 渐变效果持续时间（推荐：1-2秒）
   - **Fade Color:** 屏幕渐变颜色（推荐：黑色）

**注意：** 使用PICO Screen Fade后，SceneTransitionManager中的"Fade Panel"字段可以留空，因为渐变效果由PICO平台原生处理。如果需要在代码中控制渐变，可以通过PICO Screen Fade的API调用。

---

## 传送锚点设置

### 传送类型

1. **Same Scene Teleport（同场景传送）** - 在同一场景内移动玩家
2. **Different Scene Teleport（不同场景传送）** - 加载新场景并生成玩家

### 步骤1：创建出生点

在每个场景中创建出生点标记：

1. **Home场景：**
   - 创建Empty GameObject
   - 命名为"StartGamePoint"
   - 将其放置在所需的出生位置
   - 旋转使其面向所需方向（朝向UI）

2. **Island场景：**
   - 创建"SpawnPoint_Island" - 放置在岛屿起始区域
   - 创建"SpawnPoint_Chamber1" - 放置在密室1入口
   - 创建"SpawnPoint_Chamber2" - 放置在密室2入口
   
   **注意：** 所有三个出生点都在同一个Island场景中

### 步骤2：创建传送锚点

#### Home → Island 传送

1. 在 **Home** 场景中：
   - 创建Empty GameObject
   - 命名为"Teleport_ToIsland"
   - 添加Collider（Box Collider或Capsule Collider）
   - 勾选"Is Trigger"
   - 添加 `TeleportAnchor.cs` 脚本
   - 配置：
     - Teleport Type: **Different Scene**
     - Target Scene Name: "Island"
     - Spawn Point Name: "SpawnPoint_Island"

#### Island → 密室1 传送（山洞入口）

1. 在 **Island** 场景中（山洞入口处）：
   - 创建Empty GameObject
   - 命名为"Teleport_ToChamber1"
   - 添加触发碰撞体（Box/Capsule）
   - 添加 `TeleportAnchor.cs` 脚本
   - 配置：
     - Teleport Type: **Same Scene**
     - Target Transform: 将"SpawnPoint_Chamber1" GameObject拖到这里

#### 密室1 → 密室2 传送

1. 在 **Island** 场景中（密室1末尾）：
   - 创建Empty GameObject
   - 命名为"Teleport_ToChamber2"
   - 添加触发碰撞体
   - 添加 `TeleportAnchor.cs` 脚本
   - 配置：
     - Teleport Type: **Same Scene**
     - Target Transform: 将"SpawnPoint_Chamber2" GameObject拖到这里

**注意：** 所有密室都在Island场景中，因此在岛屿区域和密室之间的过渡使用 **Same Scene（同场景）** 传送类型。

---

## 宝珠通关序列设置

宝珠通关序列使用 `OrbVictorySequence.cs` 脚本，当笼子谜题完成后触发一系列通关动画。

### 步骤1：创建宝珠

1. 在 **Island** 场景中（密室2末尾，笼子附近）：
   - 创建3D球体（右键 → `3D Object > Sphere`）
   - 命名为 "TreasureOrb"
   - 适当缩放（例如：0.3, 0.3, 0.3）
   - 添加发光材质（推荐自发光材质）

2. **添加组件：**
   - 添加 `OrbVictorySequence` 组件

### 步骤2：创建淡出 Canvas

1. 在场景中创建新的 Canvas：
   - 命名为 "FadeCanvas"
   - 推荐使用 World Space 模式（VR兼容）
   - 如果使用 World Space，将其放置为玩家摄像机的子对象

2. **配置 Canvas：**
   - 添加 `CanvasGroup` 组件到 Canvas
   - 创建子对象 Image，覆盖整个 Canvas
   - 设置 Image 颜色为黑色 (0, 0, 0, 255)
   - 设置 CanvasGroup 的 Alpha = 0（初始隐藏）

### 步骤3：创建结语 Canvas

1. 创建另一个 Canvas：
   - 命名为 "CreditsCanvas"
   - 与 FadeCanvas 使用相同的渲染模式
   - 如果使用 World Space，放置在玩家可见位置

2. **配置 Canvas：**
   - 添加 `CanvasGroup` 组件
   - 创建 TextMeshPro 文本子对象
   - 设置文本样式：居中对齐，白色，适当字号
   - 设置 CanvasGroup 的 Alpha = 0（初始隐藏）

**注意：** 结语文本内容由脚本自动设置，包括：
- 感谢游玩信息
- 开发阶段说明
- SJTU Design 作品
- 作者：Ma Jiale、Zhang Qi
- 鸣谢：张安东老师

### 步骤4：配置 OrbVictorySequence 组件

1. 选择 TreasureOrb
2. 在 Inspector 中配置 OrbVictorySequence：

| 字段 | 描述 | 建议值 |
|------|------|--------|
| Target Cage | 目标笼子 | 拖入场景中的 TimableCage 对象 |
| Orbit Duration | 绕圈动画时长 | 3 |
| Orbit Loops | 绕圈次数 | 3 |
| Orbit Radius | 绕圈半径 | 1 |
| Orbit Height | 绕圈高度 | 2 |
| Fly To Player Duration | 飞向玩家时长 | 2 |
| Distance From Player | 离玩家距离 | 0.5 |
| Orb Fade Out Duration | 宝珠渐隐时长 | 2 |
| Screen Fade Duration | 屏幕淡出时长 | 2 |
| Credits Display Time | 结语显示时长 | 8 |
| Credits Fade In Duration | 结语淡入时长 | 1 |
| Fade Canvas Group | FadeCanvas 的 CanvasGroup |
| Credits Canvas Group | CreditsCanvas 的 CanvasGroup |

### 步骤5：可选 - 添加视觉效果

1. **添加光源：**
   - 添加 Point Light 作为 TreasureOrb 的子对象
   - 设置颜色以匹配宝珠
   - 设置 Range: 5

2. **添加音效：**
   - 在 OrbVictorySequence 组件中：
     - Victory Sound: 通关音效
     - Credits Music: 结语背景音乐（可选）

### 通关序列流程

当笼子谜题完成时，脚本会按顺序执行：

1. 🔄 宝珠在笼子上方绕圈飞行（3秒，3圈）
2. 🚀 宝珠飞向玩家面前（2秒）
3. 👻 宝珠逐渐变透明并消失（2秒）
4. ⬛ 屏幕逐渐变黑（2秒）
5. 📜 显示结语文本（8秒）
6. 🏠 返回 Home 场景，恢复主菜单状态

---

## 构建设置

### 配置场景名称

1. 在SceneTransitionManager中：
   - **Home Scene Name:** "Home"
   - **Island Scene Name:** "Island"
   - **Chamber Scene Name:** 不使用（Island场景包含所有内容）

2. 验证构建设置（`File > Build Settings`）：
   - Scene 0: Home
   - Scene 1: Island
   
确保这些名称与构建设置中的场景名称完全匹配。

---

## 测试指南

### 无VR头显测试

VRPlayerMovement脚本包含键盘回退功能：
- **W/A/S/D** 或 **方向键** 进行移动
- 这允许在没有VR硬件的情况下进行测试

### 测试流程

1. **从Home场景开始：**
   - 应该看到主菜单UI
   - 玩家还不能移动
   - 点击"Start Game"按钮

2. **开始游戏后：**
   - UI应该消失
   - 玩家现在可以移动（使用左摇杆或WASD）
   - 走到传送锚点

3. **Island场景 - 岛屿区域：**
   - 玩家在SpawnPoint_Island出生
   - 在岛屿周围行走
   - 进入山洞触发传送到密室1

4. **Island场景 - 密室1：**
   - 玩家传送到SpawnPoint_Chamber1
   - 穿过密室1
   - 到达末尾的传送点前往密室2

5. **Island场景 - 密室2：**
   - 玩家传送到SpawnPoint_Chamber2
   - 找到并收集宝珠
   - 2秒后，返回Home场景

6. **返回Home：**
   - 回到Home场景的出生点
   - 主菜单UI出现（暂时没有胜利屏幕）
   - 玩家不能移动
   - 可以点击"Start Game"再次游玩或点击"Quit"退出

---

## 常见问题和解决方案

### 问题：玩家掉落穿过地板
- **解决方案：** 确保地板有碰撞体，CharacterController有正确的高度/半径

### 问题：传送不起作用
- **解决方案：** 
  - 检查玩家是否有"Player"标签
  - 确保碰撞体标记为"Is Trigger"
  - 验证场景名称在构建设置中完全匹配

### 问题：UI不显示
- **解决方案：**
  - 检查UI面板是否已分配给UIManager
  - 确保Canvas处于World Space模式（用于VR）
  - 将Canvas放置在出生点前方

### 问题：菜单期间玩家可以移动
- **解决方案：** 游戏状态应该是"MainMenu"，这会禁用移动

### 问题：VR手柄不工作
- **解决方案：**
  - 安装XR Interaction Toolkit
  - 确保XR Plugin Management已配置
  - 检查设备是否正确连接

---

## 脚本参考快速指南

### GameManager
- 控制：游戏状态、玩家移动权限
- 关键方法：`StartGame()`, `CompleteGame()`, `QuitGame()`

### UIManager
- 控制：所有UI面板和按钮
- 自动连接：按钮点击处理器

### SceneTransitionManager
- 控制：场景加载、玩家生成、淡入淡出效果
- 关键方法：`LoadScene()`, `LoadSceneWithSpawnPoint()`

### TeleportAnchor
- 类型：同场景或不同场景传送
- 触发：玩家进入时自动激活

### OrbVictorySequence
- 功能：笼子完成后触发通关序列
- 动画：宝珠绕圈飞行 → 飞向玩家 → 渐隐
- 自动：屏幕淡出 → 显示结语 → 返回Home场景

### VRPlayerMovement
- 控制：VR摇杆移动
- 回退：键盘输入用于测试

---

## 最终检查清单

- [ ] XR Interaction Toolkit已安装并配置
- [ ] 两个场景（Home和Island）已创建并添加到构建设置
- [ ] GameManager、UIManager、SceneTransitionManager已在Home场景中创建
- [ ] XR Origin (XR Rig)已创建，带有CharacterController和VRPlayerMovement
- [ ] XR Origin已标记为"Player"
- [ ] 主菜单UI已创建并分配给UIManager
- [ ] 出生点已创建：SpawnPoint_Home、SpawnPoint_Island、SpawnPoint_Chamber1、SpawnPoint_Chamber2
- [ ] 从Home到Island的传送锚点（Different Scene类型）
- [ ] Island场景内的传送锚点（Same Scene类型，用于山洞入口和密室过渡）
- [ ] 宝珠已在密室2区域创建，带有OrbVictorySequence脚本
- [ ] FadeCanvas和CreditsCanvas已创建并配置
- [ ] 所有场景名称在脚本和构建设置中匹配
- [ ] VR摄像机已在VRPlayerMovement脚本中分配
- [ ] 已测试从开始到结束的完整游戏流程

---

## 支持

如果遇到问题：
1. 检查Unity Console中的错误消息
2. 验证Inspector中的所有对象分配
3. 确保所有脚本已附加到正确的对象
4. 逐步测试游戏流程
5. 使用Debug.Log语句跟踪游戏状态

祝你的VR游戏开发顺利！
