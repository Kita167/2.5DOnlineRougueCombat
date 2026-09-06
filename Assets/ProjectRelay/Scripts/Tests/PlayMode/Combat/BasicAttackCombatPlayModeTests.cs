using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectRelay.Core;
using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Gameplay.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectRelay.Tests.PlayMode.Combat
{
    /// <summary>
    /// 验证普通攻击在真实 Unity 物理世界中的单次命中、过滤和组件生命周期清理。
    /// 每个测试临时创建运行对象，不依赖项目场景、Prefab、Animator 或 VFX 资产。
    /// </summary>
    public sealed class BasicAttackCombatPlayModeTests
    {
        private readonly List<GameObject> mCreatedObjects = new List<GameObject>();

        private PlayerMovementConfig mMovementConfig;
        private BasicAttackDefinition mDefinition;
        private PlayerActionStateMachine mStateMachine;
        private GameObject mAttackerObject;
        private CombatantIdentity mAttackerIdentity;
        private BasicAttackController mAttackController;

        /// <summary>
        /// 为每个测试建立独立攻击定义、动作状态机和有效玩家攻击者。
        /// </summary>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            mMovementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            mDefinition = ScriptableObject.CreateInstance<BasicAttackDefinition>();
            mStateMachine = new PlayerActionStateMachine(mMovementConfig);
            mStateMachine.SetEnabled(true);

            mAttackerObject = CreateObject("BasicAttackPlayModeAttacker", Vector3.zero);
            mAttackerIdentity = mAttackerObject.AddComponent<CombatantIdentity>();
            Assert.That(
                mAttackerIdentity.Initialize(new CombatantId(100UL), Faction.Player),
                Is.True);
            mAttackerObject.AddComponent<SphereCollider>();
            mAttackController = mAttackerObject.AddComponent<BasicAttackController>();
            Assert.That(
                mAttackController.Initialize(mStateMachine, mDefinition),
                Is.True);

            Physics.SyncTransforms();
            yield return null;
        }

        /// <summary>
        /// 销毁运行时对象和临时定义，并等待一帧让 Unity 完成生命周期清理。
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int _index = mCreatedObjects.Count - 1; _index >= 0; _index--)
            {
                GameObject _createdObject = mCreatedObjects[_index];

                if (_createdObject != null)
                {
                    Object.Destroy(_createdObject);
                }
            }

            mCreatedObjects.Clear();

            if (mDefinition != null)
            {
                Object.Destroy(mDefinition);
            }

            if (mMovementConfig != null)
            {
                Object.Destroy(mMovementConfig);
            }

            yield return null;
        }

        /// <summary>
        /// 验证进入 Active 时，一个拥有多个 Collider 的目标只受伤一次，且完全不需要 Presenter。
        /// </summary>
        [UnityTest]
        public IEnumerator Tick_EnteringActive_DamagesMultiColliderTargetOnceWithoutPresenter()
        {
            Health _targetHealth = CreateTarget(
                "MultiColliderEnemy",
                new Vector3(0.0f, 0.0f, 1.0f),
                new CombatantId(200UL),
                Faction.Enemy,
                true);
            int _confirmedDamageCount = 0;
            mAttackController.DamageConfirmed += _result => _confirmedDamageCount++;
            Physics.SyncTransforms();

            Assert.That(mAttackController.TryStartAttack(Vector3.forward), Is.True);
            mAttackController.Tick(mDefinition.WindupDuration);

            Assert.That(
                _targetHealth.CurrentHealth,
                Is.EqualTo(_targetHealth.MaximumHealth - mDefinition.BaseDamage));
            Assert.That(_confirmedDamageCount, Is.EqualTo(1));
            Assert.That(mAttackController.LastAppliedHitCount, Is.EqualTo(1));
            Assert.That(
                mAttackerObject.GetComponentInChildren<BasicAttackPresenter>(),
                Is.Null);
            yield return null;
        }

        /// <summary>
        /// 验证自身、友方、已死亡和范围外目标都会在伤害入口之前被过滤。
        /// </summary>
        [UnityTest]
        public IEnumerator Tick_EnteringActive_FiltersSelfFriendlyDeadAndOutOfRangeTargets()
        {
            Health _friendlyHealth = CreateTarget(
                "FriendlyTarget",
                new Vector3(-0.3f, 0.0f, 1.0f),
                new CombatantId(201UL),
                Faction.Player,
                false);
            Health _deadEnemyHealth = CreateTarget(
                "DeadEnemyTarget",
                new Vector3(0.3f, 0.0f, 1.0f),
                new CombatantId(202UL),
                Faction.Enemy,
                false);
            Health _outsideEnemyHealth = CreateTarget(
                "OutsideEnemyTarget",
                new Vector3(0.0f, 0.0f, 4.0f),
                new CombatantId(203UL),
                Faction.Enemy,
                false);
            DamageContext _lethalDamage = new DamageContext(
                new CombatantId(999UL),
                _deadEnemyHealth.Identity.Id,
                Faction.Player,
                Faction.Enemy,
                new StableId("playmode-test-lethal"),
                DamageType.Physical,
                _deadEnemyHealth.MaximumHealth);
            Assert.That(
                _deadEnemyHealth.TryApplyDamage(
                    _lethalDamage,
                    out DamageResult _result),
                Is.True);
            Assert.That(_result.Killed, Is.True);
            Physics.SyncTransforms();

            Assert.That(mAttackController.TryStartAttack(Vector3.forward), Is.True);
            mAttackController.Tick(mDefinition.WindupDuration);

            Assert.That(
                _friendlyHealth.CurrentHealth,
                Is.EqualTo(_friendlyHealth.MaximumHealth));
            Assert.That(_deadEnemyHealth.CurrentHealth, Is.Zero);
            Assert.That(
                _outsideEnemyHealth.CurrentHealth,
                Is.EqualTo(_outsideEnemyHealth.MaximumHealth));
            Assert.That(mAttackController.LastAppliedHitCount, Is.Zero);
            yield return null;
        }

        /// <summary>
        /// 验证组件在攻击中途禁用会释放动作锁并清空阶段，重新启用后可以安全发起新攻击。
        /// </summary>
        [UnityTest]
        public IEnumerator Enabled_DuringAttackDisableAndEnable_ResetsThenAllowsNewAttack()
        {
            Assert.That(mAttackController.TryStartAttack(Vector3.forward), Is.True);
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Attacking));

            mAttackController.enabled = false;

            Assert.That(mAttackController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
            Assert.That(mAttackController.LockedAttackDirection, Is.EqualTo(Vector3.zero));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Free));

            mAttackController.enabled = true;
            Assert.That(mAttackController.TryStartAttack(Vector3.right), Is.True);
            yield return null;
        }

        /// <summary>
        /// 创建带身份、生命和 Collider 的测试目标，并可添加第二个子 Collider 验证去重。
        /// </summary>
        /// <param name="_name">用于失败日志识别的对象名称。</param>
        /// <param name="_position">目标根对象的世界空间位置。</param>
        /// <param name="_id">目标使用的非零运行时身份。</param>
        /// <param name="_faction">目标参与过滤的阵营。</param>
        /// <param name="_addChildCollider">是否添加指向同一 Health 的第二个 Collider。</param>
        /// <returns>已经初始化为满生命的目标 Health。</returns>
        private Health CreateTarget(
            string _name,
            Vector3 _position,
            CombatantId _id,
            Faction _faction,
            bool _addChildCollider)
        {
            GameObject _targetObject = CreateObject(_name, _position);
            CombatantIdentity _identity =
                _targetObject.AddComponent<CombatantIdentity>();
            Assert.That(_identity.Initialize(_id, _faction), Is.True);
            Health _health = _targetObject.AddComponent<Health>();
            Assert.That(_health.Initialize(), Is.True);
            _targetObject.AddComponent<SphereCollider>();

            if (_addChildCollider)
            {
                GameObject _childObject = CreateObject(
                    $"{_name}ChildCollider",
                    _position + new Vector3(0.1f, 0.0f, 0.0f));
                _childObject.transform.SetParent(_targetObject.transform, true);
                _childObject.AddComponent<BoxCollider>();
            }

            return _health;
        }

        /// <summary>
        /// 创建并登记需要在 TearDown 中销毁的场景对象。
        /// </summary>
        /// <param name="_name">场景对象名称。</param>
        /// <param name="_position">场景对象的世界空间位置。</param>
        /// <returns>已经写入位置并登记所有权的 GameObject。</returns>
        private GameObject CreateObject(string _name, Vector3 _position)
        {
            GameObject _gameObject = new GameObject(_name);
            _gameObject.transform.position = _position;
            mCreatedObjects.Add(_gameObject);
            return _gameObject;
        }
    }
}
