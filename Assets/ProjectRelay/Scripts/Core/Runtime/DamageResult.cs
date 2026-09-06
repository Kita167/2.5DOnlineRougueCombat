namespace ProjectRelay.Core
{
    /// <summary>
    /// 保存一次伤害请求的不可变结算结果。
    /// 监听者可直接使用该快照，不需要在事件发生后重新读取可变生命状态。
    /// </summary>
    public readonly struct DamageResult
    {
        /// <summary>获取伤害来源的运行时身份。</summary>
        public CombatantId SourceId { get; }

        /// <summary>获取伤害目标的运行时身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>获取产生伤害的稳定攻击定义标识。</summary>
        public StableId AttackId { get; }

        /// <summary>获取经过非负处理后请求扣除的生命值。</summary>
        public float RequestedDamage { get; }

        /// <summary>获取经过目标剩余生命钳制后的实际扣除值。</summary>
        public float ActualDamage { get; }

        /// <summary>获取应用伤害前的目标生命值。</summary>
        public float HealthBefore { get; }

        /// <summary>获取应用伤害后的目标生命值。</summary>
        public float HealthAfter { get; }

        /// <summary>获取本次伤害是否使目标首次进入死亡状态。</summary>
        public bool Killed { get; }

        /// <summary>获取请求未被应用的原因；成功时为 None。</summary>
        public DamageRejectionReason RejectionReason { get; }

        /// <summary>获取本次请求是否实际修改了生命值。</summary>
        public bool IsApplied =>
            RejectionReason == DamageRejectionReason.None && ActualDamage > 0.0f;

        /// <summary>
        /// 创建由 DamageResolver 产生的完整伤害结果。
        /// </summary>
        /// <param name="_context">原始伤害请求。</param>
        /// <param name="_requestedDamage">非负请求伤害。</param>
        /// <param name="_actualDamage">生命钳制后的实际伤害。</param>
        /// <param name="_healthBefore">应用前生命。</param>
        /// <param name="_healthAfter">应用后生命。</param>
        /// <param name="_killed">是否首次致死。</param>
        /// <param name="_rejectionReason">拒绝原因或 None。</param>
        internal DamageResult(
            DamageContext _context,
            float _requestedDamage,
            float _actualDamage,
            float _healthBefore,
            float _healthAfter,
            bool _killed,
            DamageRejectionReason _rejectionReason)
        {
            SourceId = _context.SourceId;
            TargetId = _context.TargetId;
            AttackId = _context.AttackId;
            RequestedDamage = _requestedDamage;
            ActualDamage = _actualDamage;
            HealthBefore = _healthBefore;
            HealthAfter = _healthAfter;
            Killed = _killed;
            RejectionReason = _rejectionReason;
        }
    }
}
