namespace ProjectRelay.Core
{
    /// <summary>
    /// 定义战斗单位在单局规则中的阵营归属。
    /// 阵营关系的具体过滤规则由权威战斗执行层决定。
    /// </summary>
    public enum Faction
    {
        /// <summary>尚未配置阵营。</summary>
        None = 0,

        /// <summary>玩家及其友方单位。</summary>
        Player = 1,

        /// <summary>敌对单位。</summary>
        Enemy = 2,

        /// <summary>不默认归入玩家或敌人的中立单位。</summary>
        Neutral = 3
    }
}
