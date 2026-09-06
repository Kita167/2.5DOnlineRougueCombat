using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存 PlayerController 在单个玩法帧内提交给控制状态机的不可变输入快照。
    /// 构造时会过滤非法方向并保留摇杆输入强度。
    /// </summary>
    public readonly struct PlayerControlInput
    {
        private const float mMinimumDirectionSqrMagnitude = 0.0001f;

        /// <summary>获取限制在 XZ 平面且长度不超过 1 的移动方向。</summary>
        public Vector3 MoveDirection { get; }

        /// <summary>获取归一化后的角色当前世界空间平面朝向。</summary>
        public Vector3 FacingDirection { get; }

        /// <summary>获取本帧是否消费到一次新的 Dash 意图。</summary>
        public bool IsDashPressed { get; }

        /// <summary>获取本帧是否消费到一次新的 Attack 意图。</summary>
        public bool IsAttackPressed { get; }

        /// <summary>获取本帧是否存在足以驱动移动状态的有效输入。</summary>
        public bool HasMoveInput =>
            MoveDirection.sqrMagnitude > mMinimumDirectionSqrMagnitude;

        /// <summary>
        /// 创建经过平面约束和有限值校验的控制输入快照。
        /// </summary>
        /// <param name="_moveDirection">相机转换后的世界空间移动方向。</param>
        /// <param name="_facingDirection">角色当前真实世界空间朝向。</param>
        /// <param name="_isDashPressed">本帧是否出现 Dash 意图。</param>
        /// <param name="_isAttackPressed">本帧是否出现 Attack 意图。</param>
        public PlayerControlInput(
            Vector3 _moveDirection,
            Vector3 _facingDirection,
            bool _isDashPressed,
            bool _isAttackPressed)
        {
            MoveDirection = GetSafePlanarInput(_moveDirection);
            FacingDirection = GetSafePlanarDirection(_facingDirection);
            IsDashPressed = _isDashPressed;
            IsAttackPressed = _isAttackPressed;
        }

        /// <summary>
        /// 将移动输入投影到 XZ 平面，在保留模拟量强度的同时限制最大长度。
        /// </summary>
        private static Vector3 GetSafePlanarInput(Vector3 _direction)
        {
            _direction.y = 0.0f;

            if (!HasFinitePlanarComponents(_direction))
            {
                return Vector3.zero;
            }

            return Vector3.ClampMagnitude(_direction, 1.0f);
        }

        /// <summary>
        /// 将朝向投影到 XZ 平面，并在方向有效时返回单位向量。
        /// </summary>
        private static Vector3 GetSafePlanarDirection(Vector3 _direction)
        {
            _direction.y = 0.0f;

            if (
                !HasFinitePlanarComponents(_direction) ||
                _direction.sqrMagnitude <= mMinimumDirectionSqrMagnitude)
            {
                return Vector3.zero;
            }

            return _direction.normalized;
        }

        /// <summary>
        /// 检查方向在当前玩法使用的 XZ 平面分量是否均为有限值。
        /// </summary>
        private static bool HasFinitePlanarComponents(Vector3 _direction)
        {
            return
                !float.IsNaN(_direction.x) &&
                !float.IsInfinity(_direction.x) &&
                !float.IsNaN(_direction.z) &&
                !float.IsInfinity(_direction.z);
        }
    }
}
