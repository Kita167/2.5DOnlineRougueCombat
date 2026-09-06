using NUnit.Framework;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Player
{
    /// <summary>
    /// 验证新 Player 控制状态机的 Idle、Move、模拟量输入和异常输入行为。
    /// </summary>
    public sealed class PlayerControlStateMachineMovementTests
    {
        private PlayerMovementConfig mMovementConfig;
        private PlayerControlStateMachine mStateMachine;

        /// <summary>
        /// 为每个测试创建独立配置和已启用的控制状态机。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mMovementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            mStateMachine = new PlayerControlStateMachine(mMovementConfig);
            mStateMachine.SetEnabled(true);
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
        /// 验证没有有效移动输入时保持 Idle 并输出零水平速度。
        /// </summary>
        [Test]
        public void Tick_WithoutMoveInput_RemainsIdleWithZeroVelocity()
        {
            PlayerControlInput _input = CreateInput(Vector3.zero);

            PlayerControlOutput _output = mStateMachine.Tick(_input, 0.02f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
            Assert.That(_output.HorizontalVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(_output.CanTurn, Is.True);
        }

        /// <summary>
        /// 验证移动输入在同一个 Tick 进入 Move 并立即输出配置速度。
        /// </summary>
        [Test]
        public void Tick_WithMoveInput_EntersMoveAndOutputsVelocityImmediately()
        {
            PlayerControlInput _input = CreateInput(Vector3.right);

            PlayerControlOutput _output = mStateMachine.Tick(_input, 0.02f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Move));
            Assert.That(
                _output.HorizontalVelocity,
                Is.EqualTo(Vector3.right * mMovementConfig.MoveSpeed));
            Assert.That(_output.CanTurn, Is.True);
        }

        /// <summary>
        /// 验证模拟量输入强度不会被普通移动状态错误归一化。
        /// </summary>
        [Test]
        public void Tick_WithAnalogMoveInput_PreservesInputMagnitude()
        {
            Vector3 _halfInput = Vector3.right * 0.5f;

            PlayerControlOutput _output =
                mStateMachine.Tick(CreateInput(_halfInput), 0.02f);

            Assert.That(
                _output.HorizontalVelocity,
                Is.EqualTo(_halfInput * mMovementConfig.MoveSpeed));
        }

        /// <summary>
        /// 验证长度超过一的对角输入会被限制，避免移动速度高于配置值。
        /// </summary>
        [Test]
        public void Tick_WithOversizedDiagonalInput_ClampsToConfiguredSpeed()
        {
            Vector3 _diagonalInput = new Vector3(1.0f, 0.0f, 1.0f);

            PlayerControlOutput _output =
                mStateMachine.Tick(CreateInput(_diagonalInput), 0.02f);

            Assert.That(
                _output.HorizontalVelocity.magnitude,
                Is.EqualTo(mMovementConfig.MoveSpeed).Within(0.0001f));
        }

        /// <summary>
        /// 验证移动输入停止时在同一个 Tick 返回 Idle 并清零速度。
        /// </summary>
        [Test]
        public void Tick_WhenMoveInputStops_ReturnsIdleWithZeroVelocityImmediately()
        {
            mStateMachine.Tick(CreateInput(Vector3.forward), 0.02f);

            PlayerControlOutput _output =
                mStateMachine.Tick(CreateInput(Vector3.zero), 0.02f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
            Assert.That(_output.HorizontalVelocity, Is.EqualTo(Vector3.zero));
        }

        /// <summary>
        /// 验证 NaN 和无穷移动分量被转换为无输入，不会污染速度输出。
        /// </summary>
        [Test]
        public void Tick_WithInvalidMoveInput_RemainsIdleWithFiniteZeroVelocity()
        {
            Vector3 _invalidInput = new Vector3(
                float.NaN,
                0.0f,
                float.PositiveInfinity);

            PlayerControlOutput _output =
                mStateMachine.Tick(CreateInput(_invalidInput), 0.02f);

            Assert.That(
                mStateMachine.CurrentStateId,
                Is.EqualTo(PlayerControlStateId.Idle));
            Assert.That(_output.HorizontalVelocity, Is.EqualTo(Vector3.zero));
        }

        /// <summary>
        /// 使用给定移动方向创建其余字段稳定的本帧输入。
        /// </summary>
        /// <param name="_moveDirection">需要测试的世界空间移动输入。</param>
        /// <returns>朝向固定向前且没有动作按键的控制输入。</returns>
        private static PlayerControlInput CreateInput(Vector3 _moveDirection)
        {
            return new PlayerControlInput(
                _moveDirection,
                Vector3.forward,
                false,
                false);
        }
    }
}
