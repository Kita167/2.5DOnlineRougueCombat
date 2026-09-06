using NUnit.Framework;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Player
{
    /// <summary>
    /// 验证新 Player 控制状态机的初始状态、显式转移图和状态变化通知。
    /// </summary>
    public sealed class PlayerControlStateMachineTransitionTests
    {
        private PlayerMovementConfig mMovementConfig;
        private PlayerControlStateMachine mStateMachine;

        /// <summary>
        /// 为每个测试创建独立配置和处于 Disabled 的新控制状态机。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mMovementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            mStateMachine = new PlayerControlStateMachine(mMovementConfig);
        }

        /// <summary>
        /// 销毁测试期间创建的临时 ScriptableObject。
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
        /// 验证新实例从 Disabled 开始且只允许进入已注册的 Idle。
        /// </summary>
        [Test]
        public void Constructor_WithValidConfig_StartsDisabledWithExplicitTransitions()
        {
            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Disabled));
            Assert.That(
                mStateMachine.CanTransitionTo(PlayerControlStateId.Idle),
                Is.True);
            Assert.That(
                mStateMachine.CanTransitionTo(PlayerControlStateId.Move),
                Is.False);
            Assert.That(
                mStateMachine.CanTransitionTo(PlayerControlStateId.Attack),
                Is.False);
            Assert.That(
                mStateMachine.CanTransitionTo(PlayerControlStateId.Dash),
                Is.False);
        }

        /// <summary>
        /// 验证启用控制完成状态切换后发布包含新状态和原因的通知。
        /// </summary>
        [Test]
        public void SetEnabled_FromDisabled_EntersIdleAndRaisesStateChanged()
        {
            PlayerControlTransition _observedTransition = default;
            PlayerControlStateId _stateSeenByObserver = PlayerControlStateId.Disabled;
            int _eventCount = 0;

            mStateMachine.StateChanged += _transition =>
            {
                _observedTransition = _transition;
                _stateSeenByObserver = mStateMachine.CurrentStateId;
                _eventCount++;
            };

            mStateMachine.SetEnabled(true);

            Assert.That(_eventCount, Is.EqualTo(1));
            Assert.That(_observedTransition.From, Is.EqualTo(PlayerControlStateId.Disabled));
            Assert.That(_observedTransition.To, Is.EqualTo(PlayerControlStateId.Idle));
            Assert.That(
                _observedTransition.Reason,
                Is.EqualTo(PlayerControlTransitionReason.Enable));
            Assert.That(_stateSeenByObserver, Is.EqualTo(PlayerControlStateId.Idle));
        }

        /// <summary>
        /// 验证移动输入可以在同一 Tick 完成 Idle 到 Move 的合法转移。
        /// </summary>
        [Test]
        public void Tick_WithMoveInput_TransitionsFromIdleToMove()
        {
            mStateMachine.SetEnabled(true);
            PlayerControlInput _input = new PlayerControlInput(
                Vector3.forward,
                Vector3.forward,
                false,
                false);

            mStateMachine.Tick(_input, 0.02f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Move));
            Assert.That(
                mStateMachine.CanTransitionTo(PlayerControlStateId.Idle),
                Is.True);
            Assert.That(
                mStateMachine.CanTransitionTo(PlayerControlStateId.Attack),
                Is.True);
            Assert.That(
                mStateMachine.CanTransitionTo(PlayerControlStateId.Dash),
                Is.True);
        }
    }
}
