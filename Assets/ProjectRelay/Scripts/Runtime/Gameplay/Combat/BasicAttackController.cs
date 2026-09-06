using System;
using System.Collections.Generic;
using ProjectRelay.Core;
using ProjectRelay.Gameplay.Player;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 管理普通攻击阶段、锁定方向，并在进入 Active 时执行一次权威近战命中结算。
    /// 本组件通过 PlayerActionStateMachine 申请动作锁，但不读取输入，也不播放动画或 VFX。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatantIdentity))]
    public sealed class BasicAttackController : MonoBehaviour
    {
        private const int mMaximumPhaseTransitionsPerTick = 8;
        private const float mMinimumDirectionSqrMagnitude = 0.0001f;

        [SerializeField]
        [Tooltip("当前玩家普通攻击使用的只读设计参数。")]
        private BasicAttackDefinition mDefinition;

        [SerializeField]
        [Tooltip("提供攻击者运行时身份和阵营的同对象组件。")]
        private CombatantIdentity mCombatantIdentity;

        [SerializeField]
        [Tooltip("计算近战查询中心的起点；为空时使用本组件 Transform。")]
        private Transform mAttackOrigin;

        private PlayerActionStateMachine mActionStateMachine;
        private MeleeHitQuery mMeleeHitQuery;
        private HashSet<Health> mHitTargets;
        private BasicAttackPhase mCurrentPhase = BasicAttackPhase.Idle;
        private Vector3 mLockedAttackDirection;
        private float mPhaseTimeRemaining;
        private bool mIsInitialized;
        private bool mHasWarnedHitBufferCapacity;

        /// <summary>获取当前使用的只读普通攻击定义。</summary>
        public BasicAttackDefinition Definition => mDefinition;

        /// <summary>获取执行普通攻击的战斗单位运行时身份。</summary>
        public CombatantId SourceId =>
            mCombatantIdentity != null ? mCombatantIdentity.Id : CombatantId.None;

        /// <summary>获取执行普通攻击的战斗单位阵营。</summary>
        public Faction SourceFaction =>
            mCombatantIdentity != null ? mCombatantIdentity.Faction : Faction.None;

        /// <summary>获取当前普通攻击运行时阶段。</summary>
        public BasicAttackPhase CurrentPhase => mCurrentPhase;

        /// <summary>获取当前阶段尚未经过的时间，结果始终非负。</summary>
        public float PhaseTimeRemaining => mPhaseTimeRemaining;

        /// <summary>获取攻击开始时锁定并归一化的世界空间平面方向。</summary>
        public Vector3 LockedAttackDirection => mLockedAttackDirection;

        /// <summary>获取控制器是否已经绑定有效定义和玩家动作状态机。</summary>
        public bool IsInitialized => mIsInitialized;

        /// <summary>获取当前是否仍处于占用 Attacking 动作锁的三个攻击阶段。</summary>
        public bool IsAttackInProgress =>
            mCurrentPhase == BasicAttackPhase.Windup ||
            mCurrentPhase == BasicAttackPhase.Active ||
            mCurrentPhase == BasicAttackPhase.Recovery;

        /// <summary>获取最近一次 Active 查询返回的原始 Collider 候选数量。</summary>
        public int LastCandidateCount { get; private set; }

        /// <summary>获取最近一次攻击成功应用伤害的唯一 Health 数量。</summary>
        public int LastAppliedHitCount { get; private set; }

        /// <summary>
        /// 在阶段状态实际改变后发布旧阶段、新阶段和本次攻击定义 ID。
        /// </summary>
        public event Action<BasicAttackPhase, BasicAttackPhase, StableId> PhaseChanged;

        /// <summary>
        /// 在目标 Health 已经应用伤害后发布不可变结果，供表现和只读观察者消费。
        /// </summary>
        public event Action<DamageResult> DamageConfirmed;

        /// <summary>
        /// 组件禁用或场景退出时取消攻击、冷却和动作锁，防止重新启用后继承旧状态。
        /// </summary>
        private void OnDisable()
        {
            ForceReset();
        }

        /// <summary>
        /// 使用 Inspector 中的攻击定义绑定玩家动作状态机并建立空闲状态。
        /// </summary>
        /// <param name="_actionStateMachine">拥有玩家互斥动作状态的状态机。</param>
        /// <returns>状态机和 Inspector 定义有效时返回 true。</returns>
        public bool Initialize(PlayerActionStateMachine _actionStateMachine)
        {
            return Initialize(_actionStateMachine, mDefinition);
        }

        /// <summary>
        /// 显式绑定玩家动作状态机和普通攻击定义，供组合根及自动测试装配。
        /// 重复初始化会先安全清理上一轮攻击运行时状态。
        /// </summary>
        /// <param name="_actionStateMachine">拥有玩家互斥动作状态的状态机。</param>
        /// <param name="_definition">运行期间只读的普通攻击设计参数。</param>
        /// <returns>依赖和定义全部有效时返回 true。</returns>
        public bool Initialize(
            PlayerActionStateMachine _actionStateMachine,
            BasicAttackDefinition _definition)
        {
            ForceReset();
            mIsInitialized = false;
            mActionStateMachine = null;
            mMeleeHitQuery = null;
            mHitTargets = null;

            if (_actionStateMachine == null)
            {
                Debug.LogError(
                    "[Combat] BasicAttackController 初始化失败：动作状态机为空。",
                    this);
                return false;
            }

            if (_definition == null || !_definition.IsValid)
            {
                Debug.LogError(
                    "[Combat] BasicAttackController 初始化失败：普通攻击定义为空或包含非法配置。",
                    this);
                return false;
            }

            if (mCombatantIdentity == null)
            {
                mCombatantIdentity = GetComponent<CombatantIdentity>();
            }

            if (
                mCombatantIdentity == null ||
                !mCombatantIdentity.Id.IsValid ||
                mCombatantIdentity.Faction == Faction.None)
            {
                Debug.LogError(
                    "[Combat] BasicAttackController 初始化失败：攻击者身份或阵营无效。",
                    this);
                return false;
            }

            if (mAttackOrigin == null)
            {
                mAttackOrigin = transform;
            }

            mActionStateMachine = _actionStateMachine;
            mDefinition = _definition;
            mMeleeHitQuery = new MeleeHitQuery(mDefinition.HitBufferCapacity);
            mHitTargets = new HashSet<Health>(mDefinition.HitBufferCapacity);
            mHasWarnedHitBufferCapacity = false;
            mIsInitialized = true;
            return true;
        }

        /// <summary>
        /// 在 Free 且无冷却时锁定攻击方向、申请 Attacking 动作锁并进入 Windup。
        /// 拒绝不会改变阶段、计时、方向或玩家动作状态。
        /// </summary>
        /// <param name="_attackDirection">攻击开始时使用的世界空间方向。</param>
        /// <returns>请求被接受并启动一次攻击时返回 true。</returns>
        public bool TryStartAttack(Vector3 _attackDirection)
        {
            if (
                !mIsInitialized ||
                !isActiveAndEnabled ||
                mDefinition == null ||
                !mDefinition.IsValid ||
                mCurrentPhase != BasicAttackPhase.Idle ||
                mActionStateMachine == null ||
                !mActionStateMachine.CurrentConstraints.CanAttack)
            {
                return false;
            }

            Vector3 _safeAttackDirection = GetNormalizedPlanarDirection(_attackDirection);

            if (_safeAttackDirection == Vector3.zero)
            {
                return false;
            }

            if (
                !mActionStateMachine.TryEnterAttacking(
                    mDefinition.MovementSpeedMultiplier,
                    _safeAttackDirection))
            {
                return false;
            }

            mHitTargets.Clear();
            LastCandidateCount = 0;
            LastAppliedHitCount = 0;
            mLockedAttackDirection = _safeAttackDirection;
            EnterPhase(BasicAttackPhase.Windup, mDefinition.WindupDuration);
            AdvancePhases(0.0f);
            return true;
        }

        /// <summary>
        /// 推进攻击阶段并消费能够跨越多个阶段的时间增量，避免低帧率跳过 Active。
        /// </summary>
        /// <param name="_deltaTime">当前玩法帧使用的时间增量；非法或负值按零处理。</param>
        public void Tick(float _deltaTime)
        {
            if (!mIsInitialized || mCurrentPhase == BasicAttackPhase.Idle)
            {
                return;
            }

            if (
                mActionStateMachine == null ||
                mActionStateMachine.CurrentState == PlayerActionState.Disabled ||
                (IsAttackInProgress &&
                    mActionStateMachine.CurrentState != PlayerActionState.Attacking))
            {
                ForceReset();
                return;
            }

            float _safeDeltaTime =
                float.IsNaN(_deltaTime) || float.IsInfinity(_deltaTime)
                    ? 0.0f
                    : Mathf.Max(0.0f, _deltaTime);
            AdvancePhases(_safeDeltaTime);
        }

        /// <summary>
        /// 取消当前攻击和冷却，释放 Attacking 动作锁并回到 Idle。
        /// 该操作可在未初始化、重复禁用和场景清理路径中安全调用。
        /// </summary>
        public void ForceReset()
        {
            if (mActionStateMachine != null)
            {
                mActionStateMachine.InterruptAttacking();
            }

            mLockedAttackDirection = Vector3.zero;
            mPhaseTimeRemaining = 0.0f;
            LastCandidateCount = 0;
            LastAppliedHitCount = 0;

            if (mHitTargets != null)
            {
                mHitTargets.Clear();
            }

            ChangePhase(BasicAttackPhase.Idle);
        }

        /// <summary>
        /// 使用剩余时间循环推进阶段；零时长阶段也会立即向前移动但受到固定次数保护。
        /// </summary>
        /// <param name="_deltaTime">已经验证的非负时间增量。</param>
        private void AdvancePhases(float _deltaTime)
        {
            float _remainingDeltaTime = _deltaTime;
            int _transitionCount = 0;

            while (
                mCurrentPhase != BasicAttackPhase.Idle &&
                _transitionCount < mMaximumPhaseTransitionsPerTick)
            {
                if (mPhaseTimeRemaining > Mathf.Epsilon)
                {
                    if (_remainingDeltaTime <= 0.0f)
                    {
                        break;
                    }

                    float _consumedTime =
                        Mathf.Min(_remainingDeltaTime, mPhaseTimeRemaining);
                    mPhaseTimeRemaining -= _consumedTime;
                    _remainingDeltaTime -= _consumedTime;

                    if (mPhaseTimeRemaining > Mathf.Epsilon)
                    {
                        break;
                    }

                    mPhaseTimeRemaining = 0.0f;
                }

                EnterNextPhase();
                _transitionCount++;
            }

            if (
                _transitionCount >= mMaximumPhaseTransitionsPerTick &&
                mCurrentPhase != BasicAttackPhase.Idle)
            {
                Debug.LogError(
                    "[Combat] BasicAttackController 阶段推进超过安全上限，已强制重置。",
                    this);
                ForceReset();
            }
        }

        /// <summary>
        /// 根据当前阶段进入唯一合法的下一阶段，并在 Recovery 结束时释放动作锁。
        /// </summary>
        private void EnterNextPhase()
        {
            switch (mCurrentPhase)
            {
                case BasicAttackPhase.Windup:
                    EnterPhase(BasicAttackPhase.Active, mDefinition.ActiveDuration);
                    break;

                case BasicAttackPhase.Active:
                    EnterPhase(BasicAttackPhase.Recovery, mDefinition.RecoveryDuration);
                    break;

                case BasicAttackPhase.Recovery:
                    mActionStateMachine.CompleteAttacking();
                    mLockedAttackDirection = Vector3.zero;

                    if (mHitTargets != null)
                    {
                        mHitTargets.Clear();
                    }

                    EnterPhase(BasicAttackPhase.Cooldown, mDefinition.CooldownDuration);
                    break;

                case BasicAttackPhase.Cooldown:
                    EnterPhase(BasicAttackPhase.Idle, 0.0f);
                    break;

                default:
                    ForceReset();
                    break;
            }
        }

        /// <summary>
        /// 写入新阶段的非负剩余时间，然后向只读观察者发布阶段变化。
        /// </summary>
        /// <param name="_newPhase">需要进入的攻击阶段。</param>
        /// <param name="_duration">该阶段配置的持续时间。</param>
        private void EnterPhase(BasicAttackPhase _newPhase, float _duration)
        {
            mPhaseTimeRemaining = Mathf.Max(0.0f, _duration);
            ChangePhase(_newPhase);

            if (
                _newPhase == BasicAttackPhase.Active &&
                mCurrentPhase == BasicAttackPhase.Active &&
                isActiveAndEnabled)
            {
                ResolveActiveHits();
            }
        }

        /// <summary>
        /// 只在阶段确实变化时写入状态并发布包含稳定攻击 ID 的通知。
        /// </summary>
        /// <param name="_newPhase">需要写入的新阶段。</param>
        private void ChangePhase(BasicAttackPhase _newPhase)
        {
            if (mCurrentPhase == _newPhase)
            {
                return;
            }

            BasicAttackPhase _previousPhase = mCurrentPhase;
            mCurrentPhase = _newPhase;
            StableId _attackId =
                mDefinition != null ? mDefinition.AttackId : StableId.None;
            PhaseChanged?.Invoke(_previousPhase, _newPhase, _attackId);
        }

        /// <summary>
        /// 在 Active 入口执行唯一一次球形候选查询、过滤并向目标 Health 提交伤害。
        /// </summary>
        private void ResolveActiveHits()
        {
            if (
                mMeleeHitQuery == null ||
                mHitTargets == null ||
                mDefinition == null ||
                mCombatantIdentity == null)
            {
                return;
            }

            Transform _origin = mAttackOrigin != null ? mAttackOrigin : transform;
            Vector3 _queryCenter =
                _origin.position +
                mLockedAttackDirection * mDefinition.ForwardOffset;
            int _candidateCount = mMeleeHitQuery.Query(
                _queryCenter,
                mDefinition.HitRadius,
                mDefinition.TargetLayerMask);
            LastCandidateCount = _candidateCount;

            if (
                _candidateCount >= mMeleeHitQuery.Capacity &&
                !mHasWarnedHitBufferCapacity)
            {
                mHasWarnedHitBufferCapacity = true;
                Debug.LogWarning(
                    $"[Combat] 普通攻击 {mDefinition.AttackId} 的近战查询已填满 " +
                    $"{mMeleeHitQuery.Capacity} 个候选，请检查 LayerMask 或提高缓冲容量。",
                    this);
            }

            for (int _index = 0; _index < _candidateCount; _index++)
            {
                Collider _candidate = mMeleeHitQuery.GetCandidate(_index);

                if (
                    _candidate == null ||
                    _candidate.transform.IsChildOf(mCombatantIdentity.transform))
                {
                    continue;
                }

                Health _targetHealth = _candidate.GetComponentInParent<Health>();

                if (
                    _targetHealth == null ||
                    !_targetHealth.isActiveAndEnabled ||
                    _targetHealth.IsDead)
                {
                    continue;
                }

                CombatantIdentity _targetIdentity = _targetHealth.Identity;

                if (
                    _targetIdentity == null ||
                    !_targetIdentity.Id.IsValid ||
                    _targetIdentity.Id == SourceId ||
                    _targetIdentity.Faction == Faction.None ||
                    _targetIdentity.Faction == SourceFaction ||
                    !mHitTargets.Add(_targetHealth))
                {
                    continue;
                }

                DamageContext _damageContext = new DamageContext(
                    SourceId,
                    _targetIdentity.Id,
                    SourceFaction,
                    _targetIdentity.Faction,
                    mDefinition.AttackId,
                    DamageType.Physical,
                    mDefinition.BaseDamage);

                if (
                    _targetHealth.TryApplyDamage(
                        _damageContext,
                        out DamageResult _damageResult))
                {
                    LastAppliedHitCount++;
                    DamageConfirmed?.Invoke(_damageResult);
                }
            }
        }

        /// <summary>
        /// 将任意方向投影到 XZ 平面，并在方向有效时返回单位向量。
        /// </summary>
        /// <param name="_direction">需要锁定的世界空间方向。</param>
        /// <returns>有效时返回 XZ 平面单位方向，否则返回零向量。</returns>
        private static Vector3 GetNormalizedPlanarDirection(Vector3 _direction)
        {
            _direction.y = 0.0f;

            if (
                float.IsNaN(_direction.x) ||
                float.IsInfinity(_direction.x) ||
                float.IsNaN(_direction.z) ||
                float.IsInfinity(_direction.z))
            {
                return Vector3.zero;
            }

            return _direction.sqrMagnitude > mMinimumDirectionSqrMagnitude
                ? _direction.normalized
                : Vector3.zero;
        }
    }
}
