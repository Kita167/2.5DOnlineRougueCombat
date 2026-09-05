# Project Relay 核心玩法模块开发总规划

> 文档版本：v1.0  
> 编写日期：2026-09-05  
> 适用阶段：第一阶段——核心玩法与离线竖切  
> 上位计划：[ProjectPlan.md](./ProjectPlan.md)

---

## 1. 文档目的

本文档把项目计划中的“核心玩法”拆成可执行、可验收、可按依赖顺序推进的开发模块。目标不是先搭建一套庞大的通用框架，而是在 2026 年 9 月内形成一局完整、可重复运行的离线游戏流程，并保证核心规则以后能平滑接入 Netcode for GameObjects。

本文档回答以下问题：

1. 核心玩法由哪些模块组成，各模块之间如何依赖。
2. 哪个模块先做，达到什么条件后才能开始下一个模块。
3. 单机阶段如何保留未来 Host 权威联机所需的边界。
4. 每个阶段的交付物、验收条件和超期删减规则是什么。

首个开发模块“玩家控制基础”的具体实现安排见 [PlayerControlModulePlan.md](./PlayerControlModulePlan.md)。

---

## 2. 阶段目标与范围

### 2.1 9 月必须形成的结果

在 9 月 21 日前完成第一条可玩竖切：

```text
进入 BattleSandbox
→ 玩家移动和朝向
→ 玩家发动普通攻击
→ 敌人追击并攻击
→ 玩家或敌人受到伤害
→ 敌人死亡
→ 当前波次结束
```

在 9 月 30 日前完成第一个离线完整 Build：

```text
启动
→ Frontend 主界面
→ 开始游戏
→ Battle 初始化
→ 普通波次
→ 一次三选一
→ 简化 Boss
→ 胜利或失败
→ 结算
→ 返回 Frontend
```

### 2.2 P0 玩法范围

- 一个可控制角色。
- 基础移动、朝向和冲刺。
- 一种普通攻击。
- 至少一个主动技能；第二主动技能为 P1。
- 两种普通敌人为最低交付，第三种为 P1。
- 一个单阶段 Boss 为最低交付，第二阶段为 P1。
- 两个普通波次和一个 Boss 波次为最低交付。
- 至少一次局内能力三选一。
- 胜利、失败、结算和重新开始。
- Battle 退出后所有局内状态正确重置。

### 2.3 当前阶段明确不做

- NGO、RPC、NetworkVariable 和网络对象生成。
- Addressables 接入、远程场景和对象池通用框架。
- 装备、背包、掉落词条和复杂 Build 系统。
- 多角色、多武器、程序化地图。
- 完整客户端预测、回滚或帧同步。
- 为尚未出现的第二种实现创建通用工厂、总线或仓储层。

单机阶段只预留清楚的权威与表现边界，不在玩法脚本里加入尚不可验证的网络代码。

---

## 3. 核心开发原则

### 3.1 以纵向闭环驱动开发

每次先把一条从输入到结果的链路做通，再增加内容数量。例如先完成“一种攻击命中一种敌人并使其死亡”，再增加第二技能和第二敌人。禁止先批量创建技能、敌人和配置，却没有任何一条能完整运行的链路。

### 3.2 严格按依赖顺序推进

下游模块只有在上游模块达到退出标准后才能开始。普通攻击依赖玩家控制和战斗基础；敌人死亡依赖生命与伤害；波次依赖敌人生成与死亡事件；结算依赖 Battle 流程状态。不得用临时的跨层调用绕开未完成的依赖。

### 3.3 输入只表达意图

输入层只能产生“移动、攻击、冲刺、释放技能”等意图，不得：

- 直接扣减目标生命。
- 直接生成奖励或推进波次。
- 直接播放业务 UI。
- 决定战斗胜负。

相同的命令入口以后可以由本地输入、AI 或网络请求触发。

### 3.4 权威规则与表现分离

单机阶段由本地权威实现决定命中、伤害、死亡、波次和胜负；联机阶段由 Host 决定相同结果。动画、音效、VFX、伤害数字和镜头震动只消费已经确定的结果，不反向修改规则状态。

### 3.5 配置与运行时状态分离

- Definition Data：ScriptableObject，只读，保存移动速度、攻击参数、敌人参数、波次配置等设计数据。
- Runtime State：普通 C# 对象或组件中的本局状态，例如当前生命、冷却和当前波次。
- Save Data：以后由 Save DTO 保存的跨局数据。
- Network State：12 月联机阶段才加入的复制状态。

运行时不得修改 Definition 资产，也不得把 MonoBehaviour、GameObject 或 Transform 写入存档。

### 3.6 先采用最小可解释实现

- 第一版玩家移动使用 CharacterController，不引入自制物理框架。
- 第一版敌人使用 NavMeshAgent 和小型状态机。
- 第一版命中优先使用近战范围检测，不先制作网络投射物。
- 第一版配置直接引用本地 ScriptableObject，11 月再由资源服务接管加载。
- 只有已经出现真实复用、替换或测试需求的边界才抽接口。

---

## 4. 总体逻辑架构

```text
┌────────────────────────────────────────────────────────────┐
│ Input / AI Intent                                          │
│ LocalPlayerInputSource、EnemyBrain                         │
└─────────────────────────────┬──────────────────────────────┘
                              │ 意图/命令
┌─────────────────────────────▼──────────────────────────────┐
│ Actor Controllers                                          │
│ PlayerController、EnemyController、BossController          │
│ 动作状态、冷却、移动许可、请求时机                           │
└─────────────────────────────┬──────────────────────────────┘
                              │ 战斗请求
┌─────────────────────────────▼──────────────────────────────┐
│ Authority Boundary                                         │
│ 单机：LocalCombatCommandGateway                            │
│ 联机：NetworkCombatCommandGateway（后续）                  │
└─────────────────────────────┬──────────────────────────────┘
                              │ 已校验命令
┌─────────────────────────────▼──────────────────────────────┐
│ Gameplay Rules                                             │
│ ActionExecutor、HitQuery、DamageResolver、Health、Death    │
└─────────────────────────────┬──────────────────────────────┘
                              │ 已确认状态/事件
┌─────────────────────────────▼──────────────────────────────┐
│ Encounter & Battle Flow                                    │
│ Spawn、Wave、Upgrade、Boss、Win/Lose、Result               │
└─────────────────────────────┬──────────────────────────────┘
                              │ 只读状态/表现事件
┌─────────────────────────────▼──────────────────────────────┐
│ Presentation                                               │
│ Animator、VFX、Audio、Camera、HUD、Result UI               │
└────────────────────────────────────────────────────────────┘
```

这些是逻辑边界，不要求为每一层建立程序集。9 月仍按上位计划控制在 `Game.Core`、`Game.Runtime`、`Game.UI`、`Game.Infrastructure`、`Game.Tests` 和 `Game.Editor` 范围内。

### 4.1 程序集职责

| 程序集 | 核心玩法阶段职责 | 允许依赖 |
| --- | --- | --- |
| `Game.Core` | 结果类型、稳定 ID、伤害数据、纯规则接口和不依赖场景的类型 | 尽量不依赖 Unity 场景对象 |
| `Game.Runtime` | 玩家、战斗、敌人、技能、Boss、波次、Battle 流程 | `Game.Core` |
| `Game.UI` | HUD、三选一、暂停和结算表现 | `Game.Core`、只读 Runtime API |
| `Game.Infrastructure` | 当前阶段仅提供场景/平台适配；以后承载 Save、Addressables、Network | `Game.Core`，必要时依赖 Runtime 边界 |
| `Game.Tests` | EditMode 与 PlayMode 测试 | 被测试程序集 |
| `Game.Editor` | 配置校验、调试菜单和构建辅助 | 仅编辑器环境 |

禁止形成 `Game.Runtime → Game.UI` 或 `Game.Core → Game.Runtime` 的反向依赖。

### 4.2 核心模块及职责

| 模块 | 主要职责 | 不应承担的职责 |
| --- | --- | --- |
| Battle Composition | 创建并连接本局服务、角色、生成器和流程控制器 | 全局存档、资源下载、具体战斗规则 |
| Player Control | 采集输入、移动、朝向、动作状态和冲刺 | 计算伤害、修改敌人生命、操作 HUD |
| Combat Core | 命中查询、伤害计算、生命、死亡、阵营和无敌判定 | 输入读取、动画状态、波次推进 |
| Ability | 普通攻击/技能定义、冷却、释放条件和执行步骤 | 直接访问键盘、直接更新 UI |
| Enemy AI | 目标选择、寻路、攻击意图和个体状态 | 决定波次完成、直接发放奖励 |
| Encounter/Wave | 读取波次配置、生成敌人、统计存活、推进阶段 | 处理具体敌人 AI、绘制 HUD |
| Upgrade Selection | 生成三个候选、应用本局 Modifier、恢复游戏 | 修改永久存档、创建新技能框架 |
| Boss | Boss 阶段切换、技能选择和阶段配置 | Battle 总胜负、UI 页面切换 |
| Battle Flow | 管理整局状态、暂停规则、胜负和退出清理 | 直接计算每次攻击伤害 |
| Presentation | 动画、特效、音频、镜头和界面展示 | 修改权威生命、冷却、波次和结果 |

---

## 5. 关键运行时模型

### 5.1 Battle 流程状态机

Battle 使用一个显式状态机管理整局生命周期：

```text
Initializing
→ Countdown
→ NormalWave
→ RewardSelection
→ NormalWave（按配置重复）
→ BossWave
→ Finishing
→ Result
→ Exiting
```

任意战斗中状态都可进入：

```text
NormalWave / BossWave → Failed → Finishing → Result
任意可玩状态 → Aborting → Exiting
```

规则：

- 只有 BattleFlowController 能推进整局状态。
- WaveController 只报告波次完成，不直接打开三选一或结算界面。
- 玩家死亡、Boss 死亡等事件先提交给 BattleFlowController，再由其决定胜负。
- 进入和退出每个状态都必须成对注册/释放定时器、事件和生成对象。
- `Result` 保留在 Battle 场景内；确认返回后才统一清理并切换场景。

### 5.2 玩家状态模型

玩家不使用一个把 Idle、Move、Attack、Dash、Hit、Dead 全部混在一起的单层枚举。采用两个正交部分：

1. **Locomotion**：连续计算移动、重力、碰撞与朝向，不把 Idle/Move 做成互斥业务状态。
2. **Action State Machine**：管理会互相抢占或锁定的动作，计划状态为 `Free`、`Dashing`、`Attacking`、`Casting`、`HitStun`、`Disabled` 和 `Dead`。

这样可以避免“MovingAttack”“IdleAttack”“MovingCast”等组合状态爆炸。每个 Action State 明确声明：

- 是否允许读取移动。
- 是否允许旋转。
- 是否允许进入下一个动作。
- 是否具有无敌帧。
- 被打断时如何清理。

### 5.3 敌人状态模型

普通敌人采用小型有限状态机：

```text
Spawning → AcquiringTarget → Chasing → Attacking
                         ↘ Stunned ↗
任意存活状态 → Dead
```

- NavMeshAgent 只负责导航和速度表现。
- EnemyBrain 决定何时产生移动/攻击意图。
- 攻击仍走与玩家一致的命中、伤害和生命链路。
- 死亡事件只能发出一次，死亡后立即从波次存活计数中移除。

### 5.4 Boss 状态模型

Boss 的“生命阶段”和“当前动作”分离：

- `BossPhaseController` 根据生命阈值决定阶段并切换可用技能集合。
- `BossActionController` 选择、前摇、执行、后摇和冷却具体技能。
- 阶段切换不能写进某个具体技能脚本，具体技能也不能决定整局胜利。

P0 降级为单阶段时，只替换配置和阶段数量，不改动 Battle 流程。

### 5.5 伤害链路

```text
Attack/Skill Intent
→ Combat Command Gateway
→ Action 条件校验
→ 命中窗口开启
→ HitQuery 找到候选目标
→ 阵营/重复命中/无敌校验
→ DamageResolver 计算 DamageResult
→ Health 应用结果
→ 发布 Damaged 或 Died 事件
→ 表现层播放反馈
→ Wave/Battle 监听死亡结果
```

硬性规则：

- 只有权威执行路径可以调用生命修改入口。
- `Health` 负责当前值、上下限和死亡一次性语义；不负责找目标。
- `DamageResolver` 不访问 Animator、UI、音频或输入。
- 攻击者、技能、目标、伤害类型必须带稳定的运行时上下文，方便以后诊断和联网校验。
- 动画事件只能通知“命中窗口到达”，不得在 Animation Event 内直接扣血。

### 5.6 配置资产

第一阶段建议只建立确实会使用的 Definition：

| Definition | 关键字段 |
| --- | --- |
| `PlayerMovementDefinition` | 移速、旋转速度、重力、冲刺速度/时长/冷却 |
| `BasicAttackDefinition` | 前摇、命中窗口、后摇、范围、伤害、冷却 |
| `ActiveSkillDefinition` | 稳定 ID、释放时序、范围、伤害、冷却、表现引用占位 |
| `EnemyDefinition` | 最大生命、移速、攻击距离、攻击配置、奖励值 |
| `EncounterDefinition` | 波次列表、敌人类型、数量、生成间隔、生成点规则 |
| `UpgradeDefinition` | 稳定 ID、候选权重、Modifier 类型和值 |
| `BossDefinition` | 生命、阶段阈值、每阶段技能集合、行为参数 |

所有 Definition 在运行时只读。冷却剩余、当前生命、已经选择的能力等必须存在 Runtime State 中。

---

## 6. 开发顺序与阶段门禁

### 6.1 总顺序

```text
M0 BattleSandbox 与基础约定
→ M1 玩家控制基础
→ M2 战斗基础与普通攻击
→ M3 第一种敌人与死亡闭环
→ M4 波次与 Battle 流程
→ M5 Boss、胜负与结算
→ M6 主动技能与三选一
→ M7 内容扩展与表现
→ M8 稳定性、构建与网络风险验证
```

下表中的“完成条件”是开始下一模块的门禁，而不是最后再补的测试项。

### 6.2 M0：BattleSandbox 与最小组合根

**目标**

- 创建只用于开发的 `BattleSandbox` 场景。
- 建立 Ground、Obstacle、Player、Enemy、Hitbox 等 Layer 与碰撞矩阵。
- 建立 `BattleSceneContext` 或等价的小型组合根，用 Inspector 显式连接本局依赖。
- 建立核心 asmdef、命名空间、Input Actions 和基础测试程序集。

**完成条件**

- 场景可以独立 Play，不依赖手工调整 Hierarchy。
- Console 无错误和缺失引用。
- 停止 Play 后不留下运行时创建的资产修改。

**时间盒**：0.5 个开发日。

### 6.3 M1：玩家控制基础

**目标**

- Input System 输入采集。
- 相机相对移动、鼠标/手柄朝向。
- CharacterController 移动、重力、碰撞。
- Free/Dashing/Disabled 动作状态。
- 冲刺冷却、打断与场景退出重置。
- 基础跟随相机和动画参数输出。

**完成条件**

- 键鼠和手柄至少各完成一次完整操作验证。
- 斜向移动不比直线更快。
- 玩家不能穿过场景碰撞体，低帧率下不出现明显速度翻倍。
- 重复进入/退出 Play 或启用/禁用角色，不产生重复输入回调。
- 输入脚本不直接操作 Animator、Health 或 UI。

**时间盒**：2.5～3 个开发日。具体安排见首模块细则文档。

### 6.4 M2：战斗基础与普通攻击

**目标**

- 建立 Faction、Health、DamageContext、DamageResult 和 DamageResolver。
- 建立普通攻击的释放时序：前摇、命中窗口、后摇和冷却。
- 使用范围检测命中训练假人，保证一次挥击对同一目标只结算一次。
- 建立本地 Combat Command Gateway，未来可替换为网络请求适配器。
- 将攻击逻辑与动画/VFX 表现分离。

**完成条件**

- 玩家可以稳定攻击训练假人，生命值按配置减少。
- 攻击冷却、动作锁和取消规则可重复验证。
- DamageResolver 的核心计算具有 EditMode 测试。
- 删除动画组件后，规则仍能执行且不会产生 NullReferenceException。

**时间盒**：2～3 个开发日。

### 6.5 M3：第一种敌人与死亡闭环

**目标**

- 近战追击型敌人：生成、选目标、追击、攻击、受伤、死亡。
- 玩家也可受到伤害并死亡。
- 敌人死亡后正确停止导航、碰撞和攻击。
- 建立单次死亡事件和存活登记机制。

**完成条件**

- 玩家可以击杀敌人，敌人也可以击杀玩家。
- 死亡事件只触发一次，重复伤害不会重复计数。
- 敌人死亡后没有继续寻路或攻击的幽灵行为。
- 同时存在 10 个敌人时无明显每帧 GC Alloc 峰值。

**时间盒**：2～2.5 个开发日。

### 6.6 M4：波次与 Battle 流程

**目标**

- EncounterDefinition 驱动敌人类型、数量和生成间隔。
- WaveController 管理生成和存活计数。
- BattleFlowController 管理初始化、倒计时、战斗、波次结束和失败。
- Gameplay 调试 HUD 显示 Battle 状态、波次和存活数。

**完成条件**

- 至少一个波次从开始自动推进到结束。
- 玩家死亡后停止继续生成敌人，并进入失败状态。
- 重开本局时波次序号、计时器、订阅和敌人列表全部归零。
- 达成上位计划 9 月 21 日检查点。

**时间盒**：2 个开发日。

### 6.7 M5：Boss、胜负与结算

**目标**

- 先实现单阶段 Boss，包含两个可明显辨认的技能。
- Boss 死亡触发胜利，玩家死亡触发失败。
- Finishing 状态冻结新命令并等待必要表现完成。
- 结算显示胜负、用时、击杀数和所选能力。
- 从 Result 返回 Frontend，并可再次开始新局。

**完成条件**

- 胜利和失败都能结算并返回。
- 连续完成三局，无上局 Boss、事件、计时器或输入残留。
- Boss 状态/技能执行与 Battle 胜负判断分离。

**时间盒**：2.5～3 个开发日。

### 6.8 M6：主动技能与三选一

**目标**

- 在普通攻击链路上扩展一个主动技能。
- 建立最小局内 Modifier 模型，不创建完整装备系统。
- 波次间暂停战斗并生成三个候选项。
- 应用选择、恢复战斗并在 HUD 显示结果。

**P0 能力示例**

- 普通攻击伤害 `+20%`。
- 移动速度 `+10%`。
- 最大生命 `+25` 并按规则补充当前生命。

**完成条件**

- 候选稳定 ID 不重复，同一选择只应用一次。
- 三种简单数值效果均能通过前后数据验证。
- 选择期间敌人、计时器和输入按 Battle 状态正确暂停。

**时间盒**：2～2.5 个开发日。

### 6.9 M7：内容扩展与表现

只有 M0～M6 完整闭环稳定后才进入本阶段。

按优先级增加：

1. 第二种普通敌人，优先做远程或冲锋以体现行为差异。
2. Boss 第二阶段。
3. 第二主动技能。
4. 第三种普通敌人。
5. 动画、VFX、音频、镜头反馈和数值打磨。

新增内容必须复用既有接口和配置；如果加入一种敌人需要修改 BattleFlowController，说明模块边界错误，应先修正职责。

### 6.10 M8：稳定性、Build 与网络风险验证

**稳定性任务**

- 连续完成三局胜利和三局失败路径。
- Battle 重进 10 次，验证对象、事件、静态状态和内存趋势。
- 30/60/120 FPS 下检查移动、冲刺、攻击窗口和计时。
- 制作 Windows Mono Development Build。
- 记录已知问题和 Profiler 基线。

**3～5 天 NGO 独立风险验证**

在独立 Spike 场景中只验证：

- Host/Client 启动与 Player Spawn。
- Owner 才能启用本地输入。
- Owner Authoritative 移动的基本同步。
- Client 攻击请求由 Host 校验并扣除训练假人生命。

Spike 不与主 Battle 流程深度合并；完成后记录要调整的边界，正式网络开发仍按 12 月计划执行。

---

## 7. 2026 年 9 月建议排期

| 日期 | 主要工作 | 当日/阶段产物 |
| --- | --- | --- |
| 9/5 | M0 场景、程序集、Layer、输入资产 | 可独立运行的 BattleSandbox |
| 9/6～9/8 | M1 玩家控制基础 | 移动、朝向、相机、冲刺 |
| 9/9～9/11 | M2 战斗核心与普通攻击 | 可受击训练假人 |
| 9/12～9/14 | M3 第一种敌人 | 双向伤害与死亡闭环 |
| 9/15～9/17 | M4 波次与 Battle 状态 | 一个普通波次完整结束 |
| 9/18～9/21 | 修复、构建、检查点缓冲 | 达成基础战斗循环 |
| 9/22～9/25 | M5 简化 Boss、胜负和结算 | 可完整结束一局 |
| 9/26～9/27 | M6 一次三选一与主动技能 | 最小局内成长 |
| 9/28～9/30 | Frontend 接线、Build、回归 | v0.1 离线完整 Build |

如果实际开始日期后移，优先保留每个检查点前的 2～3 天修复缓冲，并立即执行第 11 节的删减，而不是把所有任务等比例推迟。

---

## 8. 模块间通信规则

### 8.1 允许的调用方向

```text
UI / Presentation → 只读查询或提交用户选择
Input / AI → Controller → Command Gateway → Rules
Rules → 状态结果/领域事件 → Flow 与 Presentation
Flow → 启停模块、切换 Battle 状态
```

### 8.2 事件使用规则

- 同一对象内部的强顺序逻辑使用直接方法调用。
- 一个已确认结果需要被多个表现消费者观察时使用 C# event。
- 跨场景、跨生命周期的关键流程不依赖匿名全局事件总线。
- 事件订阅必须在 `OnEnable`/`OnDisable` 或显式 `Bind`/`Unbind` 中成对出现。
- 事件参数携带结果数据，不要求监听者再次读取可能已变化的全局状态。

### 8.3 UI 读取规则

- HUD 订阅 Health、Cooldown 和 Battle 只读状态。
- HUD 不保存权威生命副本，不自行推算胜负。
- 三选一 UI 只能提交候选稳定 ID，由 Upgrade 规则确认后再显示已应用结果。
- 暂停 UI 请求 BattleFlowController 改变暂停状态，不直接遍历并禁用所有对象。

### 8.4 时间与暂停规则

- 玩法计时统一由 Battle 时间源提供，不在各脚本随意读取不同时间语义。
- 普通战斗计时使用受暂停影响的 delta time。
- UI 动画、Loading 等需要继续运行的表现可使用 unscaled time。
- 冷却、Buff 和波次生成必须明确声明是否受暂停影响。

---

## 9. 为后续联机保留的边界

### 9.1 单机阶段就必须遵守

- 本地输入源与 PlayerMotor 分离。
- 非本地所有者以后可以关闭输入源，而不影响角色表现。
- 移动由角色所有者执行；伤害、死亡、敌人、波次和胜负集中到 Authority 路径。
- 攻击与技能通过命令入口执行，不从 Input callback 直接结算。
- 表现消费已确认事件，允许以后在远端客户端重放表现。
- 所有玩法定义具有稳定 ID，不使用对象实例名作为网络或存档 ID。

### 9.2 12 月替换点

| 单机实现 | 联机阶段替换/扩展 |
| --- | --- |
| `LocalPlayerInputSource` 绑定本地角色 | 仅在 `IsOwner` 时绑定 |
| `LocalCombatCommandGateway` 立即执行 | Client 请求经 ServerRpc 到 Host 校验 |
| 本地 Health 直接保存状态 | Host 修改，NetworkVariable/同步快照复制 |
| 本地 EnemyBrain 驱动所有敌人 | 仅 Host 运行 AI，客户端只表现 |
| 本地 WaveController | 仅 Host 推进并复制当前波次 |
| 本地 BattleFlowController | Host 决定胜负并广播结果 |

### 9.3 不为联机提前做的内容

- 不把所有字段改成可序列化网络字段。
- 不在每个玩法类里加入 `IsServer` 分支。
- 不提前制作预测、回滚和网络对象池。
- 不把普通 VFX、音频和装饰物做成 NetworkObject。

---

## 10. 测试与验收策略

### 10.1 EditMode 测试重点

- DamageResolver 的伤害、减伤、暴击或边界钳制。
- Health 的零值、过量伤害、治疗上限和只死亡一次。
- Action State Machine 的允许/拒绝转移。
- Upgrade Modifier 的应用与重复应用保护。
- Battle/Wave 纯状态推进规则。

### 10.2 PlayMode 测试重点

- Input System 到玩家移动的完整链路。
- 攻击范围、LayerMask 和重复命中。
- Enemy Spawn、死亡、取消导航和波次存活计数。
- 胜利、失败、退出和重开。
- 对象禁用后事件退订，场景卸载后无残留调用。

### 10.3 每个模块的 Definition of Done

一个模块只有同时满足以下条件才视为完成：

- 主成功路径可在 BattleSandbox 中操作验证。
- 至少验证一个失败或取消路径。
- 核心规则有适合层级的自动化测试。
- Console 无 Error；Warning 已处理或记录原因。
- Inspector 无缺失引用，配置有合理默认值和 Tooltip。
- 进入、退出、禁用和销毁路径能够正确清理。
- 没有让下游模块跨层访问内部可变字段。
- 对应文档、配置说明和已知问题同步更新。

---

## 11. 截断规则

### 11.1 9 月 21 日未完成基础战斗循环

立即删除或推迟：

- 第二主动技能。
- 第三种普通敌人。
- Boss 第二阶段。
- 复杂能力组合和稀有度系统。
- 非必要动画、VFX 和音频。

保留并优先修复：

- 玩家移动与普通攻击。
- 一种敌人的受伤、死亡。
- 一个波次的开始与结束。
- Battle 退出清理。

### 11.2 9 月 30 日未形成完整单机 Build

立即降级为：

- 两个普通波次。
- 一个单阶段、两个技能的 Boss。
- 一个主动技能。
- 一次三选一，仅三个简单数值 Modifier。
- 一个角色和固定配置。

不得删减：

- 胜利和失败结算。
- 返回 Frontend 后可重新开局。
- 生命、伤害和胜负的权威边界。
- Battle 退出后的状态重置。

### 11.3 功能扩展停止条件

发现以下任一情况时，停止增加玩法内容，优先修复结构或生命周期：

- 第二局开始出现状态残留。
- 相同死亡事件被重复处理。
- UI、动画或输入脚本能够直接修改生命/胜负。
- 新敌人必须修改波次或 Battle 核心流程才能加入。
- PlayMode 退出后仍有回调访问已销毁对象。
- Profiler 显示稳定玩法循环中存在持续增长的分配或对象数量。

---

## 12. 阶段交付清单

### 12.1 9 月 21 日交付

- `BattleSandbox` 场景。
- 玩家移动、朝向、冲刺和普通攻击。
- 一种普通敌人。
- 双向伤害、死亡和一个完整波次。
- 基础 Gameplay 调试 HUD。
- 一份 Windows Development Build。
- 关键规则测试和当前已知问题清单。

### 12.2 9 月 30 日交付

- Bootstrap → Frontend → Battle → Result → Frontend 完整流程。
- 简化 Boss、胜利与失败。
- 至少一个主动技能和一次三选一。
- 连续三局回归记录。
- v0.1 Windows Build 和短演示录像。
- NGO 风险验证结果与需要调整的权威边界记录。

---

## 13. 首个立即执行项

按本规划，下一项工作是 **M0 + M1：BattleSandbox 与玩家控制基础**。执行时先完成输入资产和场景基线，再按以下顺序实现：

```text
Input Actions
→ LocalPlayerInputSource
→ Camera-relative Movement
→ CharacterController Motor
→ Aim/Facing
→ Player Action State Machine
→ Dash
→ Camera/Animation Presentation
→ Disable/Reset/PlayMode Tests
```

不得在 M1 中顺手加入伤害、技能系统或网络组件。M1 达到退出条件后，立即进入 M2，用普通攻击和训练假人验证从命令到伤害结果的第一条战斗链路。
