# Project Relay 战斗基础与普通攻击模块开发计划

> 文档版本：v1.1
> 更新日期：2026-09-06
> 对应总体规划：M2 战斗基础与普通攻击
> 上位文档：`ProjectPlan.md`、`CoreGameplayDevelopmentPlan.md`、`ProjectStandards.md`
> 前置条件：M1 玩家控制基础已完成
> 当前进度：Step 1 代码已完成，Editor 配置待用户执行

---

## 1. 本次实现内容

### 1.1 需要完成

- 阵营、伤害上下文、伤害结果、伤害计算和生命状态。
- 普通攻击前摇、命中、后摇、冷却和动作锁。
- `Attacking` Action State 及移动、转向、Dash 约束。
- 本地 Combat Command Gateway，隔离输入和权威执行路径。
- 非分配近战范围检测、阵营过滤和单次攻击目标去重。
- 可受伤、死亡且死亡事件只触发一次的训练假人。
- 规则事件与动画/VFX 表现边界。
- EditMode、PlayMode 和 BattleSandbox 验收。

### 1.2 本次不实现

- 主动技能、投射物、Buff、暴击、护甲、元素和复杂伤害公式。
- 敌人 AI、玩家受伤、复活、掉落、波次和胜负。
- 冲刺无敌帧、HitStun、完整连招和蓄力攻击。
- NGO、RPC、网络对象池和客户端预测。
- 依赖 Animation Event 才能完成的权威命中逻辑。

---

## 2. 实现思路

### 2.1 数据流

```text
LocalPlayerInputSource.Attack
→ PlayerController
→ ICombatCommandGateway
→ LocalCombatCommandGateway
→ BasicAttackController
→ PlayerActionStateMachine：校验并进入 Attacking
→ Windup → Active → Recovery → Cooldown
→ MeleeHitQuery
→ DamageResolver
→ Health
→ Damaged / Died
→ Animation、VFX、调试表现
```

### 2.2 固定边界

- `PlayerController` 是 Attack 输入的唯一消费方，只转发请求和统一传递本帧 `deltaTime`。
- `PlayerActionStateMachine` 只仲裁互斥动作和移动约束，不做命中检测或伤害计算。
- `BasicAttackController` 管理一次攻击的阶段、锁定方向、冷却和中断清理。
- `LocalCombatCommandGateway` 是当前本地权威入口；以后替换网络实现时复用同一攻击执行链路。
- `MeleeHitQuery` 只返回候选目标；阵营、重复命中和死亡状态由权威规则再次校验。
- `DamageResolver` 是无场景依赖的纯计算；`Health` 是生命值唯一写入口。
- Animator、VFX 和音效只消费已确认状态或结果事件，删除表现组件不影响攻击结算。
- 攻击定义运行时只读；阶段计时、冷却和本次已命中目标属于 Runtime State。

### 2.3 单帧调度与攻击请求算法

`PlayerController.Update` 每帧只读取一次输入和 `Time.deltaTime`，按以下顺序调度：

```text
1. 缓存本帧 deltaTime、Move、DashPressed、AttackPressed
2. 推进已有 Dash、攻击阶段和冷却计时
3. 处理 Dash 请求
4. 仅在 Dash 未开始且状态仍为 Free 时提交 Attack 请求
5. 根据最终 Action State 计算水平速度和朝向约束
6. PlayerMotor 执行一次 CharacterController.Move
7. 将实际速度和碰撞结果报告给 Action State Machine
```

- 同一帧同时按下 Dash 和 Attack 时固定为 Dash 优先，避免执行顺序随组件 Update 顺序变化。
- `PlayerActionStateMachine` 将当前 `Tick` 拆为“推进状态时间”“尝试动作转移”“计算最终速度”三个明确步骤，使攻击转移可以在 Motor 执行前生效。
- PlayerController 将 Attack 请求和值类型上下文提交给 Gateway，不直接调用攻击执行器或 Health。
- `LocalCombatCommandGateway` 将请求同步交给当前玩家的 `BasicAttackController.TryStartAttack`，返回接受或拒绝结果；拒绝不能修改 Action State、计时或冷却。
- Attack 请求保存攻击定义稳定 ID、攻击者运行时 ID、锁定方向和本地序号，不携带 `GameObject`、Animator 或目标引用。

### 2.4 攻击阶段推进算法

`BasicAttackController` 使用显式阶段和剩余时间：

```text
Idle
→ Windup
→ Active：进入时执行一次命中查询
→ Recovery：结束时释放 Attacking 动作锁
→ Cooldown
→ Idle
```

每帧使用剩余时间循环推进，避免低帧率时一次 `deltaTime` 跨过 Active 而漏掉命中：

```text
remainingDelta = max(0, deltaTime)
while remainingDelta > 0 且当前阶段需要计时
    consumed = min(remainingDelta, phaseTimeRemaining)
    phaseTimeRemaining -= consumed
    remainingDelta -= consumed

    if phaseTimeRemaining <= epsilon
        EnterNextPhase()
```

- `EnterPhase` 统一设置计时并执行阶段副作用；命中查询只放在 `EnterActive`。
- 单帧最多推进固定数量的阶段，零时长阶段直接跳过但不得形成死循环。
- Recovery 结束时 Action State 返回 `Free`，冷却继续独立计时，因此玩家可以正常移动但不能再次攻击。
- Disable、场景退出或初始化失败调用 `ForceReset`：阶段回到 Idle、冷却清零、释放动作锁并清空命中集合。

### 2.5 Action State 与移动约束算法

`PlayerActionStateMachine` 为当前状态输出一份只读约束：

| 状态 | 水平速度 | 允许转向 | 允许 Dash | 允许 Attack |
| --- | --- | --- | --- | --- |
| `Free` | `MoveDirection × MoveSpeed` | 是 | 是 | 是 |
| `Dashing` | `LockedDashDirection × DashSpeed` | 面向 Dash 方向 | 否 | 否 |
| `Attacking` | `MoveDirection × MoveSpeed × AttackMoveMultiplier` | 否 | 否 | 否 |
| `Disabled` | `Vector3.zero` | 否 | 否 | 否 |

- `TryEnterAttacking` 只接收速度倍率等动作约束，不接收伤害、范围或命中数据。
- `PlayerController` 在提交请求时读取 `PlayerFacingController.CurrentFacingDirection`；`BasicAttackController` 将请求方向投影到 XZ 平面、归一化并锁定。移动输入仍可改变位移方向，但不会改变本次攻击方向。
- PlayerController 根据约束选择传给 Facing 的方向：Free 使用最终移动方向，Dashing 使用 Dash 方向，Attacking 使用锁定攻击方向。
- Recovery 完成或强制中断必须通过状态机的集中转移入口离开 `Attacking`，外部不得直接设置 `CurrentState`。

### 2.6 近战范围检测与去重算法

Active 阶段进入时只执行一次球形范围查询：

```text
planarDirection = NormalizeXZ(lockedAttackDirection)
queryCenter = AttackOrigin.position + planarDirection × ForwardOffset
hitCount = Physics.OverlapSphereNonAlloc(
    queryCenter,
    Radius,
    reusableColliderBuffer,
    TargetLayerMask,
    QueryTriggerInteraction.Collide)
```

遍历命中结果时按以下顺序过滤：

```text
空 Collider
→ 攻击者自身层级
→ 找不到 Health 或 CombatantIdentity
→ Health 已死亡
→ 与攻击者同阵营
→ 本次攻击已处理过同一个 Health
→ 合法目标，进入伤害结算
```

- Collider 缓冲区在初始化时创建并复用，攻击时不重新分配数组。
- 使用复用的 `HashSet<Health>` 按 Health 去重，解决一个目标存在多个 Collider 时重复扣血的问题；每次攻击开始时 Clear。
- 查询结果等于缓冲区容量时记录一次开发警告，提示调整配置；运行时不在命中帧动态扩容。
- Active 持续时间只服务动作和表现，不重复查询，因此一次攻击对同一目标天然最多结算一次。

### 2.7 伤害、生命与死亡算法

合法目标生成 `DamageContext` 后，调用 `Health.TryApplyDamage(context)`。`Health` 先用当前生命快照调用纯 `DamageResolver`，再应用返回结果：

```text
requestedDamage = max(0, context.BaseDamage)
actualDamage = min(requestedDamage, currentHealth)
healthAfter = currentHealth - actualDamage
killed = currentHealth > 0 且 healthAfter == 0
```

Health 按固定顺序应用结果：

```text
1. 拒绝已经死亡或实际伤害为 0 的请求
2. 先写入 CurrentHealth；致死时同时标记 IsDead
3. 发布携带 DamageResult 的 Damaged
4. 本次首次致死时发布一次 Died
```

- `DamageResult` 保存攻击者 ID、攻击定义 ID、请求伤害、实际伤害、前后生命和是否致死，监听者不需要再次读取可能已变化的全局状态。
- Health 不查询 Collider、不判断攻击阶段、不播放动画；只有本地权威攻击执行链路可以调用伤害入口。
- 训练假人的 Reset 显式恢复 CurrentHealth 和 IsDead，不通过重新启用对象隐式复活。

### 2.8 事件与表现算法

- BasicAttackController 在状态实际改变后发布 `PhaseChanged`，参数携带旧阶段、新阶段和攻击定义 ID。
- Health 更新状态后发布 `Damaged`/`Died`；表现层只根据结果播放动画、闪白和命中特效。
- Presenter 使用显式 `Bind/Unbind` 或 `OnEnable/OnDisable` 成对订阅，不通过轮询 Health 推断是否刚刚受伤。
- Animator 参数可以跟随阶段变化，但 Animator 和 Animation Event 都不是命中时刻或伤害结果的权威来源。

### 2.9 第一版攻击规则

- 仅 `Free` 可以开始普通攻击；`Dashing`、`Attacking`、`Disabled` 拒绝请求。
- 攻击开始时锁定平面朝向，Active 阶段使用该方向查询目标。
- Attacking 期间禁止 Dash 和重复攻击；移动速度倍率由攻击配置提供，第一版默认 `0.5`。
- 每次攻击只在 Active 开始时执行一次范围查询，同一 `Health` 最多结算一次。
- Recovery 结束后回到 `Free`；冷却未结束时仍拒绝下一次攻击。
- Disable、场景退出或强制中断必须结束攻击、清空计时和命中集合，并使 Action State 可安全重置。

---

## 3. 文件管理

### 3.1 Core

目录：`Assets/ProjectRelay/Scripts/Core/Runtime/`

| 类型 | 职责 |
| --- | --- |
| `Game.Core.asmdef` | 承载低 Unity 耦合战斗数据和纯规则 |
| `StableId` | 表达攻击定义等跨配置稳定标识 |
| `Faction` | 定义玩家、敌人和中立阵营 |
| `CombatantId` | 表达单局内攻击者和目标的运行时身份 |
| `DamageType` | 定义第一版基础伤害类型 |
| `DamageContext` | 保存来源/目标 ID、攻击 ID、阵营、伤害类型和基础伤害 |
| `DamageResult` | 保存请求值、实际扣减、前后生命和死亡结果 |
| `DamageResolver` | 根据上下文和目标快照计算伤害结果 |

### 3.2 Runtime

目录：`Assets/ProjectRelay/Scripts/Runtime/Gameplay/Combat/`

| 类型 | 职责 |
| --- | --- |
| `Health` | 提供唯一伤害入口，委托 DamageResolver 计算并应用结果，保证只死亡一次 |
| `CombatantIdentity` | 暴露单局运行时 ID 和所属阵营 |
| `BasicAttackDefinition` | 保存稳定 ID、伤害、阶段时间、冷却、范围和移动倍率 |
| `BasicAttackRequest` | 保存攻击者、攻击定义、锁定方向和请求序号 |
| `CombatCommandResult` | 表达请求是否接受及拒绝原因 |
| `BasicAttackPhase` | 定义 Idle、Windup、Active、Recovery、Cooldown |
| `BasicAttackController` | 管理普通攻击运行时阶段、方向、动作状态和清理 |
| `MeleeHitQuery` | 使用复用缓冲区执行近战范围检测并返回候选目标 |
| `ICombatCommandGateway` | 定义普通攻击请求入口 |
| `LocalCombatCommandGateway` | 将本地请求交给本地权威攻击执行器 |
| `TrainingDummy` | 组合训练假人的 Health、Faction 和重置行为 |
| `BasicAttackPresenter` | 将攻击阶段和确认结果转换为 Animator/VFX 参数 |

新增或修改：

- `PlayerActionState.cs`：增加 `Attacking`。
- `PlayerActionStateMachine.cs`：拆分计时、转移和速度计算，增加攻击转移、完成、中断和移动约束。
- `PlayerActionConstraints.cs`：表达当前状态允许的移动、转向、Dash、Attack 和速度倍率。
- `PlayerController.cs`：消费 Attack，调度 Combat Controller，并统一传入 `deltaTime`。
- `BattleSandboxInstaller.cs`：连接本地 Gateway、Player Combat 和训练假人环境。
- `Game.Runtime.asmdef`：引用 `Game.Core`。

### 3.3 配置、Prefab 与测试

| 路径 | 内容 |
| --- | --- |
| `Assets/ProjectRelay/Config/Combat/BasicAttack_Default.asset` | 第一版普通攻击配置 |
| `Assets/ProjectRelay/Prefabs/PF_TrainingDummy.prefab` | 可重复受击的训练假人 |
| `Assets/ProjectRelay/Scripts/Tests/EditMode/Combat/` | DamageResolver、Health 边界和 Action 转移测试 |
| `Assets/ProjectRelay/Scripts/Tests/PlayMode/Combat/` | 攻击时序、范围命中、重复命中和表现缺失测试 |

---

## 4. 逐步执行计划

### Step 1：建立 Combat Core 与 Health

**结果**

- 训练假人可以通过明确的本地权威入口受伤，生命钳制正确，死亡只发生一次。

**实现**

1. 建立 `Game.Core` 和 Runtime 引用方向。
2. 实现 `StableId`、`CombatantId`、`Faction`、`DamageContext`、`DamageResult` 和 `DamageResolver`。
3. 实现 Health 与 Damaged/Died 结果事件。

**Editor 配合（用户执行）**

1. 创建 `PF_TrainingDummy` Prefab，根节点添加 `CombatantIdentity`、`Health`、`TrainingDummy` 和 Collider。
2. 将 `CombatantIdentity.Faction` 设为 `Enemy`，将 `Health.MaximumHealth` 设为 `100`。
3. 把同对象的 `CombatantIdentity` 和 `Health` 分别赋给组件序列化引用，并按需开启战斗日志。

**检查条件**

- 零伤害、过量伤害、死亡后重复伤害和非法目标结果明确。
- DamageResolver EditMode 测试通过。
- Died 对同一生命周期只触发一次。

### Step 2：扩展 Action State 与攻击阶段

**结果**

- 玩家可以进入 `Attacking`，阶段结束或中断后可靠返回 `Free`。

**实现**

1. 增加 `Attacking` 状态和允许/拒绝转移。
2. 实现 BasicAttackDefinition、BasicAttackPhase 和 BasicAttackController。
3. 固定攻击开始方向，并向移动输出攻击期间速度倍率和 Dash 禁止约束。
4. 将 Disable、场景退出和失败启动统一接入中断清理。

**检查条件**

- Dash 中不能攻击，攻击中不能 Dash 或重复攻击。
- Windup、Active、Recovery 和 Cooldown 时间可由配置重复验证。
- 中断后没有残留计时、方向或动作锁。

### Step 3：接入 Command Gateway 与范围命中

**结果**

- Attack 输入通过本地权威网关启动攻击，并在 Active 阶段命中训练假人。

**实现**

1. 实现 ICombatCommandGateway 和 LocalCombatCommandGateway。
2. PlayerController 消费一次 Attack 意图并提交请求，不直接访问 Health。
3. 实现基于 `Physics.OverlapSphereNonAlloc` 的 MeleeHitQuery 和复用缓冲区。
4. 按阵营、自身、死亡状态和本次攻击命中集合过滤目标。
5. 将合法目标的 `DamageContext` 交给 `Health.TryApplyDamage`，由 Health 调用 DamageResolver 并应用结果。

**Editor 配合**

1. 在玩家添加攻击原点和 BasicAttackController。
2. 配置 Hitbox/目标 LayerMask、攻击距离、半径和默认攻击资产。
3. 在 BattleSandbox 放置单个、重叠和不同阵营训练假人。

**检查条件**

- 每次按键只启动一次攻击。
- 单个和重叠 Collider 不会让同一 Health 重复受伤。
- 友方、自身、死亡目标和范围外目标不受伤。

### Step 4：接入表现并验证无表现降级

**结果**

- 攻击阶段能够驱动动画/VFX，同时规则不依赖表现对象。

**实现**

1. BasicAttackPresenter 订阅攻击阶段与 DamageResult。
2. 输出 `IsAttacking` 或等价 Animator 参数；命中反馈只响应确认结果。
3. Animation Event 只允许作为表现通知，不直接查询目标或修改 Health。

**检查条件**

- 删除或禁用 Animator、Presenter 和 VFX 后，攻击时序与伤害仍正确。
- Presenter 禁用和重新启用不会重复订阅或重复播放反馈。

### Step 5：测试与验收

**结果**

- M2 主路径、拒绝路径、清理路径和性能边界可以重复验证。

**验证**

1. EditMode 覆盖 DamageResolver、Health 边界和 Action 状态转移。
2. PlayMode 覆盖攻击时序、单次命中、阵营过滤、表现缺失和 Disable/Enable。
3. 使用大于 Windup + Active 的单帧 delta 验证不会跳过命中；使用零时长阶段验证不会死循环或重复命中。
4. 在 30、60、120 FPS 下验证阶段计时和冷却无明显偏差。
5. 连续攻击训练假人并重载 BattleSandbox，检查事件和状态无残留。
6. Profiler 检查稳定攻击循环没有每帧 GC Alloc。

**完成条件**

- 自动测试、Editor 手动验收和 Console 检查通过。
- 当前已知问题和验收结果同步到文档。

---

## 5. 最终验收

- [ ] 玩家能够稳定攻击训练假人，实际生命减少量符合 BasicAttackDefinition。
- [ ] 前摇、Active、后摇和冷却可由配置验证。
- [ ] Dash/Attack 互斥、重复输入拒绝和 Disable 中断正确。
- [ ] 一次攻击对同一目标最多结算一次。
- [ ] 自身、友方、死亡和范围外目标不会被错误结算。
- [ ] Health 不低于零，Died 每个生命周期只触发一次。
- [ ] 输入、状态机、命中查询、伤害规则、生命和表现职责无越界。
- [ ] 删除动画与 VFX 表现后，攻击和伤害规则仍可运行。
- [ ] EditMode 与 PlayMode 测试通过。
- [ ] 30/60/120 FPS 下攻击计时无明显异常。
- [ ] 稳定攻击循环没有每帧 GC Alloc。
- [ ] Console 无 Error，Prefab 和配置无缺失引用。

全部通过后进入 M3“第一种敌人与死亡闭环”。
