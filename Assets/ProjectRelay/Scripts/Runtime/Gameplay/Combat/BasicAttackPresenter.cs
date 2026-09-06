using ProjectRelay.Core;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 将普通攻击阶段和已确认伤害转换为可选 Animator 参数与命中特效。
    /// 本组件只消费权威结果，移除或禁用后不会改变攻击时序、命中或生命状态。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BasicAttackPresenter : MonoBehaviour
    {
        private static readonly int mIsAttackingParameterId =
            Animator.StringToHash("IsAttacking");
        private static readonly int mAttackPhaseParameterId =
            Animator.StringToHash("AttackPhase");
        private static readonly int mAttackHitParameterId =
            Animator.StringToHash("AttackHit");

        [SerializeField]
        [Tooltip("发布攻击阶段和确认伤害的规则控制器。")]
        private BasicAttackController mAttackController;

        [SerializeField]
        [Tooltip("可选 Animator；存在对应参数时接收 IsAttacking、AttackPhase 和 AttackHit。")]
        private Animator mAnimator;

        [SerializeField]
        [Tooltip("每次确认至少一个目标受伤时播放的可选粒子系统。")]
        private ParticleSystem mConfirmedHitEffect;

        private bool mHasIsAttackingParameter;
        private bool mHasAttackPhaseParameter;
        private bool mHasAttackHitParameter;

        /// <summary>
        /// 缓存父级攻击控制器和子级 Animator，并在初始化阶段检查可选参数是否存在。
        /// </summary>
        private void Awake()
        {
            if (mAttackController == null)
            {
                mAttackController = GetComponentInParent<BasicAttackController>();
            }

            if (mAnimator == null)
            {
                mAnimator = GetComponentInChildren<Animator>(true);
            }

            CacheAnimatorParameters();

            if (mAttackController == null)
            {
                Debug.LogError(
                    "[Combat] BasicAttackPresenter 缺少 BasicAttackController 引用。",
                    this);
                enabled = false;
            }
        }

        /// <summary>
        /// 幂等订阅攻击阶段和确认伤害事件，并同步当前阶段到可选 Animator。
        /// </summary>
        private void OnEnable()
        {
            if (mAttackController == null)
            {
                return;
            }

            mAttackController.PhaseChanged -= HandlePhaseChanged;
            mAttackController.DamageConfirmed -= HandleDamageConfirmed;
            mAttackController.PhaseChanged += HandlePhaseChanged;
            mAttackController.DamageConfirmed += HandleDamageConfirmed;
            ApplyPhase(mAttackController.CurrentPhase);
        }

        /// <summary>
        /// 对称解除全部规则事件，并把可选 Animator 恢复为非攻击表现状态。
        /// </summary>
        private void OnDisable()
        {
            if (mAttackController != null)
            {
                mAttackController.PhaseChanged -= HandlePhaseChanged;
                mAttackController.DamageConfirmed -= HandleDamageConfirmed;
            }

            if (mAnimator == null)
            {
                return;
            }

            if (mHasIsAttackingParameter)
            {
                mAnimator.SetBool(mIsAttackingParameterId, false);
            }

            if (mHasAttackPhaseParameter)
            {
                mAnimator.SetInteger(
                    mAttackPhaseParameterId,
                    (int)BasicAttackPhase.Idle);
            }

            if (mHasAttackHitParameter)
            {
                mAnimator.ResetTrigger(mAttackHitParameterId);
            }
        }

        /// <summary>
        /// 将已确认的新攻击阶段写入可选 Animator 参数。
        /// </summary>
        /// <param name="_previousPhase">阶段变化前的状态。</param>
        /// <param name="_newPhase">阶段变化后的状态。</param>
        /// <param name="_attackId">产生阶段变化的稳定攻击定义标识。</param>
        private void HandlePhaseChanged(
            BasicAttackPhase _previousPhase,
            BasicAttackPhase _newPhase,
            StableId _attackId)
        {
            ApplyPhase(_newPhase);
        }

        /// <summary>
        /// 只响应已经由 Health 应用的伤害结果，触发可选动画参数和粒子反馈。
        /// </summary>
        /// <param name="_result">攻击执行器发布的已确认伤害结果。</param>
        private void HandleDamageConfirmed(DamageResult _result)
        {
            if (!_result.IsApplied)
            {
                return;
            }

            if (mAnimator != null && mHasAttackHitParameter)
            {
                mAnimator.SetTrigger(mAttackHitParameterId);
            }

            if (mConfirmedHitEffect != null)
            {
                mConfirmedHitEffect.Play(true);
            }
        }

        /// <summary>
        /// 根据阶段计算攻击占用状态，并更新当前 Animator 实际支持的参数。
        /// </summary>
        /// <param name="_phase">需要同步到表现层的当前权威攻击阶段。</param>
        private void ApplyPhase(BasicAttackPhase _phase)
        {
            if (mAnimator == null)
            {
                return;
            }

            bool _isAttacking =
                _phase == BasicAttackPhase.Windup ||
                _phase == BasicAttackPhase.Active ||
                _phase == BasicAttackPhase.Recovery;

            if (mHasIsAttackingParameter)
            {
                mAnimator.SetBool(mIsAttackingParameterId, _isAttacking);
            }

            if (mHasAttackPhaseParameter)
            {
                mAnimator.SetInteger(mAttackPhaseParameterId, (int)_phase);
            }
        }

        /// <summary>
        /// 在初始化时扫描一次 Animator 参数，避免缺失可选参数时产生运行期警告。
        /// </summary>
        private void CacheAnimatorParameters()
        {
            if (mAnimator == null)
            {
                return;
            }

            AnimatorControllerParameter[] _parameters = mAnimator.parameters;

            for (int _index = 0; _index < _parameters.Length; _index++)
            {
                AnimatorControllerParameter _parameter = _parameters[_index];

                if (
                    _parameter.nameHash == mIsAttackingParameterId &&
                    _parameter.type == AnimatorControllerParameterType.Bool)
                {
                    mHasIsAttackingParameter = true;
                }
                else if (
                    _parameter.nameHash == mAttackPhaseParameterId &&
                    _parameter.type == AnimatorControllerParameterType.Int)
                {
                    mHasAttackPhaseParameter = true;
                }
                else if (
                    _parameter.nameHash == mAttackHitParameterId &&
                    _parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    mHasAttackHitParameter = true;
                }
            }
        }
    }
}
