using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 表达 Player 根据当前世界空间输入持续产生普通水平移动的控制状态。
    /// </summary>
    public sealed class PlayerMoveState : PlayerControlState
    {
        /// <summary>获取 Move 状态标识。</summary>
        public override PlayerControlStateId Id => PlayerControlStateId.Move;

        /// <summary>
        /// 进入移动状态时不缓存旧方向，速度始终由最新一帧输入计算。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">已经成功接受的移动转移。</param>
        public override void Enter(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
        }

        /// <summary>
        /// 先尝试 Dash 和 Attack，再在移动输入失效时请求进入 Idle。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <param name="_deltaTime">本帧安全时间增量。</param>
        /// <returns>无移动输入时返回 Idle 请求，否则返回 None。</returns>
        public override PlayerControlTransitionRequest Tick(
            PlayerControlContext _context,
            in PlayerControlInput _input,
            float _deltaTime)
        {
            PlayerControlTransitionRequest _actionRequest =
                TryStartAction(_context, _input);

            if (_actionRequest.HasRequest)
            {
                return _actionRequest;
            }

            return !_input.HasMoveInput
                ? PlayerControlTransitionRequest.Create(
                    PlayerControlStateId.Idle,
                    PlayerControlTransitionReason.MoveStopped)
                : PlayerControlTransitionRequest.None;
        }

        /// <summary>
        /// 根据保留模拟量强度的移动输入和配置速度创建普通移动输出。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <returns>可转向的世界空间水平速度输出。</returns>
        public override PlayerControlOutput CreateOutput(
            PlayerControlContext _context,
            in PlayerControlInput _input)
        {
            Vector3 _horizontalVelocity =
                _input.MoveDirection * _context.MovementConfig.MoveSpeed;

            return new PlayerControlOutput(
                _horizontalVelocity,
                true,
                false,
                Vector3.zero);
        }

        /// <summary>
        /// 离开普通移动状态时没有需要释放的独占运行时数据。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">即将执行的状态转移。</param>
        public override void Exit(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
        }
    }
}
