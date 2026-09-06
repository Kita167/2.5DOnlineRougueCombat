using NUnit.Framework;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Player
{
    /// <summary>
    /// 验证新控制状态机的 Dash 优先级、方向锁定、时序、缓存和阻挡行为。
    /// </summary>
    public sealed class PlayerControlStateMachineDashTests
    {
        private PlayerMovementConfig mMovementConfig;
        private PlayerControlStateMachine mStateMachine;

        /// <summary>
        /// 为每个测试创建并启用独立的纯移动控制状态机。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mMovementConfig =
                ScriptableObject.CreateInstance<PlayerMovementConfig>();
            mStateMachine = new PlayerControlStateMachine(mMovementConfig);
            mStateMachine.SetEnabled(true);
        }

        /// <summary>
        /// 销毁测试期间创建的临时移动配置。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (mMovementConfig != null)
            {
                Object.DestroyImmediate(mMovementConfig);
            }
        }

        /// <summary>
        /// 验证有移动输入时 Dash 锁定移动方向并输出固定配置速度。
        /// </summary>
        [Test]
        public void Tick_DashFromIdleWithMoveInput_EntersDashAndLocksMoveDirection()
        {
            PlayerControlInput _input = CreateInput(
                Vector3.right,
                Vector3.forward,
                true);

            PlayerControlOutput _output = mStateMachine.Tick(_input, 0.0f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Dash));
            Assert.That(
                _output.HorizontalVelocity,
                Is.EqualTo(Vector3.right * mMovementConfig.DashSpeed));
            Assert.That(_output.CanTurn, Is.False);
            Assert.That(_output.HasLockedFacingDirection, Is.True);
            Assert.That(_output.LockedFacingDirection, Is.EqualTo(Vector3.right));
        }

        /// <summary>
        /// 验证没有移动输入时 Dash 使用角色真实朝向作为后备方向。
        /// </summary>
        [Test]
        public void Tick_DashWithoutMoveInput_UsesFacingDirection()
        {
            PlayerControlOutput _output = mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.back, true),
                0.0f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Dash));
            Assert.That(_output.LockedFacingDirection, Is.EqualTo(Vector3.back));
            Assert.That(
                _output.HorizontalVelocity,
                Is.EqualTo(Vector3.back * mMovementConfig.DashSpeed));
        }

        /// <summary>
        /// 验证 Dash 期间改变移动和朝向输入不会修改首次锁定方向。
        /// </summary>
        [Test]
        public void Tick_WhileDashing_KeepsInitialDirectionLocked()
        {
            mStateMachine.Tick(
                CreateInput(Vector3.forward, Vector3.right, true),
                0.0f);

            PlayerControlOutput _output = mStateMachine.Tick(
                CreateInput(Vector3.left, Vector3.left),
                mMovementConfig.DashDuration * 0.5f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Dash));
            Assert.That(_output.LockedFacingDirection, Is.EqualTo(Vector3.forward));
            Assert.That(
                _output.HorizontalVelocity,
                Is.EqualTo(Vector3.forward * mMovementConfig.DashSpeed));
        }

        /// <summary>
        /// 验证 Dash 自然结束时按照结束帧的当前移动输入进入 Move。
        /// </summary>
        [Test]
        public void Tick_WhenDashDurationEnds_TransitionsToCurrentMovementState()
        {
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward, true),
                0.0f);

            PlayerControlOutput _output = mStateMachine.Tick(
                CreateInput(Vector3.left, Vector3.forward),
                mMovementConfig.DashDuration);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Move));
            Assert.That(
                _output.HorizontalVelocity,
                Is.EqualTo(Vector3.left * mMovementConfig.MoveSpeed));
        }

        /// <summary>
        /// 验证明显侧面阻挡会在移动回报阶段立即结束 Dash。
        /// </summary>
        [Test]
        public void ReportMovementResult_WhenDashIsBlocked_EndsDashImmediately()
        {
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward, true),
                0.0f);

            mStateMachine.ReportMovementResult(
                new PlayerMovementResult(Vector3.zero, CollisionFlags.Sides));

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
            Assert.That(
                mStateMachine.CurrentOutput.HorizontalVelocity,
                Is.EqualTo(Vector3.zero));

            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward, true),
                0.0f);
            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
        }

        /// <summary>
        /// 验证跨过持续时间和冷却的大时间增量不会人为延长 Dash 冷却。
        /// </summary>
        [Test]
        public void Tick_LargeDeltaAcrossDashAndCooldown_AllowsNextDash()
        {
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward, true),
                0.0f);
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward),
                mMovementConfig.DashDuration + mMovementConfig.DashCooldown);

            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.right, true),
                0.0f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Dash));
            Assert.That(
                mStateMachine.CurrentOutput.LockedFacingDirection,
                Is.EqualTo(Vector3.right));
        }

        /// <summary>
        /// 验证冷却结束前落入缓存窗口的 Dash 输入会在到期时执行。
        /// </summary>
        [Test]
        public void Tick_DashPressedNearCooldownEnd_ConsumesBufferedInputAtEnd()
        {
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward, true),
                0.0f);
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward),
                mMovementConfig.DashDuration);
            float _timeBeforeCooldownEnd =
                mMovementConfig.DashCooldown -
                mMovementConfig.DashInputBuffer * 0.5f;

            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.right, true),
                _timeBeforeCooldownEnd);
            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));

            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.right),
                mMovementConfig.DashInputBuffer * 0.5f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Dash));
            Assert.That(
                mStateMachine.CurrentOutput.LockedFacingDirection,
                Is.EqualTo(Vector3.right));
        }

        /// <summary>
        /// 验证过早按下且已经超过缓存窗口的 Dash 不会在冷却结束后触发。
        /// </summary>
        [Test]
        public void Tick_DashBufferExpiresBeforeCooldownEnd_DoesNotStartLater()
        {
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward, true),
                0.0f);
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.forward),
                mMovementConfig.DashDuration);
            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.right, true),
                0.0f);

            mStateMachine.Tick(
                CreateInput(Vector3.zero, Vector3.right),
                mMovementConfig.DashCooldown);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
        }

        /// <summary>
        /// 创建使用固定朝向和可选 Dash 边沿的控制输入。
        /// </summary>
        /// <param name="_moveDirection">世界空间移动方向。</param>
        /// <param name="_facingDirection">世界空间角色朝向。</param>
        /// <param name="_isDashPressed">本帧是否按下 Dash。</param>
        /// <returns>可直接提交给状态机的输入。</returns>
        private static PlayerControlInput CreateInput(
            Vector3 _moveDirection,
            Vector3 _facingDirection,
            bool _isDashPressed = false)
        {
            return new PlayerControlInput(
                _moveDirection,
                _facingDirection,
                _isDashPressed,
                false);
        }
    }
}
