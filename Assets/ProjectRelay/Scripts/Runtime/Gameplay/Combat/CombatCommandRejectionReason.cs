namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 说明普通攻击命令未被本地权威入口接受的确定性原因。
    /// </summary>
    public enum CombatCommandRejectionReason
    {
        /// <summary>命令已经被接受。</summary>
        None = 0,

        /// <summary>权威 Gateway 或攻击执行器尚未准备完成。</summary>
        ControllerUnavailable = 1,

        /// <summary>请求来源无效或与 Gateway 绑定的战斗单位不一致。</summary>
        InvalidSource = 2,

        /// <summary>攻击标识无效或与执行器配置的定义不一致。</summary>
        InvalidAttack = 3,

        /// <summary>请求方向无法形成有效的世界空间平面方向。</summary>
        InvalidDirection = 4,

        /// <summary>请求序号为零或已经被该 Gateway 接受过。</summary>
        InvalidSequence = 5,

        /// <summary>当前动作状态、攻击阶段或冷却不允许开始攻击。</summary>
        ActionNotAllowed = 6
    }
}
