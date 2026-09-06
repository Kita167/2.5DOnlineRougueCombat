using System;
using ProjectRelay.Core;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 保存战斗单位生命状态，并作为生命值的唯一伤害写入口。
    /// 本组件委托纯 DamageResolver 计算，不负责命中检测、攻击时序或表现。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatantIdentity))]
    public sealed class Health : MonoBehaviour
    {
        private const float mMinimumMaximumHealth = 0.0001f;

        [SerializeField]
        [Min(mMinimumMaximumHealth)]
        [Tooltip("该战斗单位每个生命周期开始时拥有的最大生命值。")]
        private float mMaximumHealth = 100.0f;

        [SerializeField]
        [Tooltip("提供目标运行时身份和阵营的同对象组件。")]
        private CombatantIdentity mCombatantIdentity;

        private float mCurrentHealth;
        private bool mIsDead;
        private bool mIsInitialized;

        /// <summary>
        /// 获取该战斗单位的最大生命值。
        /// </summary>
        public float MaximumHealth => mMaximumHealth;

        /// <summary>
        /// 获取与生命状态绑定的同对象战斗身份，供权威命中执行层读取。
        /// </summary>
        public CombatantIdentity Identity => mCombatantIdentity;

        /// <summary>
        /// 获取最近一次已确认结算后的当前生命值。
        /// </summary>
        public float CurrentHealth => mCurrentHealth;

        /// <summary>
        /// 获取该战斗单位当前生命周期是否已经死亡。
        /// </summary>
        public bool IsDead => mIsDead;

        /// <summary>
        /// 在生命值成功减少且内部状态已更新后发布完整伤害结果。
        /// </summary>
        public event Action<DamageResult> Damaged;

        /// <summary>
        /// 在当前生命周期首次进入死亡状态时发布一次致死结果。
        /// </summary>
        public event Action<DamageResult> Died;

        /// <summary>
        /// 缓存同对象战斗身份，并建立初始满生命状态。
        /// </summary>
        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// 缓存并验证战斗身份，在首次成功调用时建立满生命状态。
        /// 重复调用不会治疗或重置已经参与战斗的对象。
        /// </summary>
        /// <returns>必需依赖存在且生命状态可用时返回 true。</returns>
        public bool Initialize()
        {
            if (mIsInitialized)
            {
                return true;
            }

            if (mCombatantIdentity == null)
            {
                mCombatantIdentity = GetComponent<CombatantIdentity>();
            }

            if (mCombatantIdentity == null)
            {
                Debug.LogError("[Combat] Health 缺少 CombatantIdentity。", this);
                enabled = false;
                return false;
            }

            mMaximumHealth = GetSafeMaximumHealth(mMaximumHealth);
            ResetToFull();
            mIsInitialized = true;
            return true;
        }

        /// <summary>
        /// 通过纯规则计算并应用一次伤害，拒绝结果不会修改状态或发布事件。
        /// </summary>
        /// <param name="_context">包含来源、目标、攻击标识和基础伤害的请求。</param>
        /// <param name="_result">返回成功结算或明确拒绝原因的结果快照。</param>
        /// <returns>生命值确实减少时返回 true。</returns>
        public bool TryApplyDamage(in DamageContext _context, out DamageResult _result)
        {
            if (!Initialize())
            {
                _result = DamageResolver.Resolve(
                    _context,
                    CombatantId.None,
                    Faction.None,
                    0.0f,
                    false);
                return false;
            }

            CombatantId _targetId =
                mCombatantIdentity != null ? mCombatantIdentity.Id : CombatantId.None;
            Faction _targetFaction =
                mCombatantIdentity != null ? mCombatantIdentity.Faction : Faction.None;

            _result = DamageResolver.Resolve(
                _context,
                _targetId,
                _targetFaction,
                mCurrentHealth,
                mIsDead);

            if (!_result.IsApplied)
            {
                return false;
            }

            mCurrentHealth = _result.HealthAfter;
            bool _didDie = _result.Killed && !mIsDead;

            if (_didDie)
            {
                mIsDead = true;
            }

            Damaged?.Invoke(_result);

            if (_didDie)
            {
                Died?.Invoke(_result);
            }

            return true;
        }

        /// <summary>
        /// 显式开始新的生命周期，恢复满生命并允许下一次死亡事件触发。
        /// 该操作不发布受伤或死亡事件。
        /// </summary>
        public void ResetToFull()
        {
            mCurrentHealth = mMaximumHealth;
            mIsDead = false;
        }

        /// <summary>
        /// 在编辑器修改组件时将最大生命约束为有限正数。
        /// </summary>
        private void OnValidate()
        {
            mMaximumHealth = GetSafeMaximumHealth(mMaximumHealth);
        }

        /// <summary>
        /// 将非法或非正最大生命转换为可运行的最小正数。
        /// </summary>
        /// <param name="_maximumHealth">Inspector 或运行时提供的最大生命。</param>
        /// <returns>有限且不小于最小值的最大生命。</returns>
        private static float GetSafeMaximumHealth(float _maximumHealth)
        {
            if (float.IsNaN(_maximumHealth) || float.IsInfinity(_maximumHealth))
            {
                return 100.0f;
            }

            return Mathf.Max(mMinimumMaximumHealth, _maximumHealth);
        }
    }
}
