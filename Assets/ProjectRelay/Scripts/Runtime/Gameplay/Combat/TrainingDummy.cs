using ProjectRelay.Core;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 为开发场景组合战斗身份、生命读取、受伤日志和显式重置入口。
    /// 本组件不计算伤害，也不模拟敌人 AI 或正式死亡表现。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatantIdentity), typeof(Health))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("训练假人的战斗身份组件。")]
        private CombatantIdentity mCombatantIdentity;

        [SerializeField]
        [Tooltip("训练假人的生命状态组件。")]
        private Health mHealth;

        [SerializeField]
        [Tooltip("开启后在开发 Console 输出确认伤害和死亡结果。")]
        private bool mLogCombatEvents = true;

        /// <summary>
        /// 获取训练假人的运行时战斗身份。
        /// </summary>
        public CombatantId Id =>
            mCombatantIdentity != null ? mCombatantIdentity.Id : CombatantId.None;

        /// <summary>
        /// 获取训练假人最近一次确认后的生命值。
        /// </summary>
        public float CurrentHealth => mHealth != null ? mHealth.CurrentHealth : 0.0f;

        /// <summary>
        /// 获取训练假人在当前生命周期是否死亡。
        /// </summary>
        public bool IsDead => mHealth != null && mHealth.IsDead;

        /// <summary>
        /// 缓存并验证训练假人必须持有的同对象规则组件。
        /// </summary>
        private void Awake()
        {
            if (mCombatantIdentity == null)
            {
                mCombatantIdentity = GetComponent<CombatantIdentity>();
            }

            if (mHealth == null)
            {
                mHealth = GetComponent<Health>();
            }

            if (mCombatantIdentity == null || mHealth == null)
            {
                Debug.LogError(
                    "[Combat] TrainingDummy 缺少 CombatantIdentity 或 Health。",
                    this);
                enabled = false;
            }
        }

        /// <summary>
        /// 订阅已确认的生命结果，使开发场景可以直接观察伤害闭环。
        /// </summary>
        private void OnEnable()
        {
            if (mHealth == null)
            {
                return;
            }

            mHealth.Damaged -= OnDamaged;
            mHealth.Died -= OnDied;
            mHealth.Damaged += OnDamaged;
            mHealth.Died += OnDied;
        }

        /// <summary>
        /// 对称移除生命事件订阅，防止禁用和重载后残留回调。
        /// </summary>
        private void OnDisable()
        {
            if (mHealth == null)
            {
                return;
            }

            mHealth.Damaged -= OnDamaged;
            mHealth.Died -= OnDied;
        }

        /// <summary>
        /// 显式恢复训练假人满生命，并开启新的死亡事件生命周期。
        /// </summary>
        public void ResetDummy()
        {
            if (mHealth != null)
            {
                mHealth.ResetToFull();
            }
        }

        /// <summary>
        /// 在开发日志开启时输出已经应用的伤害和剩余生命。
        /// </summary>
        /// <param name="_result">Health 更新状态后发布的伤害结果。</param>
        private void OnDamaged(DamageResult _result)
        {
            if (!mLogCombatEvents)
            {
                return;
            }

            Debug.Log(
                $"[Combat] TrainingDummy {Id} 受到 {_result.ActualDamage:0.##} 伤害，" +
                $"剩余 {_result.HealthAfter:0.##}/{mHealth.MaximumHealth:0.##}。",
                this);
        }

        /// <summary>
        /// 在开发日志开启时输出当前生命周期唯一的死亡结果。
        /// </summary>
        /// <param name="_result">使训练假人首次进入死亡状态的伤害结果。</param>
        private void OnDied(DamageResult _result)
        {
            if (mLogCombatEvents)
            {
                Debug.Log(
                    $"[Combat] TrainingDummy {Id} 已被攻击 {_result.AttackId} 击败。",
                    this);
            }
        }
    }
}
