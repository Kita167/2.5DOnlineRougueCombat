using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Input;
using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 消费玩家输入并协调相机相对方向、动作状态、普通攻击、角色朝向与 PlayerMotor。
    /// 本组件不拥有 Input Actions，也不直接修改玩家 Transform。
    /// 攻击意图只通过 ICombatCommandGateway 提交，本组件不查询目标或计算伤害。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerFacingController))]
    [RequireComponent(typeof(BasicAttackController))]
    [RequireComponent(typeof(LocalCombatCommandGateway))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("玩家普通移动和垂直运动使用的设计参数。")]
        private PlayerMovementConfig mMovementConfig;

        [SerializeField]
        [Tooltip("负责执行 CharacterController 位移的玩家 Motor。")]
        private PlayerMotor mMotor;

        [SerializeField]
        [Tooltip("负责根据最终移动方向旋转玩家的朝向组件。")]
        private PlayerFacingController mFacingController;

        [SerializeField]
        [Tooltip("推进普通攻击阶段并向动作状态机申请 Attacking 的同对象组件。")]
        private BasicAttackController mBasicAttackController;

        [SerializeField]
        [Tooltip("BattleSandbox 使用的本地权威战斗命令入口。")]
        private LocalCombatCommandGateway mLocalCombatCommandGateway;

        private IPlayerInputSource mInputSource;
        private ICombatCommandGateway mCombatCommandGateway;
        private Camera mGameplayCamera;
        private PlayerActionStateMachine mActionStateMachine;
        private bool mIsControlEnabled;
        private ulong mNextAttackRequestSequence;

        /// <summary>
        /// 获取控制器是否已经绑定输入源和 Gameplay Camera。
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 获取玩家当前移动状态，供动画表现和只读观察者使用。
        /// </summary>
        public PlayerActionState CurrentActionState =>
            mActionStateMachine != null
                ? mActionStateMachine.CurrentState
                : PlayerActionState.Disabled;

        /// <summary>
        /// 获取当前普通攻击阶段，供调试和表现层只读观察。
        /// </summary>
        public BasicAttackPhase CurrentAttackPhase =>
            mBasicAttackController != null
                ? mBasicAttackController.CurrentPhase
                : BasicAttackPhase.Idle;

        /// <summary>
        /// 获取最近一次实际提交到 Gateway 的普通攻击命令结果。
        /// </summary>
        public CombatCommandResult LastAttackCommandResult { get; private set; }

        /// <summary>
        /// 获取实际水平速度相对当前状态目标速度的 0 到 1 归一化值。
        /// </summary>
        public float NormalizedHorizontalSpeed
        {
            get
            {
                if (mMotor == null || mMovementConfig == null)
                {
                    return 0.0f;
                }

                float _referenceSpeed =
                    CurrentActionState == PlayerActionState.Dashing
                    ? mMovementConfig.DashSpeed
                    : mMovementConfig.MoveSpeed;

                if (_referenceSpeed <= Mathf.Epsilon)
                {
                    return 0.0f;
                }

                return Mathf.Clamp01(mMotor.HorizontalVelocity.magnitude / _referenceSpeed);
            }
        }

        /// <summary>
        /// 缓存同对象上的 Motor 和朝向组件，并尽早报告缺失的移动配置。
        /// </summary>
        private void Awake()
        {
            if (mMotor == null)
            {
                mMotor = GetComponent<PlayerMotor>();
            }

            if (mFacingController == null)
            {
                mFacingController = GetComponent<PlayerFacingController>();
            }

            if (mBasicAttackController == null)
            {
                mBasicAttackController = GetComponent<BasicAttackController>();
            }

            if (mLocalCombatCommandGateway == null)
            {
                mLocalCombatCommandGateway = GetComponent<LocalCombatCommandGateway>();
            }

            if (mMovementConfig == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 缺少 PlayerMovementConfig。",
                    this);
                return;
            }

            mActionStateMachine = new PlayerActionStateMachine(mMovementConfig);
        }

        /// <summary>
        /// 在组件重新启用时恢复此前请求的输入启用状态。
        /// </summary>
        private void OnEnable()
        {
            if (IsInitialized)
            {
                mInputSource.SetInputEnabled(mIsControlEnabled);
                mActionStateMachine.SetEnabled(mIsControlEnabled);
            }
        }

        /// <summary>
        /// 每帧按固定顺序推进动作、处理 Dash/Attack，再更新朝向和执行唯一一次位移。
        /// </summary>
        private void Update()
        {
            //依赖检查
            if (
                !IsInitialized ||
                mMovementConfig == null ||
                mMotor == null ||
                mFacingController == null ||
                mActionStateMachine == null ||
                mGameplayCamera == null)
            {
                return;
            }

            //数据处理
            bool _canReadInput = mIsControlEnabled && mInputSource.IsEnabled;
            Vector2 _moveInput =
                _canReadInput
                    ? mInputSource.Move
                    : Vector2.zero;
            bool _dashPressed = _canReadInput && mInputSource.ConsumeDashPressed();
            bool _attackPressed = _canReadInput && mInputSource.ConsumeAttackPressed();
            float _deltaTime = Time.deltaTime;

            Transform _cameraTransform = mGameplayCamera.transform;
            Vector3 _worldDirection = PlayerMovementMath.GetCameraRelativeDirection(
                _moveInput,
                _cameraTransform.forward,
                _cameraTransform.right);

            //更新链路
            mActionStateMachine.AdvanceTime(_deltaTime);
            mBasicAttackController.Tick(_deltaTime);

            bool _didStartDash = mActionStateMachine.TryDash(
                _worldDirection,
                mFacingController.CurrentFacingDirection,
                _dashPressed,
                _deltaTime);

            if (
                _attackPressed &&
                !_didStartDash &&
                mActionStateMachine.CurrentState == PlayerActionState.Free)
            {
                SubmitBasicAttack();
            }

            PlayerActionConstraints _constraints =
                mActionStateMachine.CurrentConstraints;
            Vector3 _horizontalVelocity =
                mActionStateMachine.CalculateHorizontalVelocity(_worldDirection);
            Vector3 _facingDirection = _constraints.HasLockedFacingDirection
                ? _constraints.LockedFacingDirection
                : _constraints.CanTurn
                    ? _horizontalVelocity
                    : Vector3.zero;

            mFacingController.TickFacing(
                _facingDirection,
                mMovementConfig.RotationSpeed,
                _deltaTime);

            mMotor.TickMovement(
                _horizontalVelocity,
                mMovementConfig.Gravity,
                mMovementConfig.MaximumFallSpeed,
                mMovementConfig.GroundedVerticalSpeed,
                _deltaTime);

            mActionStateMachine.ReportMovementResult(
                mMotor.HorizontalVelocity,
                mMotor.LastCollisionFlags);
        }

        /// <summary>
        /// 在组件禁用时停止输入并清除运行时速度，避免重新启用后继承旧状态。
        /// </summary>
        private void OnDisable()
        {
            if (mInputSource != null)
            {
                mInputSource.SetInputEnabled(false);
            }

            if (mMotor != null)
            {
                mMotor.ResetMotion();
            }

            if (mBasicAttackController != null)
            {
                mBasicAttackController.ForceReset();
            }

            if (mActionStateMachine != null)
            {
                mActionStateMachine.ForceReset();
            }
        }

        /// <summary>
        /// 绑定当前本地玩家的输入源和 Gameplay Camera，使控制器可以开始计算移动。
        /// </summary>
        /// <param name="_inputSource">只表达玩家意图的输入源。</param>
        /// <param name="_gameplayCamera">提供相机相对移动基准的场景 Camera。</param>
        /// <returns>依赖和配置全部有效时返回 true。</returns>
        public bool Initialize(IPlayerInputSource _inputSource, Camera _gameplayCamera)
        {
            if (
                mLocalCombatCommandGateway != null &&
                !mLocalCombatCommandGateway.IsReady)
            {
                mLocalCombatCommandGateway.Initialize(mBasicAttackController);
            }

            return Initialize(
                _inputSource,
                _gameplayCamera,
                mBasicAttackController,
                mLocalCombatCommandGateway);
        }

        /// <summary>
        /// 显式绑定输入、相机、攻击执行器和命令 Gateway，并建立共享动作状态。
        /// </summary>
        /// <param name="_inputSource">只表达玩家意图的输入源。</param>
        /// <param name="_gameplayCamera">提供相机相对移动基准的场景 Camera。</param>
        /// <param name="_basicAttackController">需要由本控制器逐帧推进的攻击执行器。</param>
        /// <param name="_combatCommandGateway">接收普通攻击值类型请求的权威入口。</param>
        /// <returns>全部依赖、配置和战斗组件就绪时返回 true。</returns>
        public bool Initialize(
            IPlayerInputSource _inputSource,
            Camera _gameplayCamera,
            BasicAttackController _basicAttackController,
            ICombatCommandGateway _combatCommandGateway)
        {
            if (_inputSource == null)
            {
                Debug.LogError("[Gameplay] PlayerController 初始化失败：输入源为空。", this);
                return false;
            }

            if (_gameplayCamera == null)
            {
                Debug.LogError("[Gameplay] PlayerController 初始化失败：Gameplay Camera 为空。", this);
                return false;
            }

            if (_basicAttackController == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：普通攻击执行器为空。",
                    this);
                return false;
            }

            if (_combatCommandGateway == null || !_combatCommandGateway.IsReady)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：战斗命令 Gateway 未就绪。",
                    this);
                return false;
            }

            if (
                mMovementConfig == null ||
                mMotor == null ||
                mFacingController == null ||
                mActionStateMachine == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：移动依赖未就绪。",
                    this);
                return false;
            }

            if (!_basicAttackController.Initialize(mActionStateMachine))
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：普通攻击执行器配置无效。",
                    this);
                return false;
            }

            mInputSource = _inputSource;
            mGameplayCamera = _gameplayCamera;
            mBasicAttackController = _basicAttackController;
            mCombatCommandGateway = _combatCommandGateway;
            mNextAttackRequestSequence = 0UL;
            LastAttackCommandResult = default;
            IsInitialized = true;
            mActionStateMachine.ForceReset();

            bool _shouldEnableControl = mIsControlEnabled && isActiveAndEnabled;
            mInputSource.SetInputEnabled(_shouldEnableControl);
            mActionStateMachine.SetEnabled(_shouldEnableControl);
            return true;
        }

        /// <summary>
        /// 设置当前玩家是否允许接收控制输入；禁用控制时仍保留重力更新。
        /// </summary>
        /// <param name="_isEnabled">为 true 时允许移动输入，为 false 时清空并禁用输入。</param>
        public void SetControlEnabled(bool _isEnabled)
        {
            mIsControlEnabled = _isEnabled;

            if (!IsInitialized)
            {
                return;
            }

            bool _shouldEnableControl = _isEnabled && isActiveAndEnabled;
            mInputSource.SetInputEnabled(_shouldEnableControl);

            if (!_shouldEnableControl && mBasicAttackController != null)
            {
                mBasicAttackController.ForceReset();
            }

            mActionStateMachine.SetEnabled(_shouldEnableControl);

            if (!_isEnabled && mMotor != null)
            {
                mMotor.ResetMotion();
            }
        }

        /// <summary>
        /// 使用当前权威攻击者身份、攻击定义和锁定朝向构造值类型请求并提交 Gateway。
        /// </summary>
        private void SubmitBasicAttack()
        {
            if (
                mCombatCommandGateway == null ||
                !mCombatCommandGateway.IsReady ||
                mBasicAttackController == null ||
                mBasicAttackController.Definition == null)
            {
                return;
            }

            BasicAttackRequest _request = new BasicAttackRequest(
                mBasicAttackController.SourceId,
                mBasicAttackController.Definition.AttackId,
                mFacingController.CurrentFacingDirection,
                CreateNextAttackRequestSequence());
            LastAttackCommandResult =
                mCombatCommandGateway.SubmitBasicAttack(_request);
        }

        /// <summary>
        /// 生成当前玩家运行期间单调递增的非零普通攻击请求序号。
        /// </summary>
        /// <returns>本次请求独占的非零序号。</returns>
        private ulong CreateNextAttackRequestSequence()
        {
            unchecked
            {
                mNextAttackRequestSequence++;

                if (mNextAttackRequestSequence == 0UL)
                {
                    mNextAttackRequestSequence++;
                }
            }

            return mNextAttackRequestSequence;
        }
    }
}
