namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 定义普通攻击意图进入权威战斗执行路径的边界。
    /// 本地实现同步执行；未来网络实现可在保持请求协议不变的情况下转交 Host 校验。
    /// </summary>
    public interface ICombatCommandGateway
    {
        /// <summary>获取 Gateway 是否已经绑定可用的攻击执行器。</summary>
        bool IsReady { get; }

        /// <summary>
        /// 校验并提交一次不可变普通攻击请求。
        /// </summary>
        /// <param name="_request">输入层和场景对象解耦的普通攻击请求。</param>
        /// <returns>命令是否接受及其明确拒绝原因。</returns>
        CombatCommandResult SubmitBasicAttack(in BasicAttackRequest _request);
    }
}
