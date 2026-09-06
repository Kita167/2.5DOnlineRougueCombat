using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存 PlayerMotor 完成单次移动后回报给控制状态机的不可变实际结果。
    /// 本值隔离具体状态与 PlayerMotor 的可变运行时字段。
    /// </summary>
    public readonly struct PlayerMovementResult
    {
        /// <summary>获取 Motor 本帧得到的实际世界空间水平速度。</summary>
        public Vector3 HorizontalVelocity { get; }

        /// <summary>获取 CharacterController 本帧报告的碰撞方向。</summary>
        public CollisionFlags CollisionFlags { get; }

        /// <summary>
        /// 创建一份供控制状态机消费的移动结果快照。
        /// </summary>
        /// <param name="_horizontalVelocity">Motor 实际得到的水平速度。</param>
        /// <param name="_collisionFlags">CharacterController 报告的碰撞方向。</param>
        public PlayerMovementResult(
            Vector3 _horizontalVelocity,
            CollisionFlags _collisionFlags)
        {
            HorizontalVelocity = _horizontalVelocity;
            CollisionFlags = _collisionFlags;
        }
    }
}
