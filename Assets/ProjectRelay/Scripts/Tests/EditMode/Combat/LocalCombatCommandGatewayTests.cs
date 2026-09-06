using NUnit.Framework;
using ProjectRelay.Core;
using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Combat
{
    /// <summary>
    /// 验证本地战斗命令入口对来源、攻击定义、序号和动作状态执行权威校验。
    /// </summary>
    public sealed class LocalCombatCommandGatewayTests
    {
        private GameObject mPlayerObject;
        private PlayerMovementConfig mMovementConfig;
        private BasicAttackDefinition mDefinition;
        private PlayerActionStateMachine mStateMachine;
        private CombatantIdentity mIdentity;
        private BasicAttackController mAttackController;
        private LocalCombatCommandGateway mGateway;

        /// <summary>
        /// 为每个测试创建完整但不依赖场景资产的本地攻击命令执行链。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mMovementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            mDefinition = ScriptableObject.CreateInstance<BasicAttackDefinition>();
            mStateMachine = new PlayerActionStateMachine(mMovementConfig);
            mStateMachine.SetEnabled(true);

            mPlayerObject = new GameObject("LocalCombatGatewayTestPlayer");
            mIdentity = mPlayerObject.AddComponent<CombatantIdentity>();
            mIdentity.Initialize(new CombatantId(1000UL), Faction.Player);
            mAttackController = mPlayerObject.AddComponent<BasicAttackController>();
            Assert.That(
                mAttackController.Initialize(mStateMachine, mDefinition),
                Is.True);
            mGateway = mPlayerObject.AddComponent<LocalCombatCommandGateway>();
            Assert.That(mGateway.Initialize(mAttackController), Is.True);
        }

        /// <summary>
        /// 销毁测试创建的对象和配置，避免命令序号与组件事件跨测试残留。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (mPlayerObject != null)
            {
                Object.DestroyImmediate(mPlayerObject);
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
        /// 验证合法命令被接受并启动 Windup，相同序号不能被再次接受。
        /// </summary>
        [Test]
        public void SubmitBasicAttack_ValidThenDuplicate_AcceptsOnce()
        {
            BasicAttackRequest _request = CreateRequest(1UL);

            CombatCommandResult _accepted = mGateway.SubmitBasicAttack(_request);
            CombatCommandResult _duplicate = mGateway.SubmitBasicAttack(_request);

            Assert.That(_accepted.WasProcessed, Is.True);
            Assert.That(_accepted.IsAccepted, Is.True);
            Assert.That(_accepted.RejectionReason, Is.EqualTo(CombatCommandRejectionReason.None));
            Assert.That(mAttackController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Windup));
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
                mDefinition.AttackId,
                Vector3.forward,
                1UL);

            CombatCommandResult _result = mGateway.SubmitBasicAttack(_request);

            Assert.That(_result.IsAccepted, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(CombatCommandRejectionReason.InvalidSource));
            Assert.That(mAttackController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
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

            CombatCommandResult _result = mGateway.SubmitBasicAttack(_request);

            Assert.That(_result.IsAccepted, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(CombatCommandRejectionReason.InvalidAttack));
            Assert.That(mAttackController.CurrentPhase, Is.EqualTo(BasicAttackPhase.Idle));
        }

        /// <summary>
        /// 验证 Dash 已占用动作状态时合法请求仍会被明确拒绝且不打断 Dash。
        /// </summary>
        [Test]
        public void SubmitBasicAttack_WhileDashing_ReturnsActionNotAllowed()
        {
            mStateMachine.TryDash(
                Vector3.forward,
                Vector3.forward,
                true,
                0.0f);

            CombatCommandResult _result =
                mGateway.SubmitBasicAttack(CreateRequest(1UL));

            Assert.That(_result.IsAccepted, Is.False);
            Assert.That(
                _result.RejectionReason,
                Is.EqualTo(CombatCommandRejectionReason.ActionNotAllowed));
            Assert.That(mStateMachine.CurrentState, Is.EqualTo(PlayerActionState.Dashing));
        }

        /// <summary>
        /// 创建使用当前玩家身份和普通攻击定义的合法请求。
        /// </summary>
        /// <param name="_sequence">需要写入请求的本地序号。</param>
        /// <returns>除调用方指定序号外其余字段全部合法的请求。</returns>
        private BasicAttackRequest CreateRequest(ulong _sequence)
        {
            return new BasicAttackRequest(
                mIdentity.Id,
                mDefinition.AttackId,
                Vector3.forward,
                _sequence);
        }
    }
}
