using ProjectRelay.Core;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 在本地离线运行中校验普通攻击请求，并同步交给当前玩家的攻击执行器。
    /// 本组件隔离输入协调层和具体执行器，后续可由网络 Gateway 替换。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BasicAttackController))]
    public sealed class LocalCombatCommandGateway : MonoBehaviour, ICombatCommandGateway
    {
        [SerializeField]
        [Tooltip("接收通过校验的本地普通攻击请求的执行器。")]
        private BasicAttackController mBasicAttackController;

        private ulong mLastAcceptedRequestSequence;

        /// <summary>获取 Gateway 是否已经绑定攻击执行器。</summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// 缓存同对象攻击执行器，实际就绪状态由组合根调用 Initialize 建立。
        /// </summary>
        private void Awake()
        {
            if (mBasicAttackController == null)
            {
                mBasicAttackController = GetComponent<BasicAttackController>();
            }
        }

        /// <summary>
        /// 使用 Inspector 或同对象引用绑定本地攻击执行器。
        /// </summary>
        /// <returns>执行器存在时返回 true。</returns>
        public bool Initialize()
        {
            return Initialize(mBasicAttackController);
        }

        /// <summary>
        /// 显式绑定本地普通攻击执行器，并重新开始请求序号生命周期。
        /// </summary>
        /// <param name="_basicAttackController">接收合法命令的当前玩家攻击执行器。</param>
        /// <returns>执行器存在并完成绑定时返回 true。</returns>
        public bool Initialize(BasicAttackController _basicAttackController)
        {
            if (_basicAttackController == null)
            {
                Debug.LogError(
                    "[Combat] LocalCombatCommandGateway 初始化失败：攻击执行器为空。",
                    this);
                IsReady = false;
                return false;
            }

            mBasicAttackController = _basicAttackController;
            mLastAcceptedRequestSequence = 0UL;
            IsReady = true;
            return true;
        }

        /// <summary>
        /// 验证来源、攻击配置、方向和本地序号，然后同步尝试启动普通攻击。
        /// </summary>
        /// <param name="_request">玩家控制器提交的不可变普通攻击请求。</param>
        /// <returns>请求是否接受以及失败时的确定性原因。</returns>
        public CombatCommandResult SubmitBasicAttack(in BasicAttackRequest _request)
        {
            if (
                !IsReady ||
                !isActiveAndEnabled ||
                mBasicAttackController == null ||
                !mBasicAttackController.IsInitialized)
            {
                return CombatCommandResult.Rejected(
                    _request,
                    CombatCommandRejectionReason.ControllerUnavailable);
            }

            if (
                !_request.SourceId.IsValid ||
                _request.SourceId != mBasicAttackController.SourceId)
            {
                return CombatCommandResult.Rejected(
                    _request,
                    CombatCommandRejectionReason.InvalidSource);
            }

            StableId _expectedAttackId =
                mBasicAttackController.Config != null
                    ? mBasicAttackController.Config.AttackId
                    : StableId.None;

            if (!_request.AttackId.IsValid || _request.AttackId != _expectedAttackId)
            {
                return CombatCommandResult.Rejected(
                    _request,
                    CombatCommandRejectionReason.InvalidAttack);
            }

            if (!_request.HasValidDirection)
            {
                return CombatCommandResult.Rejected(
                    _request,
                    CombatCommandRejectionReason.InvalidDirection);
            }

            if (
                _request.RequestSequence == 0UL ||
                _request.RequestSequence <= mLastAcceptedRequestSequence)
            {
                return CombatCommandResult.Rejected(
                    _request,
                    CombatCommandRejectionReason.InvalidSequence);
            }

            if (!mBasicAttackController.TryStartAttack(_request.AttackDirection))
            {
                return CombatCommandResult.Rejected(
                    _request,
                    CombatCommandRejectionReason.ActionNotAllowed);
            }

            mLastAcceptedRequestSequence = _request.RequestSequence;
            return CombatCommandResult.Accepted(_request);
        }
    }
}
