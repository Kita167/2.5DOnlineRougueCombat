using ProjectRelay.Core;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 保存普通攻击的稳定标识、伤害、阶段时间、范围和动作约束等只读设计参数。
    /// 运行时阶段、剩余时间、锁定方向和命中集合不得写回本资源。
    /// </summary>
    [CreateAssetMenu(
        fileName = "BasicAttack_Default",
        menuName = "Project Relay/Gameplay/Basic Attack Definition")]
    public sealed class BasicAttackDefinition : ScriptableObject
    {
        private const int mMinimumHitBufferCapacity = 1;
        private const int mMaximumHitBufferCapacity = 256;

        [Header("Identity and Damage")]
        [SerializeField]
        [Tooltip("跨资源、存档和网络保持不变的普通攻击标识。")]
        private string mAttackId = "basic-attack";

        [SerializeField]
        [Min(0.0001f)]
        [Tooltip("进入 Active 阶段时提交给伤害规则的基础物理伤害。")]
        private float mBaseDamage = 25.0f;

        [Header("Timing")]
        [SerializeField]
        [Min(0.0f)]
        [Tooltip("攻击开始到进入有效窗口前的时间，单位为秒。")]
        private float mWindupDuration = 0.15f;

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("攻击有效窗口保持的时间，单位为秒；命中查询只在进入阶段时执行一次。")]
        private float mActiveDuration = 0.10f;

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("有效窗口结束到释放 Attacking 动作锁之间的时间，单位为秒。")]
        private float mRecoveryDuration = 0.25f;

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("动作锁释放后再次允许普通攻击前的等待时间，单位为秒。")]
        private float mCooldownDuration = 0.40f;

        [Header("Melee Query")]
        [SerializeField]
        [Min(0.0f)]
        [Tooltip("命中查询中心沿锁定攻击方向前移的距离。")]
        private float mForwardOffset = 1.0f;

        [SerializeField]
        [Min(0.0001f)]
        [Tooltip("近战球形命中查询的半径。")]
        private float mHitRadius = 0.75f;

        [SerializeField]
        [Tooltip("允许进入近战候选集合的物理层。")]
        private LayerMask mTargetLayerMask = -1;

        [SerializeField]
        [Range(mMinimumHitBufferCapacity, mMaximumHitBufferCapacity)]
        [Tooltip("非分配近战查询可复用的 Collider 缓冲区容量。")]
        private int mHitBufferCapacity = 16;

        [Header("Action Constraints")]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("Windup、Active 和 Recovery 期间普通移动速度的倍率。")]
        private float mMovementSpeedMultiplier = 0.5f;

        /// <summary>获取攻击定义的跨资源稳定标识。</summary>
        public StableId AttackId => new StableId(mAttackId);

        /// <summary>获取进入伤害规则前的基础伤害。</summary>
        public float BaseDamage => mBaseDamage;

        /// <summary>获取攻击前摇时间。</summary>
        public float WindupDuration => mWindupDuration;

        /// <summary>获取攻击有效窗口时间。</summary>
        public float ActiveDuration => mActiveDuration;

        /// <summary>获取攻击后摇时间。</summary>
        public float RecoveryDuration => mRecoveryDuration;

        /// <summary>获取动作锁释放后的冷却时间。</summary>
        public float CooldownDuration => mCooldownDuration;

        /// <summary>获取命中查询中心沿攻击方向的前移距离。</summary>
        public float ForwardOffset => mForwardOffset;

        /// <summary>获取近战球形命中查询半径。</summary>
        public float HitRadius => mHitRadius;

        /// <summary>获取允许参与近战查询的物理层。</summary>
        public LayerMask TargetLayerMask => mTargetLayerMask;

        /// <summary>获取非分配命中查询使用的固定缓冲区容量。</summary>
        public int HitBufferCapacity => mHitBufferCapacity;

        /// <summary>获取攻击动作锁期间的普通移动速度倍率。</summary>
        public float MovementSpeedMultiplier => mMovementSpeedMultiplier;

        /// <summary>
        /// 获取全部必要设计值是否有限且处于可运行范围。
        /// </summary>
        public bool IsValid =>
            AttackId.IsValid &&
            IsFinitePositive(mBaseDamage) &&
            IsFiniteNonNegative(mWindupDuration) &&
            IsFiniteNonNegative(mActiveDuration) &&
            IsFiniteNonNegative(mRecoveryDuration) &&
            IsFiniteNonNegative(mCooldownDuration) &&
            IsFiniteNonNegative(mForwardOffset) &&
            IsFinitePositive(mHitRadius) &&
            IsFiniteNonNegative(mMovementSpeedMultiplier) &&
            mMovementSpeedMultiplier <= 1.0f &&
            mHitBufferCapacity >= mMinimumHitBufferCapacity &&
            mHitBufferCapacity <= mMaximumHitBufferCapacity;

        /// <summary>
        /// 在编辑器修改资源时将数值约束到可运行范围，避免非法配置进入阶段计算。
        /// </summary>
        private void OnValidate()
        {
            mBaseDamage = Mathf.Max(
                0.0001f,
                GetFiniteNonNegative(mBaseDamage, 25.0f));
            mWindupDuration = GetFiniteNonNegative(mWindupDuration, 0.15f);
            mActiveDuration = GetFiniteNonNegative(mActiveDuration, 0.10f);
            mRecoveryDuration = GetFiniteNonNegative(mRecoveryDuration, 0.25f);
            mCooldownDuration = GetFiniteNonNegative(mCooldownDuration, 0.40f);
            mForwardOffset = GetFiniteNonNegative(mForwardOffset, 1.0f);
            mHitRadius = Mathf.Max(0.0001f, GetFiniteNonNegative(mHitRadius, 0.75f));
            mMovementSpeedMultiplier = Mathf.Clamp01(
                GetFiniteNonNegative(mMovementSpeedMultiplier, 0.5f));
            mHitBufferCapacity = Mathf.Clamp(
                mHitBufferCapacity,
                mMinimumHitBufferCapacity,
                mMaximumHitBufferCapacity);
        }

        /// <summary>
        /// 检查浮点值是否为有限正数。
        /// </summary>
        /// <param name="_value">需要验证的设计值。</param>
        /// <returns>数值有限且大于零时返回 true。</returns>
        private static bool IsFinitePositive(float _value)
        {
            return IsFiniteNonNegative(_value) && _value > 0.0f;
        }

        /// <summary>
        /// 检查浮点值是否为有限非负数。
        /// </summary>
        /// <param name="_value">需要验证的设计值。</param>
        /// <returns>数值不是 NaN 或无穷且不小于零时返回 true。</returns>
        private static bool IsFiniteNonNegative(float _value)
        {
            return !float.IsNaN(_value) && !float.IsInfinity(_value) && _value >= 0.0f;
        }

        /// <summary>
        /// 将非法浮点值替换为安全默认值，并把有限负值约束为零。
        /// </summary>
        /// <param name="_value">Inspector 提供的原始值。</param>
        /// <param name="_fallback">原始值非有限时使用的默认值。</param>
        /// <returns>有限且非负的设计值。</returns>
        private static float GetFiniteNonNegative(float _value, float _fallback)
        {
            if (float.IsNaN(_value) || float.IsInfinity(_value))
            {
                return _fallback;
            }

            return Mathf.Max(0.0f, _value);
        }
    }
}
