using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 表达基础攻击 Windup、Active 和 Recovery 占用 Player 控制权的状态。
    /// 攻击阶段、命中和冷却仍由独立的 BasicAttackController 管理。
    /// </summary>
    public sealed class PlayerAttackState : PlayerControlState
    {
        private Vector3 mLockedFacingDirection;
        private float mMovementSpeedMultiplier;

        /// <summary>获取 Attack 状态标识。</summary>
        public override PlayerControlStateId Id => PlayerControlStateId.Attack;

        /// <summary>
        /// 缓存本次已接受攻击的锁定朝向和配置移动倍率。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">携带攻击锁定方向的已接受转移。</param>
        public override void Enter(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
            mLockedFacingDirection = _transition.HasDirection
                ? _transition.Direction
                : _context.AttackDriver != null
                    ? _context.AttackDriver.LockedAttackDirection
                    : Vector3.zero;
            mMovementSpeedMultiplier =
                _context.AttackDriver?.Config != null
                    ? Mathf.Clamp01(
                        _context.AttackDriver.Config.MovementSpeedMultiplier)
                    : 0.0f;
            _context.DashRuntime.ClearInputBuffer();
        }

        /// <summary>
        /// 攻击占用期间拒绝互斥动作，Recovery 完成后按移动输入返回 Idle 或 Move。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <param name="_deltaTime">本帧安全时间增量。</param>
        /// <returns>攻击动作结束时返回移动状态请求，否则返回 None。</returns>
        public override PlayerControlTransitionRequest Tick(
            PlayerControlContext _context,
            in PlayerControlInput _input,
            float _deltaTime)
        {
            _context.DashRuntime.ClearInputBuffer();

            if (
                _context.AttackDriver != null &&
                _context.AttackDriver.IsAttackInProgress)
            {
                return PlayerControlTransitionRequest.None;
            }

            PlayerControlStateId _targetStateId = _input.HasMoveInput
                ? PlayerControlStateId.Move
                : PlayerControlStateId.Idle;
            return PlayerControlTransitionRequest.Create(
                _targetStateId,
                PlayerControlTransitionReason.AttackCompleted);
        }

        /// <summary>
        /// 应用攻击配置移动倍率并持续输出进入攻击时锁定的朝向。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <returns>攻击期间的受限水平速度和锁定朝向。</returns>
        public override PlayerControlOutput CreateOutput(
            PlayerControlContext _context,
            in PlayerControlInput _input)
        {
            Vector3 _horizontalVelocity =
                _input.MoveDirection *
                _context.MovementConfig.MoveSpeed *
                mMovementSpeedMultiplier;
            return new PlayerControlOutput(
                _horizontalVelocity,
                false,
                mLockedFacingDirection != Vector3.zero,
                mLockedFacingDirection);
        }

        /// <summary>
        /// 离开攻击状态时清除本状态缓存；禁用和强制重置还会中断攻击执行器。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">即将执行的攻击退出转移。</param>
        public override void Exit(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
            if (
                _transition.To == PlayerControlStateId.Disabled ||
                _transition.Reason == PlayerControlTransitionReason.ForceReset)
            {
                _context.AttackDriver?.ForceReset();
            }

            mLockedFacingDirection = Vector3.zero;
            mMovementSpeedMultiplier = 0.0f;
        }
    }
}
