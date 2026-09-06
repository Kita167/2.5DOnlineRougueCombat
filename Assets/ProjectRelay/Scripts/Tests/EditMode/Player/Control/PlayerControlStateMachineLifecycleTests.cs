using NUnit.Framework;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Player
{
    /// <summary>
    /// 验证新 Player 控制状态机在禁用、重复重置和非法时间输入下保持干净生命周期。
    /// </summary>
    public sealed class PlayerControlStateMachineLifecycleTests
    {
        private PlayerMovementConfig mMovementConfig;
        private PlayerControlStateMachine mStateMachine;

        /// <summary>
        /// 为每个测试创建独立配置和新控制状态机。
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
        /// 验证禁用移动中的状态机会清除累计时间和最终输出。
        /// </summary>
        [Test]
        public void SetEnabled_WhenMoving_DisablesAndClearsRuntime()
        {
            mStateMachine.SetEnabled(true);
            PlayerControlInput _moveInput = new PlayerControlInput(
                Vector3.forward,
                Vector3.forward,
                false,
                false);
            mStateMachine.Tick(_moveInput, 0.25f);

            mStateMachine.SetEnabled(false);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Disabled));
            Assert.That(mStateMachine.ElapsedTime, Is.Zero);
            Assert.That(mStateMachine.CurrentOutput.HorizontalVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(mStateMachine.CurrentOutput.CanTurn, Is.False);
        }

        /// <summary>
        /// 验证重复禁用和强制重置不会重复发布没有发生的状态转移。
        /// </summary>
        [Test]
        public void ForceReset_WhenAlreadyDisabled_DoesNotRaiseDuplicateTransition()
        {
            int _eventCount = 0;
            mStateMachine.StateChanged += _transition => _eventCount++;

            mStateMachine.SetEnabled(false);
            mStateMachine.ForceReset();

            Assert.That(_eventCount, Is.Zero);
            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Disabled));
        }

        /// <summary>
        /// 验证负值、NaN 和无穷时间不会减少或污染状态机累计时间。
        /// </summary>
        [Test]
        public void Tick_WithInvalidDeltaTime_DoesNotPolluteElapsedTime()
        {
            mStateMachine.SetEnabled(true);
            PlayerControlInput _input = new PlayerControlInput(
                Vector3.zero,
                Vector3.forward,
                false,
                false);

            mStateMachine.Tick(_input, -1.0f);
            mStateMachine.Tick(_input, float.NaN);
            mStateMachine.Tick(_input, float.PositiveInfinity);

            Assert.That(mStateMachine.ElapsedTime, Is.Zero);
            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
        }
    }
}
