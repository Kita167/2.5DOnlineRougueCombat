using ProjectRelay.Core;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 保存一次普通攻击命令是否被权威入口接受及其拒绝原因。
    /// 默认结构表示尚无命令结果，不会被误判为成功。
    /// </summary>
    public readonly struct CombatCommandResult
    {
        /// <summary>获取本结果是否对应一份已经处理的命令。</summary>
        public bool WasProcessed { get; }

        /// <summary>获取命令是否已经进入攻击执行器。</summary>
        public bool IsAccepted =>
            WasProcessed && RejectionReason == CombatCommandRejectionReason.None;

        /// <summary>获取命令来源的运行时身份。</summary>
        public CombatantId SourceId { get; }

        /// <summary>获取命令使用的稳定攻击配置标识。</summary>
        public StableId AttackId { get; }

        /// <summary>获取命令携带的本地请求序号。</summary>
        public ulong RequestSequence { get; }

        /// <summary>获取命令被拒绝的原因；成功时为 None。</summary>
        public CombatCommandRejectionReason RejectionReason { get; }

        /// <summary>
        /// 创建一份已经处理的命令结果。
        /// </summary>
        /// <param name="_request">产生结果的原始请求。</param>
        /// <param name="_rejectionReason">明确拒绝原因或成功时的 None。</param>
        private CombatCommandResult(
            in BasicAttackRequest _request,
            CombatCommandRejectionReason _rejectionReason)
        {
            WasProcessed = true;
            SourceId = _request.SourceId;
            AttackId = _request.AttackId;
            RequestSequence = _request.RequestSequence;
            RejectionReason = _rejectionReason;
        }

        /// <summary>
        /// 创建已经被权威入口接受的命令结果。
        /// </summary>
        /// <param name="_request">被接受的普通攻击请求。</param>
        /// <returns>拒绝原因为 None 的已处理结果。</returns>
        public static CombatCommandResult Accepted(in BasicAttackRequest _request)
        {
            return new CombatCommandResult(
                _request,
                CombatCommandRejectionReason.None);
        }

        /// <summary>
        /// 创建携带明确原因的拒绝结果。
        /// </summary>
        /// <param name="_request">被拒绝的普通攻击请求。</param>
        /// <param name="_reason">非 None 的拒绝原因。</param>
        /// <returns>不会被 IsAccepted 误判为成功的已处理结果。</returns>
        public static CombatCommandResult Rejected(
            in BasicAttackRequest _request,
            CombatCommandRejectionReason _reason)
        {
            CombatCommandRejectionReason _safeReason =
                _reason == CombatCommandRejectionReason.None
                    ? CombatCommandRejectionReason.ActionNotAllowed
                    : _reason;
            return new CombatCommandResult(_request, _safeReason);
        }
    }
}
