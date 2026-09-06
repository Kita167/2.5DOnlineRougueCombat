namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 定义一次普通攻击从准备到冷却结束的确定性运行时阶段。
    /// 阶段只表达时序，不负责命中检测、伤害计算或表现播放。
    /// </summary>
    public enum BasicAttackPhase
    {
        /// <summary>当前没有攻击或冷却状态。</summary>
        Idle = 0,

        /// <summary>攻击已经接受，正在等待生效时刻。</summary>
        Windup = 1,

        /// <summary>攻击进入有效窗口；后续命中查询在进入该阶段时执行一次。</summary>
        Active = 2,

        /// <summary>有效窗口结束，动作锁仍保持到本阶段结束。</summary>
        Recovery = 3,

        /// <summary>动作锁已经释放，但下一次攻击仍被冷却拒绝。</summary>
        Cooldown = 4
    }
}
