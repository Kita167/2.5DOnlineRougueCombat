namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 定义玩家移动模块中互斥的移动状态，由 PlayerLocomotionStateMachine 统一管理状态转换。
    /// 技能施放、受击和死亡等非移动状态不属于此枚举。
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
        Dashing = 2
    }
}
