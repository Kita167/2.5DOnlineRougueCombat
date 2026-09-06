using System;

namespace ProjectRelay.Core
{
    /// <summary>
    /// 以伤害上下文和目标生命快照执行无副作用的基础伤害计算。
    /// 本类型不写入生命、不判断命中，也不发布表现事件。
    /// </summary>
    public static class DamageResolver
    {
        /// <summary>
        /// 验证请求和接收目标，并将有效伤害限制在目标当前剩余生命范围内。
        /// </summary>
        /// <param name="_context">需要结算的不可变伤害请求。</param>
        /// <param name="_expectedTargetId">实际接收请求的目标身份。</param>
        /// <param name="_expectedTargetFaction">实际接收请求的目标阵营。</param>
        /// <param name="_currentHealth">结算前的目标生命快照。</param>
        /// <param name="_isTargetDead">目标在结算前是否已死亡。</param>
        /// <returns>成功伤害或带有明确拒绝原因的不可变结果。</returns>
        public static DamageResult Resolve(
            in DamageContext _context,
            CombatantId _expectedTargetId,
            Faction _expectedTargetFaction,
            float _currentHealth,
            bool _isTargetDead)
        {
            if (
                !_expectedTargetId.IsValid ||
                !_context.TargetId.IsValid ||
                _context.TargetId != _expectedTargetId ||
                _expectedTargetFaction == Faction.None ||
                _context.TargetFaction != _expectedTargetFaction)
            {
                return CreateRejected(
                    _context,
                    DamageRejectionReason.InvalidTarget,
                    GetSafeHealthForRejectedResult(_currentHealth));
            }

            if (!_context.SourceId.IsValid || _context.SourceFaction == Faction.None)
            {
                return CreateRejected(
                    _context,
                    DamageRejectionReason.InvalidSource,
                    GetSafeHealthForRejectedResult(_currentHealth));
            }

            if (!_context.AttackId.IsValid)
            {
                return CreateRejected(
                    _context,
                    DamageRejectionReason.InvalidAttack,
                    GetSafeHealthForRejectedResult(_currentHealth));
            }

            if (float.IsNaN(_currentHealth) || float.IsInfinity(_currentHealth) || _currentHealth < 0.0f)
            {
                return CreateRejected(
                    _context,
                    DamageRejectionReason.InvalidHealthState,
                    0.0f);
            }

            if (float.IsNaN(_context.BaseDamage) || float.IsInfinity(_context.BaseDamage))
            {
                return CreateRejected(
                    _context,
                    DamageRejectionReason.InvalidDamage,
                    _currentHealth);
            }

            if (_isTargetDead || _currentHealth <= 0.0f)
            {
                return CreateRejected(
                    _context,
                    DamageRejectionReason.TargetAlreadyDead,
                    _currentHealth);
            }

            float _requestedDamage = Math.Max(0.0f, _context.BaseDamage);

            if (_requestedDamage <= 0.0f)
            {
                return new DamageResult(
                    _context,
                    _requestedDamage,
                    0.0f,
                    _currentHealth,
                    _currentHealth,
                    false,
                    DamageRejectionReason.NonPositiveDamage);
            }

            float _actualDamage = Math.Min(_requestedDamage, _currentHealth);
            float _healthAfter = Math.Max(0.0f, _currentHealth - _actualDamage);
            bool _killed = _currentHealth > 0.0f && _healthAfter <= 0.0f;

            return new DamageResult(
                _context,
                _requestedDamage,
                _actualDamage,
                _currentHealth,
                _healthAfter,
                _killed,
                DamageRejectionReason.None);
        }

        /// <summary>
        /// 创建不会改变生命快照的拒绝结果。
        /// </summary>
        /// <param name="_context">原始伤害请求。</param>
        /// <param name="_reason">确定性的拒绝原因。</param>
        /// <param name="_health">用于结果快照的安全生命值。</param>
        /// <returns>未应用伤害的结果。</returns>
        private static DamageResult CreateRejected(
            in DamageContext _context,
            DamageRejectionReason _reason,
            float _health)
        {
            return new DamageResult(
                _context,
                0.0f,
                0.0f,
                _health,
                _health,
                false,
                _reason);
        }

        /// <summary>
        /// 将非法生命快照转换为只供拒绝结果展示的零值，避免事件数据继续传播 NaN。
        /// </summary>
        /// <param name="_health">尚未验证的目标生命快照。</param>
        /// <returns>有限且非负的生命值，否则返回零。</returns>
        private static float GetSafeHealthForRejectedResult(float _health)
        {
            return float.IsNaN(_health) || float.IsInfinity(_health) || _health < 0.0f
                ? 0.0f
                : _health;
        }
    }
}
