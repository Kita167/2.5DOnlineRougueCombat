namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 定义玩家控制状态发生切换的稳定原因，供测试、调试和只读观察者使用。
    /// </summary>
    public enum PlayerControlTransitionReason
    {
        Initialize = 0,
        Enable = 1,
        Disable = 2,
        MoveStarted = 3,
        MoveStopped = 4,
        AttackStarted = 5,
        AttackCompleted = 6,
        DashStarted = 7,
        DashCompleted = 8,
        DashBlocked = 9,
        ForceReset = 10
    }
}
