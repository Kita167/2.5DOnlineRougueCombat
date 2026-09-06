namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 定义玩家控制层中同一时刻只能激活一个的互斥状态标识。
    /// Disabled 用于生命周期控制，其余值表达当前控制行为。
    /// </summary>
    public enum PlayerControlStateId
    {
        Disabled = 0,
        Idle = 1,
        Move = 2,
        Attack = 3,
        Dash = 4
    }
}
