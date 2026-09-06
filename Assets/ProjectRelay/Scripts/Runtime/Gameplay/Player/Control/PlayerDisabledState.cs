using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 表达 Player 控制被禁用时的安全状态，拒绝动作意图并持续输出零速度。
    /// </summary>
    public sealed class PlayerDisabledState : PlayerControlState
    {
        /// <summary>获取 Disabled 状态标识。</summary>
        public override PlayerControlStateId Id => PlayerControlStateId.Disabled;

        /// <summary>
        /// 进入禁用状态时不创建额外运行时数据，完整清理由状态机 ForceReset 负责。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">已经成功接受的禁用转移。</param>
        public override void Enter(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
            _context.ResetRuntime();
        }

        /// <summary>
        /// 忽略禁用期间的所有控制输入并保持当前状态。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <param name="_deltaTime">本帧安全时间增量。</param>
        /// <returns>始终返回无转移请求。</returns>
        public override PlayerControlTransitionRequest Tick(
            PlayerControlContext _context,
            in PlayerControlInput _input,
            float _deltaTime)
        {
            return PlayerControlTransitionRequest.None;
        }

        /// <summary>
        /// 为禁用状态创建禁止转向且速度为零的控制输出。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_input">本帧不可变控制输入。</param>
        /// <returns>禁用状态的零输出。</returns>
        public override PlayerControlOutput CreateOutput(
            PlayerControlContext _context,
            in PlayerControlInput _input)
        {
            return new PlayerControlOutput(
                Vector3.zero,
                false,
                false,
                Vector3.zero);
        }

        /// <summary>
        /// 离开禁用状态时不恢复任何旧运行时数据。
        /// </summary>
        /// <param name="_context">状态共享的控制上下文。</param>
        /// <param name="_transition">即将执行的启用转移。</param>
        public override void Exit(
            PlayerControlContext _context,
            in PlayerControlTransition _transition)
        {
        }
    }
}
