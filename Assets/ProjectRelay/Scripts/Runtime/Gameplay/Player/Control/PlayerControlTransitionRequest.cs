using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存状态 Tick 提交给状态机的不可变转移请求；请求本身不直接修改当前状态。
    /// </summary>
    public readonly struct PlayerControlTransitionRequest
    {
        /// <summary>获取本值是否包含需要处理的状态转移。</summary>
        public bool HasRequest { get; }

        /// <summary>获取请求进入的目标状态。</summary>
        public PlayerControlStateId TargetStateId { get; }

        /// <summary>获取请求转移的原因。</summary>
        public PlayerControlTransitionReason Reason { get; }

        /// <summary>获取请求是否携带有效平面方向。</summary>
        public bool HasDirection { get; }

        /// <summary>获取请求携带的世界空间平面方向。</summary>
        public Vector3 Direction { get; }

        /// <summary>获取一份没有转移意图的默认请求。</summary>
        public static PlayerControlTransitionRequest None => default;

        /// <summary>
        /// 创建不携带方向的状态转移请求。
        /// </summary>
        /// <param name="_targetStateId">希望进入的目标状态。</param>
        /// <param name="_reason">触发请求的原因。</param>
        /// <returns>包含目标状态和原因的不可变请求。</returns>
        public static PlayerControlTransitionRequest Create(
            PlayerControlStateId _targetStateId,
            PlayerControlTransitionReason _reason)
        {
            return new PlayerControlTransitionRequest(
                true,
                _targetStateId,
                _reason,
                false,
                Vector3.zero);
        }

        /// <summary>
        /// 创建携带世界空间平面方向的状态转移请求。
        /// </summary>
        /// <param name="_targetStateId">希望进入的目标状态。</param>
        /// <param name="_reason">触发请求的原因。</param>
        /// <param name="_direction">目标状态进入时需要使用的方向。</param>
        /// <returns>包含目标状态、原因和方向的不可变请求。</returns>
        public static PlayerControlTransitionRequest Create(
            PlayerControlStateId _targetStateId,
            PlayerControlTransitionReason _reason,
            Vector3 _direction)
        {
            return new PlayerControlTransitionRequest(
                true,
                _targetStateId,
                _reason,
                true,
                _direction);
        }

        /// <summary>
        /// 创建状态内部返回给状态机的不可变转移请求。
        /// </summary>
        private PlayerControlTransitionRequest(
            bool _hasRequest,
            PlayerControlStateId _targetStateId,
            PlayerControlTransitionReason _reason,
            bool _hasDirection,
            Vector3 _direction)
        {
            HasRequest = _hasRequest;
            TargetStateId = _targetStateId;
            Reason = _reason;
            HasDirection = _hasDirection;
            Direction = _direction;
        }
    }
}
