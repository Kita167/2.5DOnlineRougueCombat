using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存一次已经成功执行的玩家控制状态转移及其可选平面方向上下文。
    /// </summary>
    public readonly struct PlayerControlTransition
    {
        /// <summary>获取转移前的控制状态。</summary>
        public PlayerControlStateId From { get; }

        /// <summary>获取转移后的控制状态。</summary>
        public PlayerControlStateId To { get; }

        /// <summary>获取触发本次转移的稳定原因。</summary>
        public PlayerControlTransitionReason Reason { get; }

        /// <summary>获取本次转移是否携带有效平面方向。</summary>
        public bool HasDirection { get; }

        /// <summary>获取 Dash 或 Attack 进入时使用的世界空间平面方向。</summary>
        public Vector3 Direction { get; }

        /// <summary>
        /// 创建一份只描述已经接受转移的不可变上下文。
        /// </summary>
        /// <param name="_from">转移前状态。</param>
        /// <param name="_to">转移后状态。</param>
        /// <param name="_reason">触发转移的原因。</param>
        /// <param name="_hasDirection">转移是否携带方向。</param>
        /// <param name="_direction">已经校验的世界空间平面方向。</param>
        internal PlayerControlTransition(
            PlayerControlStateId _from,
            PlayerControlStateId _to,
            PlayerControlTransitionReason _reason,
            bool _hasDirection,
            Vector3 _direction)
        {
            From = _from;
            To = _to;
            Reason = _reason;
            HasDirection = _hasDirection;
            Direction = _direction;
        }
    }
}
