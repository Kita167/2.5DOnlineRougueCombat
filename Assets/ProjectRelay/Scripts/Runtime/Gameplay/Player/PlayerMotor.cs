using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 使用 CharacterController 执行玩家水平位移、贴地和重力。
    /// 本组件不读取输入，也不决定当前动作是否允许移动。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        private CharacterController mCharacterController;
        private float mVerticalVelocity;

        /// <summary>
        /// 获取最近一次移动后得到的实际世界空间水平速度。
        /// </summary>
        public Vector3 HorizontalVelocity { get; private set; }

        /// <summary>
        /// 获取玩家最近一次移动后的接地状态。
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// 获取最近一次 CharacterController.Move 返回的碰撞方向。
        /// </summary>
        public CollisionFlags LastCollisionFlags { get; private set; }

        /// <summary>
        /// 缓存同对象上的 CharacterController，确保运行时移动不执行组件查找。
        /// </summary>
        private void Awake()
        {
            mCharacterController = GetComponent<CharacterController>();
            IsGrounded = mCharacterController.isGrounded;
        }

        /// <summary>
        /// 在组件禁用时清除速度和碰撞状态，防止重新启用后继承旧运动。
        /// </summary>
        private void OnDisable()
        {
            ResetMotion();
        }

        /// <summary>
        /// 合并目标水平速度与垂直运动，并通过一次 CharacterController.Move 推进当前帧。
        /// </summary>
        /// <param name="_horizontalVelocity">已经计算完成的世界空间水平速度。</param>
        /// <param name="_gravity">当前使用的向下加速度。</param>
        /// <param name="_maximumFallSpeed">允许达到的最大向下速度。</param>
        /// <param name="_groundedVerticalSpeed">接地时用于维持贴地的垂直速度。</param>
        /// <param name="_deltaTime">当前玩法帧使用的时间增量。</param>
        public void TickMovement(
            Vector3 _horizontalVelocity,
            float _gravity,
            float _maximumFallSpeed,
            float _groundedVerticalSpeed,
            float _deltaTime)
        {
            if (_deltaTime <= Mathf.Epsilon)
            {
                HorizontalVelocity = Vector3.zero;
                IsGrounded = mCharacterController.isGrounded;
                LastCollisionFlags = CollisionFlags.None;
                return;
            }

            _horizontalVelocity.y = 0.0f;

            if (mCharacterController.isGrounded && mVerticalVelocity < 0.0f)
            {
                mVerticalVelocity = _groundedVerticalSpeed;
            }
            else
            {
                mVerticalVelocity += _gravity * _deltaTime;
                mVerticalVelocity = Mathf.Max(mVerticalVelocity, _maximumFallSpeed);
            }

            Vector3 _positionBeforeMove = transform.position;
            Vector3 _combinedVelocity = _horizontalVelocity + Vector3.up * mVerticalVelocity;
            LastCollisionFlags = mCharacterController.Move(_combinedVelocity * _deltaTime);

            Vector3 _actualDisplacement = transform.position - _positionBeforeMove;
            _actualDisplacement.y = 0.0f;
            HorizontalVelocity = _actualDisplacement / _deltaTime;

            IsGrounded =
                (LastCollisionFlags & CollisionFlags.Below) != 0 ||
                mCharacterController.isGrounded;

            if (IsGrounded && mVerticalVelocity < 0.0f)
            {
                mVerticalVelocity = _groundedVerticalSpeed;
            }
        }

        /// <summary>
        /// 清除运行时速度、碰撞和接地缓存，供禁用、退出和后续状态重置流程调用。
        /// </summary>
        public void ResetMotion()
        {
            mVerticalVelocity = 0.0f;
            HorizontalVelocity = Vector3.zero;
            LastCollisionFlags = CollisionFlags.None;
            IsGrounded = mCharacterController != null && mCharacterController.isGrounded;
        }
    }
}
