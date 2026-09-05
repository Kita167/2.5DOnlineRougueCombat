using System;
using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 管理玩家 Free、Dashing 和 Disabled 移动状态，以及冲刺方向、计时、冷却和输入缓存。
    /// 本类只输出目标水平速度，不读取设备输入，也不直接移动场景对象。
    /// </summary>
    public sealed class PlayerActionStateMachine
    {
        private const float mMinimumDirectionSqrMagnitude = 0.0001f;
        private const float mBlockedDashSpeedRatio = 0.5f;

        private readonly PlayerMovementConfig mMovementConfig;

        private Vector3 mDashDirection;
        private float mDashTimeRemaining;
        private float mDashCooldownRemaining;
        private float mDashInputBufferRemaining;
        private bool mHasBufferedDash;

        /// <summary>
        /// 获取当前互斥移动状态；新实例和强制重置后的状态均为 Disabled。
        /// </summary>
        public PlayerActionState CurrentState { get; private set; } =
            PlayerActionState.Disabled;

        /// <summary>
        /// 使用只读移动配置创建状态机，运行时计时不会写回该配置资源。
        /// </summary>
        /// <param name="_movementConfig">提供普通移动和冲刺设计参数的配置。</param>
        /// <exception cref="ArgumentNullException">移动配置为空时抛出。</exception>
        public PlayerActionStateMachine(PlayerMovementConfig _movementConfig)
        {
            mMovementConfig =
                _movementConfig ?? throw new ArgumentNullException(nameof(_movementConfig));
        }

        /// <summary>
        /// 推进状态计时、处理冲刺意图，并输出当前状态允许的世界空间水平速度。
        /// </summary>
        /// <param name="_moveDirection">长度不超过 1 的当前世界空间移动方向。</param>
        /// <param name="_currentFacingDirection">没有移动输入时用于冲刺的角色实际当前朝向。</param>
        /// <param name="_dashPressed">本帧是否消费到一次新的冲刺意图。</param>
        /// <param name="_deltaTime">当前玩法帧使用的时间增量。</param>
        /// <returns>当前状态输出的世界空间水平速度。</returns>
        public Vector3 Tick(
            Vector3 _moveDirection,
            Vector3 _currentFacingDirection,
            bool _dashPressed,
            float _deltaTime)
        {
            if (CurrentState == PlayerActionState.Disabled)
            {
                ClearDashInputBuffer();
                return Vector3.zero;
            }

            float _safeDeltaTime = Mathf.Max(0.0f, _deltaTime);
            UpdateCooldown(_safeDeltaTime);
            UpdateDashDuration(_safeDeltaTime);

            if (_dashPressed)
            {
                BufferDashInput();
            }

            TryStartDash(_moveDirection, _currentFacingDirection);
            AgeDashInputBuffer(_dashPressed, _safeDeltaTime);

            if (CurrentState == PlayerActionState.Dashing)
            {
                return mDashDirection * mMovementConfig.DashSpeed;
            }

            _moveDirection.y = 0.0f;
            return Vector3.ClampMagnitude(_moveDirection, 1.0f) * mMovementConfig.MoveSpeed;
        }

        /// <summary>
        /// 在 Motor 完成移动后检查冲刺是否受到明显侧面阻挡，并在需要时提前结束冲刺。
        /// </summary>
        /// <param name="_actualHorizontalVelocity">Motor 本帧得到的实际世界空间水平速度。</param>
        /// <param name="_collisionFlags">CharacterController 本帧返回的碰撞方向。</param>
        public void ReportMovementResult(
            Vector3 _actualHorizontalVelocity,
            CollisionFlags _collisionFlags)
        {
            if (
                CurrentState != PlayerActionState.Dashing ||
                !mMovementConfig.EndDashWhenBlocked ||
                (_collisionFlags & CollisionFlags.Sides) == 0)
            {
                return;
            }

            _actualHorizontalVelocity.y = 0.0f;

            float _speedAlongDash = Vector3.Dot(_actualHorizontalVelocity, mDashDirection);
            float _minimumAllowedSpeed = mMovementConfig.DashSpeed * mBlockedDashSpeedRatio;

            if (_speedAlongDash <= _minimumAllowedSpeed)
            {
                EndDash();
            }
        }

        /// <summary>
        /// 设置状态机是否接受控制；启用时从 Disabled 进入 Free，禁用时执行完整重置。
        /// </summary>
        /// <param name="_isEnabled">为 true 时允许普通移动和冲刺，为 false 时清除全部临时状态。</param>
        public void SetEnabled(bool _isEnabled)
        {
            if (!_isEnabled)
            {
                ForceReset();
                return;
            }

            if (CurrentState == PlayerActionState.Disabled)
            {
                CurrentState = PlayerActionState.Free;
            }
        }

        /// <summary>
        /// 强制进入 Disabled，并清除冲刺方向、持续时间、冷却和输入缓存。
        /// </summary>
        public void ForceReset()
        {
            CurrentState = PlayerActionState.Disabled;
            mDashDirection = Vector3.zero;
            mDashTimeRemaining = 0.0f;
            mDashCooldownRemaining = 0.0f;
            ClearDashInputBuffer();
        }

        /// <summary>
        /// 减少冲刺冷却剩余时间，并将结果限制为非负值。
        /// </summary>
        /// <param name="_deltaTime">本帧有效的非负时间增量。</param>
        private void UpdateCooldown(float _deltaTime)
        {
            if (mDashCooldownRemaining > 0.0f)
            {
                mDashCooldownRemaining = Mathf.Max(0.0f, mDashCooldownRemaining - _deltaTime);
            }
        }

        /// <summary>
        /// 推进当前冲刺持续时间，并在时间耗尽时进入 Free 和启动冷却。
        /// </summary>
        /// <param name="_deltaTime">本帧有效的非负时间增量。</param>
        private void UpdateDashDuration(float _deltaTime)
        {
            if (CurrentState != PlayerActionState.Dashing)
            {
                return;
            }

            mDashTimeRemaining -= _deltaTime;

            if (mDashTimeRemaining <= 0.0f)
            {
                EndDash();
            }
        }

        /// <summary>
        /// 记录一次待执行冲刺意图，使冷却即将结束时的提前按键仍可生效。
        /// </summary>
        private void BufferDashInput()
        {
            mHasBufferedDash = true;
            mDashInputBufferRemaining = mMovementConfig.DashInputBuffer;
        }

        /// <summary>
        /// 在新按键帧之后减少未消费输入的保留时间，并在到期时清除意图。
        /// </summary>
        /// <param name="_wasPressedThisFrame">本帧是否刚记录新输入，防止新输入立即损失一帧时间。</param>
        /// <param name="_deltaTime">本帧有效的非负时间增量。</param>
        private void AgeDashInputBuffer(bool _wasPressedThisFrame, float _deltaTime)
        {
            if (!mHasBufferedDash)
            {
                return;
            }

            if (mDashInputBufferRemaining <= 0.0f)
            {
                ClearDashInputBuffer();
                return;
            }

            if (_wasPressedThisFrame)
            {
                return;
            }

            mDashInputBufferRemaining -= _deltaTime;

            if (mDashInputBufferRemaining <= 0.0f)
            {
                ClearDashInputBuffer();
            }
        }

        /// <summary>
        /// 在 Free、冷却结束且存在缓存输入时，选择当前移动或角色实际朝向并开始冲刺。
        /// </summary>
        /// <param name="_moveDirection">当前世界空间移动方向。</param>
        /// <param name="_currentFacingDirection">没有移动输入时使用的角色实际当前朝向。</param>
        private void TryStartDash(Vector3 _moveDirection, Vector3 _currentFacingDirection)
        {
            if (
                CurrentState != PlayerActionState.Free ||
                mDashCooldownRemaining > 0.0f ||
                !mHasBufferedDash)
            {
                return;
            }

            if (
                mMovementConfig.DashSpeed <= Mathf.Epsilon ||
                mMovementConfig.DashDuration <= Mathf.Epsilon)
            {
                ClearDashInputBuffer();
                return;
            }

            Vector3 _dashDirection = GetNormalizedPlanarDirection(_moveDirection);

            if (_dashDirection == Vector3.zero)
            {
                _dashDirection = GetNormalizedPlanarDirection(_currentFacingDirection);
            }

            if (_dashDirection == Vector3.zero)
            {
                return;
            }

            CurrentState = PlayerActionState.Dashing;
            mDashDirection = _dashDirection;
            mDashTimeRemaining = mMovementConfig.DashDuration;
            ClearDashInputBuffer();
        }

        /// <summary>
        /// 结束当前冲刺、返回 Free，并从结束时刻开始计算完整冷却。
        /// </summary>
        private void EndDash()
        {
            if (CurrentState != PlayerActionState.Dashing)
            {
                return;
            }

            CurrentState = PlayerActionState.Free;
            mDashDirection = Vector3.zero;
            mDashTimeRemaining = 0.0f;
            mDashCooldownRemaining = mMovementConfig.DashCooldown;
        }

        /// <summary>
        /// 清除尚未执行的冲刺意图及其剩余保留时间。
        /// </summary>
        private void ClearDashInputBuffer()
        {
            mHasBufferedDash = false;
            mDashInputBufferRemaining = 0.0f;
        }

        /// <summary>
        /// 将任意方向投影到 XZ 平面，并在方向有效时返回单位向量。
        /// </summary>
        /// <param name="_direction">需要处理的世界空间方向。</param>
        /// <returns>有效时返回 XZ 平面单位方向，否则返回零向量。</returns>
        private static Vector3 GetNormalizedPlanarDirection(Vector3 _direction)
        {
            _direction.y = 0.0f;

            return _direction.sqrMagnitude > mMinimumDirectionSqrMagnitude
                ? _direction.normalized
                : Vector3.zero;
        }
    }
}
