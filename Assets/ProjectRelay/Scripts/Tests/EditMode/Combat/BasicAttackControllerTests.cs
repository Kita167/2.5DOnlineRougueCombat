using System.Collections.Generic;
using NUnit.Framework;
using ProjectRelay.Core;
using ProjectRelay.Gameplay.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectRelay.Tests.EditMode.Combat
{
    /// <summary>
    /// 验证普通攻击执行器独立管理阶段、冷却、方向和重置，不依赖 Player 控制状态机。
    /// </summary>
    public sealed class BasicAttackControllerTests
    {
        private GameObject mControllerObject;
        private BasicAttackConfig mConfig;
        private BasicAttackController mController;

        /// <summary>
        /// 为每个测试创建独立攻击配置、有效攻击者和攻击执行器。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mConfig = ScriptableObject.CreateInstance<BasicAttackConfig>();
            mControllerObject = new GameObject("BasicAttackControllerTest");
            CombatantIdentity _identity =
                mControllerObject.AddComponent<CombatantIdentity>();
            Assert.That(
                _identity.Initialize(new CombatantId(100UL), Faction.Player),
                Is.True);
            mController = mControllerObject.AddComponent<BasicAttackController>();
            Assert.That(mController.Initialize(mConfig), Is.True);
        }

        /// <summary>
        /// 销毁测试期间创建的 GameObject 和 ScriptableObject。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (mControllerObject != null)
            {
                Object.DestroyImmediate(mControllerObject);
            }

            if (mConfig != null)
            {
                Object.DestroyImmediate(mConfig);
            }
        }

        /// <summary>
        /// 验证合法请求锁定平面方向并独立进入 Windup。
        /// </summary>
        [Test]
        public void TryStartAttack_FromIdle_EntersWindupAndLocksDirection()
        {
            bool _didStart = mController.TryStartAttack(
                new Vector3(2.0f, 5.0f, 0.0f));

            Assert.That(_didStart, Is.True);
            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Windup));
            Assert.That(
                mController.LockedAttackDirection,
                Is.EqualTo(Vector3.right));
        }

        /// <summary>
        /// 验证一个大时间增量可以经过全部阶段且不会跳过 Active。
        /// </summary>
        [Test]
        public void Tick_LargeDelta_VisitsAllPhasesAndReturnsIdle()
        {
            List<BasicAttackPhase> _visitedPhases =
                new List<BasicAttackPhase>();
            mController.PhaseChanged += (_previous, _next, _attackId) =>
                _visitedPhases.Add(_next);
            mController.TryStartAttack(Vector3.forward);

            float _totalDuration =
                mConfig.WindupDuration +
                mConfig.ActiveDuration +
                mConfig.RecoveryDuration +
                mConfig.CooldownDuration;
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
            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(
                mController.LockedAttackDirection,
                Is.EqualTo(Vector3.zero));
        }

        /// <summary>
        /// 验证全部零时长阶段会在启动调用内安全跳过且每个阶段只进入一次。
        /// </summary>
        [Test]
        public void TryStartAttack_WithZeroDurations_CompletesWithoutLooping()
        {
            SerializedObject _serializedConfig = new SerializedObject(mConfig);
            _serializedConfig.FindProperty("mWindupDuration").floatValue = 0.0f;
            _serializedConfig.FindProperty("mActiveDuration").floatValue = 0.0f;
            _serializedConfig.FindProperty("mRecoveryDuration").floatValue = 0.0f;
            _serializedConfig.FindProperty("mCooldownDuration").floatValue = 0.0f;
            _serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            List<BasicAttackPhase> _visitedPhases =
                new List<BasicAttackPhase>();
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
            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
        }

        /// <summary>
        /// 验证 Recovery 结束后进入 Cooldown，并在冷却结束前拒绝下一次攻击。
        /// </summary>
        [Test]
        public void TryStartAttack_DuringCooldown_RejectsUntilCooldownEnds()
        {
            mController.TryStartAttack(Vector3.forward);
            float _attackDuration =
                mConfig.WindupDuration +
                mConfig.ActiveDuration +
                mConfig.RecoveryDuration;

            mController.Tick(_attackDuration);

            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Cooldown));
            Assert.That(mController.TryStartAttack(Vector3.right), Is.False);

            mController.Tick(mConfig.CooldownDuration);

            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mController.TryStartAttack(Vector3.right), Is.True);
        }

        /// <summary>
        /// 验证重复请求不会重置阶段计时或改变首次锁定方向。
        /// </summary>
        [Test]
        public void TryStartAttack_WhileAttacking_RejectsWithoutChangingRuntimeState()
        {
            mController.TryStartAttack(Vector3.forward);
            float _phaseTimeBefore = mController.PhaseTimeRemaining;

            bool _didStartAgain = mController.TryStartAttack(Vector3.right);

            Assert.That(_didStartAgain, Is.False);
            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Windup));
            Assert.That(
                mController.PhaseTimeRemaining,
                Is.EqualTo(_phaseTimeBefore));
            Assert.That(
                mController.LockedAttackDirection,
                Is.EqualTo(Vector3.forward));
        }

        /// <summary>
        /// 验证强制中断可以清空阶段、方向、计时和冷却。
        /// </summary>
        [Test]
        public void ForceReset_DuringAttack_ClearsAllRuntimeState()
        {
            mController.TryStartAttack(Vector3.forward);

            mController.ForceReset();

            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mController.PhaseTimeRemaining, Is.Zero);
            Assert.That(
                mController.LockedAttackDirection,
                Is.EqualTo(Vector3.zero));
        }

        /// <summary>
        /// 验证重复初始化收到非法配置时会清理旧攻击且不会保持就绪。
        /// </summary>
        [Test]
        public void Initialize_InvalidAfterValid_CleansPreviousRuntimeState()
        {
            Assert.That(mController.TryStartAttack(Vector3.forward), Is.True);
            LogAssert.Expect(
                LogType.Error,
                "[Combat] BasicAttackController 初始化失败：普通攻击配置为空或包含非法值。");

            bool _didInitialize = mController.Initialize(null);

            Assert.That(_didInitialize, Is.False);
            Assert.That(mController.IsInitialized, Is.False);
            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
        }

        /// <summary>
        /// 验证常见帧率下阶段和冷却都能稳定推进到 Idle，且 Active 只进入一次。
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
                mConfig.WindupDuration +
                mConfig.ActiveDuration +
                mConfig.RecoveryDuration +
                mConfig.CooldownDuration;
            int _frameCount =
                Mathf.CeilToInt(_totalDuration / _deltaTime) + 1;

            for (int _frameIndex = 0; _frameIndex < _frameCount; _frameIndex++)
            {
                mController.Tick(_deltaTime);
            }

            Assert.That(_activeEntryCount, Is.EqualTo(1));
            Assert.That(
                mController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
        }
    }
}
