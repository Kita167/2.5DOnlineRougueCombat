using System.Collections.Generic;
using NUnit.Framework;
using ProjectRelay.Core;
using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectRelay.Tests.EditMode.Combat
{
    /// <summary>
    /// 验证普通攻击阶段时序、动作锁、冷却拒绝和强制中断清理。
    /// </summary>
    public sealed class BasicAttackControllerTests
    {
        private GameObject mControllerObject;
        private PlayerMovementConfig mMovementConfig;
        private BasicAttackDefinition mDefinition;
        private PlayerActionStateMachine mStateMachine;
        private BasicAttackController mController;

        /// <summary>
        /// 为每个测试创建独立定义、动作状态机和攻击控制器。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mMovementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            mDefinition = ScriptableObject.CreateInstance<BasicAttackDefinition>();
            mStateMachine = new PlayerActionStateMachine(mMovementConfig);
            mStateMachine.SetEnabled(true);

            mControllerObject = new GameObject("BasicAttackControllerTest");
            CombatantIdentity _identity =
                mControllerObject.AddComponent<CombatantIdentity>();
            Assert.That(
                _identity.Initialize(new CombatantId(100UL), Faction.Player),
                Is.True);
            mController = mControllerObject.AddComponent<BasicAttackController>();
            Assert.That(mController.Initialize(mStateMachine, mDefinition), Is.True);
        }

        /// <summary>
        /// 销毁测试期间创建的 GameObject 和 ScriptableObject，避免运行时状态跨测试残留。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (mControllerObject != null)
            {
                Object.DestroyImmediate(mControllerObject);
            }

            if (mDefinition != null)
            {
                Object.DestroyImmediate(mDefinition);
            }

            if (mMovementConfig != null)
            {
                Object.DestroyImmediate(mMovementConfig);
            }
        }

        /// <summary>
        /// 验证合法请求锁定平面方向、进入 Windup 并同时占用 Attacking 状态。
        /// </summary>
        [Test]
        public void TryStartAttack_FromFree_EntersWindupAndLocksDirection()
        {
            bool _didStart = mController.TryStartAttack(
                new Vector3(2.0f, 5.0f, 0.0f));

            Assert.That(_didStart, Is.True);
            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Windup));
            Assert.That(mController.LockedAttackDirection, Is.EqualTo(Vector3.right));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Attacking));
            Assert.That(
                mStateMachine.CurrentConstraints.MovementSpeedMultiplier,
                Is.EqualTo(mDefinition.MovementSpeedMultiplier));
        }

        /// <summary>
        /// 验证一个大时间增量可以依次经过全部阶段，不会跳过 Active 或留下动作锁。
        /// </summary>
        [Test]
        public void Tick_LargeDelta_VisitsAllPhasesAndReturnsIdle()
        {
            List<BasicAttackPhase> _visitedPhases = new List<BasicAttackPhase>();
            mController.PhaseChanged += (_previous, _next, _attackId) =>
                _visitedPhases.Add(_next);
            mController.TryStartAttack(Vector3.forward);

            float _totalDuration =
                mDefinition.WindupDuration +
                mDefinition.ActiveDuration +
                mDefinition.RecoveryDuration +
                mDefinition.CooldownDuration;
            mController.Tick(_totalDuration + 1.0f);

            CollectionAssert.AreEqual(
                new[]
                {
                    BasicAttackPhase.Windup,
                    BasicAttackPhase.Active,
                    BasicAttackPhase.Recovery,
                    BasicAttackPhase.Cooldown,
                    BasicAttackPhase.Idle
                },
                _visitedPhases);
            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));
            Assert.That(mController.LockedAttackDirection, Is.EqualTo(Vector3.zero));
        }

        /// <summary>
        /// 验证全部零时长阶段会在启动调用内安全跳过且每个阶段只进入一次。
        /// </summary>
        [Test]
        public void TryStartAttack_WithZeroDurations_CompletesWithoutLooping()
        {
            SerializedObject _serializedDefinition = new SerializedObject(mDefinition);
            _serializedDefinition.FindProperty("mWindupDuration").floatValue = 0.0f;
            _serializedDefinition.FindProperty("mActiveDuration").floatValue = 0.0f;
            _serializedDefinition.FindProperty("mRecoveryDuration").floatValue = 0.0f;
            _serializedDefinition.FindProperty("mCooldownDuration").floatValue = 0.0f;
            _serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            List<BasicAttackPhase> _visitedPhases = new List<BasicAttackPhase>();
            mController.PhaseChanged += (_previous, _next, _attackId) =>
                _visitedPhases.Add(_next);

            bool _didStart = mController.TryStartAttack(Vector3.forward);

            Assert.That(_didStart, Is.True);
            CollectionAssert.AreEqual(
                new[]
                {
                    BasicAttackPhase.Windup,
                    BasicAttackPhase.Active,
                    BasicAttackPhase.Recovery,
                    BasicAttackPhase.Cooldown,
                    BasicAttackPhase.Idle
                },
                _visitedPhases);
            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));
        }

        /// <summary>
        /// 验证 Recovery 结束时立即释放动作锁，但 Cooldown 结束前仍拒绝下一次攻击。
        /// </summary>
        [Test]
        public void TryStartAttack_DuringCooldown_RejectsUntilCooldownEnds()
        {
            mController.TryStartAttack(Vector3.forward);
            float _attackDuration =
                mDefinition.WindupDuration +
                mDefinition.ActiveDuration +
                mDefinition.RecoveryDuration;

            mController.Tick(_attackDuration);

            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Cooldown));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));
            Assert.That(mController.TryStartAttack(Vector3.right), Is.False);

            mController.Tick(mDefinition.CooldownDuration);

            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mController.TryStartAttack(Vector3.right), Is.True);
        }

        /// <summary>
        /// 验证重复请求不会重置当前阶段计时或改变首次锁定的攻击方向。
        /// </summary>
        [Test]
        public void TryStartAttack_WhileAttacking_RejectsWithoutChangingRuntimeState()
        {
            mController.TryStartAttack(Vector3.forward);
            float _phaseTimeBefore = mController.PhaseTimeRemaining;

            bool _didStartAgain = mController.TryStartAttack(Vector3.right);

            Assert.That(_didStartAgain, Is.False);
            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Windup));
            Assert.That(mController.PhaseTimeRemaining, Is.EqualTo(_phaseTimeBefore));
            Assert.That(mController.LockedAttackDirection, Is.EqualTo(Vector3.forward));
        }

        /// <summary>
        /// 验证强制中断可以从攻击阶段回到 Idle、释放动作锁并清空方向和计时。
        /// </summary>
        [Test]
        public void ForceReset_DuringAttack_ClearsPhaseAndActionLock()
        {
            mController.TryStartAttack(Vector3.forward);

            mController.ForceReset();

            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mController.PhaseTimeRemaining, Is.Zero);
            Assert.That(mController.LockedAttackDirection, Is.EqualTo(Vector3.zero));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));
        }

        /// <summary>
        /// 验证重复初始化收到非法依赖时会先清理旧攻击，且不会继续以旧配置保持就绪。
        /// </summary>
        [Test]
        public void Initialize_InvalidAfterValid_CleansPreviousRuntimeState()
        {
            Assert.That(mController.TryStartAttack(Vector3.forward), Is.True);
            LogAssert.Expect(
                LogType.Error,
                "[Combat] BasicAttackController 初始化失败：动作状态机为空。");

            bool _didInitialize = mController.Initialize(null, mDefinition);

            Assert.That(_didInitialize, Is.False);
            Assert.That(mController.IsInitialized, Is.False);
            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));
        }

        /// <summary>
        /// 验证常见目标帧率下阶段和冷却总时长都能稳定推进到 Idle，且 Active 只进入一次。
        /// </summary>
        /// <param name="_framesPerSecond">用于模拟固定帧时间的目标帧率。</param>
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void Tick_AtCommonFrameRates_CompletesWithSingleActiveEntry(
            int _framesPerSecond)
        {
            int _activeEntryCount = 0;
            mController.PhaseChanged += (_previous, _next, _attackId) =>
            {
                if (_next == BasicAttackPhase.Active)
                {
                    _activeEntryCount++;
                }
            };
            mController.TryStartAttack(Vector3.forward);

            float _deltaTime = 1.0f / _framesPerSecond;
            float _totalDuration =
                mDefinition.WindupDuration +
                mDefinition.ActiveDuration +
                mDefinition.RecoveryDuration +
                mDefinition.CooldownDuration;
            int _frameCount = Mathf.CeilToInt(_totalDuration / _deltaTime) + 1;

            for (int _frameIndex = 0; _frameIndex < _frameCount; _frameIndex++)
            {
                mController.Tick(_deltaTime);
            }

            Assert.That(_activeEntryCount, Is.EqualTo(1));
            Assert.That(mController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));
        }
    }
}
