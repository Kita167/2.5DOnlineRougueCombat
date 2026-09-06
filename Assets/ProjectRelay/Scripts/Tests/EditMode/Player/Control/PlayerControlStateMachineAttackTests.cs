using NUnit.Framework;
using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Player
{
    /// <summary>
    /// 验证新控制状态机与独立攻击执行器之间的启动、占用、结束和重置边界。
    /// </summary>
    public sealed class PlayerControlStateMachineAttackTests
    {
        private PlayerControlStateMachineTestRig mRig;

        /// <summary>
        /// 为每个测试创建并启用完整本地 Player 控制运行时。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mRig = new PlayerControlStateMachineTestRig();
        }

        /// <summary>
        /// 释放测试装配及其创建的临时 Unity 对象。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            mRig?.Dispose();
        }

        /// <summary>
        /// 验证 Idle 可以通过 Gateway 启动攻击并锁定朝向和移动倍率。
        /// </summary>
        [Test]
        public void Tick_AttackFromIdle_EntersAttackAndAppliesConfiguredConstraints()
        {
            PlayerControlInput _input =
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.right,
                    Vector3.forward,
                    false,
                    true);

            PlayerControlOutput _output = mRig.StateMachine.Tick(_input, 0.0f);

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Attack));
            Assert.That(
                mRig.AttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Windup));
            Assert.That(mRig.AttackDriver.LastCommandResult.IsAccepted, Is.True);
            Assert.That(_output.CanTurn, Is.False);
            Assert.That(_output.HasLockedFacingDirection, Is.True);
            Assert.That(
                _output.LockedFacingDirection,
                Is.EqualTo(Vector3.forward));
            Assert.That(
                _output.HorizontalVelocity,
                Is.EqualTo(
                    Vector3.right *
                    mRig.MovementConfig.MoveSpeed *
                    mRig.AttackConfig.MovementSpeedMultiplier));
        }

        /// <summary>
        /// 验证 Move 状态可以进入 Attack，转移不会要求先经过 Idle。
        /// </summary>
        [Test]
        public void Tick_AttackFromMove_TransitionsDirectlyToAttack()
        {
            PlayerControlInput _moveInput =
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.forward,
                    Vector3.forward);
            mRig.StateMachine.Tick(_moveInput, 0.0f);

            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.forward,
                    Vector3.forward,
                    false,
                    true),
                0.0f);

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Attack));
        }

        /// <summary>
        /// 验证同帧同时按下 Dash 和 Attack 时仅 Dash 获得互斥控制权。
        /// </summary>
        [Test]
        public void Tick_DashAndAttackPressedTogether_PrioritizesDashOnly()
        {
            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.forward,
                    Vector3.forward,
                    true,
                    true),
                0.0f);

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Dash));
            Assert.That(
                mRig.AttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(
                mRig.AttackDriver.LastCommandResult.WasProcessed,
                Is.False);
        }

        /// <summary>
        /// 验证 Dash 期间的 Attack 边沿不会启动攻击或延迟到 Dash 结束后执行。
        /// </summary>
        [Test]
        public void Tick_AttackPressedDuringDash_IsIgnoredWithoutDeferredStart()
        {
            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.zero,
                    Vector3.forward,
                    true),
                0.0f);
            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.zero,
                    Vector3.forward,
                    false,
                    true),
                mRig.MovementConfig.DashDuration * 0.5f);
            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.zero,
                    Vector3.forward),
                mRig.MovementConfig.DashDuration * 0.5f);

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
            Assert.That(
                mRig.AttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
        }

        /// <summary>
        /// 验证 Attack 期间的 Dash 边沿不会被缓存到 Recovery 结束之后。
        /// </summary>
        [Test]
        public void Tick_DashPressedDuringAttack_IsIgnoredWithoutDeferredStart()
        {
            StartAttack();
            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.zero,
                    Vector3.forward,
                    true),
                mRig.AttackConfig.WindupDuration * 0.5f);

            float _remainingAttackDuration =
                mRig.AttackConfig.WindupDuration * 0.5f +
                mRig.AttackConfig.ActiveDuration +
                mRig.AttackConfig.RecoveryDuration;
            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.zero,
                    Vector3.forward),
                _remainingAttackDuration);
            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.zero,
                    Vector3.forward),
                0.0f);

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
            Assert.That(
                mRig.AttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Cooldown));
        }

        /// <summary>
        /// 验证 Recovery 结束即释放控制状态，并按结束帧移动输入进入 Move。
        /// </summary>
        [Test]
        public void Tick_WhenRecoveryEnds_TransitionsToMoveWhileCooldownContinues()
        {
            StartAttack();
            float _attackDuration =
                mRig.AttackConfig.WindupDuration +
                mRig.AttackConfig.ActiveDuration +
                mRig.AttackConfig.RecoveryDuration;

            PlayerControlOutput _output = mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.right,
                    Vector3.forward),
                _attackDuration);

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Move));
            Assert.That(
                mRig.AttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Cooldown));
            Assert.That(
                _output.HorizontalVelocity,
                Is.EqualTo(Vector3.right * mRig.MovementConfig.MoveSpeed));
        }

        /// <summary>
        /// 验证 Recovery 结束帧收到的 Attack 边沿只由旧 Attack 状态消费，不会重复提交。
        /// </summary>
        [Test]
        public void Tick_AttackPressedWhenRecoveryEnds_DoesNotSubmitSecondCommand()
        {
            StartAttack();
            float _attackDuration =
                mRig.AttackConfig.WindupDuration +
                mRig.AttackConfig.ActiveDuration +
                mRig.AttackConfig.RecoveryDuration;

            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.zero,
                    Vector3.forward,
                    false,
                    true),
                _attackDuration);

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
            Assert.That(mRig.AttackDriver.LastCommandResult.IsAccepted, Is.True);
            Assert.That(
                mRig.AttackDriver.LastCommandResult.RequestSequence,
                Is.EqualTo(1UL));
        }

        /// <summary>
        /// 验证禁用会中断攻击并清空冷却，重新启用后仍可提交下一序号攻击。
        /// </summary>
        [Test]
        public void SetEnabled_DisableDuringAttack_ResetsAndAllowsFreshAttack()
        {
            StartAttack();

            mRig.StateMachine.SetEnabled(false);

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Disabled));
            Assert.That(
                mRig.AttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));

            mRig.StateMachine.SetEnabled(true);
            StartAttack();

            Assert.That(
                mRig.StateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Attack));
            Assert.That(mRig.AttackDriver.LastCommandResult.IsAccepted, Is.True);
            Assert.That(
                mRig.AttackDriver.LastCommandResult.RequestSequence,
                Is.EqualTo(2UL));
        }

        /// <summary>
        /// 从 Idle 使用默认前方向启动一次普通攻击。
        /// </summary>
        private void StartAttack()
        {
            mRig.StateMachine.Tick(
                PlayerControlStateMachineTestRig.CreateInput(
                    Vector3.zero,
                    Vector3.forward,
                    false,
                    true),
                0.0f);
        }
    }
}
