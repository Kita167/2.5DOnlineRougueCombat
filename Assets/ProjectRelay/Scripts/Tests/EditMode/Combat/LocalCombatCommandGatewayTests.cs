using NUnit.Framework;
using ProjectRelay.Core;
using ProjectRelay.Gameplay.Combat;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Combat
{
    /// <summary>
    /// 验证本地战斗命令入口对来源、攻击配置、序号和执行器状态进行权威校验。
    /// </summary>
    public sealed class LocalCombatCommandGatewayTests
    {
        private GameObject mPlayerObject;
        private BasicAttackConfig mConfig;
        private CombatantIdentity mIdentity;
        private BasicAttackController mAttackController;
        private LocalCombatCommandGateway mGateway;

        /// <summary>
        /// 为每个测试创建不依赖 Player 控制状态机的本地攻击命令执行链。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mConfig = ScriptableObject.CreateInstance<BasicAttackConfig>();
            mPlayerObject = new GameObject("LocalCombatGatewayTestPlayer");
            mIdentity = mPlayerObject.AddComponent<CombatantIdentity>();
            mIdentity.Initialize(new CombatantId(1000UL), Faction.Player);
            mAttackController =
                mPlayerObject.AddComponent<BasicAttackController>();
            Assert.That(mAttackController.Initialize(mConfig), Is.True);
            mGateway =
                mPlayerObject.AddComponent<LocalCombatCommandGateway>();
            Assert.That(mGateway.Initialize(mAttackController), Is.True);
        }

        /// <summary>
        /// 销毁测试创建的对象和配置，避免命令序号跨测试残留。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (mPlayerObject != null)
            {
                Object.DestroyImmediate(mPlayerObject);
            }

            if (mConfig != null)
            {
                Object.DestroyImmediate(mConfig);
            }
        }

        /// <summary>
        /// 验证合法命令被接受并启动 Windup，相同序号不能被再次接受。
        /// </summary>
        [Test]
        public void SubmitBasicAttack_ValidThenDuplicate_AcceptsOnce()
        {
            BasicAttackRequest _request = CreateRequest(1UL);

            CombatCommandResult _accepted =
                mGateway.SubmitBasicAttack(_request);
            CombatCommandResult _duplicate =
                mGateway.SubmitBasicAttack(_request);

            Assert.That(_accepted.WasProcessed, Is.True);
            Assert.That(_accepted.IsAccepted, Is.True);
            Assert.That(
                _accepted.RejectionReason,
                Is.EqualTo(CombatCommandRejectionReason.None));
            Assert.That(
                mAttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Windup));
            Assert.That(_duplicate.IsAccepted, Is.False);
            Assert.That(
                _duplicate.RejectionReason,
                Is.EqualTo(CombatCommandRejectionReason.InvalidSequence));
        }

        /// <summary>
        /// 验证来源身份与 Gateway 绑定玩家不一致时不会启动攻击。
        /// </summary>
        [Test]
        public void SubmitBasicAttack_WrongSource_ReturnsInvalidSource()
        {
            BasicAttackRequest _request = new BasicAttackRequest(
                new CombatantId(9999UL),
                mConfig.AttackId,
                Vector3.forward,
                1UL);

            CombatCommandResult _result =
                mGateway.SubmitBasicAttack(_request);

            Assert.That(_result.IsAccepted, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(CombatCommandRejectionReason.InvalidSource));
            Assert.That(
                mAttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
        }

        /// <summary>
        /// 验证请求攻击标识与执行器配置不一致时不会启动攻击。
        /// </summary>
        [Test]
        public void SubmitBasicAttack_WrongAttackId_ReturnsInvalidAttack()
        {
            BasicAttackRequest _request = new BasicAttackRequest(
                mIdentity.Id,
                new StableId("different-attack"),
                Vector3.forward,
                1UL);

            CombatCommandResult _result =
                mGateway.SubmitBasicAttack(_request);

            Assert.That(_result.IsAccepted, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(CombatCommandRejectionReason.InvalidAttack));
            Assert.That(
                mAttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Idle));
        }

        /// <summary>
        /// 验证执行器已有攻击进行时，较新的合法命令会返回 ActionNotAllowed。
        /// </summary>
        [Test]
        public void SubmitBasicAttack_WhileAttackInProgress_ReturnsActionNotAllowed()
        {
            CombatCommandResult _first =
                mGateway.SubmitBasicAttack(CreateRequest(1UL));

            CombatCommandResult _second =
                mGateway.SubmitBasicAttack(CreateRequest(2UL));

            Assert.That(_first.IsAccepted, Is.True);
            Assert.That(_second.IsAccepted, Is.False);
            Assert.That(
                _second.RejectionReason,
                Is.EqualTo(CombatCommandRejectionReason.ActionNotAllowed));
            Assert.That(
                mAttackController.CurrentPhase,
                Is.EqualTo(BasicAttackPhase.Windup));
        }

        /// <summary>
        /// 创建使用当前玩家身份和普通攻击配置的合法请求。
        /// </summary>
        /// <param name="_sequence">需要写入请求的本地序号。</param>
        /// <returns>除调用方指定序号外其余字段全部合法的请求。</returns>
        private BasicAttackRequest CreateRequest(ulong _sequence)
        {
            return new BasicAttackRequest(
                mIdentity.Id,
                mConfig.AttackId,
                Vector3.forward,
                _sequence);
        }
    }
}
