using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 表达 Player 锁定进入方向并以固定速度移动的 Dash 控制状态。
    /// </summary>
    public sealed class PlayerDashState : PlayerControlState
    {
        private const float mBlockedSpeedRatio = 0.5f;

        /// <summary>获取 Dash 状态标识。</summary>
        public override PlayerControlStateId Id => PlayerControlStateId.Dash;

        /// <summary>
        /// 进入 Dash 时使用由 PlayerDashRuntime 已经接受并锁定的方向。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">携带本次 Dash 方向的已接受转移。</param>
        public override void Enter(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
        }

        /// <summary>
        /// Dash 持续期间忽略互斥动作输入，到期后按当前移动输入返回 Idle 或 Move。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <param name="_deltaTime">本帧安全时间增量。</param>
        /// <returns>到期时返回目标移动状态请求，否则返回 None。</returns>
        public override PlayerControlTransitionRequest Tick(
            PlayerControlContext _context,
            in PlayerControlInput _input,
            float _deltaTime)
        {
            _context.DashRuntime.ClearInputBuffer();

            if (!_context.DashRuntime.HasEnded(_context.ElapsedTime))
            {
                return PlayerControlTransitionRequest.None;
            }

            return CreateMovementTransition(
                _input,
                PlayerControlTransitionReason.DashCompleted);
        }

        /// <summary>
        /// 使用进入时锁定的方向和配置速度创建不可转向的 Dash 输出。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <returns>固定 Dash 速度和锁定朝向。</returns>
        public override PlayerControlOutput CreateOutput(
            PlayerControlContext _context,
            in PlayerControlInput _input)
        {
            Vector3 _direction = _context.DashRuntime.Direction;
            return new PlayerControlOutput(
                _direction * _context.MovementConfig.DashSpeed,
                false,
                _direction != Vector3.zero,
                _direction);
        }

        /// <summary>
        /// 离开 Dash 时从理论结束时刻或实际中断时刻开始计算冷却。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">即将执行的 Dash 退出转移。</param>
        public override void Exit(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
            bool _didCompleteNaturally =
                _transition.Reason == PlayerControlTransitionReason.DashCompleted;
            _context.DashRuntime.End(
                _context.ElapsedTime,
                _didCompleteNaturally);
        }

        /// <summary>
        /// 在侧面碰撞使沿 Dash 方向实际速度不足时立即请求结束 Dash。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_movementResult">Motor 完成位移后的不可变结果。</param>
        /// <returns>确认明显受阻时返回 Idle 或 Move 请求，否则返回 None。</returns>
        public override PlayerControlTransitionRequest ReportMovementResult(
            PlayerControlContext _context,
            in PlayerMovementResult _movementResult)
        {
            if (
                !_context.MovementConfig.EndDashWhenBlocked ||
                (_movementResult.CollisionFlags & CollisionFlags.Sides) == 0)
            {
                return PlayerControlTransitionRequest.None;
            }

            Vector3 _actualVelocity = _movementResult.HorizontalVelocity;
            _actualVelocity.y = 0.0f;
            float _speedAlongDash = Vector3.Dot(
                _actualVelocity,
                _context.DashRuntime.Direction);
            float _minimumAllowedSpeed =
                _context.MovementConfig.DashSpeed * mBlockedSpeedRatio;

            return _speedAlongDash <= _minimumAllowedSpeed
                ? CreateMovementTransition(
                    _context.CurrentInput,
                    PlayerControlTransitionReason.DashBlocked)
                : PlayerControlTransitionRequest.None;
        }

        /// <summary>
        /// 根据当前移动输入创建 Dash 完成或受阻后的目标状态请求。
        /// </summary>
        /// <param name="_input">用于选择 Idle 或 Move 的当前输入。</param>
        /// <param name="_reason">DashCompleted 或 DashBlocked。</param>
        /// <returns>目标为 Idle 或 Move 的转移请求。</returns>
        private static PlayerControlTransitionRequest CreateMovementTransition(
            in PlayerControlInput _input,
            PlayerControlTransitionReason _reason)
        {
            PlayerControlStateId _targetStateId = _input.HasMoveInput
                ? PlayerControlStateId.Move
                : PlayerControlStateId.Idle;
            return PlayerControlTransitionRequest.Create(
                _targetStateId,
                _reason);
        }
    }
}
