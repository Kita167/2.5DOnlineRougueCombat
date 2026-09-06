using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存当前控制状态在单个玩法帧内输出的水平速度和朝向策略。
    /// PlayerController 只消费本值，不重新推导状态规则。
    /// </summary>
    public readonly struct PlayerControlOutput
    {
        /// <summary>获取状态机要求 Motor 使用的世界空间水平速度。</summary>
        public Vector3 HorizontalVelocity { get; }

        /// <summary>获取本帧是否允许根据水平速度更新角色朝向。</summary>
        public bool CanTurn { get; }

        /// <summary>获取本帧是否存在必须保持的世界空间朝向。</summary>
        public bool HasLockedFacingDirection { get; }

        /// <summary>获取已经校验和归一化的锁定朝向。</summary>
        public Vector3 LockedFacingDirection { get; }

        /// <summary>
        /// 创建一份完整的不可变控制输出。
        /// </summary>
        /// <param name="_horizontalVelocity">最终世界空间水平速度。</param>
        /// <param name="_canTurn">是否允许跟随水平速度转向。</param>
        /// <param name="_hasLockedFacingDirection">是否存在锁定朝向。</param>
        /// <param name="_lockedFacingDirection">需要保持的世界空间平面朝向。</param>
        internal PlayerControlOutput(
            Vector3 _horizontalVelocity,
            bool _canTurn,
            bool _hasLockedFacingDirection,
            Vector3 _lockedFacingDirection)
        {
            HorizontalVelocity = _horizontalVelocity;
            CanTurn = _canTurn;
            HasLockedFacingDirection = _hasLockedFacingDirection;
            LockedFacingDirection = _lockedFacingDirection;
        }
    }
}
