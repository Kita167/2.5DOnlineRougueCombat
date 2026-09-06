namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 定义一个 Player 互斥控制状态统一的进入、逐帧更新、输出、退出和移动回报契约。
    /// 状态只能提交转移请求，不能直接修改状态机的当前状态。
    /// </summary>
    public abstract class PlayerControlState
    {
        /// <summary>获取当前状态实例对应的稳定状态标识。</summary>
        public abstract PlayerControlStateId Id { get; }

        /// <summary>
        /// 在状态机成功切换到本状态后初始化本状态拥有的运行时数据。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">已经成功接受的状态转移。</param>
        public abstract void Enter(
            PlayerControlContext _context,
            in PlayerControlTransition _transition);

        /// <summary>
        /// 处理本帧输入并返回一个可选转移请求；本方法不直接写入当前状态。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <param name="_deltaTime">已经过滤为有限非负值的帧时间。</param>
        /// <returns>需要状态机处理的转移请求，不需要切换时返回 None。</returns>
        public abstract PlayerControlTransitionRequest Tick(
            PlayerControlContext _context,
            in PlayerControlInput _input,
            float _deltaTime);

        /// <summary>
        /// 根据本状态规则创建可直接交给 Facing 和 Motor 的本帧输出。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <returns>本状态计算完成的控制输出。</returns>
        public abstract PlayerControlOutput CreateOutput(
            PlayerControlContext _context,
            in PlayerControlInput _input);

        /// <summary>
        /// 在状态机离开本状态前清理只属于本状态的运行时数据。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">即将执行的状态转移。</param>
        public abstract void Exit(
            PlayerControlContext _context,
            in PlayerControlTransition _transition);

        /// <summary>
        /// 接收 Motor 完成移动后的实际结果；默认状态不需要处理该反馈。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_movementResult">本帧不可变移动结果。</param>
        /// <returns>移动结果要求立即结束当前状态时返回转移请求，否则返回 None。</returns>
        public virtual PlayerControlTransitionRequest ReportMovementResult(
            PlayerControlContext _context,
            in PlayerMovementResult _movementResult)
        {
            return PlayerControlTransitionRequest.None;
        }

        /// <summary>
        /// 按 Dash 优先于 Attack 的固定规则处理 Idle 和 Move 可发起的动作。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <returns>成功开始动作时返回对应状态请求，否则返回 None。</returns>
        protected static PlayerControlTransitionRequest TryStartAction(
            PlayerControlContext _context,
            in PlayerControlInput _input)
        {
            if (_input.IsDashPressed)
            {
                _context.DashRuntime.BufferInput(_context.ElapsedTime);
            }

            if (
                _context.DashRuntime.TryBegin(
                    _input,
                    _context.ElapsedTime,
                    out UnityEngine.Vector3 _dashDirection))
            {
                return PlayerControlTransitionRequest.Create(
                    PlayerControlStateId.Dash,
                    PlayerControlTransitionReason.DashStarted,
                    _dashDirection);
            }

            if (
                _input.IsAttackPressed &&
                _context.AttackDriver != null &&
                _context.AttackDriver.TryStartAttack(_input.FacingDirection))
            {
                _context.DashRuntime.ClearInputBuffer();
                return PlayerControlTransitionRequest.Create(
                    PlayerControlStateId.Attack,
                    PlayerControlTransitionReason.AttackStarted,
                    _input.FacingDirection);
            }

            return PlayerControlTransitionRequest.None;
        }
    }
}
