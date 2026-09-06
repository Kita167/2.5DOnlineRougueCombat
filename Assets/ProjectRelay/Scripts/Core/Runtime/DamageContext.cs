namespace ProjectRelay.Core
{
    /// <summary>
    /// 保存一次伤害请求所需的不可变权威输入。
    /// 该数据只包含值，不持有场景对象、表现组件或网络实现引用。
    /// </summary>
    public readonly struct DamageContext
    {
        /// <summary>获取发起伤害的单局运行时身份。</summary>
        public CombatantId SourceId { get; }

        /// <summary>获取接收伤害的单局运行时身份。</summary>
        public CombatantId TargetId { get; }

        /// <summary>获取伤害来源阵营的请求时快照。</summary>
        public Faction SourceFaction { get; }

        /// <summary>获取目标阵营的请求时快照。</summary>
        public Faction TargetFaction { get; }

        /// <summary>获取产生本次伤害的稳定攻击定义标识。</summary>
        public StableId AttackId { get; }

        /// <summary>获取伤害规则类别。</summary>
        public DamageType DamageType { get; }

        /// <summary>获取尚未经过生命钳制的基础伤害。</summary>
        public float BaseDamage { get; }

        /// <summary>
        /// 创建一次不依赖场景引用的伤害请求。
        /// 参数合法性由 DamageResolver 统一验证，以便返回明确拒绝原因。
        /// </summary>
        /// <param name="_sourceId">发起伤害的运行时身份。</param>
        /// <param name="_targetId">接收伤害的运行时身份。</param>
        /// <param name="_sourceFaction">来源阵营快照。</param>
        /// <param name="_targetFaction">目标阵营快照。</param>
        /// <param name="_attackId">攻击定义的稳定标识。</param>
        /// <param name="_damageType">伤害规则类别。</param>
        /// <param name="_baseDamage">尚未经过生命钳制的基础伤害。</param>
        public DamageContext(
            CombatantId _sourceId,
            CombatantId _targetId,
            Faction _sourceFaction,
            Faction _targetFaction,
            StableId _attackId,
            DamageType _damageType,
            float _baseDamage)
        {
            SourceId = _sourceId;
            TargetId = _targetId;
            SourceFaction = _sourceFaction;
            TargetFaction = _targetFaction;
            AttackId = _attackId;
            DamageType = _damageType;
            BaseDamage = _baseDamage;
        }
    }
}
