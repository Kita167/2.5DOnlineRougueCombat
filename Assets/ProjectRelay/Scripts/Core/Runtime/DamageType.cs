namespace ProjectRelay.Core
{
    /// <summary>
    /// 标识伤害使用的规则类别，供后续抗性和表现系统扩展。
    /// M2 第一版只实现不含修正的物理伤害。
    /// </summary>
    public enum DamageType
    {
        /// <summary>不含元素或特殊修正的基础物理伤害。</summary>
        Physical = 0
    }
}
