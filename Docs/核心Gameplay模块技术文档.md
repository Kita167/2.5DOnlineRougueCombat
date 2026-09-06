# Project Relay 核心 Gameplay 模块技术文档

> 文档版本：v1.1
>
> 首次整理：2026-09-06
>
> 当前代码基线：Git `a774524` 加当前工作区内尚未提交的 Combat 实现与 Player 控制状态机 Step 0–2
>
> Unity 版本：`6000.3.23f1`
>
> 关联文档：`ProjectPlan.md`、`PlayerControlModulePlan.md`、`PlayerControlStateMachineRefactorPlan.md`、`CombatAndBasicAttackModulePlan.md`、`ProjectStandards.md`

---

## 0. 文档定位与阅读方式

本文档是核心 Gameplay **当前实际实现**的技术说明，不是远期计划。计划文档回答“以后要做什么”，本文档回答：

1. 仓库里现在真实存在什么。
2. 游戏运行时每帧发生什么。
3. Player Control、Action State、Combat、Damage、Health 如何连接。
4. 当前“本地权威”已经做到什么，尚未做到什么。
5. 修改代码时还需要同步修改哪些资产、测试和文档。

文档中的状态含义：

- **已实现并已装配**：代码和磁盘中的 Prefab/Scene 引用都存在。
- **代码已实现、资产未装配**：脚本可编译、测试代码存在，但当前 Prefab/Scene 不能直接形成可玩闭环。
- **仅规划**：计划文档提到，当前仓库没有对应运行时代码。

如果本文档与计划文档对“当前完成度”的描述不一致，以代码、Prefab、Scene 的实际内容和本文档的“当前状态”章节为准。

---

## 1. 当前项目结论

### 1.1 一句话概括

项目已经完成了玩家移动/冲刺的代码与场景基础，并完成了第一版普通攻击从“输入意图”到“命中、扣血、死亡事件”的代码闭环；但是新增 Combat 组件尚未写入 `PF_Player.prefab` 和 `SampleScene.unity`，训练假人 Prefab/场景对象也不存在，所以当前磁盘版本还不是可直接操作的战斗场景。

### 1.2 当前完成度矩阵

| 模块 | 当前状态 | 实际内容 |
| --- | --- | --- |
| 工程基础 | 已实现并已装配 | Unity 6.3、URP、Input System、asmdef、EditMode/PlayMode 测试程序集 |
| 玩家输入 | 已实现并已装配 | 键鼠/手柄 Move、Attack、Interact、Dash 意图采集 |
| 玩家移动 | 已实现并已装配 | 相机相对移动、`CharacterController`、重力、贴地、碰撞 |
| 玩家朝向 | 已实现并已装配 | Idle/Move 时随移动方向转向，Dash/Attack 时使用锁定方向 |
| Player 控制 FSM | 已完成并接线 | `Disabled / Idle / Move / Dash / Attack` 互斥状态、完整输出和测试 |
| 基础 Combat Core | 代码已实现 | 身份、阵营、伤害上下文、伤害结果、纯伤害计算 |
| 普通攻击 | 玩家侧已实现并装配 | 请求、Gateway、阶段、近战查询、过滤、去重、伤害提交；场景尚无训练目标 |
| 生命/死亡 | 代码已实现、资产未装配 | `Health` 唯一写入口，`Damaged / Died` 事件 |
| Combat 表现 | 代码已实现、资产未装配 | 可选 Animator 参数和命中特效 Presenter |
| 训练假人 | 仅脚本已实现 | `TrainingDummy` 存在，但没有 `PF_TrainingDummy.prefab` 或场景实例 |
| 敌人 AI、波次、Boss、技能、结算 | 仅规划 | 当前没有对应运行时代码 |
| 联机 | 仅预留边界 | 没有 NGO、RPC、NetworkVariable、网络 Spawn 或同步逻辑 |
| UI、存档、Addressables、热更新 | 仅规划 | 当前没有对应业务程序集与实现 |

### 1.3 当前场景的阻塞问题

`BattleSandboxInstaller.Start()` 现在要求以下六个引用全部非空：

```text
PlayerController
LocalPlayerInputSource
Gameplay Camera
TopDownCameraController
BasicAttackController
LocalCombatCommandGateway
```

但磁盘中的 `SampleScene.unity` 只保存了前四个旧引用；`PF_Player.prefab` 也没有 `CombatantIdentity`、`BasicAttackController` 和 `LocalCombatCommandGateway`。因此 Installer 会在 `Start()` 直接报错并返回，`PlayerController.Initialize()` 不会执行，玩家不会进入可控制状态。

这不是 Combat 算法没有实现，而是 **代码集成已经前进，Unity 资产装配尚未同步**。

---

## 2. 总体架构

### 2.1 程序集依赖

```text
Game.Tests.EditMode ─┐
                     ├──> Game.Runtime ───> Game.Core
Game.Tests.PlayMode ─┘

Game.Core
  - noEngineReferences = true
  - 不依赖 UnityEngine
  - 保存值类型、结果类型和纯伤害规则

Game.Runtime
  - 依赖 Game.Core、Unity.InputSystem
  - 保存 MonoBehaviour、ScriptableObject、物理查询、输入和表现协调
```

当前并不存在规划中的 `Game.UI`、`Game.Infrastructure`、`Game.Editor` 等业务程序集。

### 2.2 运行时调用方向

```text
设备输入
  ↓
LocalPlayerInputSource                 只采集/缓存意图
  ↓ IPlayerInputSource
PlayerController                       每帧协调者
  ├──> PlayerControlStateMachine       Idle/Move/Attack/Dash 仲裁与完整控制输出
  │      ├──> PlayerDashRuntime         Dash 时序、冷却和输入缓存
  │      └──> PlayerBasicAttackDriver  普通攻击命令桥接与阶段观察
  │              ↓
  ├────────> ICombatCommandGateway     攻击命令权威边界
  │      ↓
  │    LocalCombatCommandGateway       当前本地校验实现
  │      ↓
  │    BasicAttackController           攻击阶段与命中执行
  │      ├──> MeleeHitQuery             查找 Collider 候选
  │      └──> Health                    提交每个合法目标的伤害
  │              ↓
  │          DamageResolver             纯计算 DamageResult
  ├──> PlayerFacingController          应用最终朝向
  └──> PlayerMotor                     唯一 CharacterController.Move

已确认结果
  ├──> BasicAttackPresenter            攻击动画/VFX
  ├──> PlayerAnimationPresenter        移动/冲刺动画参数
  └──> TrainingDummy                   开发日志/重置
```

调用方向的核心原则是：**输入不改生命，表现不决定命中，Health 不找目标，状态机不算伤害。**

### 2.3 两种“结果”不要混淆

Combat 中存在两类不同层级的结果：

| 类型 | 回答的问题 | 产生位置 |
| --- | --- | --- |
| `CombatCommandResult` | “这次攻击命令允许开始吗？” | `LocalCombatCommandGateway` |
| `DamageResult` | “对这个具体目标实际扣了多少血、是否致死？” | `DamageResolver`，由 `Health` 应用 |

一次攻击命令可以被接受，但最终没有任何 `DamageResult`，例如挥空、只碰到友军、目标已死亡或候选缓冲区没有包含敌人。

---

## 3. 代码与资源目录

### 3.1 `Assets/ProjectRelay/Scripts/Core/Runtime`

| 类型 | 职责 |
| --- | --- |
| `StableId` | 跨资源、存档、网络可传递的稳定字符串 ID；空白值无效 |
| `CombatantId` | 单局运行期战斗单位 ID；`0` 无效 |
| `Faction` | `None / Player / Enemy / Neutral` 阵营值 |
| `DamageType` | 当前只有 `Physical` |
| `DamageContext` | 一次伤害请求的来源、目标、阵营、攻击 ID、类型和基础伤害快照 |
| `DamageRejectionReason` | 伤害计算拒绝原因 |
| `DamageResult` | 实际伤害、前后生命、是否致死和拒绝原因 |
| `DamageResolver` | 不修改对象的纯伤害计算与合法性校验 |

### 3.2 `Assets/ProjectRelay/Scripts/Runtime/Input`

| 类型 | 职责 |
| --- | --- |
| `IPlayerInputSource` | Controller 依赖的输入抽象 |
| `LocalPlayerInputSource` | 持有 Input Actions 实例、订阅回调并缓存意图 |
| `Generated/ProjectRelayInputActions` | Input System 自动生成代码，不应手改 |

### 3.3 `Assets/ProjectRelay/Scripts/Runtime/Gameplay/Player`

| 类型 | 职责 |
| --- | --- |
| `PlayerMovementConfig` | 移速、转速、重力、冲刺参数的只读 Config |
| `PlayerMovementMath` | 二维输入到相机相对世界方向的纯转换 |
| `PlayerMotor` | 唯一执行 `CharacterController.Move` 的组件 |
| `PlayerFacingController` | 平滑旋转朝向节点并提供当前真实朝向 |
| `PlayerController` | 每帧读取输入并编排所有玩家子模块 |
| `PlayerAnimationPresenter` | 把实际速度和 Dash 状态写给可选 Animator |

`Player/Control` 下是当前唯一的角色控制状态机：

| 类型 | 职责 |
| --- | --- |
| `PlayerControlStateId`、`PlayerControlTransitionReason` | 提供稳定状态 ID 和显式转移原因 |
| `PlayerControlInput`、`PlayerControlOutput`、`PlayerMovementResult` | 隔离每帧输入、完整控制输出与 Motor 回报 |
| `PlayerControlTransition`、`PlayerControlTransitionRequest` | 区分已接受转移与状态提交的无副作用请求 |
| `PlayerControlContext` | 保存共享只读 Config、攻击 Driver、Dash Runtime、累计时间和当前输出 |
| `PlayerControlState` | 定义状态统一的 `Enter / Tick / CreateOutput / Exit` 契约 |
| `PlayerDisabledState`、`PlayerIdleState`、`PlayerMoveState` | 实现禁用、站立和普通移动规则 |
| `PlayerAttackState`、`PlayerDashState` | 实现基础攻击占用规则和 Dash 控制规则 |
| `PlayerBasicAttackDriver` | 隔离控制 FSM 与攻击 Gateway/执行器 |
| `PlayerDashRuntime` | 独立保存 Dash 方向、时序、冷却和输入缓存 |
| `PlayerControlStateMachine` | 注册状态、校验转移并统一执行生命周期和通知 |

`PlayerController` 只创建和驱动这一套 FSM；旧动作状态机、旧状态枚举和旧约束快照已经删除。

### 3.4 `Assets/ProjectRelay/Scripts/Runtime/Gameplay/Combat`

| 类型 | 职责 |
| --- | --- |
| `CombatantIdentity` | 把场景对象映射为 `CombatantId + Faction` |
| `Health` | 当前生命的唯一写入口和死亡一次性语义 |
| `BasicAttackConfig` | 普攻 ID、伤害、阶段时间、范围、LayerMask、移动倍率 |
| `BasicAttackRequest` | 玩家提交给权威边界的不可变攻击命令 |
| `CombatCommandResult` | 命令接受/拒绝结果 |
| `ICombatCommandGateway` | Controller 与具体权威实现之间的接口 |
| `LocalCombatCommandGateway` | 当前离线本地权威实现 |
| `BasicAttackPhase` | `Idle / Windup / Active / Recovery / Cooldown` |
| `BasicAttackController` | 独立推进攻击阶段、命中查询、伤害提交和冷却 |
| `MeleeHitQuery` | 复用固定 Collider 数组的非分配球形查询 |
| `BasicAttackPresenter` | 监听已确认阶段/伤害并播放可选表现 |
| `TrainingDummy` | 训练假人的开发日志和显式重置入口 |

### 3.5 场景与配置

- `SampleScene.unity`：当前唯一 Build Scene，包含 Main Camera、CameraRig、光照、Volume、地面、障碍、玩家实例和 Installer。
- `PF_Player.prefab`：已有 CharacterController、Motor、Controller、InputSource、Facing 和简单 Mesh/Collider；Combat 组件尚未写入。
- `PlayerMovement_Default.asset`：玩家移动配置，已经被 `PF_Player` 引用。
- `BasicAttack_Default.asset`：普攻配置文件已经存在，但尚未被 Prefab/Scene 引用。
- `PF_TrainingDummy.prefab`：计划文档提到，但当前不存在。

---

## 4. 初始化、启停与对象连接

### 4.1 组件 `Awake`

关键初始化行为：

1. `LocalPlayerInputSource.Awake` 创建一份独占的 Input Actions 实例。
2. `PlayerMotor.Awake` 缓存 CharacterController。
3. `PlayerFacingController.Awake` 确定需要旋转的 Transform。
4. `PlayerController.Awake` 只缓存同对象组件；完整 FSM 在战斗命令链准备好后统一创建。
5. `CombatantIdentity.Awake` 在没有外部权威 ID 时分配进程内递增的临时 ID。
6. `Health.Awake` 缓存 Identity，并仅在首次初始化时恢复满血。

### 4.2 Installer 的预期 `Start` 顺序

资产装配完成后，`BattleSandboxInstaller.Start()` 应按以下顺序连接对象：

```text
LocalCombatCommandGateway.Initialize(BasicAttackController)
  ↓
PlayerController.Initialize(
    InputSource,
    GameplayCamera,
    BasicAttackController,
    ICombatCommandGateway)
  ↓
BasicAttackController.Initialize(BasicAttackConfig)
  ↓
new PlayerBasicAttackDriver(BasicAttackController, Gateway)
  ↓
new PlayerControlStateMachine(PlayerMovementConfig, Driver)
  ↓
TopDownCameraController.Bind(Player Transform)
  ↓
PlayerController.SetControlEnabled(true)
```

最重要的连接是：`PlayerController` 是 `PlayerControlStateMachine` 的唯一创建者和逐帧驱动者；`BasicAttackController` 不反向持有 FSM。两者只通过 `PlayerBasicAttackDriver` 的命令提交和只读阶段观察连接。

### 4.3 禁用与清理

禁用控制或组件时会清理：

- 输入 Action Map 和未消费输入。
- Motor 的垂直速度、水平速度、碰撞标记。
- Dash 方向、时长、冷却和输入缓存。
- Attack 阶段、阶段时间、锁定方向、命中集合和动作锁。
- Camera 的跟随目标和平滑速度。

多个组件可能重复调用 Reset，但这些入口按幂等方式设计，重复清理不会重复扣血或留下攻击锁。

---

## 5. Player Control 逐层解释

### 5.1 输入层在做什么

`LocalPlayerInputSource` 将设备输入转成两种数据：

- 连续值：`Move`，一直保存当前二维输入。
- 一次性意图：`AttackPressed`、`InteractPerformed`、`DashPressed`，读取后立即清零。

当前绑定：

| Action | 键鼠 | 手柄 |
| --- | --- | --- |
| Move | WASD / 方向键 | 左摇杆 |
| Attack | 鼠标左键 | 右肩键 |
| Interact | E | 西侧按钮；该 Action 整体使用 Hold interaction |
| Dash | Space | 左肩键 |

输入回调本身不启动攻击或移动角色，只更新缓存。真正的玩法执行统一发生在 `PlayerController.Update()`。

### 5.2 相机相对移动

`PlayerMovementMath.GetCameraRelativeDirection` 的计算是：

```text
flatForward = Camera.forward 投影到 XZ 平面
flatRight   = Camera.right 投影到 XZ 平面
worldDir    = flatForward * input.y + flatRight * input.x
worldDir    = ClampMagnitude(worldDir, 1)
```

最后一步保证斜向输入不会比直线移动更快。相机方向接近垂直、投影失效时有默认方向兜底。

### 5.3 每帧固定执行顺序

`PlayerController.Update()` 是当前玩家 Gameplay 的总调度器：

```text
1. 检查初始化依赖
2. 一次性读取 Move、DashPressed、AttackPressed、deltaTime
3. 把二维输入转换为世界空间方向
4. 构造不可变 `PlayerControlInput`
5. `PlayerControlStateMachine.Tick(input, deltaTime)`，内部推进攻击阶段并完成状态转移
6. 读取唯一 `PlayerControlOutput`
7. 根据 Output 更新锁定朝向或普通移动朝向
8. `PlayerMotor.TickMovement(...)`，本帧只 Move 一次
9. 把实际速度和碰撞结果作为 `PlayerMovementResult` 报告给状态机
```

这个顺序带来几个明确规则：

- 同帧同时按 Dash 和 Attack：Dash 优先。
- Attack 过程中按 Dash：Dash 被丢弃，不会缓存到攻击结束。
- Dash/Attack 期间按 Attack：意图被消费但不排队。
- Attack 冷却期间玩家状态已经回到 Idle 或 Move；可以移动、可以 Dash，但 Attack 会由攻击控制器拒绝。
- Recovery 恰好在本帧结束时，状态会在同一 Tick 回到 Idle 或 Move，不产生额外一帧动作锁。

### 5.4 为什么需要 `PlayerControlStateMachine`

它解决的不是动画切换，而是“玩家同一时刻能不能做另一件互斥动作”。当前状态：

```text
Disabled --启用控制--> Idle
Idle <--------移动输入--------> Move
Idle/Move --Dash 请求合法--> Dash --时间结束/撞墙--> Idle/Move
Idle/Move --Attack 被接受--> Attack --Recovery 完成--> Idle/Move
任意状态 --禁用/重置--> Disabled
```

状态约束：

| Action State | 移动输出 | 可随输入转向 | 可 Dash | 可 Attack | 锁定朝向 |
| --- | --- | --- | --- | --- | --- |
| `Disabled` | 0 | 否 | 否 | 否 | 无 |
| `Idle` | 0 | 是 | 是 | 是 | 无 |
| `Move` | `MoveDir × MoveSpeed` | 是 | 是 | 是 | 无 |
| `Dash` | `DashDir × DashSpeed` | 否 | 否 | 否 | Dash 方向 |
| `Attack` | `MoveDir × MoveSpeed × AttackMultiplier` | 否 | 否 | 否 | Attack 方向 |

特别注意：Attack 不是完全禁止移动。当前默认倍率为 `0.5`，玩家仍可以用输入改变位移方向，但朝向与攻击查询方向保持锁定。因此当前行为允许“面朝攻击方向、以半速侧移或后退”。

### 5.5 Dash 细节

- 有移动输入时使用移动方向；无输入时使用角色当前真实朝向。
- Dash 输入可缓存 `0.1s`，主要用于冷却即将结束时的提前按键。
- Dash 期间速度固定为 `12m/s`，不受后续移动输入影响。
- 持续 `0.18s` 后按当前移动输入返回 Idle 或 Move，并开始 `0.8s` 冷却。
- 如果发生侧面碰撞，沿 Dash 方向的实际速度不高于目标速度的 50%，可提前结束 Dash。
- Dash 冷却只阻止下一次 Dash，不阻止 Attack。

### 5.6 Motor 与朝向

`PlayerMotor` 是 Transform 位移的唯一执行者。它合并水平速度和垂直速度，每帧调用一次 `CharacterController.Move`，再用实际位移反算水平速度供动画和撞墙逻辑使用。

`PlayerFacingController` 只旋转配置的视觉朝向节点。Idle/Move 时朝最终移动方向旋转；Dash 和 Attack 时使用状态机提供的锁定方向；没有有效方向时保持当前朝向。

`PlayerAnimationPresenter` 使用 Motor 的实际速度设置：

- `Speed`：相对普通移速或 Dash 移速的 `0..1` 值。
- `IsDashing`：当前 `PlayerControlStateId` 是否为 Dash。

它目前未装配到 `PF_Player`，没有 Animator 时会自动禁用，不影响规则。

`TopDownCameraController` 在 `LateUpdate` 中跟随绑定目标，只修改 CameraRig 的位置，不搜索玩家、也不控制相机旋转。场景中的 Main Camera 是 CameraRig 子节点；当前场景偏移为 `(5.36, 6.94, 0)`，平滑时间为 `0.08s`。Installer 绑定目标时会先立即对齐一次，之后再用 `SmoothDamp` 跟随，从而避免首次进入场景时从旧位置缓慢滑入。

### 5.7 当前控制状态机边界

`PlayerControlStateMachine` 注册 `Disabled`、`Idle`、`Move`、`Dash` 和 `Attack`，明确行为为：

```text
Disabled --SetEnabled(true)--> Idle
Idle --有效移动输入--> Move
Move --移动输入归零/失效--> Idle
Idle/Move --Dash 被接受--> Dash --完成/受阻--> Idle/Move
Idle/Move --Attack 被接受--> Attack --Recovery 完成--> Idle/Move
任意已启用状态 --禁用/ForceReset--> Disabled
```

它接收一次性 `PlayerControlInput`，在同一个 `Tick` 内完成合法转移并返回完整 `PlayerControlOutput`。移动输入保留 `0..1` 模拟量强度，超长对角输入限制为 1，NaN/Infinity 输入与非法 Delta 按安全零值处理。状态只返回 `PlayerControlTransitionRequest`，只有状态机能够写入当前状态；成功转移严格按 `Exit → CurrentState 写入 → Enter → StateChanged` 执行。`PlayerController` 每帧只调用一次公开 `Tick`，旧状态机已经删除。

---

## 6. Combat 完整调用链

### 6.1 从按键到命令被接受

```text
鼠标左键 / 手柄右肩键
  ↓ performed
LocalPlayerInputSource.mAttackPressed = true
  ↓ ConsumeAttackPressed()
PlayerControlStateMachine 在 Idle/Move 中按固定优先级仲裁
  ↓
PlayerBasicAttackDriver 创建 BasicAttackRequest
  - SourceId：BasicAttackController 所属 CombatantIdentity.Id
  - AttackId：BasicAttackConfig.AttackId
  - AttackDirection：PlayerFacingController.CurrentFacingDirection
  - RequestSequence：Driver 生命周期内递增的非零序号
  ↓
ICombatCommandGateway.SubmitBasicAttack(request)
  ↓
LocalCombatCommandGateway 校验请求
  ↓
BasicAttackController.TryStartAttack(direction)
  ↓
进入 Windup，返回 CombatCommandResult.Accepted
  ↓
PlayerControlStateMachine 进入 Attack
```

攻击方向来自 **角色当前朝向**，不是鼠标世界坐标，也不是右摇杆瞄准方向。当前输入资产没有独立 Aim Action。

### 6.2 Gateway 为什么存在

`PlayerControlStateMachine` 不直接调用 `BasicAttackController.TryStartAttack()`；它通过 `PlayerBasicAttackDriver` 和 `ICombatCommandGateway` 提交。这个边界把控制仲裁、命令验证和攻击执行分开。

当前本地 Gateway 依次校验：

1. Gateway 和攻击控制器是否就绪、启用。
2. `SourceId` 是否有效并与绑定的攻击者一致。
3. `AttackId` 是否有效并与控制器配置一致。
4. XZ 平面攻击方向是否有限且非零。
5. 请求序号是否非零并大于上一次 **已接受** 的序号。
6. 当前攻击阶段和冷却是否允许启动；控制状态合法性已由 FSM 保证。

任一步失败都返回带明确原因的 `CombatCommandResult`，不会重置当前攻击、改变锁定方向或刷新冷却。

序号的当前语义是“防止同一份已接受命令再次被接受”，不是完整网络防重放协议。被 ActionNotAllowed 拒绝的序号不会写入 `mLastAcceptedRequestSequence`。

### 6.3 为什么 Control State 和 Attack Phase 要分开

这是最容易混乱的部分。两者描述的是不同维度：

| 系统 | 关心的问题 | 状态 |
| --- | --- | --- |
| `PlayerControlStateMachine` | 玩家当前由哪种互斥控制规则生成移动和朝向 | Disabled、Idle、Move、Dash、Attack |
| `BasicAttackController` | 这一次普通攻击内部进行到哪个时刻 | Idle、Windup、Active、Recovery、Cooldown |

二者关系：

```text
Attack Phase:  Idle → Windup → Active → Recovery → Cooldown → Idle
Control State: Idle/Move →──── Attack ─────────→ Idle/Move ───────
```

- Windup、Active、Recovery 三个阶段共同占用 `Attack` 控制状态。
- Recovery 结束时释放动作锁，控制状态按当前输入回到 Idle 或 Move。
- Cooldown 仍由攻击控制器计时，但不再占用玩家动作，所以可移动和 Dash。
- Control State 不知道 Windup/Active/Recovery 的具体时间，也不知道伤害和范围。
- Attack Controller 不自己计算移动速度；`PlayerAttackState` 从 Config 和 Driver 读取倍率与锁定方向。

这种拆分避免把状态膨胀成 `MovingWindup`、`IdleWindup`、`DashingRecovery` 等组合枚举。

### 6.4 普通攻击时间轴

当前 `BasicAttack_Default.asset`：

| 阶段 | 时长 | 行为 |
| --- | ---: | --- |
| Windup | `0.15s` | 已锁定方向和动作，尚不查询目标 |
| Active | `0.10s` | **进入阶段时只查询一次**，不是每帧查询 |
| Recovery | `0.25s` | 命中结束，仍保持 Attack 控制状态 |
| Cooldown | `0.40s` | 已回到 Idle/Move，但不能开始下一次普攻 |

因此：

- 动作锁总时长约 `0.50s`。
- 从接受攻击到再次允许攻击约 `0.90s`。
- Active 的 `0.10s` 目前主要服务阶段/表现，不代表持续命中窗口。

`AdvancePhases` 会消费整份 `deltaTime`。如果低帧率的一帧跨过多个阶段，它会顺序进入每个阶段，保证 Active 的入口副作用不会被跳过。全部阶段配置为零时也会立即前进，并由单帧最多 8 次转移保护防止死循环。

### 6.5 Active 阶段如何找目标

进入 Active 时计算：

```text
queryCenter = AttackOrigin.position
            + LockedAttackDirection * ForwardOffset

Physics.OverlapSphereNonAlloc(
    queryCenter,
    HitRadius,
    reusableColliderBuffer,
    TargetLayerMask,
    QueryTriggerInteraction.Collide)
```

默认配置：

- 前移距离：`1.0m`
- 球半径：`0.75m`
- 候选数组容量：`16`
- LayerMask：全部 Layer
- Trigger：参与查询

查询本身只返回 Collider。`BasicAttackController` 再按顺序过滤：

1. 空 Collider。
2. 攻击者自身 Transform 层级中的 Collider。
3. 找不到父级 `Health` 的 Collider。
4. Health 未启用或已经死亡。
5. 目标 Identity 无效。
6. 目标 ID 与攻击者相同。
7. 目标阵营为 None，或与攻击者阵营相同。
8. 本次攻击已经处理过同一个 `Health`。

第 8 步使用复用的 `HashSet<Health>`，所以一个角色有多个 Collider 也只扣一次血。

如果查询结果刚好填满 16 个位置，控制器只警告一次，不会在命中帧扩容。由于默认 LayerMask 是全部 Layer，环境 Collider 也会占用容量；正式装配应设置专用目标层。

### 6.6 从命中候选到扣血

合法目标会生成 `DamageContext`：

```text
SourceId       = 攻击者运行时 ID
TargetId       = 目标运行时 ID
SourceFaction  = 攻击者当前阵营
TargetFaction  = 目标当前阵营
AttackId       = basic-attack
DamageType     = Physical
BaseDamage     = 25
```

然后调用 `targetHealth.TryApplyDamage(context, out result)`：

```text
Health 读取当前身份、阵营、CurrentHealth、IsDead
  ↓
DamageResolver.Resolve(...) 进行纯计算
  ↓
若 result.IsApplied == false：不改状态、不发事件
  ↓
CurrentHealth = result.HealthAfter
若首次致死：IsDead = true
  ↓
Damaged(result)
  ↓ 首次致死才有
Died(result)
```

有效伤害公式：

```text
requestedDamage = max(0, BaseDamage)
actualDamage    = min(requestedDamage, CurrentHealth)
healthAfter     = max(0, CurrentHealth - actualDamage)
killed          = 伤害前存活 && healthAfter == 0
```

`DamageResolver` 还会拒绝：目标不匹配、来源无效、攻击 ID 无效、NaN/Infinity 伤害、非法生命快照、非正伤害和已死亡目标。拒绝结果不会修改 Health。

### 6.7 阵营规则当前实际含义

当前友军过滤非常简单：`TargetFaction == SourceFaction` 时跳过，否则可以伤害。

这意味着：

- Player 不伤害 Player。
- Enemy 不伤害 Enemy。
- Player 可以伤害 Enemy 和 Neutral。
- Enemy 可以伤害 Player 和 Neutral。
- Neutral 也会把 Player、Enemy 视为不同阵营。

`DamageResolver` 本身不判断友军关系；友军过滤由权威攻击执行器完成。以后如果需要队伍、召唤物、PVP、可破坏中立物，应把“是否敌对”抽成明确规则，而不是继续依赖枚举不相等。

### 6.8 事件和表现

规则层发布：

- `BasicAttackController.PhaseChanged(previous, next, attackId)`
- `BasicAttackController.DamageConfirmed(result)`
- `Health.Damaged(result)`
- `Health.Died(result)`

`BasicAttackPresenter` 可选消费以下 Animator 参数：

- `IsAttacking`：Windup/Active/Recovery 为 true。
- `AttackPhase`：当前 `BasicAttackPhase` 整数值。
- `AttackHit`：每个已确认伤害触发一次 Trigger。

它还可以播放 `ParticleSystem`。删除 Presenter、Animator 或 VFX 不会改变命中和生命。多目标命中时 `DamageConfirmed` 会对每个目标发布一次，因此当前 Presenter 也会收到多次命中回调。

### 6.9 `TrainingDummy`

`TrainingDummy` 不是伤害规则本身，只是开发用组合组件：

- 读取同对象 `CombatantIdentity` 和 `Health`。
- 订阅 Damaged/Died 输出 Console 日志。
- 提供 `ResetDummy()` 显式恢复满血。
- 不做 AI、不播放正式死亡表现、不决定波次。

---

## 7. “本地权威”与未来 Host 权威

### 7.1 当前到底是什么权威

当前是 **离线进程内的本地权威**：

- 输入方构造 `BasicAttackRequest`。
- `LocalCombatCommandGateway` 在同一进程同步校验。
- 同一台机器上的 `BasicAttackController` 做物理查询。
- 同一台机器上的 `Health` 写入生命。

这里“权威”的意思是：代码规定只有这条执行链可以决定攻击是否开始并修改生命；它不是网络安全意义上的服务器权威，也不能防止修改客户端代码。

当前项目没有安装 Netcode for GameObjects，也没有：

- `NetworkObject` / `NetworkBehaviour`
- ServerRpc/ClientRpc 或自定义消息
- 网络对象 Spawn/Despawn
- 生命、状态或阶段同步
- 客户端预测、回滚、延迟补偿
- 断线、重连和 Host 迁移

### 7.2 已经为联网保留的部分

- 攻击请求是值类型，不携带 GameObject、Transform、Animator 或目标引用。
- Controller 依赖 `ICombatCommandGateway`，可替换本地/网络实现。
- 请求包含 SourceId、AttackId、方向和序号。
- 伤害结果是完整快照，适合记录、诊断或复制表现。
- `StableId` 与运行时 `CombatantId` 分开。
- 表现只消费已确认事件。

### 7.3 联网时不能直接照搬的部分

未来 `NetworkCombatCommandGateway` 不应简单把客户端数据原样转发给本地 Controller。Host 仍需：

| 当前本地行为 | 未来 Host 应做的事 |
| --- | --- |
| SourceId 来自本地组件 | 根据发送连接/Owner 反查真实 CombatantId |
| AttackId 来自客户端请求 | 校验该角色当前装备/允许使用的攻击 |
| 方向由客户端给出 | 校验有限值、角度和允许的转向范围 |
| Sequence 只记录最后接受值 | 按连接/玩家维护已处理序号与重放策略 |
| 本地 `Time.deltaTime` 推进阶段 | Host 维护权威阶段或权威生效时刻 |
| 本机 Physics 查询 | Host 世界执行查询；若需要再设计延迟补偿 |
| Health 直接写本地字段 | Host 写权威生命并向客户端复制 |
| C# event 驱动表现 | 客户端根据复制状态/确认消息播放表现 |

当前请求没有客户端 Tick、时间戳或瞄准点，因此它只适合项目计划中的简单 Host 权威近战，不支持精确回滚命中。

---

## 8. 当前配置与正确装配方式

### 8.1 玩家移动配置

| 字段 | 当前值 |
| --- | ---: |
| MoveSpeed | `5` |
| RotationSpeed | `720°/s` |
| Gravity | `-25` |
| MaximumFallSpeed | `-40` |
| GroundedVerticalSpeed | `-2` |
| DashSpeed | `12` |
| DashDuration | `0.18s` |
| DashCooldown | `0.8s` |
| DashInputBuffer | `0.1s` |
| EndDashWhenBlocked | `true` |

`PlayerMovement_Default.asset` 中的 `m_EditorClassIdentifier` 仍保留旧类名 `PlayerMovementDefinition`，脚本 GUID 和字段仍指向当前 `PlayerMovementConfig`。这是重命名后的序列化残留，建议在 Unity 中确认资源引用无 Missing 后重新保存资产。

### 8.2 普通攻击配置

| 字段 | 当前值 |
| --- | ---: |
| AttackId | `basic-attack` |
| BaseDamage | `25` |
| Windup / Active / Recovery | `0.15 / 0.10 / 0.25s` |
| Cooldown | `0.40s` |
| ForwardOffset | `1.0m` |
| HitRadius | `0.75m` |
| TargetLayerMask | 全部 Layer |
| HitBufferCapacity | `16` |
| MovementSpeedMultiplier | `0.5` |

`BasicAttack_Default.asset` 保留了原脚本 `.meta` GUID，并已由 Unity 的 `AssetDatabase` 测试确认可加载为有效 `BasicAttackConfig`。Unity 原生 `ForceReserializeAssets` 后，文本中的 `m_EditorClassIdentifier` 仍保留历史类名，但 MonoScript GUID 和全部字段有效；不要为清理该提示字段手写资产 YAML。

### 8.3 玩家 Prefab 应有的组件

```text
PF_Player (Root, Player Layer)
├── CharacterController
├── CombatantIdentity                  Faction = Player
├── PlayerMotor
├── PlayerFacingController             FacingTransform = Mesh/视觉根
├── LocalPlayerInputSource
├── PlayerController                   绑定 MovementConfig 与同对象组件
├── BasicAttackController              绑定 BasicAttackConfig、Identity、AttackOrigin
├── LocalCombatCommandGateway          绑定 BasicAttackController
├── PlayerAnimationPresenter           可选
└── BasicAttackPresenter               可选
```

建议建立独立 `AttackOrigin` 子节点，放在角色身体合适高度。当前是 3D `OverlapSphere`，Origin 的 Y 高度会直接影响能否命中。

### 8.4 训练假人 Prefab 应有的组件

```text
PF_TrainingDummy
├── CombatantIdentity                  Faction = Enemy
├── Health                             MaximumHealth = 100
├── TrainingDummy
└── 至少一个 3D Collider               放在目标查询 Layer
```

### 8.5 Scene Installer 应连接

- Player 实例的 `PlayerController`
- Player 实例的 `LocalPlayerInputSource`
- Main Camera
- CameraRig 的 `TopDownCameraController`
- 同一 Player 的 `BasicAttackController`
- 同一 Player 的 `LocalCombatCommandGateway`

此外应放置至少一个 Enemy 训练假人，并设置专用目标 Layer/LayerMask，避免地面和环境 Collider 填满 16 个候选槽位。

---

## 9. 测试与验证状态

### 9.1 已有 EditMode 测试

- `DamageResolverTests`：正常伤害、过量伤害、零伤害、目标不匹配、死亡目标。
- `HealthTests`：致死与事件一次性、Reset 新生命周期、错误目标不改生命。
- `BasicAttackControllerTests`：阶段、方向锁、跨阶段大 delta、零时长、防重入、冷却、中断、重复初始化、30/60/120 FPS。
- `LocalCombatCommandGatewayTests`：有效/重复序号、错误来源、错误攻击 ID、执行中拒绝。
- `BasicAttackConfigAssetTests`：保留脚本 GUID 的类型改名后，默认 Config 资源仍可加载且数据有效。
- `PlayerControlStateMachineTransitionTests`：初始状态、完整显式合法边、状态变化通知。
- `PlayerControlStateMachineLifecycleTests`：禁用清理、重复重置和非法 Delta。
- `PlayerControlStateMachineMovementTests`：Idle/Move、同帧速度、模拟量、对角限幅、停步和非法输入。
- `PlayerControlStateMachineDashTests`：方向锁定、持续时间、冷却、输入 Buffer、侧面阻挡和大 Delta。
- `PlayerControlStateMachineAttackTests`：攻击约束、同帧优先级、互斥输入、完成和禁用重置。
- `PlayerControlAssetWiringTests`：Player Prefab 组件/Config 与 SampleScene Installer 引用完整性。

### 9.2 已有 PlayMode 测试

- 多 Collider 目标只受伤一次，且不需要 Presenter。
- 自身、友军、死亡、范围外目标被过滤。
- 攻击中 Disable/Enable 能清理并再次攻击。
- SampleScene 真实加载后，Installer 能初始化 Player 且新控制 FSM 进入 Idle。

这些 PlayMode 测试运行时动态创建对象，不依赖项目里的 `PF_Player`、`SampleScene` 或训练假人 Prefab，因此测试通过不等价于 Scene 装配完成。

### 9.3 本次文档审计验证

- Unity 6000.3.23f1 完整导入并编译重构后的运行时和测试程序集，无 C# 编译错误。
- 删除旧状态机前的完整 EditMode 回归：`62/62` 通过，用于证明新旧行为测试在迁移点同时成立。
- 完成 Player Prefab/Scene 接线后的最终 EditMode 回归：`53/53` 通过。
- 最终 PlayMode 回归：`4/4` 通过，覆盖物理命中、过滤、Disable/Enable 和 SampleScene 初始化。
- `BasicAttack_Default.asset` 已通过 `AssetDatabase.LoadAssetAtPath<BasicAttackConfig>` 验证类型和数据有效。
- `PlayerControlAssetWiringTests` 确认 Player Prefab 的 Combat 组件/Config 和 SampleScene Installer 引用完整。
- Windows 64 位 Development Build 成功；生成的 Player 稳定运行 6 秒，日志无控制或战斗初始化错误，临时构建产物随后已清理。

---

## 10. 已知问题、设计限制与优先级

### 10.1 P0：阻塞当前战斗命中闭环

1. 没有 `PF_TrainingDummy.prefab`，场景中也没有可受伤目标。
2. 没有独立 Enemy/Hitbox Layer；当前攻击查询全部 Layer。

Player 控制和攻击发起链已经装配完成；在上述目标与 Layer 问题完成前，仍不应把 M2 描述为“BattleSandbox 手动命中闭环完成”。

### 10.2 当前明确行为，不一定是 Bug

- Attack 使用角色当前朝向，不使用鼠标指向。
- 攻击中可半速移动，但不可转向。
- Attack/Dash 输入在不允许执行时通常被丢弃；只有 Dash 有冷却输入缓存。
- Active 只在入口查询一次，不是持续 `0.1s` 多帧命中。
- 不同 Faction 一律视为可攻击，Neutral 不是默认友方。
- Trigger Collider 会参加查询。
- Cooldown 不占用 Attacking 状态。
- 一个攻击命中多个目标时会产生多个 `DamageConfirmed`。

### 10.3 后续扩展前需要注意

- `PlayerControlStateMachine` 已接入 `PlayerController`，旧状态机类型已经删除；后续不得重新引入双权威兼容路径。
- 当前 Gateway 只支持一种 `BasicAttackRequest`，不是通用技能命令总线。
- 当前 PlayerController 直接持有一个 BasicAttackController，尚不支持武器切换、连招表或多攻击定义。
- `Health.TryApplyDamage` 和 `ResetToFull` 的公开 API 本身不验证调用者是否权威，权威性依赖工程调用纪律。
- 友军过滤不在 DamageResolver，新增其他伤害入口时必须重复或抽取关系规则。
- CombatantId 的本地自增只保证单进程运行期唯一；联网必须由 Host 分配或映射。
- 物理查询容量固定且结果顺序不保证；LayerMask 过宽时可能漏掉真正目标。
- 暂停使用 `Time.deltaTime == 0` 能冻结阶段，但若不禁用输入，暂停帧仍可能消费并开始一个停在 Windup 的攻击。
- 当前使用 Unity 3D `CharacterController`、Collider、Physics，不是 Physics2D。

---

## 11. 如何在现有架构上继续开发

### 11.1 新增敌人攻击

敌人 AI 应只产生攻击意图。最终仍应复用或对齐：

```text
EnemyBrain
→ EnemyController / Authority Gateway
→ Attack Controller
→ Hit Query
→ DamageContext
→ Player Health
```

不要让 EnemyBrain 直接调用 `playerHealth.TryApplyDamage`。在做敌人之前还需要给玩家装配 `CombatantIdentity + Health`，并明确玩家死亡如何进入 Action State/Battle Flow。

### 11.2 新增主动技能

不要把技能时间、范围、伤害继续塞进 `PlayerControlStateMachine`。后续技能应通过独立 Ability System 执行；控制 FSM 只表达角色在该动作期间的移动和朝向规则。

当第二种真实攻击出现后，再考虑把 BasicAttack 的公共阶段推进提取为小型 Action Executor。当前只有一种攻击，不需要提前做庞大通用技能框架。

### 11.3 新增受击与死亡

- `Health.Damaged` 只说明伤害已经应用，不应直接把整局判负。
- 角色受击表现监听 `DamageResult`。
- 是否进入 HitStun 由玩家/敌人的动作控制器决定。
- `Health.Died` 每个生命周期只发一次。
- Wave/Battle Flow 监听死亡结果，再决定存活计数和胜负。
- 死亡清理必须中断攻击、Dash、输入与表现订阅。

### 11.4 新增网络 Gateway

保留 `PlayerController → ICombatCommandGateway` 依赖方向。建议网络实现把 Owner 的请求发给 Host，由 Host 找到自己的权威执行器；不要在 `PlayerController`、`Health`、Presenter 中散落 `IsServer`/`IsOwner` 分支。

---

## 12. 文档同步规则

从本文档建立后，以下改动必须在同一个提交/变更中同步本文档：

| 改动类型 | 必须同步的章节 |
| --- | --- |
| 新增/删除 Gameplay 类型 | 第 1、2、3 章 |
| 修改初始化、Update 顺序或组件依赖 | 第 4、5 章 |
| 修改状态、转移、输入优先级 | 第 5、6 章 |
| 修改攻击请求、Gateway 或权威归属 | 第 6、7 章 |
| 修改伤害、阵营、Health、死亡语义 | 第 6 章 |
| 修改 ScriptableObject 字段或默认值 | 第 8 章 |
| 修改 Prefab/Scene 装配 | 第 1、3、8、10 章 |
| 新增/修改测试 | 第 9 章 |
| 完成已知问题或加入新限制 | 第 10 章 |

每次更新至少执行：

1. 更新文档顶部版本、日期和代码基线。
2. 检查“当前完成度矩阵”是否仍准确。
3. 检查数据流、状态图、配置表和 Prefab 组件清单。
4. 记录真实验证结果，不用“代码存在”代替“场景已验收”。
5. 在下方变更记录追加一行；重大架构决定另建 ADR。

### 12.1 变更记录

| 日期 | 版本 | 内容 |
| --- | --- | --- |
| 2026-09-06 | v1.1 | 记录 Player 控制状态机 Step 0–2：新增独立状态契约、Disabled/Idle/Move、完整移动输出和回归测试；明确尚未接管 PlayerController |
| 2026-09-06 | v1.0 | 首次按当前代码、测试、Prefab、Scene 和配置资产整理；明确 Combat 代码已完成但场景资产尚未装配 |
