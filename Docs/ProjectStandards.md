# Project Relay 项目开发规范

> 文档版本：v1.0  
> 生效日期：2026-09-05  
> 适用范围：项目自有代码、资源、场景、配置、测试与文档  
> Unity 版本基线：`6000.3.23f1`

---

## 1. 规范目标

本规范用于保证 Project Relay 在半年开发过程中保持统一、可查找、可维护和可交接。所有新文件默认遵守本文；确需例外时，必须在代码、提交说明或架构决策记录中写明原因。

规范优先级：

1. 可运行、数据安全和生命周期正确。
2. 模块职责与依赖方向清楚。
3. 命名、格式和目录一致。
4. 性能优化和扩展性必须有实际测量或需求支撑。

不为了追求目录或模式的绝对整齐而阻塞可玩闭环，但一旦形成约定，后续同类内容必须保持一致。

---

## 2. 项目根目录规范

```text
ProjectRoot/
├── Assets/
├── Docs/
├── Packages/
├── ProjectSettings/
├── .gitignore
├── .gitattributes
└── README.md
```

### 2.1 版本库必须提交

- `Assets` 中的项目资源和对应 `.meta` 文件。
- `Packages/manifest.json` 与 `Packages/packages-lock.json`。
- `ProjectSettings`。
- `Docs`、README、许可证与第三方资源声明。
- Addressables 正式配置和需要保存的发布状态文件。
- Build Profile 及构建脚本。

### 2.2 禁止提交

- `Library/`
- `Temp/`
- `Logs/`
- `obj/`
- `UserSettings/`
- IDE 的个人缓存和用户设置。
- 本地构建输出、崩溃转储和临时 Profiler 捕获；需要作为证据保存的文件放入约定的文档证据目录。
- 密钥、Token、个人绝对路径和只适用于个人机器的配置。

`.meta` 文件与对应资源必须一起移动、重命名和提交，禁止只提交其中一个。

---

## 3. Assets 目录规范

### 3.1 顶层规则

所有项目自有内容统一放在：

```text
Assets/ProjectRelay/
```

第三方内容统一放在：

```text
Assets/ThirdParty/
```

Unity 或 Package 自动生成且要求固定位置的目录可以保留在 `Assets` 根目录，例如：

- `AddressableAssetsData/`
- `TextMesh Pro/`
- Package 明确要求的 Generated 目录。

除这些例外外，不在 `Assets` 根目录散放项目自有 Prefab、材质、脚本或配置。

项目当前由模板生成的 `Assets/Scenes`、`Assets/Settings` 应在开始正式开发时通过 Unity Project 窗口迁移到 `Assets/ProjectRelay` 对应目录。移动后必须检查引用和 `.meta`，不在文件资源管理器中进行会丢失 `.meta` 的移动。

### 3.2 标准目录树

```text
Assets/
├── ProjectRelay/
│   ├── Art/
│   │   ├── Animations/
│   │   ├── Characters/
│   │   ├── Enemies/
│   │   ├── Environment/
│   │   ├── Materials/
│   │   ├── Models/
│   │   ├── Shaders/
│   │   ├── Textures/
│   │   └── VFX/
│   ├── Audio/
│   │   ├── BGM/
│   │   ├── SFX/
│   │   └── Mixers/
│   ├── Configs/
│   │   ├── Gameplay/
│   │   ├── UI/
│   │   └── Versions/
│   ├── Prefabs/
│   │   ├── Characters/
│   │   ├── Enemies/
│   │   ├── Environment/
│   │   ├── Gameplay/
│   │   ├── UI/
│   │   └── VFX/
│   ├── Scenes/
│   │   ├── Game/
│   │   ├── Dev/
│   │   └── Tests/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Runtime/
│   │   ├── UI/
│   │   ├── Infrastructure/
│   │   ├── Editor/
│   │   └── Tests/
│   ├── Settings/
│   │   ├── Input/
│   │   ├── Physics/
│   │   ├── Rendering/
│   │   └── Build/
│   └── UI/
│       ├── Fonts/
│       ├── Icons/
│       ├── Sprites/
│       └── Themes/
├── ThirdParty/
└── AddressableAssetsData/       # Addressables 接入后由 Unity 管理
```

不创建空目录占位。目录在第一份真实内容出现时创建。

### 3.3 Scripts 内的功能分类

```text
Scripts/
├── Core/
│   └── Runtime/                 # Game.Core.asmdef
├── Runtime/
│   ├── Input/
│   │   └── Generated/
│   ├── GameFlow/
│   ├── Gameplay/
│   │   ├── Player/
│   │   ├── Combat/
│   │   ├── Abilities/
│   │   ├── Enemies/
│   │   ├── Boss/
│   │   ├── Encounter/
│   │   └── Upgrades/
│   └── Presentation/
├── UI/
│   ├── Runtime/
│   │   ├── Pages/
│   │   ├── Popups/
│   │   ├── HUD/
│   │   └── Presenters/
│   └── Tests/
├── Infrastructure/
│   ├── Save/
│   ├── Assets/
│   ├── Update/
│   ├── Network/
│   └── Platform/
├── Editor/
├── Tests/
│   ├── EditMode/
│   └── PlayMode/
```

分类优先按“功能职责”而不是按 Unity 基类。例如 `PlayerHealth.cs` 放在 `Gameplay/Combat` 或团队确定的 Player 子功能位置，不建立一个包含所有 MonoBehaviour 的总目录。

### 3.4 场景规范

正式场景：

```text
Scenes/Game/Bootstrap.unity
Scenes/Game/Frontend.unity
Scenes/Game/Battle.unity
```

开发场景：

```text
Scenes/Dev/BattleSandbox.unity
Scenes/Dev/UISandbox.unity
Scenes/Dev/NetworkSpike.unity
```

测试场景放在 `Scenes/Tests`，不加入正式 Build Profile。正式 Build Profile 的场景顺序必须由构建前校验确认。

场景职责：

- `Bootstrap`：唯一全局组合根和长期服务，不承载战斗对象。
- `Frontend`：主界面、设置、角色配置和房间 UI。
- `Battle`：玩家、敌人、波次、能力选择、暂停和结算。
- `Dev` 场景：可独立运行某个功能，不引用正式流程中的隐式状态。

### 3.5 Prefab 规范

- Prefab 根节点名称与资源文件名一致。
- Prefab Variant 只用于确实共享稳定结构的变体，避免多层嵌套 Variant。
- 运行时必须存在的组件引用优先在 Prefab Inspector 显式配置，并在 `Awake` 校验。
- 不在 Prefab 中保存对场景对象的引用。
- 视觉子对象放在 `VisualRoot` 下，规则组件不要通过深层 Transform 路径查找视觉对象。
- 网络 Prefab 接入后，NetworkObject 放在明确的根节点，装饰性 VFX 不因此全部成为网络对象。

---

## 4. 资源命名规范

### 4.1 通用规则

- 目录和资源使用英文，采用 PascalCase 或统一前缀格式。
- 文件名不使用空格、中文、括号、临时编号和含义不明的缩写。
- 禁止 `New Prefab`、`Material 1`、`test2_final_final` 等名称进入主分支。
- 同一类资源只使用一种命名方案，不混用多个前缀。

### 4.2 推荐命名

| 资源 | 格式 | 示例 |
| --- | --- | --- |
| 场景 | PascalCase | `Battle.unity`、`BattleSandbox.unity` |
| Prefab | `PF_名称` | `PF_Player`、`PF_EnemyMelee` |
| Prefab Variant | `PF_基础_变体` | `PF_EnemyMelee_Elite` |
| Material | `MAT_名称` | `MAT_PlayerBody` |
| Texture | `T_名称_用途` | `T_Player_Albedo` |
| Normal Map | `T_名称_Normal` | `T_Stone_Normal` |
| Mesh | `SM_名称` | `SM_ArenaWall` |
| Animation Clip | `AN_对象_动作` | `AN_Player_Dash` |
| Animator Controller | `AC_对象` | `AC_Player` |
| Avatar Mask | `AM_用途` | `AM_UpperBody` |
| VFX Prefab/Graph | `VFX_名称` | `VFX_BasicAttackHit` |
| SFX | `SFX_对象_动作` | `SFX_Player_Dash` |
| BGM | `BGM_场景` | `BGM_Battle` |
| Sprite Atlas | `SA_用途` | `SA_BattleHUD` |
| Input Actions | PascalCase | `ProjectRelayInput.inputactions` |
| ScriptableObject 配置 | `类型_变体` | `PlayerMovement_Default` |

脚本文件不加 `SC_` 等资源前缀，必须与顶层类型同名。

---

## 5. Assembly Definition 与依赖规范

### 5.1 程序集

| asmdef | 职责 |
| --- | --- |
| `Game.Core` | 稳定 ID、DTO、结果类型、纯规则和低 Unity 耦合接口 |
| `Game.Runtime` | GameFlow、玩家、技能、敌人、Boss、波次和玩法表现协调 |
| `Game.UI` | 页面、Popup、HUD、Presenter 和 UI 配置 |
| `Game.Infrastructure` | Save、Addressables、Update、Network 和平台实现 |
| `Game.Tests` | EditMode/PlayMode 测试；按需要再拆两个测试 asmdef |
| `Game.Editor` | 构建、校验、调试工具；仅 Editor 平台 |

### 5.2 依赖方向

```text
Game.UI ───────────────→ Game.Runtime ─────→ Game.Core
                              ↑                  ↑
Game.Infrastructure ──────────┘──────────────────┘

Game.Editor / Game.Tests → 按需引用以上程序集
箭头表示“可以引用”；任何箭头都不得反向形成循环。
```

规则：

- `Game.Core` 不引用 UI、Infrastructure 或具体场景对象。
- `Game.Runtime` 不引用 `Game.UI`。
- `Game.UI` 只通过公开只读 API、Presenter 或事件观察 Runtime。
- Infrastructure 实现 Core/Runtime 定义的外部边界，不让玩法代码直接调用 Addressables、文件系统或 Transport。
- 出现循环依赖时调整职责或提取最小共享类型，禁止通过移除 asmdef 掩盖问题。
- 不为只有一个脚本的小功能单独建立 asmdef。

---

## 6. C# 文件规范

### 6.1 一个文件一个顶层类型

- 每个 `.cs` 文件只存放一个顶层 `class`、`interface`、`struct`、`enum` 或 `record`。
- 文件名必须与顶层类型名完全一致。
- 测试类同样遵守一文件一类。
- 不把多个小 enum 或 DTO 塞进 `Common.cs`、`Types.cs`、`Utils.cs`。
- 私有嵌套类型只有在不可能被外部复用且能明显提升局部可读性时才允许；默认仍拆文件。
- 不使用手写 partial class 逃避一文件一职责。Unity/Input System 自动生成代码除外。

### 6.2 自动生成与第三方代码例外

以下代码可不遵守本项目字段命名和注释格式：

- Input System 自动生成类。
- Source Generator 或 Unity 自动生成文件。
- 未修改的第三方插件源码。

例外代码必须放在 `Generated` 或 `ThirdParty`，禁止手工修改。若必须修改第三方源码，应复制到项目自有目录、记录来源与修改点，并按本规范维护。

### 6.3 命名空间

根命名空间统一为：

```text
ProjectRelay
```

按模块扩展：

```text
ProjectRelay.Core
ProjectRelay.GameFlow
ProjectRelay.Gameplay.Player
ProjectRelay.Gameplay.Combat
ProjectRelay.Gameplay.Enemies
ProjectRelay.Gameplay.Abilities
ProjectRelay.UI.HUD
ProjectRelay.Infrastructure.Save
ProjectRelay.Infrastructure.Network
ProjectRelay.Editor
ProjectRelay.Tests.EditMode
```

命名空间表达逻辑职责，不照搬过深的物理目录。禁止使用默认 `AssemblyCSharp` 命名空间或无命名空间的项目自有类型。

---

## 7. C# 命名规范

本节为项目硬性规范。项目自有手写代码必须统一执行。

### 7.1 命名表

| 对象 | 规则 | 示例 |
| --- | --- | --- |
| 类/结构/枚举 | PascalCase | `PlayerMotor`、`DamageResult` |
| 接口 | `I` + PascalCase | `IPlayerInputSource` |
| 方法 | PascalCase | `ApplyDamage`、`TryEnterState` |
| 私有成员变量 | `m` + PascalCase | `mMoveSpeed`、`mCurrentState` |
| 私有静态/只读成员 | `m` + PascalCase | `mSharedBuffer`、`mDefaultTimeout` |
| public 成员变量 | PascalCase | `MoveSpeed` |
| public 属性 | PascalCase | `CurrentHealth`、`IsEnabled` |
| public 事件 | PascalCase | `Died`、`StateChanged` |
| 方法传入参数 | `_` + camelCase | `_damageContext`、`_deltaTime` |
| 方法内 local variable | `_` + camelCase | `_targetPosition`、`_index` |
| 泛型类型参数 | `T` + PascalCase | `TValue`、`TDefinition` |
| bool | 使用 Is/Has/Can/Should 表意 | `mIsGrounded`、`CanDash`、`_hasTarget` |

### 7.2 公有字段限制

公有字段、属性和事件直接使用 PascalCase，不增加 `Pub` 前缀。项目中应尽量避免 public mutable field，Inspector 配置统一使用：

```csharp
[SerializeField] private float mMoveSpeed;
```

对外读取使用只读或私有 setter 属性：

```csharp
public float CurrentSpeed { get; private set; }
```

只有序列化协议、明确的简单数据载体或 Unity API 强制要求时才使用 public field，并必须说明原因。

### 7.3 参数与局部变量

所有函数参数和局部变量都使用下划线前缀，包括：

- `for`/`foreach` 变量。
- `out` 变量。
- `catch` 异常变量。
- Lambda 参数。
- 局部常量和局部函数参数。

示例：

```csharp
for (int _index = 0; _index < _targets.Count; _index++)
{
    IDamageable _target = _targets[_index];
    _target.ApplyDamage(_damageContext);
}
```

---

## 8. C# 格式规范

- 使用 4 个空格缩进，不使用 Tab。
- 大括号使用 Allman 风格，左括号单独一行。
- 一行只写一条语句。
- 建议每行不超过 120 个字符；超出时按语义换行。
- 所有成员写显式访问修饰符。
- `using` 位于文件顶部，移除未使用引用；System、Unity、项目命名空间分组可由 IDE 统一排序。
- 默认使用块级 namespace，项目内不混用多种风格。
- 简单且类型明显时可使用 `var`；当返回类型不明显或会降低可读性时写出显式类型。
- 比较 UnityEngine.Object 时遵守 Unity 的空值语义，不使用会绕开已销毁对象判断的写法。
- 不在同一表达式中堆叠多层副作用。
- 不用 `#region` 隐藏过大的类；类过大时按职责拆分。
- 不保留大段注释掉的旧代码，历史由 Git 保存。

### 8.1 类内成员顺序

统一按以下顺序组织：

1. 常量和静态只读成员。
2. `[SerializeField]` 私有配置字段。
3. 其他私有运行时字段。
4. public 属性和事件。
5. Unity 生命周期方法：`Awake`、`OnEnable`、`Start`、`Update`、`LateUpdate`、`OnDisable`、`OnDestroy`。
6. public 方法。
7. 接口实现方法。
8. 事件回调。
9. private 辅助方法。

同一项目中始终保持该顺序，减少查找成本。

---

## 9. 注释与文档规范

### 9.1 类和函数职责注释为必需项

每个项目自有顶层类型和每个函数都必须写清楚职责注释：

- class、interface、struct、enum：使用 `/// <summary>` 说明其唯一职责和所处边界。
- public/protected/internal 方法：使用 XML 注释说明行为；有参数、返回值或异常语义时写 `param`、`returns`、必要的 `exception`。
- private 方法和 Unity 生命周期函数：同样使用 `/// <summary>`，说明为什么存在、读取/修改什么状态或何时调用。
- 属性和事件若含义不能由名称完整表达，也必须使用 XML 注释。

禁止只写“初始化”“每帧调用”“处理数据”这类没有职责信息的注释。

好的注释回答：

- 这个类型/函数负责什么，不负责什么。
- 调用时机或前置条件是什么。
- 会修改哪些重要状态或触发哪些外部结果。
- 为什么采用看似不直观的处理方式。

### 9.2 示例

```csharp
namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 使用 CharacterController 执行玩家位移与重力，并向表现层暴露本帧实际速度。
    /// 本类不读取输入，也不决定动作是否允许执行。
    /// </summary>
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private CharacterController mCharacterController;
        private Vector3 mVerticalVelocity;

        /// <summary>
        /// 获取最近一次移动后得到的世界空间水平速度。
        /// </summary>
        public Vector3 HorizontalVelocity { get; private set; }

        /// <summary>
        /// 缓存必需组件并在进入玩法前验证 Prefab 引用完整性。
        /// </summary>
        private void Awake()
        {
            if (mCharacterController == null)
            {
                mCharacterController = GetComponent<CharacterController>();
            }
        }

        /// <summary>
        /// 将已解析的世界方向转换为一帧 CharacterController 位移，并合并重力结果。
        /// </summary>
        /// <param name="_worldDirection">长度不超过 1 的 XZ 平面移动方向。</param>
        /// <param name="_speed">本帧使用的最终水平速度。</param>
        /// <param name="_deltaTime">由玩法时间源提供的帧时间。</param>
        public void TickMovement(Vector3 _worldDirection, float _speed, float _deltaTime)
        {
            Vector3 _clampedDirection = Vector3.ClampMagnitude(_worldDirection, 1.0f);
            Vector3 _horizontalMotion = _clampedDirection * (_speed * _deltaTime);
            mCharacterController.Move(_horizontalMotion + mVerticalVelocity * _deltaTime);
        }
    }
}
```

字段含义可用 `[Tooltip]` 补充 Inspector 说明；Tooltip 不能替代类和函数职责注释。

### 9.3 TODO 规范

使用可搜索格式：

```csharp
// TODO(owner-or-issue): 说明待办原因与完成条件。
```

不提交没有上下文的 `TODO: fix later`。阻塞性问题同时记录到任务清单或 Issue。

---

## 10. Unity 生命周期规范

### 10.1 生命周期职责

| 方法 | 约定职责 |
| --- | --- |
| `Awake` | 缓存同对象依赖、创建纯 C# 状态、验证必需配置 |
| `OnEnable` | 订阅事件、启用本对象拥有的输入/计时 |
| `Start` | 仅处理需要其他对象已完成 Awake 的首次绑定 |
| `Update` | 输入消费、非物理玩法 Tick；避免分配和查找 |
| `FixedUpdate` | 只用于 Rigidbody 物理推进，不因习惯把全部玩法放入 |
| `LateUpdate` | 相机跟随等依赖本帧最终位置的表现 |
| `OnDisable` | 与 OnEnable 对称退订、停止输入/计时、清空短期状态 |
| `OnDestroy` | 最终 Dispose、解除长期绑定；不能成为唯一清理路径 |

### 10.2 事件订阅

- `OnEnable` 订阅的事件必须在 `OnDisable` 退订。
- 显式 `Bind` 必须有幂等的 `Unbind`。
- Lambda 订阅必须保存委托引用，否则无法正确退订。
- 静态事件必须特别审查，默认禁止用于跨场景玩法通信。
- 发布者销毁顺序不确定时，订阅者清理必须能安全重复执行。

### 10.3 依赖获取

- 必需的同 Prefab 组件用 `[RequireComponent]`、序列化引用或 `Awake` 缓存。
- 禁止在 Update/FixedUpdate 中调用 `GetComponent`、`Find`、`FindAnyObjectByType`。
- 场景级依赖由 Bootstrap/Battle Composition Root 显式装配。
- 不使用大量 `DontDestroyOnLoad` 单例；只有 Bootstrap 管理的长期服务允许跨场景存在。
- 不通过对象名称或 Hierarchy 路径寻找关键业务对象。

### 10.4 协程和异步

- 简单的帧序列或纯表现可使用 Coroutine。
- 有取消、失败、返回结果或资源所有权的流程优先使用 Task/UniTask 等项目已批准方案；当前未引入额外异步库前使用 Unity/标准 API。
- 异步操作必须定义 Owner、取消时机、过期结果处理和异常路径。
- 对象关闭不等于底层 Addressables 操作一定取消；完成后的过期结果必须释放。
- `async void` 仅允许 Unity 事件入口且内部捕获并记录异常；普通业务方法返回 Task。

---

## 11. 架构与职责规范

### 11.1 Composition Root

- `Bootstrap` 是长期服务的唯一 Composition Root。
- `Battle` 有本局 Composition Root，负责连接 Player、Wave、BattleFlow 和表现。
- 业务对象不在运行时自行搜索全局服务。
- Service Locator 和全局可变单例默认禁止；若确有必要，必须记录生命周期、线程/场景边界和测试替换方式。

### 11.2 输入、规则与表现

```text
Input/AI Intent → Controller → Authority/Rules → Result/Event → Presentation/UI
```

- Input 只产生意图。
- UI 只提交用户选择和展示只读状态。
- Animator、VFX 和 Audio 不修改权威 Gameplay State。
- Health、Damage、Wave 和 Result 不引用具体 UI。
- Animation Event 只通知明确的时间点，不在事件函数里散落伤害公式。

### 11.3 状态机

- 只有存在互斥状态、转移规则和进入/退出清理时才使用状态机。
- Idle/Move 等可由连续参数表达的表现不强制做业务状态。
- 状态转移集中管理，不允许外部直接赋值当前状态。
- 每个状态明确 Enter、Tick、Exit；强制中断也必须执行清理。
- Animator 状态机不是 Gameplay 状态的权威来源。

### 11.4 数据分类

- Definition：ScriptableObject，编辑期创建、运行时只读。
- Runtime State：组件或普通对象持有，只在本局/本次运行存在。
- Save Data：纯 DTO，不含 UnityEngine.Object 引用。
- Network State：只同步远端需要且具有明确所有权的状态。

禁止运行时直接修改共享 ScriptableObject 资产。需要动态数值时，从 Definition 读取基础值并计算 Runtime 值。

### 11.5 稳定 ID

- 技能、敌人、能力和存档条目使用显式稳定 ID。
- 不使用显示名称、Prefab 名称、数组下标或 GetInstanceID 作为持久化/网络 ID。
- ID 一旦进入存档或正式版本，不随资源重命名而改变。
- Editor 校验工具负责检查重复与缺失 ID。

---

## 12. 核心系统专项规范

### 12.1 Input System

- 项目输入资产统一为 `ProjectRelayInput.inputactions`。
- Gameplay、UI、Debug 使用独立 Action Map。
- 自动生成类只由明确的 Input Source/Service 持有。
- 输入 callback 只更新意图或缓冲，不直接执行玩法结果。
- 输入启用、禁用、订阅、退订必须对称。
- 联机阶段只有本地 Owner 启用玩家输入。

### 12.2 战斗

- 伤害计算不写在输入、UI、动画或 VFX 脚本中。
- Health 是生命状态唯一写入口，死亡事件只发送一次。
- 命中检测、伤害计算、生命应用和表现反馈分层。
- 同一次攻击对同一目标是否允许重复命中必须显式配置/记录。
- 单机使用本地 Authority；联机由 Host 校验和修改权威结果。

### 12.3 UI

- View 不直接访问 Save 文件、Addressables 或 NetworkManager。
- 页面通过 Presenter/ViewModel 或明确接口消费业务状态。
- 页面打开/关闭必须有生命周期，事件订阅与资源释放成对。
- Loading 使用引用计数或 token，禁止多个流程互相直接隐藏。
- 异步页面必须识别关闭后的过期结果。
- UI 不自行维护生命、波次和胜负的第二份权威状态。

### 12.4 Save

- 只序列化 DTO。
- 保存路径、序列化和业务 Capture/Restore 分离。
- 使用 SchemaVersion、临时文件、成功替换和备份恢复。
- 保存失败不得破坏上一次有效文件。
- 不保存 Transform、GameObject、MonoBehaviour 或运行时 Instance ID。

### 12.5 Addressables

- Gameplay/UI 不直接调用 `Addressables.Load...`。
- 统一通过 `IAssetService` 和 Lease/Owner 语义加载、实例化和释放。
- 每次加载都必须能回答“谁拥有 Handle、何时释放”。
- 对象池归还实例不等于可以释放底层资源；池和资源 Lease 生命周期必须一起设计。
- Addressables 生成目录与 content state 文件按官方流程管理，不手工随意编辑。

### 12.6 Network

- Owner 读取本地输入并负责第一版玩家移动。
- Host 负责伤害、生命、死亡、敌人、Boss、波次、奖励和胜负。
- 持续状态使用 NetworkVariable/快照，瞬时行为使用 RPC 或消息；按语义选择。
- 不把每个字段都做成 NetworkVariable。
- 不在所有玩法脚本中散落 `IsServer`/`IsOwner` 分支；通过网络 Bridge 或 Command Gateway 隔离。
- 普通 VFX、音频和伤害数字默认本地表现，不创建装饰性 NetworkObject。

---

## 13. 错误处理与日志

- 可恢复错误返回明确 Result 或抛出由上层捕获的特定异常，不使用 bool 丢失失败原因。
- Prefab 必需引用缺失时快速失败，并用 `Debug.LogError(message, context)` 指向具体对象。
- 网络、存档、资源和更新错误必须包含阶段、资源/玩家 ID、错误码或异常信息。
- 不在每帧路径重复打印同一错误；使用一次性、节流或状态变化日志。
- 不吞异常；如果选择降级继续，日志必须写清降级行为。
- 正式 Build 可降低普通调试日志，但错误恢复和诊断信息保留。
- 禁止在日志中输出 Token、个人路径中的敏感信息或可识别账号数据。

推荐日志前缀：

```text
[Gameplay]
[UI]
[Save]
[Assets]
[Update]
[Network]
[Build]
```

---

## 14. 性能规范

- 不以“可能更快”为理由提前复杂化；先用 Profiler 定位。
- 高频 Update 中禁止 LINQ、装箱、字符串拼接和临时集合分配。
- 重复查询使用缓存；NonAlloc API 只有在测量表明确实需要时采用。
- 对象池在对象频繁生成销毁且 Profiler 显示收益时引入，不建立无使用者的通用池框架。
- 配置读取不应每帧使用字符串查找或反射。
- 避免所有敌人每帧执行昂贵全场搜索；目标选择和路径更新使用合理频率。
- 优化提交必须记录测试场景、优化前后数据和是否改变行为。

关键性能检查点：

- 空 Battle 场景基线。
- 1 名玩家 + 10 个敌人。
- 普通攻击/VFX 峰值。
- Battle 连续进入退出 10 次。
- UI 页面连续打开关闭。
- Host + Client 双开。

---

## 15. 测试规范

### 15.1 测试分层

- EditMode：纯规则、状态机、数据迁移、ID 校验和错误边界。
- PlayMode：MonoBehaviour 生命周期、输入、物理命中、场景、Prefab 和协程。
- Build Smoke Test：启动、完整一局、退出和重新进入。
- 双开测试：连接、Spawn、战斗、断线和返回。

### 15.2 测试命名

测试方法采用：

```text
MethodName_Condition_ExpectedResult
```

示例：

```text
ApplyDamage_WhenHealthReachesZero_RaisesDiedOnce
TryDash_WhenCooldownActive_ReturnsFalse
LoadSave_WhenPrimaryCorrupted_UsesBackup
```

测试代码同样遵守变量前缀、一文件一类和职责注释规范。

### 15.3 Bug 修复要求

能稳定复现且适合自动化的 Bug，先增加失败测试再修复。无法自动化时，在 Issue/记录中写明：

- 复现步骤。
- 预期与实际结果。
- 影响版本和平台。
- 修复验证步骤。

---

## 16. Git 与提交规范

### 16.1 分支建议

```text
main
feat/player-control
feat/combat-core
fix/dash-wall-lock
docs/core-gameplay-plan
spike/ngo-authority
```

- `main` 始终保持可打开、可编译。
- 功能、修复和 Spike 使用短生命周期分支。
- Spike 结果不直接混入正式流程，先整理结论和可保留代码。

### 16.2 Commit 格式

```text
[Gameplay] Add camera-relative player movement
[Combat] Add single-hit damage resolution
[UI] Fix HUD event unsubscription
[Docs] Define core gameplay development order
[Build] Lock Unity and package versions
```

一次提交只解决一个可描述的问题。提交前至少检查：

- Unity 编译通过。
- Console 无新增 Error。
- `.meta` 完整。
- 没有误提交 Library、临时 Build 或个人配置。
- 对应测试通过。

### 16.3 Unity 项目设置

- Asset Serialization：Force Text。
- Version Control Mode：Visible Meta Files。
- 锁定 Unity 补丁版本和核心 Package 版本。
- 场景、Prefab 等 YAML 冲突必须在 Unity 中打开验证，不只依赖文本合并结果。

---

## 17. 文档规范

项目文档统一放在 `Docs/`，使用 UTF-8 Markdown：

```text
Docs/
├── ProjectPlan.md
├── CoreGameplayDevelopmentPlan.md
├── PlayerControlModulePlan.md
├── ProjectStandards.md
├── Decisions/                   # 出现真实 ADR 时创建
├── Testing/                     # 测试记录与矩阵
├── Profiling/                   # Profiler/Memory/Frame Debugger 记录
└── Releases/                    # 版本说明与已知问题
```

文档规则：

- 每份计划写版本、日期、适用范围和上位/关联文档。
- 路径、类名、命令和代码使用反引号。
- 截图注明 Unity 版本、场景和采样条件。
- 重要架构变化使用 ADR，内容包括背景、决定、替代方案和后果。
- 功能完成时同步更新对应计划的状态、验收结果和已知问题。
- 文档不复制大段容易过期的代码实现，优先描述职责、边界和验证方式。

### 17.1 功能模块细则计划格式

玩家控制、战斗、敌人、波次、UI 页面等具体功能的开发细则必须是面向执行的短计划，只包含以下内容：

1. **本次实现内容**：列出本轮实际交付的功能，并明确本轮不做什么。
2. **实现思路**：说明核心数据流、主要职责边界和必须固定的技术选择。
3. **文件管理**：列出新增或修改的目录、脚本和配置资产，并写清每个类型的职责。
4. **逐步执行计划**：按真实依赖顺序拆分步骤。每一步必须同时写明：
   - 本步要完成的结果。
   - 涉及的新建或修改脚本。
   - 需要在 Unity Editor 中完成的配置和引用。
   - 可以进入下一步的检查条件。
5. **最终验收**：使用可操作、可观察的清单描述功能完成标准。

细则计划不得重复上位总体规划中的大段背景、长期架构、风险分析和远期功能。已经完成并通过验证的模块只作为“现有前置条件”简要说明，不得再次列入待执行步骤。计划应让开发者能快速看清本轮功能、涉及类型、执行顺序、Editor 配合和验收结果。

---

## 18. Code Review 检查清单

### 18.1 结构与职责

- [ ] 一个 `.cs` 文件只有一个顶层类型，文件名匹配。
- [ ] 类只承担一个清楚职责，没有同时处理输入、规则和 UI。
- [ ] 程序集和命名空间依赖方向正确，无循环引用。
- [ ] 没有为尚不存在的需求增加多余抽象。

### 18.2 命名与注释

- [ ] 私有成员使用 `mVar` 格式。
- [ ] 公有变量、属性和事件直接使用 PascalCase，不添加 `Pub` 前缀。
- [ ] 参数和局部变量使用 `_var` 格式。
- [ ] 每个类和函数有具体职责注释。
- [ ] public API 的参数、返回值和重要副作用写清楚。

### 18.3 Unity 生命周期

- [ ] 事件订阅/退订成对。
- [ ] 对象禁用、销毁和场景退出路径可安全重复执行。
- [ ] Update 中没有组件查找、LINQ 或明显每帧分配。
- [ ] ScriptableObject 在运行时只读。
- [ ] Inspector 必需引用有验证和清晰错误。

### 18.4 系统边界

- [ ] 输入和 UI 没有直接修改权威 Gameplay State。
- [ ] 伤害、死亡、波次和胜负不由表现层决定。
- [ ] 存档不包含 UnityEngine.Object。
- [ ] Addressables Handle/Lease Owner 明确。
- [ ] 联机状态的 Owner/Host/Client 权责明确。

### 18.5 验证

- [ ] 成功路径和至少一个失败/取消路径已验证。
- [ ] 对应 EditMode/PlayMode 测试通过。
- [ ] Console 无新增 Error 或未解释 Warning。
- [ ] 需要性能关注的改动有 Profiler 数据。
- [ ] 目录、资源名、`.meta` 和文档同步更新。

---

## 19. 功能完成标准

一个功能只有同时满足以下条件才可标记 Done：

1. 主路径可在目标场景或 Build 中实际操作。
2. 失败、取消、禁用和退出路径不会留下脏状态。
3. 代码符合目录、命名、注释和一文件一类型规范。
4. 事件、资源、输入、异步和网络所有权清楚。
5. 对应层级的测试通过。
6. 没有新增阻塞性 Console Error、持续 GC Alloc 或对象泄漏。
7. 配置具有合法默认值和 Inspector 说明。
8. 文档、已知问题和验收记录已同步。
9. Windows Development Build 中完成最小冒烟测试。

规范不是一次性文档。若实现证明某条规则不适合项目，应先记录变更原因、更新本文版本，再统一修改相关代码；不得只在个别文件中悄悄采用另一套风格。
