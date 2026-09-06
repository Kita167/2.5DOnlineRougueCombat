using System;
using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 管理玩家 Disabled、Free、Dashing 和 Attacking 互斥状态及其动作约束。
    /// 本类只仲裁动作和输出目标水平速度，不推进攻击阶段，也不包含伤害或命中规则。
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
        private Vector3 mLockedAttackDirection;
        private float mAttackMovementSpeedMultiplier;

        /// <summary>
        /// 获取当前互斥动作状态；新实例和强制重置后的状态均为 Disabled。
        /// </summary>
        public PlayerActionState CurrentState { get; private set; } =
            PlayerActionState.Disabled;

        /// <summary>
        /// 获取当前状态对移动、转向、冲刺和攻击施加的只读约束。
        /// </summary>
        public PlayerActionConstraints CurrentConstraints => CreateCurrentConstraints();

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
        /// 使用兼容入口推进状态计时、处理冲刺意图，并输出当前状态允许的世界空间水平速度。
        /// 新的协调代码可分别调用 AdvanceTime、TryDash 和 CalculateHorizontalVelocity，
        /// 以便在最终速度计算前插入攻击等其他动作转移。
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

            float _safeDeltaTime = GetSafeDeltaTime(_deltaTime);
            AdvanceTime(_safeDeltaTime);
            TryDash(
                _moveDirection,
                _currentFacingDirection,
                _dashPressed,
                _safeDeltaTime);
            return CalculateHorizontalVelocity(_moveDirection);
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
        /// <param name="_isEnabled">为 true 时允许玩家动作，为 false 时清除全部临时状态。</param>
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
        /// 推进冲刺持续时间和冷却；攻击阶段时间由 BasicAttackController 独立推进。
        /// </summary>
        /// <param name="_deltaTime">当前玩法帧使用的时间增量；负值按零处理。</param>
        public void AdvanceTime(float _deltaTime)
        {
            if (CurrentState == PlayerActionState.Disabled)
            {
                return;
            }

            float _safeDeltaTime = GetSafeDeltaTime(_deltaTime);
            UpdateCooldown(_safeDeltaTime);
            UpdateDashDuration(_safeDeltaTime);
        }

        /// <summary>
        /// 记录并尝试执行一次冲刺意图，同时维护尚未消费的冲刺输入缓存。
        /// Attacking 状态会直接拒绝冲刺且不会把该输入保留到攻击结束后。
        /// </summary>
        /// <param name="_moveDirection">当前世界空间移动方向。</param>
        /// <param name="_currentFacingDirection">没有移动输入时使用的角色当前朝向。</param>
        /// <param name="_dashPressed">本帧是否出现新的冲刺意图。</param>
        /// <param name="_deltaTime">输入缓存本帧经过的时间；负值按零处理。</param>
        /// <returns>本次调用实际从其他状态进入 Dashing 时返回 true。</returns>
        public bool TryDash(
            Vector3 _moveDirection,
            Vector3 _currentFacingDirection,
            bool _dashPressed,
            float _deltaTime)
        {
            if (CurrentState == PlayerActionState.Disabled)
            {
                ClearDashInputBuffer();
                return false;
            }

            if (CurrentState == PlayerActionState.Attacking)
            {
                ClearDashInputBuffer();
                return false;
            }

            if (_dashPressed)
            {
                BufferDashInput();
            }

            PlayerActionState _stateBeforeAttempt = CurrentState;
            TryStartBufferedDash(_moveDirection, _currentFacingDirection);
            AgeDashInputBuffer(_dashPressed, GetSafeDeltaTime(_deltaTime));
            return
                _stateBeforeAttempt != PlayerActionState.Dashing &&
                CurrentState == PlayerActionState.Dashing;
        }

        /// <summary>
        /// 在 Free 状态进入 Attacking，并锁定攻击期间的朝向和普通移动速度倍率。
        /// </summary>
        /// <param name="_movementSpeedMultiplier">攻击期间普通移动速度倍率，限制在 0 到 1。</param>
        /// <param name="_lockedFacingDirection">攻击开始时需要锁定的世界空间方向。</param>
        /// <returns>状态和方向有效且成功进入 Attacking 时返回 true。</returns>
        public bool TryEnterAttacking(
            float _movementSpeedMultiplier,
            Vector3 _lockedFacingDirection)
        {
            if (CurrentState != PlayerActionState.Free)
            {
                return false;
            }

            if (
                float.IsNaN(_movementSpeedMultiplier) ||
                float.IsInfinity(_movementSpeedMultiplier))
            {
                return false;
            }

            Vector3 _safeFacingDirection =
                GetNormalizedPlanarDirection(_lockedFacingDirection);

            if (_safeFacingDirection == Vector3.zero)
            {
                return false;
            }

            CurrentState = PlayerActionState.Attacking;
            mLockedAttackDirection = _safeFacingDirection;
            mAttackMovementSpeedMultiplier = Mathf.Clamp01(_movementSpeedMultiplier);
            ClearDashInputBuffer();
            return true;
        }

        /// <summary>
        /// 在攻击正常完成时从 Attacking 返回 Free，并清除全部攻击约束。
        /// </summary>
        /// <returns>调用前确实处于 Attacking 且完成了转移时返回 true。</returns>
        public bool CompleteAttacking()
        {
            return ExitAttacking();
        }

        /// <summary>
        /// 在攻击被禁用或强制取消时从 Attacking 返回 Free，并清除全部攻击约束。
        /// </summary>
        /// <returns>调用前确实处于 Attacking 且完成了转移时返回 true。</returns>
        public bool InterruptAttacking()
        {
            return ExitAttacking();
        }

        /// <summary>
        /// 根据当前动作状态和移动输入计算最终世界空间水平速度。
        /// </summary>
        /// <param name="_moveDirection">长度可超过 1 的世界空间移动方向。</param>
        /// <returns>应用冲刺速度或当前普通移动倍率后的水平速度。</returns>
        public Vector3 CalculateHorizontalVelocity(Vector3 _moveDirection)
        {
            if (CurrentState == PlayerActionState.Disabled)
            {
                return Vector3.zero;
            }

            if (CurrentState == PlayerActionState.Dashing)
            {
                return mDashDirection * mMovementConfig.DashSpeed;
            }

            _moveDirection.y = 0.0f;
            Vector3 _normalizedMoveDirection = Vector3.ClampMagnitude(_moveDirection, 1.0f);
            return
                _normalizedMoveDirection *
                mMovementConfig.MoveSpeed *
                CurrentConstraints.MovementSpeedMultiplier;
        }

        /// <summary>
        /// 强制进入 Disabled，并清除冲刺、攻击约束、计时、冷却和输入缓存。
        /// </summary>
        public void ForceReset()
        {
            CurrentState = PlayerActionState.Disabled;
            mDashDirection = Vector3.zero;
            mDashTimeRemaining = 0.0f;
            mDashCooldownRemaining = 0.0f;
            ClearAttackConstraints();
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
        private void TryStartBufferedDash(
            Vector3 _moveDirection,
            Vector3 _currentFacingDirection)
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
        /// 正常完成或中断攻击时集中离开 Attacking，避免外部直接修改 CurrentState。
        /// </summary>
        /// <returns>成功离开 Attacking 时返回 true。</returns>
        private bool ExitAttacking()
        {
            if (CurrentState != PlayerActionState.Attacking)
            {
                return false;
            }

            CurrentState = PlayerActionState.Free;
            ClearAttackConstraints();
            return true;
        }

        /// <summary>
        /// 清除只属于当前攻击的锁定朝向和移动倍率。
        /// </summary>
        private void ClearAttackConstraints()
        {
            mLockedAttackDirection = Vector3.zero;
            mAttackMovementSpeedMultiplier = 0.0f;
        }

        /// <summary>
        /// 根据当前互斥状态创建供 Controller 和表现层读取的动作约束。
        /// </summary>
        /// <returns>与当前状态一致的只读约束。</returns>
        private PlayerActionConstraints CreateCurrentConstraints()
        {
            switch (CurrentState)
            {
                case PlayerActionState.Free:
                    return new PlayerActionConstraints(
                        1.0f,
                        true,
                        true,
                        true,
                        false,
                        Vector3.zero);

                case PlayerActionState.Dashing:
                    return new PlayerActionConstraints(
                        0.0f,
                        false,
                        false,
                        false,
                        true,
                        mDashDirection);

                case PlayerActionState.Attacking:
                    return new PlayerActionConstraints(
                        mAttackMovementSpeedMultiplier,
                        false,
                        false,
                        false,
                        true,
                        mLockedAttackDirection);

                default:
                    return new PlayerActionConstraints(
                        0.0f,
                        false,
                        false,
                        false,
                        false,
                        Vector3.zero);
            }
        }

        /// <summary>
        /// 将任意方向投影到 XZ 平面，并在方向有效时返回单位向量。
        /// </summary>
        /// <param name="_direction">需要处理的世界空间方向。</param>
        /// <returns>有效时返回 XZ 平面单位方向，否则返回零向量。</returns>
        private static Vector3 GetNormalizedPlanarDirection(Vector3 _direction)
        {
            _direction.y = 0.0f;

            if (
                float.IsNaN(_direction.x) ||
                float.IsInfinity(_direction.x) ||
                float.IsNaN(_direction.z) ||
                float.IsInfinity(_direction.z))
            {
                return Vector3.zero;
            }

            return _direction.sqrMagnitude > mMinimumDirectionSqrMagnitude
                ? _direction.normalized
                : Vector3.zero;
        }

        /// <summary>
        /// 将非法或负的时间增量转换为零，防止计时状态传播 NaN 或无穷值。
        /// </summary>
        /// <param name="_deltaTime">调用方提供的时间增量。</param>
        /// <returns>有限且非负的时间增量。</returns>
        private static float GetSafeDeltaTime(float _deltaTime)
        {
            return float.IsNaN(_deltaTime) || float.IsInfinity(_deltaTime)
                ? 0.0f
                : Mathf.Max(0.0f, _deltaTime);
        }
    }
}
