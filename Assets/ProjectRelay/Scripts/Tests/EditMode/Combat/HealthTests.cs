using NUnit.Framework;
using ProjectRelay.Core;
using ProjectRelay.Gameplay.Combat;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Combat
{
    /// <summary>
    /// 验证 Health 是生命唯一写入口，并保证事件顺序和单生命周期死亡唯一性。
    /// </summary>
    public sealed class HealthTests
    {
        private static readonly CombatantId mSourceId = new CombatantId(100UL);
        private static readonly StableId mAttackId = new StableId("health-test-attack");

        private GameObject mTargetObject;
        private CombatantIdentity mIdentity;
        private Health mHealth;

        /// <summary>
        /// 为每个测试创建拥有完整身份和生命组件的独立目标。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mTargetObject = new GameObject("HealthTestTarget");
            mIdentity = mTargetObject.AddComponent<CombatantIdentity>();
            mIdentity.Initialize(new CombatantId(200UL), Faction.Enemy);
            mHealth = mTargetObject.AddComponent<Health>();
            Assert.That(mHealth.Initialize(), Is.True);
        }

        /// <summary>
        /// 销毁测试目标，避免组件和事件跨测试残留。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (mTargetObject != null)
            {
                Object.DestroyImmediate(mTargetObject);
            }
        }

        /// <summary>
        /// 验证致死伤害先更新状态，再依次发布一次 Damaged 和 Died。
        /// </summary>
        [Test]
        public void TryApplyDamage_LethalDamage_UpdatesStateAndDiesOnce()
        {
            int _damagedCount = 0;
            int _diedCount = 0;

            mHealth.Damaged += _result =>
            {
                _damagedCount++;
                Assert.That(mHealth.CurrentHealth, Is.EqualTo(_result.HealthAfter));
                Assert.That(mHealth.IsDead, Is.True);
            };
            mHealth.Died += _result =>
            {
                _diedCount++;
                Assert.That(_result.Killed, Is.True);
                Assert.That(mHealth.IsDead, Is.True);
            };

            DamageContext _lethalContext = CreateContext(150.0f);

            bool _wasApplied = mHealth.TryApplyDamage(_lethalContext, out DamageResult _result);

            Assert.That(_wasApplied, Is.True);
            Assert.That(_result.ActualDamage, Is.EqualTo(mHealth.MaximumHealth));
            Assert.That(mHealth.CurrentHealth, Is.Zero);
            Assert.That(mHealth.IsDead, Is.True);
            Assert.That(_damagedCount, Is.EqualTo(1));
            Assert.That(_diedCount, Is.EqualTo(1));

            bool _wasAppliedAfterDeath =
                mHealth.TryApplyDamage(CreateContext(10.0f), out DamageResult _rejectedResult);

            Assert.That(_wasAppliedAfterDeath, Is.False);
            Assert.That(
                _rejectedResult.RejectionReason,
                Is.EqualTo(DamageRejectionReason.TargetAlreadyDead));
            Assert.That(_damagedCount, Is.EqualTo(1));
            Assert.That(_diedCount, Is.EqualTo(1));
        }

        /// <summary>
        /// 验证显式重置开始新生命周期，并允许下一次死亡事件再次触发。
        /// </summary>
        [Test]
        public void ResetToFull_AfterDeath_StartsNewLifecycle()
        {
            int _diedCount = 0;
            mHealth.Died += _result => _diedCount++;

            mHealth.TryApplyDamage(CreateContext(150.0f), out DamageResult _firstResult);
            mHealth.ResetToFull();

            Assert.That(mHealth.CurrentHealth, Is.EqualTo(mHealth.MaximumHealth));
            Assert.That(mHealth.IsDead, Is.False);

            mHealth.TryApplyDamage(CreateContext(150.0f), out DamageResult _secondResult);

            Assert.That(_firstResult.Killed, Is.True);
            Assert.That(_secondResult.Killed, Is.True);
            Assert.That(_diedCount, Is.EqualTo(2));
        }

        /// <summary>
        /// 验证错误目标身份不会修改生命或发布伤害事件。
        /// </summary>
        [Test]
        public void TryApplyDamage_WrongTarget_DoesNotMutateHealth()
        {
            int _damagedCount = 0;
            mHealth.Damaged += _result => _damagedCount++;

            DamageContext _wrongTargetContext = new DamageContext(
                mSourceId,
                new CombatantId(999UL),
                Faction.Player,
                Faction.Enemy,
                mAttackId,
                DamageType.Physical,
                25.0f);

            bool _wasApplied =
                mHealth.TryApplyDamage(_wrongTargetContext, out DamageResult _result);

            Assert.That(_wasApplied, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(DamageRejectionReason.InvalidTarget));
            Assert.That(mHealth.CurrentHealth, Is.EqualTo(mHealth.MaximumHealth));
            Assert.That(_damagedCount, Is.Zero);
        }

        /// <summary>
        /// 创建指向当前测试目标的基础物理伤害请求。
        /// </summary>
        /// <param name="_baseDamage">本次测试使用的基础伤害。</param>
        /// <returns>目标身份和阵营与 Health 所属对象一致的请求。</returns>
        private DamageContext CreateContext(float _baseDamage)
        {
            return new DamageContext(
                mSourceId,
                mIdentity.Id,
                Faction.Player,
                mIdentity.Faction,
                mAttackId,
                DamageType.Physical,
                _baseDamage);
        }
    }
}
