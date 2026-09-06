using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 表达 Player 已启用但没有有效移动输入时的站立状态。
    /// </summary>
    public sealed class PlayerIdleState : PlayerControlState
    {
        /// <summary>获取 Idle 状态标识。</summary>
        public override PlayerControlStateId Id => PlayerControlStateId.Idle;

        /// <summary>
        /// 进入站立状态时不保留上一个移动状态的速度。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">已经成功接受的站立转移。</param>
        public override void Enter(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
        }

        /// <summary>
        /// 先尝试 Dash 和 Attack，再在出现有效移动输入时请求进入 Move。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <param name="_deltaTime">本帧安全时间增量。</param>
        /// <returns>有移动输入时返回 Move 请求，否则返回 None。</returns>
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

            return _input.HasMoveInput
                ? PlayerControlTransitionRequest.Create(
                    PlayerControlStateId.Move,
                    PlayerControlTransitionReason.MoveStarted)
                : PlayerControlTransitionRequest.None;
        }

        /// <summary>
        /// 为站立状态创建允许后续响应转向但当前速度为零的控制输出。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <returns>站立状态的零速度输出。</returns>
        public override PlayerControlOutput CreateOutput(
            PlayerControlContext _context,
            in PlayerControlInput _input)
        {
            return new PlayerControlOutput(
                Vector3.zero,
                true,
                false,
                Vector3.zero);
        }

        /// <summary>
        /// 离开站立状态时没有需要释放的独占运行时数据。
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
