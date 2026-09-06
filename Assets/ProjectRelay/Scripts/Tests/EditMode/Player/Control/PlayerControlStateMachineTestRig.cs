using System;
using NUnit.Framework;
using ProjectRelay.Core;
using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Tests.EditMode.Player
{
    /// <summary>
    /// 为 Player 控制状态机测试装配独立配置、本地攻击命令链和可释放运行时。
    /// </summary>
    internal sealed class PlayerControlStateMachineTestRig : IDisposable
    {
        private readonly GameObject mPlayerObject;

        /// <summary>获取测试使用的移动配置。</summary>
        public PlayerMovementConfig MovementConfig { get; }

        /// <summary>获取测试使用的普通攻击配置。</summary>
        public BasicAttackConfig AttackConfig { get; }

        /// <summary>获取测试使用的普通攻击执行器。</summary>
        public BasicAttackController AttackController { get; }

        /// <summary>获取测试使用的 Player 攻击桥接器。</summary>
        public PlayerBasicAttackDriver AttackDriver { get; }

        /// <summary>获取已经启用的完整 Player 控制状态机。</summary>
        public PlayerControlStateMachine StateMachine { get; }

        /// <summary>
        /// 创建一套不依赖场景资源的完整本地 Player 控制运行时。
        /// </summary>
        public PlayerControlStateMachineTestRig()
        {
            MovementConfig =
                ScriptableObject.CreateInstance<PlayerMovementConfig>();
            AttackConfig =
                ScriptableObject.CreateInstance<BasicAttackConfig>();
            mPlayerObject = new GameObject("PlayerControlStateMachineTestRig");

            CombatantIdentity _identity =
                mPlayerObject.AddComponent<CombatantIdentity>();
            Assert.That(
                _identity.Initialize(new CombatantId(2000UL), Faction.Player),
                Is.True);

            AttackController =
                mPlayerObject.AddComponent<BasicAttackController>();
            Assert.That(AttackController.Initialize(AttackConfig), Is.True);

            LocalCombatCommandGateway _gateway =
                mPlayerObject.AddComponent<LocalCombatCommandGateway>();
            Assert.That(_gateway.Initialize(AttackController), Is.True);

            AttackDriver =
                new PlayerBasicAttackDriver(AttackController, _gateway);
            StateMachine =
                new PlayerControlStateMachine(MovementConfig, AttackDriver);
            StateMachine.SetEnabled(true);
        }

        /// <summary>
        /// 销毁本测试装配创建的对象和临时配置。
        /// </summary>
        public void Dispose()
        {
            if (mPlayerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(mPlayerObject);
            }

            if (MovementConfig != null)
            {
                UnityEngine.Object.DestroyImmediate(MovementConfig);
            }

            if (AttackConfig != null)
            {
                UnityEngine.Object.DestroyImmediate(AttackConfig);
            }
        }

        /// <summary>
        /// 创建使用指定移动、朝向和动作边沿的安全输入快照。
        /// </summary>
        /// <param name="_moveDirection">世界空间移动方向。</param>
        /// <param name="_facingDirection">角色当前世界空间朝向。</param>
        /// <param name="_isDashPressed">本帧是否按下 Dash。</param>
        /// <param name="_isAttackPressed">本帧是否按下 Attack。</param>
        /// <returns>可直接提交给状态机的输入。</returns>
        public static PlayerControlInput CreateInput(
            Vector3 _moveDirection,
            Vector3 _facingDirection,
            bool _isDashPressed = false,
            bool _isAttackPressed = false)
        {
            return new PlayerControlInput(
                _moveDirection,
                _facingDirection,
                _isDashPressed,
                _isAttackPressed);
        }
    }
}
