# Unity传统游戏客户端暑期实习作品项目计划书

## 可执行修订版 v2.0

**开发周期：** 2026年9月—2027年2月
**可投递版本截止：** 2027年1月31日
**最终包装截止：** 2027年2月下旬
**集中投递时间：** 职位开放后立即开始，不等待所有加分项完成
**目标岗位：** Unity客户端开发实习生、游戏客户端开发实习生、UI/业务客户端实习生
**主要平台：** Windows PC
**项目类型：** 低多边形3D俯视角双人联机生存挑战
**项目名称：** Project Relay（暂定）

---

# 一、项目定位

本项目通过一个6～8分钟、从启动到结算完整可玩的游戏Demo，展示以下能力：

1. UGUI业务界面和生命周期管理。
2. 本地数据持久化与版本迁移。
3. Addressables资源生命周期管理。
4. Addressables远程资源更新。
5. 基于Netcode for GameObjects的双人联机。
6. Host权威战斗逻辑。
7. 异步流程、错误处理与状态恢复。
8. Profiler、Memory Profiler和Frame Debugger分析能力。
9. Windows IL2CPP构建与完整作品交付。
10. 条件允许时，展示HybridCLR代码热更新。

项目不追求庞大的玩法内容，而是证明：

> 能够把UI、存档、资源、网络和游戏流程组织成可维护、可调试、可验证的客户端系统，并最终交付一个稳定的可执行版本。

---

# 二、项目优先级

所有需求分成四个等级。

## 2.1 P0：必须完成

如果P0没有完成，项目不进入加分模块。

- 单机完整游戏流程
- 双人联机核心流程
- 核心UGUI框架
- 可靠本地存档
- Addressables资源加载与释放
- 至少一次V1到V2资源热更新
- 开发者诊断面板核心页面
- Windows Mono测试构建
- Windows IL2CPP正式构建
- GitHub README
- 2～3分钟演示视频
- 至少两篇高质量技术总结
- Profiler和Memory Profiler分析记录

## 2.2 P1：尽量完成

- 第二个主动技能
- 第三种普通敌人
- Boss第二阶段
- 多存档槽
- 可复用长列表
- 弱网测试
- 完整资源版本清单
- HybridCLR代码热更新
- 四篇技术总结

## 2.3 P2：有明确余量才完成

- 第二张小型地图
- 第二套皮肤
- 更多UI动效
- 更复杂的Boss技能
- HybridCLR与主项目深度整合
- 局域网IP输入和连接历史

## 2.4 P3：移出半年主计划

以下内容从主排期中删除，不再为其预留固定开发时间：

- DOTS/ECS压力测试
- Android适配
- Netcode for Entities
- Dedicated Server
- Relay和公网服务
- 完整Lobby服务
- 战斗中Late Join
- 断线重连
- Host Migration

只有在Windows正式版、README和视频全部完成以后，才能重新考虑P3内容。

---

# 三、项目范围

## 3.1 最终理想玩法范围

- 1个主要角色
- 普通攻击
- 闪避或冲刺
- 2个主动技能
- 3种普通敌人
- 1个两阶段Boss
- 1张完整战斗地图
- 3个普通波次
- 1个Boss波次
- 局内能力三选一
- 双人联机
- 胜利、失败和战斗结算
- 单局时长6～8分钟

## 3.2 最低可接受玩法范围

如果进度落后，玩法允许降低到：

- 1个角色
- 普通攻击
- 冲刺
- 1个主动技能
- 2种普通敌人
- 1个单阶段Boss
- 1张地图
- 2个普通波次
- 1个Boss波次
- 1次能力三选一
- 双人完整流程
- 单局时长4～6分钟

玩法数量可以减少，但以下工程质量不能删除：

- 战斗退出后状态正确重置
- Host决定伤害和胜负
- 资源正确释放
- UI事件正确退订
- 网络断开后能够退出
- 存档失败不破坏有效数据

---

# 四、核心流程

```text
启动客户端
→ 初始化基础服务
→ 检查资源版本
→ 进入主界面
→ 选择角色配置
→ 开始单机或创建/加入本地房间
→ 玩家准备
→ 进入战斗
→ 完成普通波次
→ 进行能力选择
→ 击败Boss
→ 战斗结算
→ 保存本地进度
→ 返回主界面
```

不接入Relay和Lobby服务时，“创建/加入房间”实际指：

- Host启动本地监听
- Client连接`localhost`或指定局域网IP
- 一台电脑双开测试
- 两台局域网设备连接作为可选验证

README中不把它描述成完整公网房间系统。

---

# 五、技术栈

| 模块      | 方案                                        |
| ------- | ----------------------------------------- |
| Unity版本 | Unity 6.3 LTS，锁定具体补丁版本                    |
| 编程语言    | C#                                        |
| 渲染管线    | URP                                       |
| UI      | UGUI + TextMeshPro                        |
| 输入      | Unity Input System                        |
| 资源管理    | Addressables                              |
| 资源底层认知  | AssetBundle                               |
| 存档      | System.IO + JSON + DTO                    |
| 网络      | Netcode for GameObjects + Unity Transport |
| 资源热更新   | Addressables Remote Catalog               |
| 代码热更新   | HybridCLR，条件式保留                           |
| 性能分析    | Profiler、Memory Profiler、Frame Debugger   |
| 版本管理    | Git + GitHub                              |
| 正式构建    | Windows IL2CPP                            |

Unity 6.3 LTS官方支持到2027年12月，适合锁定版本完成项目。[Unity 6支持周期](https://unity.com/releases/unity-6/support)

项目创建后必须提交并锁定：

- `Packages/manifest.json`
- `Packages/packages-lock.json`
- Unity具体补丁版本
- Addressables配置
- Build Profile
- 第三方插件版本

除非遇到阻塞性Bug，中途不升级Unity大版本和核心Package。

---

# 六、修订后的总体架构

## 6.1 场景结构

初版只保留三个主要场景：

```text
Bootstrap
├── Frontend
└── Battle
```

### Bootstrap

包含长期存在的基础设施：

- GameBootstrap
- SaveService
- AssetService
- UpdateService
- UIService
- AudioService
- NetworkService
- SceneFlowService
- 日志与异常处理
- 全局Loading和错误界面

Bootstrap是Composition Root，负责创建和连接服务，但不承载战斗逻辑。

避免出现大量互相依赖的`DontDestroyOnLoad`单例。

### Frontend

承载：

- 主界面
- 设置
- 角色配置
- Host/Join
- 房间准备

这些内容优先作为同一个场景中的不同UI页面，不拆成多个场景。

### Battle

承载：

- 战斗
- 能力选择
- 暂停
- 胜利和失败
- 战斗结算

Result不再作为独立场景，避免增加网络场景切换复杂度。

## 6.2 Addressables与网络场景边界

主版本采用：

- Bootstrap、Frontend和Battle场景随Player构建
- 联机期间通过NGO的网络场景流程进入Battle
- UI Prefab、角色外观、敌人外观、特效、音频和配置使用Addressables
- 第二张远程地图不进入P0范围

NGO要求需要同步给客户端的网络场景通过`NetworkSceneManager`管理，因此主战斗场景不同时承担“远程Addressable场景”和“NGO Late Join场景同步”两套职责。[NGO场景管理](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects%402.13/manual/basics/scenemanagement/using-networkscenemanager.html)

## 6.3 程序集结构

初期控制在以下范围：

```text
Game.Core
├── 通用接口
├── DTO
├── 稳定ID
├── 事件和结果类型
└── 与Unity低耦合的规则

Game.Runtime
├── GameFlow
├── 玩家
├── 技能
├── 敌人
├── Boss
└── 波次与结算

Game.UI
├── Page
├── Popup
├── HUD
├── Presenter
└── UI配置

Game.Infrastructure
├── Save
├── Addressables
├── Update
├── Network
└── Platform

Game.Tests
└── EditMode和PlayMode测试

Game.Editor
├── 构建工具
├── 存档调试
└── Addressables辅助工具
```

暂时不建立：

- Game.Application
- Game.HotUpdate
- Game.ECS

只有真实需求出现后才拆分。

HybridCLR开始实施时，再添加：

```text
Game.HotUpdate
```

## 6.4 数据分类

| 数据类型            | 示例         | 生命周期        |
| --------------- | ---------- | ----------- |
| Definition Data | 技能、敌人、物品定义 | 编辑期创建，运行时只读 |
| Runtime State   | 当前生命、冷却、波次 | 本局或本次运行     |
| Save Data       | 解锁、设置、成绩   | 跨运行持久化      |
| Network State   | 权威生命、准备状态  | 联机期间存在      |

禁止：

- 直接保存MonoBehaviour
- 保存GameObject或Transform引用
- 把所有Runtime State都写入存档
- 把所有字段都做成NetworkVariable
- 让客户端本地存档成为联机战斗权威

---

# 七、模块计划与截断标准

# 7.1 核心玩法

## 计划目标

- 1个角色
- 普通攻击
- 冲刺
- 2个主动技能
- 3种普通敌人
- 两阶段Boss
- 三选一能力
- 胜负与结算

## 工程要求

- 输入与角色执行逻辑分离
- 技能参数配置化
- 战斗流程有明确状态
- Boss状态和技能执行分离
- 战斗退出时可以统一清理
- 单机和联机共享主要战斗规则
- 伤害计算不直接写在UI或输入脚本中

## 9月21日检查点

必须完成：

```text
玩家移动
→ 普通攻击
→ 敌人受伤
→ 敌人死亡
→ 波次结束
```

如果没有完成，立即删除：

- 第二主动技能
- 第三种普通敌人
- 复杂能力Build
- Boss复杂技能组合

## 9月30日检查点

必须有第一个单机完整Build：

```text
启动
→ 主界面
→ 战斗
→ 简化Boss
→ 结算
→ 返回
```

如果没有完成：

- Boss降低为单阶段
- 普通波次从3个降为2个
- 角色/装备配置暂时只保留一个选项
- 三选一只实现3个简单数值效果
- 取消非必要动画和特效

## 10月31日后

停止增加主要玩法系统。

之后只能：

- 修复Bug
- 调整数值
- 增加已有框架下的配置内容
- 为网络改造权威边界

不再增加新武器系统、装备词条系统或复杂角色机制。

---

# 7.2 UGUI系统

## 必须界面

1. 启动更新界面
2. 主界面
3. 设置Popup
4. 角色配置界面
5. Host/Join与房间准备界面
6. 战斗HUD
7. 能力三选一
8. 暂停界面
9. 战斗结算
10. 通用确认、Toast、Loading和断线提示
11. 开发者诊断面板

不再单独制作：

- 复杂关卡选择页
- 商城
- 邮件
- 任务
- 成就
- 大型背包

## UI框架要求

- UI层级
- 页面栈
- Popup管理
- Loading引用计数
- 异步页面加载
- 识别过期打开结果
- 页面生命周期
- 事件订阅和退订
- 页面资源释放
- View与业务逻辑分离
- 输入屏蔽

推荐层级：

```text
Background
Normal
Popup
Guide
Loading
System
```

## 10月15日检查点

必须完成：

- 页面栈
- Popup
- Loading
- 三个实际业务页面
- 页面打开和关闭
- 基础事件退订
- 最小SaveService

如果没有完成，立即：

- 合并Host/Join和房间准备页面
- 设置界面改成Popup
- 删除独立关卡选择页
- 删除非核心UI动画
- 暂停制作列表复用

## 10月31日检查点

必须完成：

- 核心UI流程
- 异步页面接口
- 页面关闭后的过期结果处理
- UI/Save诊断页面
- 至少两种桌面分辨率适配
- 页面反复打开关闭测试

如果没有完成，删除：

- 多存档槽UI
- 复杂装备列表
- UI对象池的泛化设计
- 非必要动效
- 多语言正式内容

但不能删除：

- 页面生命周期
- 页面栈
- 事件退订
- 异步结果处理
- 资源释放
- Loading和错误提示

---

# 7.3 数据持久化

## 系统结构

```text
Gameplay
↓ Capture
SaveData DTO
↓
ISaveSerializer
↓
IStorage
↓
File System
```

建议增加：

```text
SaveEnvelope
├── SchemaVersion
├── GameVersion
├── SavedAtUtc
└── Payload
```

## 必须完成

- SaveData DTO
- Capture/Restore
- 稳定ID
- 存档版本号
- V1到V2迁移
- 临时文件写入
- 成功后替换
- 备份
- 主文件损坏恢复
- 自动存档
- 设置即时保存
- 错误日志
- 故障注入测试

## 可以删减

- 多存档槽
- 存档压缩
- 加密
- 云存档
- 多种序列化器的实际实现
- 任意版本间直接迁移
- 复杂校验码系统

“序列化器可替换”只需要通过接口和测试证明，不需要真的实现三套序列化器。

## 10月31日检查点

必须通过：

1. 正常保存和读取。
2. 人为破坏主文件后读取备份。
3. V1存档迁移到V2。
4. 模拟保存失败时保留旧文件。
5. 退出游戏后重新启动可以恢复。

如果未通过，优先删除：

- 多存档槽
- 装备复杂结构
- 多序列化器
- 存档诊断面板的非核心统计

可靠性不能删除。

---

# 7.4 Addressables资源管理

## 管理对象

P0范围：

- UI Prefab
- 角色Prefab或外观
- 敌人Prefab或外观
- 技能特效
- 音频
- 配置

P1范围：

- 图集动态加载
- 战斗场景
- 第二地图

主战斗场景在P0阶段不通过远程Addressables更新。

## 封装目标

```text
Gameplay / UI
↓
IAssetService
↓
AddressablesAssetService
↓
Unity Addressables
```

封装层负责：

- 泛型异步加载
- Prefab实例化
- 释放
- Handle登记
- 加载失败
- 过期请求处理
- 预加载
- Owner追踪
- 对象池配合
- 调试记录

封装层不重新实现Addressables内部引用计数。

推荐暴露类似：

```text
IAssetLease<T>
IInstanceLease
ISceneLease
```

调用者持有Lease，释放Lease时释放对应Handle。

## 11月15日检查点

必须完成：

- UI异步加载
- Prefab加载与实例化
- Lease或明确的Handle所有权
- 加载失败处理
- 过期页面不会实例化
- 对象池与资源释放关系
- Assets诊断页基础数据

如果没有完成，删除：

- Addressables场景加载
- 音频动态管理
- 复杂预加载策略
- 通用缓存算法
- 自动依赖分析工具

保留UI、Prefab和特效三个核心案例。

## 11月30日检查点

必须完成：

- 反复进入退出Battle测试
- UI反复打开关闭测试
- Build Layout分析
- 至少一次重复依赖问题定位
- Memory Profiler前后快照
- Handle数量回到基线

验收不要求Release后内存数字立即下降，而要求：

- 业务层不再持有资源
- Handle回到基线
- 多次循环不存在持续单向增长
- 能解释Bundle依赖为什么仍可能存在

Addressables采用Handle和引用计数管理加载内容，但Released不一定表示内存会在同一帧立即下降。[Addressables内存管理](https://docs.unity3d.com/Packages/com.unity.addressables%402.7/manual/memory-assets.html)

---

# 7.5 资源热更新

## P0更新内容

只要求完成：

- 1个角色皮肤或敌人外观
- 1套技能特效
- 1份配置

第二地图不属于P0。

## 版本清单

除了Catalog，还要建立业务版本清单：

```text
VersionManifest
├── ClientVersion
├── MinCompatibleClientVersion
├── ContentVersion
├── CatalogVersion
└── LogicVersion
```

## 必须完成

- Remote Catalog
- Remote Group
- 检查Catalog更新
- 获取下载大小
- 下载进度
- 下载失败
- 重试
- 无网络提示
- 下载后缓存
- 客户端与内容版本检查
- 保存正式版本的`addressables_content_state.bin`
- V1到V2演示视频

官方内容更新流程要求保存正式发布版本对应的`addressables_content_state.bin`，并基于原发布版本生成更新内容。[Addressables内容更新](https://docs.unity3d.com/Packages/com.unity.addressables%402.7/manual/content-update-builds-overview.html)

## 更新边界

资源更新可以增加：

- 外观
- 材质
- 特效
- 音频
- 已有系统能够读取的配置

资源更新不能单独增加Player中不存在的新C#行为。

## 11月30日检查点

目标是完整V1到V2。

如果没有完成，允许延长到12月7日，但立即降低范围：

- 删除远程地图
- 删除新敌人行为
- 只更新一个皮肤和一个配置
- 使用简单静态HTTP服务器
- 不实现自动旧版本回滚
- 失败时进入重试或错误恢复界面

如果12月7日仍未完成：

- 将热更新流程隔离为项目内的独立演示入口
- 主游戏仍使用Addressables本地资源
- 暂停所有HybridCLR工作
- 先完成资源更新，再进入完整网络开发

资源热更新属于P0，不能直接跳过，但允许从“完整内容更新系统”降低为“可复现的最小V1→V2演示”。

---

# 7.6 网络同步

## 网络模式

```text
玩家A：Host + Client
玩家B：Client
```

P0只保证：

- 本机双开
- `localhost`连接

局域网IP输入属于P1。

## 权威划分

### Owner负责

- 读取本地输入
- 控制本地玩家移动
- 播放即时输入反馈
- 提交技能请求

### Host负责

- 校验技能请求
- 计算伤害
- 修改生命值
- 判断死亡
- 驱动敌人和Boss
- 推进波次
- 生成掉落或奖励
- 决定胜利和失败
- 生成结算结果

### 客户端表现层负责

- 动画
- 音效
- 普通特效
- 伤害数字
- 插值后的远端表现

玩家移动初版采用Owner Authoritative；战斗结果采用Host Authoritative。

不在半年项目中实现完整客户端预测和服务器回滚。NGO的NetworkTransform插值用于改善远端运动连续性，但不把它描述成完整预测系统。[NGO插值说明](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects%402.13/manual/learn/clientside-interpolation.html)

## 状态与事件划分

适合持续同步的状态：

- 生命值
- 准备状态
- 当前波次
- Boss阶段
- 战斗状态
- 能力选择结果

适合RPC或消息的行为：

- 技能请求
- 冲刺请求
- 一次性特效
- 音效通知
- 战斗结束通知

不把每个字段都做成NetworkVariable，也不把持续状态只依靠一次RPC维护。

## 12月15日检查点

必须完成：

- Host/Client启动
- Client连接
- 玩家Spawn
- 准备状态
- 进入Battle
- 玩家移动同步
- 攻击请求
- Host权威扣血

如果没有完成，立即删除：

- 局域网IP历史
- 复杂房间UI
- 网络投射物
- 精确技能前摇同步
- 网络诊断面板非核心图表

只保留两个方块/角色完成攻击和扣血。

## 12月31日检查点

必须完成最小联机流程：

```text
连接
→ 准备
→ 进入Battle
→ 击杀敌人
→ 显示胜负
→ 返回Frontend
```

如果没有完成：

- 敌人降为1种
- Boss降为单阶段
- 掉落物不生成网络对象，改为Host直接发放结果
- 三选一暂时只同步选择结果
- 删除网络暂停
- 删除所有Late Join尝试
- Client断线统一返回主界面
- Host退出时Client统一返回主界面

## 1月15日检查点

必须完成双人完整战斗。

如果没有完成：

- 波次从3个降为2个
- 第二主动技能删除
- 第三种敌人删除
- Boss只保留2个技能
- 能力选择只出现一次
- 所有装饰性NetworkObject改成本地表现
- 不再增加任何网络功能

## 1月25日检查点

网络内容冻结。

此后只允许：

- 修复阻塞Bug
- 修复断线卡死
- 修复重复Spawn
- 修复场景状态残留
- 优化插值参数
- 补充诊断信息
- 完成测试记录

---

# 7.7 HybridCLR代码热更新

## 定位

HybridCLR尽量保留，但不能成为可投递版本的前置条件。

## 前期准备

9月至12月只做以下准备：

- 保持合理的asmdef边界
- 不让Bootstrap依赖未来热更程序集
- 建立资源、客户端和逻辑版本号
- 避免大量跨程序集反向依赖
- 学习AOT、IL2CPP和程序集基础

在核心项目稳定前不正式接入HybridCLR。

## 最小演示目标

```text
V1技能伤害公式
→ 下载新HotUpdate DLL
→ 重启或重新进入业务模块
→ 技能公式改变
→ UI显示新逻辑版本
```

## 程序集边界

```text
AOT主包
├── Bootstrap
├── UpdateService
├── AssetService
├── DLL加载器
├── 日志
└── 错误恢复

HotUpdate
├── 技能数值规则
├── 活动规则
└── 逻辑版本信息
```

不要在第一个版本中把整个UI和战斗系统迁移进热更程序集。

## 1月25日启动条件

只有同时满足以下条件才能正式开始：

- 单机流程稳定
- 双人完整流程已贯通
- 资源更新可复现
- 存档可靠性测试通过
- Windows Mono Build稳定
- 没有阻塞性内存泄漏

如果任意一项不满足，HybridCLR延后到2月，不占用1月核心修复时间。

## 2月10日检查点

如果已在IL2CPP主项目中完成最小热更：

- 保留并继续补充错误恢复
- 录制独立演示
- 在README中说明程序集边界

如果未完成：

- 停止与主工程深度整合
- 改成独立技术Demo
- 主项目只保留逻辑版本设计
- 不继续改动稳定的AOT主流程

## 2月15日最终检查点

如果独立Demo仍不能稳定完成：

- 删除HybridCLR交付要求
- README不宣传代码热更新
- 将学习笔记保留为个人知识总结
- 时间全部转向主项目Bug修复、视频和面试准备

HybridCLR的删除优先级仍低于ECS和Android，但高于破坏主项目稳定性的风险。

---

# 7.8 开发者诊断面板

PC按F1打开。

## P0页面

| 页面       | 内容                             |
| -------- | ------------------------------ |
| Save     | 路径、版本、最后保存时间、备份状态              |
| Assets   | 地址、类型、Handle拥有者、状态、耗时          |
| Update   | 内容版本、Catalog状态、下载状态            |
| Network  | Host/Client、连接数、Tick、Ownership |
| UI       | 页面栈、Popup、Loading计数            |
| Gameplay | 波次、敌人数量、Boss状态                 |

## 删减规则

如果进度不足：

- Update合并到Assets
- Gameplay合并到Network
- 不制作实时曲线
- 不制作复杂图表
- 不制作可视化网络拓扑
- 只保留文本和表格
- ECS页面永久删除

诊断面板的重点是可观察性，不是视觉华丽程度。

---

# 八、修订后的月度排期

# 2026年9月：离线竖切与网络风险验证

## 主要目标

先证明游戏可以运行完整一局，再扩展系统。

## 开发任务

- 创建Unity 6.3 LTS URP项目
- 建立Git仓库
- 确定素材来源与许可
- 玩家移动、攻击、冲刺
- 一种敌人
- 简化Boss
- 波次状态
- 主界面、HUD、结算
- 最小JSON存档
- Bootstrap和GameFlow
- 3～5天NGO独立风险验证

## 月底交付

- v0.1 Windows Build
- 单机完整流程
- 第一段演示视频
- 基础项目结构
- 网络权威边界说明

## 截断

月底单机不完整时，删除第二技能、第三敌人、Boss第二阶段和复杂三选一。

---

# 2026年10月：UGUI与存档

## 主要目标

完成UI和本地数据两条主要作品展示线。

## 开发任务

- UI层级
- 页面栈
- Popup
- Loading
- 页面生命周期
- 异步加载接口
- 过期结果处理
- 设置和角色配置
- SaveEnvelope
- 备份恢复
- V1到V2迁移
- 故障注入
- UI/Save诊断页

## 月底交付

- v0.2
- 核心界面完整
- 存档可靠性测试
- 页面循环打开关闭测试
- 第一篇技术总结

## 截断

删除多存档槽、复杂装备列表、非核心动效和独立关卡选择页。

---

# 2026年11月：Addressables与资源更新

## 主要目标

完成资源生命周期和V1→V2资源更新。

## 开发任务

- IAssetService
- Handle/Lease
- UI与Prefab异步加载
- 对象池配合
- Addressables分组
- Build Layout
- Remote Catalog
- 下载大小和进度
- 错误重试
- VersionManifest
- Memory Profiler
- Assets/Update诊断页

## 月底交付

- v0.3
- 真实资源更新演示
- 资源生命周期分析
- Memory Profiler记录
- 第二篇技术总结

## 截断

删除远程地图、音频动态管理、复杂缓存和自动依赖分析，只保留皮肤、特效、配置更新。

---

# 2026年12月：基础联机

## 主要目标

建立可运行的双人最小闭环。

## 开发任务

- NetworkManager
- Host/Client
- `localhost`连接
- 玩家Spawn
- 准备状态
- 网络场景切换
- 移动同步
- 攻击请求
- Host权威生命
- 简单敌人
- 退出与断线
- Network诊断页

## 月底交付

- v0.4
- 一台电脑双开
- 从房间进入Battle
- 击杀敌人
- 显示结果并返回

## 截断

删除局域网附加功能、投射物同步、复杂房间UI、Late Join和断线重连。

---

# 2027年1月：完整双人战斗与功能冻结

## 主要目标

形成可投递版本，而不是继续堆叠功能。

## 开发任务

- Host驱动敌人
- Boss状态
- 波次
- 能力选择
- 胜负
- 结算
- 本地进度衔接
- 弱网测试
- 多轮完整流程测试
- 资源和事件清理
- README初版
- 演示视频初版

## 1月15日

双人完整流程必须贯通。

## 1月25日

功能冻结。

## 1月31日

形成Portfolio Candidate：

- Windows Build
- 双人完整流程
- README
- 基础演示视频
- 两篇技术总结
- Profiler证据
- 已知问题列表

## 截断

1月15日未贯通时，删除第二技能、第三敌人、第二Boss阶段、网络掉落物和额外能力选择。

---

# 2027年2月：稳定、HybridCLR与作品包装

## 2月1日至10日

优先：

- 阻塞Bug
- IL2CPP构建
- 资源泄漏
- UI事件泄漏
- 存档异常
- 下载失败
- 网络断线

满足启动条件后，尝试HybridCLR最小整合。

## 2月10日至15日

根据检查结果决定：

- 主项目保留HybridCLR
- 降级为独立Demo
- 完全删除

## 2月15日以后

停止所有技术扩展，只进行：

- Windows Release
- README完善
- 架构图
- 性能数据
- 演示视频
- 项目截图
- 简历描述
- 面试问题
- Release Tag
- 第三方资源声明

## 月底交付

- v1.0正式版本
- 2～3分钟主视频
- HybridCLR独立短视频，可选
- 两篇必需技术文章
- 另外两篇文章作为P1
- 面试复盘材料

---

# 九、硬性截止表

| 截止时间   | 必须完成              | 未完成时立即删除或降低          |
| ------ | ----------------- | -------------------- |
| 9月21日  | 基础战斗循环            | 第二技能、第三敌人、复杂能力       |
| 9月30日  | 单机完整一局            | Boss第二阶段、额外波次、复杂装备   |
| 10月15日 | UI框架和最小存档         | 独立页面、UI动画、列表复用暂缓     |
| 10月31日 | UI和可靠存档           | 多槽、复杂装备、多序列化器        |
| 11月15日 | Addressables生命周期  | 场景加载、音频管理、复杂缓存       |
| 11月30日 | V1→V2资源更新         | 远程地图、新敌人，只更新皮肤和配置    |
| 12月7日  | 最小资源更新仍需完成        | 暂停HybridCLR，更新流程独立演示 |
| 12月15日 | 连接、Spawn、移动、攻击    | 局域网附加功能、投射物同步        |
| 12月31日 | 最小双人战斗闭环          | 网络掉落、复杂Boss、复杂房间     |
| 1月15日  | 双人完整一局            | 第二技能、第三敌人、Boss第二阶段   |
| 1月25日  | 功能冻结              | 所有新玩法和新系统            |
| 1月31日  | 可投递版本             | HybridCLR不得阻塞投递      |
| 2月10日  | HybridCLR主项目演示    | 降级为独立Demo            |
| 2月15日  | HybridCLR独立Demo   | 完全删除代码热更新交付          |
| 2月20日  | README、视频、Release | 停止写新技术文章，优先交付        |
| 2月底    | v1.0              | 只修严重Bug              |

---

# 十、主要风险与修复方案

## 10.1 过度设计

### 风险

先编写大量接口、程序集和通用框架，玩法迟迟不能运行。

### 处理

- 任何抽象必须至少有一个真实使用场景。
- 只有出现第二个实现或明显测试需求时才抽接口。
- 9月必须先完成可玩竖切。
- 不提前建立HotUpdate和ECS程序集。

## 10.2 网络接入导致玩法重写

### 风险

单机代码默认本地直接扣血、生成奖励和推进波次。

### 处理

- 9月底进行网络风险验证。
- 输入只产生意图。
- 伤害计算集中到Authority层。
- 表现和权威逻辑分离。
- 单机模式可以使用本地Authority实现相同接口。

## 10.3 Addressables与NGO场景冲突

### 风险

同时使用Addressables远程场景和NGO网络场景同步，增加Late Join和加载顺序问题。

### 处理

- P0主战斗场景随Player构建。
- 网络场景由NGO管理。
- Addressables负责UI、外观、特效和配置。
- 远程地图移到P2。

## 10.4 UI先完成、资源系统后接入导致返工

### 处理

10月的UIService保留页面加载入口，但初期可以使用本地Prefab Provider；11月替换为Addressables实现。

UI页面本身不直接调用：

```csharp
Addressables.LoadAssetAsync
```

## 10.5 误把“取消”理解为终止Addressables底层加载

### 处理

业务层取消主要表示：

- 页面不再需要结果
- 结果到达后不实例化
- 完成后立即释放Handle

不假设所有Addressables异步操作都能真正停止底层加载。

## 10.6 更新失败自动回滚过度承诺

### 处理

P0只保证：

- 检查失败可以重试
- 下载失败不进入依赖新资源的玩法
- 本地错误UI始终可用
- 缓存内容可用时继续使用
- 更新状态和错误原因可观察

不承诺任意阶段的完整事务回滚。

## 10.7 作品完成过晚

### 处理

- 1月31日形成可投递版本。
- 2月是增强和包装，不是第一次形成完整版本。
- 岗位开放后立即投递。
- HybridCLR不影响主版本提交。

## 10.8 文档数量挤占开发

### 处理

必需文章从4篇降低为2篇：

1. Addressables资源生命周期与热更新
2. NGO权威、RPC与状态同步

存档和UGUI文章作为P1。

每月只记录过程，1月至2月再整理成正式文章。

---

# 十一、测试与验收

## 11.1 游戏流程

- 连续完成3局。
- 胜利和失败均可结算。
- 退出Battle后重新进入状态正确。
- 暂停和返回不会卡死。
- 重复进入场景不会重复创建全局服务。

## 11.2 UI

- 页面栈顺序正确。
- 页面关闭后事件退订。
- 异步加载完成时可以识别页面已关闭。
- Loading计数不会变成负数。
- Popup不会重复遮挡或永久锁定输入。
- 两种桌面分辨率下核心UI可用。

## 11.3 存档

- 正常保存和读取。
- 主文件损坏后恢复备份。
- V1迁移到V2。
- 保存失败不覆盖有效文件。
- 不包含运行时Unity对象引用。

## 11.4 资源

- UI打开关闭后Handle回到基线。
- Battle反复进入退出10次无持续单向增长。
- 共享资源不会因单个页面关闭而提前释放。
- 过期异步结果不会生成幽灵对象。
- Build Layout中记录至少一个重复依赖案例。
- V1客户端能够下载V2资源。

## 11.5 网络

- Host和Client能够完成整局。
- Client不能直接修改权威生命值。
- Host负责敌人、Boss、波次和结算。
- Client退出后对象正确销毁。
- Host退出后Client安全返回Frontend。
- 模拟延迟时远端移动基本连续。
- 断线不会永久停留在Loading或Battle。
- 不要求战斗中Late Join和重连。

## 11.6 Build

- Windows Mono开发构建成功。
- Windows IL2CPP正式构建成功。
- 克隆仓库后能够按README构建。
- Package版本明确。
- 不提交Library、Temp和无关大型缓存。
- 第三方资源具有来源与许可说明。

---

# 十二、作品集交付物

## 必须交付

- Windows IL2CPP可执行版本
- GitHub仓库
- README
- 2～3分钟演示视频
- 项目截图
- 架构图
- Profiler分析记录
- Memory Profiler对比
- 已知问题列表
- Release Tag
- 两篇深度技术总结
- 简历项目描述
- 面试问题清单

## HybridCLR条件交付

- 主项目代码热更新，理想
- 独立技术Demo，可接受
- 无稳定实现则不写入简历

## 不在本期交付

- Android版本
- ECS对比场景
- 公网服务器
- Dedicated Server
- 战斗中重连
- Host Migration

---

# 十三、最终完成等级

## A级：理想成果

- 完整单机与双人流程
- 完整UGUI框架
- 可靠存档
- Addressables资源生命周期
- V1→V2资源更新
- 开发者诊断面板
- Windows IL2CPP
- 完整README和视频
- HybridCLR最小主项目演示

## B级：合格且有竞争力

- 完整单机流程
- 双人完整核心流程
- UGUI、存档和Addressables完整
- 最小资源热更新
- Windows IL2CPP
- 诊断面板
- README、Profiler证据和视频
- HybridCLR为独立Demo或未完成

## C级：最低可投递

- 单机完整
- UGUI和存档可靠
- Addressables加载释放清晰
- V1→V2资源更新成功
- 双人能够连接、战斗并结算
- Windows版本稳定
- README和视频完整

如果达到C级，应当按时投递，而不是继续等待HybridCLR、Android或ECS。

---

# 十四、最终执行原则

1. 先形成闭环，再增加深度。
2. 每个月必须产生一个可运行Build。
3. 每个截止日期必须真正执行删减。
4. 新功能不能破坏已有完整流程。
5. 诊断、测试和文档与功能同步产生。
6. 1月25日以后不增加核心功能。
7. 1月31日必须形成可投递版本。
8. ECS和Android不占用半年主排期。
9. HybridCLR只有在P0稳定后才能开始。
10. 项目的最终价值由稳定性、解释深度和验证证据决定，而不是技术名词数量。

最终目标是让面试官得出以下结论：

> 这个候选人不仅能实现游戏功能，还理解UI、数据、资源、网络和异步生命周期；能够控制项目范围、分析性能问题、处理失败路径，并独立交付一个结构清晰且可以实际运行的Unity客户端项目。
