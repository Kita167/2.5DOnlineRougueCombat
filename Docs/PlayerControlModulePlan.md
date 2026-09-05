# Project Relay 玩家控制模块细则开发计划

> 文档版本：v2.1  
> 更新日期：2026-09-05  
> 对应总体规划：M1 玩家控制基础  
> 前置条件：Input 接入层已完成

---

## 1. 本次实现内容

### 1.1 已有前置条件

以下内容已经完成，本计划不再重复实现：

- `Game.Runtime.asmdef`。
- Input Actions 自动生成类。
- `IPlayerInputSource`。
- `LocalPlayerInputSource`。
- `Move`、`Attack`、`Interact`、`Dash` 输入采集。

本模块只消费：

- `IPlayerInputSource.Move`。
- `IPlayerInputSource.ConsumeDashPressed()`。
- `IPlayerInputSource.SetInputEnabled(...)`。

Attack 和 Interact 留给后续模块使用。

### 1.2 本次需要完成

- 基于 `CharacterController` 的玩家移动。
- 相机相对的 XZ 平面移动方向。
- 贴地、重力、最大下落速度和墙体碰撞。
- 玩家跟随移动方向旋转；无移动输入时保持最后朝向。
- `Free`、`Dashing`、`Disabled` 三种移动状态。
- 冲刺方向、持续时间、冷却和撞墙提前结束。
- 俯视角相机平滑跟随。
- BattleSandbox 中的玩家 Prefab、地面和障碍测试环境。
- 基础动画参数输出和手动验收。

### 1.3 本次不实现

- Input Actions 或 Input 接入层重构。
- 鼠标位置和右摇杆独立瞄准；当前没有对应 Input Action，本轮先面向移动方向。
- 普通攻击、交互业务、技能、生命、受击和伤害。
- 冲刺无敌帧。
- Rigidbody、Root Motion 和 Cinemachine。
- NGO、NetworkTransform 或 Owner 判断。

---

## 2. 实现思路

### 2.1 数据流

```text
LocalPlayerInputSource
        │ Move / Dash Intent
        ▼
PlayerController
        ├── 将 Move 转换为相机相对世界方向
        ├── 将 Dash 输入提交给移动状态机
        ▼
PlayerLocomotionStateMachine
        ├── Free：输出普通移动速度
        ├── Dashing：输出锁定方向的冲刺速度
        └── Disabled：输出零水平速度
        │
        ▼
PlayerMotor
        └── 合并水平速度、重力和碰撞后调用 CharacterController.Move

PlayerFacingController ← 最终移动/冲刺方向
TopDownCameraController ← 玩家 Transform
```

### 2.2 固定技术选择

- `PlayerController` 是玩家控制入口，只协调输入和各子模块，不直接操作 Transform，也不承载技能、伤害等业务规则。
- `PlayerMotor` 不读取 Input System，只接收已经计算好的水平速度。
- 使用 `Update` 驱动 `CharacterController`，不用 `FixedUpdate`。
- `PlayerLocomotionStateMachine` 使用一个普通 C# 类和一个 enum；当前不为三个简单移动状态分别建立状态类。
- 技能系统后续单独管理施放阶段，并通过“是否允许移动/冲刺、速度倍率和朝向覆盖”等约束与玩家移动协调，不把技能状态塞入移动状态机。
- 所有移动统一经过 `CharacterController.Move`。
- 设计参数保存在只读的 `PlayerMovementConfig` 中，冷却和计时保存在运行时状态中。
- 相机由场景持有，通过 `BattleSandboxInstaller` 绑定玩家，不放入玩家 Prefab。

### 2.3 初始参数

| 参数 | 初始值 |
| --- | --- |
| Move Speed | `5 m/s` |
| Rotation Speed | `720°/s` |
| Gravity | `-25 m/s²` |
| Maximum Fall Speed | `-40 m/s` |
| Grounded Vertical Speed | `-2 m/s` |
| Dash Speed | `12 m/s` |
| Dash Duration | `0.18s` |
| Dash Cooldown | `0.80s` |
| Dash Input Buffer | `0.10s` |
| End Dash When Blocked | 开启 |
| Camera Offset | `(0, 10, -8)` |
| Camera Smooth Time | `0.08s` |

---

## 3. 文件管理

### 3.1 玩家代码

目录：

```text
Assets/ProjectRelay/Scripts/Runtime/Gameplay/Player/
```

| 脚本 | 职责 |
| --- | --- |
| `PlayerMovementConfig.cs` | 保存移动、重力、旋转和冲刺设计参数 |
| `PlayerMovementMath.cs` | 提供相机相对方向和输入归一化的纯计算 |
| `PlayerMotor.cs` | 包装 CharacterController，执行位移、贴地、重力和碰撞 |
| `PlayerFacingController.cs` | 根据移动或冲刺方向平滑旋转玩家 |
| `PlayerLocomotionState.cs` | 定义 Free、Dashing、Disabled 移动状态 |
| `PlayerLocomotionStateMachine.cs` | 管理移动状态、Dash 计时、冷却和速度输出 |
| `PlayerController.cs` | 消费 Input Source 并协调 Locomotion State、Motor 和 Facing |
| `PlayerAnimationPresenter.cs` | 将实际速度和移动状态写入 Animator |

### 3.2 场景辅助代码

```text
Assets/ProjectRelay/Scripts/Runtime/Presentation/Camera/
└── TopDownCameraController.cs

Assets/ProjectRelay/Scripts/Runtime/Dev/
└── BattleSandboxInstaller.cs
```

| 脚本 | 职责 |
| --- | --- |
| `TopDownCameraController.cs` | 在 LateUpdate 平滑跟随当前玩家 |
| `BattleSandboxInstaller.cs` | 将场景 Camera、CameraRig、Input Source 和玩家控制器连接起来 |

---

## 4. 逐步执行计划

### Step 1：完成普通移动

> 当前状态：代码已完成并通过编译；等待 Unity Editor 场景、Prefab、Layer 和引用配置后进行本步验收。

#### 本步结果

玩家能够在 BattleSandbox 中进行相机相对移动，并正确处理地面、重力和墙体碰撞。

#### 涉及脚本

新建：

- `PlayerMovementConfig.cs`
- `PlayerMovementMath.cs`
- `PlayerMotor.cs`
- `PlayerController.cs`
- `BattleSandboxInstaller.cs`

实现要点：

- `PlayerMovementMath` 将 Camera Forward/Right 投影到 XZ 平面。
- 输入方向最终长度钳制为 1，避免斜向加速。
- `PlayerMotor` 每帧合并水平位移和垂直速度，只调用一次 `CharacterController.Move`。
- 接地且正在向下移动时，垂直速度重置为 `-2`。
- 非接地时应用重力，并限制到最大下落速度。
- `PlayerController` 必须完成初始化后才开始读取 Input。

#### Editor 配合

1. 将现有 SampleScene 移动并重命名为：

   ```text
   Assets/ProjectRelay/Scenes/Dev/BattleSandbox.unity
   ```

2. 创建 Layer：

   ```text
   Player
   Ground
   Obstacle
   ```

3. 创建地面：

   - Cube。
   - Position：`(0, -0.5, 0)`。
   - Scale：`(30, 1, 30)`。
   - Layer：Ground。

4. 创建至少三面测试墙，Layer 设置为 Obstacle。
5. 创建玩家根对象 `PF_Player`，Layer 设置为 Player。
6. 玩家根对象添加：

   - CharacterController。
   - LocalPlayerInputSource。
   - PlayerMotor。
   - PlayerController。

7. CharacterController 配置：

| 参数 | 值 |
| --- | --- |
| Radius | `0.35` |
| Height | `1.8` |
| Center | `(0, 0.9, 0)` |
| Slope Limit | `45` |
| Step Offset | `0.25` |
| Skin Width | `0.035` |
| Min Move Distance | `0` |

8. 不添加 Rigidbody。
9. 创建 `PlayerMovement_Default.asset`，填入第 2.3 节参数。
10. 创建 `BattleSandboxRoot`，添加 `BattleSandboxInstaller` 并绑定 Main Camera、PlayerController 和 LocalPlayerInputSource。

#### 本步检查

- WASD 和左摇杆可以移动。
- 相机旋转后，移动方向仍与屏幕方向一致。
- 斜向移动不比直线更快。
- 玩家不会穿过地面和墙体。
- 松开输入后立即停止水平移动。
- Console 无 Error。

---

### Step 2：完成朝向与相机跟随

> 当前状态：代码已完成并通过编译；等待 Unity Editor 添加组件、搭建 CameraRig 并绑定引用后进行本步验收。

#### 本步结果

玩家朝当前移动方向平滑旋转，相机平滑跟随玩家。

#### 涉及脚本

新建：

- `PlayerFacingController.cs`
- `TopDownCameraController.cs`

修改：

- `PlayerController.cs`
- `BattleSandboxInstaller.cs`

实现要点：

- 移动方向长度超过最小阈值时，朝向节点按配置角速度向目标方向插值旋转。
- 无移动输入时保持朝向节点的实际当前旋转。
- 使用 `Quaternion.RotateTowards`，旋转速度来自 Config。
- CameraRig 在 `LateUpdate` 跟随，避免比玩家 Update 更早更新造成抖动。
- Camera Target 必须支持显式 Bind/Unbind。

#### Editor 配合

1. 玩家根对象添加 `PlayerFacingController`。
2. 创建：

   ```text
   CameraRig
   └── Main Camera
   ```

3. CameraRig 添加 `TopDownCameraController`。
4. 设置：

   - Rotation：`(50, 0, 0)`。
   - Offset：`(0, 10, -8)`。
   - Smooth Time：`0.08`。
   - Main Camera FOV：`50`。
   - Main Camera Tag：`MainCamera`。

5. 在 BattleSandboxInstaller 中绑定 TopDownCameraController。

#### 本步检查

- 玩家移动时平滑面向移动方向。
- 停止移动后不会自动恢复世界 Forward。
- 相机跟随无明显一帧抖动。
- 删除或禁用玩家时，相机不会产生 NullReferenceException。

---

### Step 3：完成移动状态机与冲刺

> 当前状态：代码已完成并通过编译；等待 Unity Editor 检查引用并完成空旷、正面撞墙和斜角撞墙验收。

#### 本步结果

玩家可以进入 Dash，Dash 期间锁定方向，结束后受冷却限制并返回 Free。

#### 涉及脚本

新建：

- `PlayerLocomotionState.cs`
- `PlayerLocomotionStateMachine.cs`

修改：

- `PlayerController.cs`
- `PlayerFacingController.cs`

实现要点：

- 初始状态为 Disabled，Installer 完成绑定后切换为 Free。
- Free 输出普通移动速度。
- Dashing 输出进入状态时锁定的方向和 Dash Speed。
- Disabled 输出零水平速度并拒绝 Dash。
- Dash 方向优先使用当前移动方向；没有移动时读取朝向节点经过插值后的实际当前朝向。
- Dash 输入最多缓存 `0.10s`。
- Dash 在 `0.18s` 后结束，冷却从结束时开始。
- Dash 遇到侧面碰撞且沿冲刺方向的实际速度明显不足时提前结束。
- 禁用玩家或退出场景时调用 ForceReset。
- 不使用 Coroutine。

#### Editor 配合

1. 确认 PlayerController 引用了 PlayerMovementConfig、PlayerMotor 和 PlayerFacingController。
2. 在空旷区域测试理论 Dash 距离。
3. 分别正面和斜角冲向墙体。
4. 如需调整手感，只修改 `PlayerMovement_Default.asset`。

#### 本步检查

- Space 和手柄 Dash 按键都能触发。
- 按住 Dash 不会每帧重复触发。
- 冷却期间再次按键不会进入 Dash。
- Dash 期间不能改变方向。
- Dash 撞墙不穿透，也不会永久失去控制。
- Disable/Enable 玩家后不会执行旧 Dash 输入。

---

### Step 4：完成动画参数输出与最终验收

> 当前状态：核心动画参数输出代码已完成；Animator 配置和 Profiler 采样保留为 Editor 验收项。

#### 本步结果

玩家移动状态可以驱动动画参数，并在 BattleSandbox 中完成最终手动验收。

#### 涉及脚本

新建：

- `PlayerAnimationPresenter.cs`

修改：

- `PlayerController.cs`

Presenter 输出：

- `Speed`：当前实际水平速度归一化值。
- `IsDashing`：当前是否处于 Dashing。

#### Editor 配合

1. 在玩家模型子节点添加 `Animator`，在玩家根节点添加 `PlayerAnimationPresenter`；只有一个 Animator 时两个引用可以留空自动查找。
2. Animator Controller 添加 `Speed`（Float）和 `IsDashing`（Bool），名称与类型必须完全一致。
3. 用 `Speed` 驱动 Idle/Move 的 1D Blend Tree：Idle 阈值为 `0`，Move 阈值为 `1`。
4. Locomotion → Dash 使用 `IsDashing == true`；Dash → Locomotion 使用 `IsDashing == false`。两条过渡关闭 Has Exit Time，Transition Duration 建议 `0.05s`。
5. 关闭 Animator 的 Apply Root Motion，实际位移仍由 `CharacterController` 负责。
6. 如果暂时没有角色动画，只保留 Presenter 代码，不把 Animator 作为本模块验收阻塞项。
7. 在 Profiler 中检查稳定移动时是否存在每帧 GC Alloc。

#### 本步检查

- Animator 参数会随移动和冲刺状态正确变化。
- 没有 Animator 时玩家控制仍能正常运行。
- 重复进入和退出 Play Mode 不会产生输入或状态倍增。

---

## 5. 最终验收

- [ ] 键盘和手柄均可控制移动。
- [ ] 移动方向为相机相对方向。
- [ ] 斜向移动速度与直线一致。
- [ ] 角色正确贴地，不穿过地面或障碍。
- [ ] 玩家平滑面向当前移动方向。
- [ ] 相机稳定跟随，无明显抖动。
- [ ] Free、Dashing、Disabled 状态可以正确切换。
- [ ] Dash 距离、持续时间和冷却符合 Config。
- [ ] Dash 撞墙后能够恢复控制。
- [ ] 禁用、启用和场景退出会清除临时状态。
- [ ] 连续重载 BattleSandbox 10 次无异常。
- [ ] 稳定移动时没有每帧 GC Alloc。
- [ ] Console 无 Error。
- [ ] Input、Motor、Locomotion State、Facing 和 Camera 职责没有互相越界。

完成本计划后，玩家移动模块停止增加功能。下一阶段新增独立的技能/战斗控制器，由 `PlayerController` 转发输入并协调移动约束；伤害与技能阶段不得写入移动状态机。
