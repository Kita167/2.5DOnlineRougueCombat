namespace ProjectRelay.Core
{
    /// <summary>
    /// 说明一次伤害请求未被应用的确定性原因。
    /// 调用方可据此区分无效上下文、目标状态和合法的零伤害结果。
    /// </summary>
    public enum DamageRejectionReason
    {
        /// <summary>请求已成功应用。</summary>
        None = 0,

        /// <summary>请求来源身份或来源阵营无效。</summary>
        InvalidSource = 1,

        /// <summary>目标身份、目标阵营或接收该请求的目标不匹配。</summary>
        InvalidTarget = 2,

        /// <summary>攻击定义没有稳定标识。</summary>
        InvalidAttack = 3,

        /// <summary>基础伤害为非有限数值。</summary>
        InvalidDamage = 4,

        /// <summary>目标生命快照为非法数值。</summary>
        InvalidHealthState = 5,

        /// <summary>请求伤害小于或等于零。</summary>
        NonPositiveDamage = 6,

        /// <summary>目标已经死亡或没有剩余生命。</summary>
        TargetAlreadyDead = 7
    }
}
