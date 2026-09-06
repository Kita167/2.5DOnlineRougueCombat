using System;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存 Player 控制状态共享的只读配置和状态机拥有的帧运行时数据。
    /// 本类不读取输入、不决定状态转移，也不执行 CharacterController 位移。
    /// </summary>
    public sealed class PlayerControlContext
    {
        private PlayerControlOutput mCurrentOutput;
        private PlayerControlInput mCurrentInput;

        /// <summary>获取所有控制状态共享的只读移动配置。</summary>
        public PlayerMovementConfig MovementConfig { get; }

        /// <summary>获取基础攻击命令和阶段运行时桥接器；纯移动状态机可以为空。</summary>
        public PlayerBasicAttackDriver AttackDriver { get; }

        /// <summary>获取当前状态机拥有的 Dash 运行时。</summary>
        public PlayerDashRuntime DashRuntime { get; }

        /// <summary>获取状态机从最近一次完整重置后累计的安全运行时间。</summary>
        public float ElapsedTime { get; private set; }

        /// <summary>获取当前状态为本帧生成的完整控制输出。</summary>
        public PlayerControlOutput CurrentOutput => mCurrentOutput;

        /// <summary>获取最近一次 Tick 提交的不可变输入快照。</summary>
        public PlayerControlInput CurrentInput => mCurrentInput;

        /// <summary>
        /// 使用必需移动配置创建玩家控制状态共享上下文。
        /// </summary>
        /// <param name="_movementConfig">状态计算速度时使用的只读配置。</param>
        /// <exception cref="ArgumentNullException">移动配置为空时抛出。</exception>
        public PlayerControlContext(PlayerMovementConfig _movementConfig)
            : this(_movementConfig, null)
        {
        }

        /// <summary>
        /// 使用移动配置和可选基础攻击桥接器创建玩家控制状态共享上下文。
        /// </summary>
        /// <param name="_movementConfig">状态计算速度时使用的只读配置。</param>
        /// <param name="_attackDriver">提交并观察基础攻击的桥接器；纯移动测试可以为空。</param>
        /// <exception cref="ArgumentNullException">移动配置为空时抛出。</exception>
        public PlayerControlContext(
            PlayerMovementConfig _movementConfig,
            PlayerBasicAttackDriver _attackDriver)
        {
            MovementConfig =
                _movementConfig ?? throw new ArgumentNullException(nameof(_movementConfig));
            AttackDriver = _attackDriver;
            DashRuntime = new PlayerDashRuntime(MovementConfig);
            ResetRuntime();
        }

        /// <summary>
        /// 推进状态机拥有的单调累计时间；调用方必须已经过滤非法和负时间。
        /// </summary>
        /// <param name="_deltaTime">本帧有效的非负时间增量。</param>
        internal void AdvanceTime(float _deltaTime)
        {
            ElapsedTime += _deltaTime;
            DashRuntime.Advance(ElapsedTime);
        }

        /// <summary>
        /// 保存本帧输入，使移动完成后的状态回报仍能选择正确目标状态。
        /// </summary>
        /// <param name="_input">本帧已经完成安全过滤的不可变输入。</param>
        internal void SetInput(in PlayerControlInput _input)
        {
            mCurrentInput = _input;
        }

        /// <summary>
        /// 保存当前状态生成的最终控制输出，供 PlayerController 只读消费。
        /// </summary>
        /// <param name="_output">本帧已经完成状态规则计算的输出。</param>
        internal void SetOutput(in PlayerControlOutput _output)
        {
            mCurrentOutput = _output;
        }

        /// <summary>
        /// 清空累计时间和控制输出，使重复禁用和重新初始化不会继承旧运行时数据。
        /// </summary>
        internal void ResetRuntime()
        {
            ElapsedTime = 0.0f;
            mCurrentInput = default;
            mCurrentOutput = default;
            DashRuntime.Reset();
            AttackDriver?.ForceReset();
        }
    }
}
