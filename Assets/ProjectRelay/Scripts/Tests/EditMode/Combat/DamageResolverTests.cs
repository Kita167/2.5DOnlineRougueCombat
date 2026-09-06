using NUnit.Framework;
using ProjectRelay.Core;

namespace ProjectRelay.Tests.EditMode.Combat
{
    /// <summary>
    /// 验证纯伤害规则对正常、过量、零值和非法上下文产生确定性结果。
    /// </summary>
    public sealed class DamageResolverTests
    {
        private static readonly CombatantId mSourceId = new CombatantId(1UL);
        private static readonly CombatantId mTargetId = new CombatantId(2UL);
        private static readonly StableId mAttackId = new StableId("basic-attack-test");

        /// <summary>
        /// 验证普通伤害按请求值扣除生命且不会误判死亡。
        /// </summary>
        [Test]
        public void Resolve_ValidDamage_SubtractsRequestedDamage()
        {
            DamageContext _context = CreateContext(25.0f);

            DamageResult _result = DamageResolver.Resolve(
                _context,
                mTargetId,
                Faction.Enemy,
                100.0f,
                false);

            Assert.That(_result.IsApplied, Is.True);
            Assert.That(_result.ActualDamage, Is.EqualTo(25.0f));
            Assert.That(_result.HealthBefore, Is.EqualTo(100.0f));
            Assert.That(_result.HealthAfter, Is.EqualTo(75.0f));
            Assert.That(_result.Killed, Is.False);
            Assert.That(_result.RejectionReason, Is.EqualTo(DamageRejectionReason.None));
        }

        /// <summary>
        /// 验证过量伤害只扣除剩余生命，并返回首次致死结果。
        /// </summary>
        [Test]
        public void Resolve_OverkillDamage_ClampsToRemainingHealth()
        {
            DamageContext _context = CreateContext(150.0f);

            DamageResult _result = DamageResolver.Resolve(
                _context,
                mTargetId,
                Faction.Enemy,
                40.0f,
                false);

            Assert.That(_result.IsApplied, Is.True);
            Assert.That(_result.RequestedDamage, Is.EqualTo(150.0f));
            Assert.That(_result.ActualDamage, Is.EqualTo(40.0f));
            Assert.That(_result.HealthAfter, Is.Zero);
            Assert.That(_result.Killed, Is.True);
        }

        /// <summary>
        /// 验证零伤害被明确拒绝且生命快照保持不变。
        /// </summary>
        [Test]
        public void Resolve_ZeroDamage_ReturnsNonPositiveDamage()
        {
            DamageContext _context = CreateContext(0.0f);

            DamageResult _result = DamageResolver.Resolve(
                _context,
                mTargetId,
                Faction.Enemy,
                100.0f,
                false);

            Assert.That(_result.IsApplied, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(DamageRejectionReason.NonPositiveDamage));
            Assert.That(_result.HealthBefore, Is.EqualTo(100.0f));
            Assert.That(_result.HealthAfter, Is.EqualTo(100.0f));
        }

        /// <summary>
        /// 验证请求目标与实际接收者不一致时不会产生伤害。
        /// </summary>
        [Test]
        public void Resolve_MismatchedTarget_ReturnsInvalidTarget()
        {
            DamageContext _context = CreateContext(25.0f);
            CombatantId _differentTargetId = new CombatantId(3UL);

            DamageResult _result = DamageResolver.Resolve(
                _context,
                _differentTargetId,
                Faction.Enemy,
                100.0f,
                false);

            Assert.That(_result.IsApplied, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(DamageRejectionReason.InvalidTarget));
            Assert.That(_result.ActualDamage, Is.Zero);
        }

        /// <summary>
        /// 验证已经死亡的目标返回明确拒绝原因且不会重复致死。
        /// </summary>
        [Test]
        public void Resolve_DeadTarget_ReturnsTargetAlreadyDead()
        {
            DamageContext _context = CreateContext(25.0f);

            DamageResult _result = DamageResolver.Resolve(
                _context,
                mTargetId,
                Faction.Enemy,
                0.0f,
                true);

            Assert.That(_result.IsApplied, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(DamageRejectionReason.TargetAlreadyDead));
            Assert.That(_result.Killed, Is.False);
        }

        /// <summary>
        /// 创建指向固定测试目标的基础物理伤害请求。
        /// </summary>
        /// <param name="_baseDamage">本次测试使用的基础伤害。</param>
        /// <returns>包含完整合法身份和阵营的请求。</returns>
        private static DamageContext CreateContext(float _baseDamage)
        {
            return new DamageContext(
                mSourceId,
                mTargetId,
                Faction.Player,
                Faction.Enemy,
                mAttackId,
                DamageType.Physical,
                _baseDamage);
        }
    }
}
