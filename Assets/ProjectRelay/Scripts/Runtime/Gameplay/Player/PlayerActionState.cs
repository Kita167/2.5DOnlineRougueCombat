namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 定义玩家控制期间互斥的动作状态，由 PlayerActionStateMachine 统一管理状态转换。
    /// 攻击阶段由独立战斗控制器推进，本枚举只表达它对移动和其他动作的占用。
    /// </summary>
    public enum PlayerActionState
    {
        /// <summary>
        /// 玩家控制被禁用，不接受移动或冲刺意图。
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// 玩家可以普通移动并尝试开始冲刺。
        /// </summary>
        Free = 1,

        /// <summary>
        /// 玩家沿进入状态时锁定的方向进行冲刺。
        /// </summary>
        Dashing = 2,

        /// <summary>
        /// 玩家正在执行一次攻击，移动倍率、朝向和其他动作受到攻击约束。
        /// </summary>
        Attacking = 3
    }
}
