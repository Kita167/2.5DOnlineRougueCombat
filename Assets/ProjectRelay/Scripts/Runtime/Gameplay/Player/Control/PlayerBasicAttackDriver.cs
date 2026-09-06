using System;
using ProjectRelay.Gameplay.Combat;
using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 在 Player 控制状态机和普通攻击命令边界之间提交请求并暴露只读攻击运行时。
    /// 本类不决定控制状态转移，也不执行命中或伤害计算。
    /// </summary>
    public sealed class PlayerBasicAttackDriver
    {
        private readonly BasicAttackController mAttackController;
        private readonly ICombatCommandGateway mCombatCommandGateway;

        private ulong mNextRequestSequence;

        /// <summary>获取 Driver 是否绑定了已经就绪的攻击执行链。</summary>
        public bool IsReady =>
            mAttackController != null &&
            mAttackController.IsInitialized &&
            mCombatCommandGateway != null &&
            mCombatCommandGateway.IsReady;

        /// <summary>获取当前普通攻击使用的只读配置。</summary>
        public BasicAttackConfig Config => mAttackController.Config;

        /// <summary>获取普通攻击是否仍占用角色互斥控制状态。</summary>
        public bool IsAttackInProgress => mAttackController.IsAttackInProgress;

        /// <summary>获取攻击执行器当前锁定的世界空间平面方向。</summary>
        public Vector3 LockedAttackDirection =>
            mAttackController.LockedAttackDirection;

        /// <summary>获取最近一次提交到 Gateway 的命令结果。</summary>
        public CombatCommandResult LastCommandResult { get; private set; }

        /// <summary>
        /// 使用已经初始化的攻击执行器和 Gateway 创建 Player 普通攻击桥接器。
        /// </summary>
        /// <param name="_attackController">管理攻击阶段、命中和冷却的执行器。</param>
        /// <param name="_combatCommandGateway">校验并提交普通攻击请求的权威边界。</param>
        /// <exception cref="ArgumentNullException">任一依赖为空时抛出。</exception>
        public PlayerBasicAttackDriver(
            BasicAttackController _attackController,
            ICombatCommandGateway _combatCommandGateway)
        {
            mAttackController =
                _attackController ?? throw new ArgumentNullException(nameof(_attackController));
            mCombatCommandGateway =
                _combatCommandGateway ??
                throw new ArgumentNullException(nameof(_combatCommandGateway));
        }

        /// <summary>
        /// 构造带单调序号的普通攻击请求，并同步提交给权威 Gateway。
        /// </summary>
        /// <param name="_attackDirection">本次攻击需要锁定的世界空间平面方向。</param>
        /// <returns>Gateway 接受并启动攻击时返回 true。</returns>
        public bool TryStartAttack(Vector3 _attackDirection)
        {
            if (!IsReady || Config == null)
            {
                return false;
            }

            BasicAttackRequest _request = new BasicAttackRequest(
                mAttackController.SourceId,
                Config.AttackId,
                _attackDirection,
                CreateNextRequestSequence());
            LastCommandResult =
                mCombatCommandGateway.SubmitBasicAttack(_request);
            return LastCommandResult.IsAccepted;
        }

        /// <summary>
        /// 推进普通攻击的阶段、命中窗口和冷却计时。
        /// </summary>
        /// <param name="_deltaTime">状态机已经过滤为有限非负值的帧时间。</param>
        public void Tick(float _deltaTime)
        {
            mAttackController.Tick(_deltaTime);
        }

        /// <summary>
        /// 中断当前攻击并清除攻击冷却，不重置请求序号生命周期。
        /// </summary>
        public void ForceReset()
        {
            mAttackController.ForceReset();
        }

        /// <summary>
        /// 生成当前 Driver 生命周期内单调递增的非零请求序号。
        /// </summary>
        /// <returns>本次提交独占的非零序号。</returns>
        private ulong CreateNextRequestSequence()
        {
            unchecked
            {
                mNextRequestSequence++;

                if (mNextRequestSequence == 0UL)
                {
                    mNextRequestSequence++;
                }
            }

            return mNextRequestSequence;
        }
    }
}
