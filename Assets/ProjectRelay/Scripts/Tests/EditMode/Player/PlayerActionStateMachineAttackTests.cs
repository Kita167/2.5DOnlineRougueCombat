using NUnit.Framework;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Player
{
    /// <summary>
    /// 验证 Attacking 动作状态的进入条件、移动约束以及 Dash 互斥和清理行为。
    /// </summary>
    public sealed class PlayerActionStateMachineAttackTests
    {
        private PlayerMovementConfig mMovementConfig;
        private PlayerActionStateMachine mStateMachine;

        /// <summary>
        /// 为每个测试创建独立移动配置和已启用的动作状态机。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mMovementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            mStateMachine = new PlayerActionStateMachine(mMovementConfig);
            mStateMachine.SetEnabled(true);
        }

        /// <summary>
        /// 销毁测试期间创建的临时 ScriptableObject，避免跨测试残留。
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
        /// 验证从 Free 进入 Attacking 后锁定平面朝向并应用攻击移动倍率。
        /// </summary>
        [Test]
        public void TryEnterAttacking_FromFree_AppliesLockedActionConstraints()
        {
            bool _didEnter = mStateMachine.TryEnterAttacking(
                0.5f,
                new Vector3(2.0f, 4.0f, 0.0f));

            PlayerActionConstraints _constraints = mStateMachine.CurrentConstraints;
            Vector3 _horizontalVelocity =
                mStateMachine.CalculateHorizontalVelocity(Vector3.right);

            Assert.That(_didEnter, Is.True);
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Attacking));
            Assert.That(_constraints.MovementSpeedMultiplier, Is.EqualTo(0.5f));
            Assert.That(_constraints.CanTurn, Is.False);
            Assert.That(_constraints.CanDash, Is.False);
            Assert.That(_constraints.CanAttack, Is.False);
            Assert.That(_constraints.HasLockedFacingDirection, Is.True);
            Assert.That(_constraints.LockedFacingDirection, Is.EqualTo(Vector3.right));
            Assert.That(
                _horizontalVelocity,
                Is.EqualTo(Vector3.right * mMovementConfig.MoveSpeed * 0.5f));
        }

        /// <summary>
        /// 验证 Attacking 期间的 Dash 输入被拒绝且不会在攻击完成后自动触发。
        /// </summary>
        [Test]
        public void TryDash_WhileAttacking_RejectsAndDoesNotBufferInput()
        {
            mStateMachine.TryEnterAttacking(0.5f, Vector3.forward);

            bool _didDashDuringAttack = mStateMachine.TryDash(
                Vector3.forward,
                Vector3.forward,
                true,
                0.0f);
            bool _didComplete = mStateMachine.CompleteAttacking();
            bool _didDashAfterAttack = mStateMachine.TryDash(
                Vector3.forward,
                Vector3.forward,
                false,
                0.01f);

            Assert.That(_didDashDuringAttack, Is.False);
            Assert.That(_didComplete, Is.True);
            Assert.That(_didDashAfterAttack, Is.False);
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));
        }

        /// <summary>
        /// 验证 Dashing 期间不能进入 Attacking，现有冲刺状态不会被拒绝请求修改。
        /// </summary>
        [Test]
        public void TryEnterAttacking_WhileDashing_RejectsWithoutChangingDash()
        {
            bool _didDash = mStateMachine.TryDash(
                Vector3.forward,
                Vector3.forward,
                true,
                0.0f);
            bool _didEnterAttack =
                mStateMachine.TryEnterAttacking(0.5f, Vector3.right);

            Assert.That(_didDash, Is.True);
            Assert.That(_didEnterAttack, Is.False);
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Dashing));
        }

        /// <summary>
        /// 验证非法移动倍率或无效平面方向不会污染当前 Free 状态。
        /// </summary>
        [Test]
        public void TryEnterAttacking_WithInvalidConstraint_RejectsWithoutStateChange()
        {
            bool _didEnterWithInvalidMultiplier =
                mStateMachine.TryEnterAttacking(float.NaN, Vector3.forward);
            bool _didEnterWithInvalidDirection =
                mStateMachine.TryEnterAttacking(0.5f, Vector3.up);

            Assert.That(_didEnterWithInvalidMultiplier, Is.False);
            Assert.That(_didEnterWithInvalidDirection, Is.False);
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));
        }

        /// <summary>
        /// 验证强制重置可以从 Attacking 进入 Disabled 并清除所有动作约束。
        /// </summary>
        [Test]
        public void ForceReset_WhileAttacking_DisablesAndClearsConstraints()
        {
            mStateMachine.TryEnterAttacking(0.5f, Vector3.forward);

            mStateMachine.ForceReset();

            PlayerActionConstraints _constraints = mStateMachine.CurrentConstraints;
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Disabled));
            Assert.That(_constraints.MovementSpeedMultiplier, Is.Zero);
            Assert.That(_constraints.CanTurn, Is.False);
            Assert.That(_constraints.CanDash, Is.False);
            Assert.That(_constraints.CanAttack, Is.False);
            Assert.That(_constraints.HasLockedFacingDirection, Is.False);
            Assert.That(_constraints.LockedFacingDirection, Is.EqualTo(Vector3.zero));
        }
    }
}
