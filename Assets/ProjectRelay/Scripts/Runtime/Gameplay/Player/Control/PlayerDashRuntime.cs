using System;
using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存 Player Dash 独占的方向、结束时刻、冷却时刻和输入缓存。
    /// 本类不修改控制状态，只提供基于状态机累计时间的确定性运行时操作。
    /// </summary>
    public sealed class PlayerDashRuntime
    {
        private readonly PlayerMovementConfig mMovementConfig;

        private Vector3 mDirection;
        private float mEndTime;
        private float mCooldownEndTime;
        private float mInputBufferExpirationTime;
        private bool mIsActive;
        private bool mHasBufferedInput;

        /// <summary>获取当前 Dash 锁定的世界空间平面单位方向。</summary>
        public Vector3 Direction => mDirection;

        /// <summary>获取当前是否存在已经开始且尚未退出的 Dash。</summary>
        public bool IsActive => mIsActive;

        /// <summary>获取是否存在尚未消费且未被主动清除的 Dash 输入。</summary>
        public bool HasBufferedInput => mHasBufferedInput;

        /// <summary>获取当前 Dash 计划结束的累计时间。</summary>
        public float EndTime => mEndTime;

        /// <summary>获取下一次允许 Dash 的累计时间。</summary>
        public float CooldownEndTime => mCooldownEndTime;

        /// <summary>
        /// 使用只读移动配置创建空的 Dash 运行时。
        /// </summary>
        /// <param name="_movementConfig">提供 Dash 速度、持续时间、冷却和缓存时间的配置。</param>
        /// <exception cref="ArgumentNullException">移动配置为空时抛出。</exception>
        public PlayerDashRuntime(PlayerMovementConfig _movementConfig)
        {
            mMovementConfig =
                _movementConfig ?? throw new ArgumentNullException(nameof(_movementConfig));
            Reset();
        }

        /// <summary>
        /// 记录本帧 Dash 意图，并从当前累计时间开始计算缓存到期时刻。
        /// </summary>
        /// <param name="_elapsedTime">状态机当前安全累计时间。</param>
        public void BufferInput(float _elapsedTime)
        {
            mHasBufferedInput = true;
            mInputBufferExpirationTime =
                _elapsedTime + mMovementConfig.DashInputBuffer;
        }

        /// <summary>
        /// 清除已经超过到期时刻的 Dash 输入缓存。
        /// </summary>
        /// <param name="_elapsedTime">状态机当前安全累计时间。</param>
        public void Advance(float _elapsedTime)
        {
            if (
                mHasBufferedInput &&
                _elapsedTime > mInputBufferExpirationTime)
            {
                ClearInputBuffer();
            }
        }

        /// <summary>
        /// 在冷却、配置、缓存和方向全部合法时开始 Dash 并消费缓存。
        /// </summary>
        /// <param name="_input">提供当前移动方向和实际角色朝向的输入快照。</param>
        /// <param name="_elapsedTime">状态机当前安全累计时间。</param>
        /// <param name="_direction">成功时返回本次 Dash 锁定的单位方向。</param>
        /// <returns>本次调用确实开始 Dash 时返回 true。</returns>
        public bool TryBegin(
            in PlayerControlInput _input,
            float _elapsedTime,
            out Vector3 _direction)
        {
            _direction = Vector3.zero;
            Advance(_elapsedTime);

            if (
                mIsActive ||
                !mHasBufferedInput ||
                _elapsedTime < mCooldownEndTime)
            {
                return false;
            }

            if (
                mMovementConfig.DashSpeed <= Mathf.Epsilon ||
                mMovementConfig.DashDuration <= Mathf.Epsilon)
            {
                ClearInputBuffer();
                return false;
            }

            _direction = _input.HasMoveInput
                ? _input.MoveDirection.normalized
                : _input.FacingDirection;

            if (_direction == Vector3.zero)
            {
                return false;
            }

            mDirection = _direction;
            mEndTime = _elapsedTime + mMovementConfig.DashDuration;
            mIsActive = true;
            ClearInputBuffer();
            return true;
        }

        /// <summary>
        /// 查询当前 Dash 是否已经到达配置的自然结束时刻。
        /// </summary>
        /// <param name="_elapsedTime">状态机当前安全累计时间。</param>
        /// <returns>Dash 已激活且累计时间到达结束时刻时返回 true。</returns>
        public bool HasEnded(float _elapsedTime)
        {
            return mIsActive && _elapsedTime >= mEndTime;
        }

        /// <summary>
        /// 结束当前 Dash，并从自然结束时刻或受阻时刻开始计算冷却。
        /// </summary>
        /// <param name="_elapsedTime">状态机当前安全累计时间。</param>
        /// <param name="_didCompleteNaturally">是否因为持续时间耗尽而正常结束。</param>
        public void End(float _elapsedTime, bool _didCompleteNaturally)
        {
            if (!mIsActive)
            {
                return;
            }

            float _cooldownStartTime = _didCompleteNaturally
                ? mEndTime
                : _elapsedTime;
            mCooldownEndTime =
                _cooldownStartTime + mMovementConfig.DashCooldown;
            mDirection = Vector3.zero;
            mEndTime = 0.0f;
            mIsActive = false;
        }

        /// <summary>
        /// 清除尚未执行的 Dash 输入及其到期时刻。
        /// </summary>
        public void ClearInputBuffer()
        {
            mHasBufferedInput = false;
            mInputBufferExpirationTime = 0.0f;
        }

        /// <summary>
        /// 清除 Dash、冷却和输入缓存，建立不继承上一生命周期的空状态。
        /// </summary>
        public void Reset()
        {
            mDirection = Vector3.zero;
            mEndTime = 0.0f;
            mCooldownEndTime = 0.0f;
            mInputBufferExpirationTime = 0.0f;
            mIsActive = false;
            mHasBufferedInput = false;
        }
    }
}
