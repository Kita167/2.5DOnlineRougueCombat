using ProjectRelay.Gameplay.Combat;
using ProjectRelay.Input;
using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 将玩家输入转换为不可变控制快照，并依次协调控制状态机、朝向和 PlayerMotor。
    /// 本组件不拥有动作转移规则，不查询攻击目标，也不直接修改玩家 Transform。
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
        [Tooltip("负责根据状态机最终输出旋转玩家的朝向组件。")]
        private PlayerFacingController mFacingController;

        [SerializeField]
        [Tooltip("独立推进普通攻击阶段、命中和冷却的同对象组件。")]
        private BasicAttackController mBasicAttackController;

        [SerializeField]
        [Tooltip("BattleSandbox 使用的本地权威战斗命令入口。")]
        private LocalCombatCommandGateway mLocalCombatCommandGateway;

        private IPlayerInputSource mInputSource;
        private Camera mGameplayCamera;
        private PlayerBasicAttackDriver mBasicAttackDriver;
        private PlayerControlStateMachine mControlStateMachine;
        private bool mIsControlEnabled;

        /// <summary>获取控制器是否已经绑定全部运行时依赖。</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>获取玩家当前唯一互斥控制状态。</summary>
        public PlayerControlStateId CurrentControlState =>
            mControlStateMachine != null
                ? mControlStateMachine.CurrentStateId
                : PlayerControlStateId.Disabled;

        /// <summary>获取当前普通攻击阶段，供调试和表现层只读观察。</summary>
        public BasicAttackPhase CurrentAttackPhase =>
            mBasicAttackController != null
                ? mBasicAttackController.CurrentPhase
                : BasicAttackPhase.Idle;

        /// <summary>获取最近一次实际提交到 Gateway 的普通攻击命令结果。</summary>
        public CombatCommandResult LastAttackCommandResult =>
            mBasicAttackDriver != null
                ? mBasicAttackDriver.LastCommandResult
                : default;

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
                    CurrentControlState == PlayerControlStateId.Dash
                        ? mMovementConfig.DashSpeed
                        : mMovementConfig.MoveSpeed;

                if (_referenceSpeed <= Mathf.Epsilon)
                {
                    return 0.0f;
                }

                return Mathf.Clamp01(
                    mMotor.HorizontalVelocity.magnitude / _referenceSpeed);
            }
        }

        /// <summary>
        /// 缓存同对象上的移动、朝向和战斗组件，并尽早报告缺失的移动配置。
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
                mLocalCombatCommandGateway =
                    GetComponent<LocalCombatCommandGateway>();
            }

            if (mMovementConfig == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 缺少 PlayerMovementConfig。",
                    this);
            }
        }

        /// <summary>
        /// 在组件重新启用时恢复此前请求的输入和控制状态。
        /// </summary>
        private void OnEnable()
        {
            if (!IsInitialized)
            {
                return;
            }

            bool _shouldEnableControl = mIsControlEnabled && isActiveAndEnabled;
            mInputSource.SetInputEnabled(_shouldEnableControl);
            mControlStateMachine.SetEnabled(_shouldEnableControl);
        }

        /// <summary>
        /// 每帧依次读取输入、推进唯一状态机、更新朝向、执行位移并回报移动结果。
        /// </summary>
        private void Update()
        {
            if (
                !IsInitialized ||
                mMovementConfig == null ||
                mMotor == null ||
                mFacingController == null ||
                mControlStateMachine == null ||
                mGameplayCamera == null)
            {
                return;
            }

            bool _canReadInput = mIsControlEnabled && mInputSource.IsEnabled;
            Vector2 _moveInput = _canReadInput
                ? mInputSource.Move
                : Vector2.zero;
            bool _dashPressed =
                _canReadInput && mInputSource.ConsumeDashPressed();
            bool _attackPressed =
                _canReadInput && mInputSource.ConsumeAttackPressed();
            float _deltaTime = Time.deltaTime;

            Transform _cameraTransform = mGameplayCamera.transform;
            Vector3 _worldDirection =
                PlayerMovementMath.GetCameraRelativeDirection(
                    _moveInput,
                    _cameraTransform.forward,
                    _cameraTransform.right);
            PlayerControlInput _controlInput = new PlayerControlInput(
                _worldDirection,
                mFacingController.CurrentFacingDirection,
                _dashPressed,
                _attackPressed);
            PlayerControlOutput _controlOutput =
                mControlStateMachine.Tick(_controlInput, _deltaTime);

            Vector3 _facingDirection =
                _controlOutput.HasLockedFacingDirection
                    ? _controlOutput.LockedFacingDirection
                    : _controlOutput.CanTurn
                        ? _controlOutput.HorizontalVelocity
                        : Vector3.zero;
            mFacingController.TickFacing(
                _facingDirection,
                mMovementConfig.RotationSpeed,
                _deltaTime);

            mMotor.TickMovement(
                _controlOutput.HorizontalVelocity,
                mMovementConfig.Gravity,
                mMovementConfig.MaximumFallSpeed,
                mMovementConfig.GroundedVerticalSpeed,
                _deltaTime);
            PlayerMovementResult _movementResult = new PlayerMovementResult(
                mMotor.HorizontalVelocity,
                mMotor.LastCollisionFlags);
            mControlStateMachine.ReportMovementResult(_movementResult);
        }

        /// <summary>
        /// 在组件禁用时停止输入并清除状态、攻击运行时和 Motor 速度。
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

            if (mControlStateMachine != null)
            {
                mControlStateMachine.ForceReset();
            }
            else if (mBasicAttackController != null)
            {
                mBasicAttackController.ForceReset();
            }
        }

        /// <summary>
        /// 使用同对象战斗组件绑定当前本地玩家的输入源和 Gameplay Camera。
        /// </summary>
        /// <param name="_inputSource">只表达玩家意图的输入源。</param>
        /// <param name="_gameplayCamera">提供相机相对移动基准的场景 Camera。</param>
        /// <returns>依赖和配置全部有效时返回 true。</returns>
        public bool Initialize(
            IPlayerInputSource _inputSource,
            Camera _gameplayCamera)
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
        /// 显式绑定输入、相机、攻击执行器和命令 Gateway，并创建唯一控制状态机。
        /// </summary>
        /// <param name="_inputSource">只表达玩家意图的输入源。</param>
        /// <param name="_gameplayCamera">提供相机相对移动基准的场景 Camera。</param>
        /// <param name="_basicAttackController">管理普通攻击运行时的执行器。</param>
        /// <param name="_combatCommandGateway">接收普通攻击值类型请求的权威入口。</param>
        /// <returns>全部依赖、配置和战斗组件就绪时返回 true。</returns>
        public bool Initialize(
            IPlayerInputSource _inputSource,
            Camera _gameplayCamera,
            BasicAttackController _basicAttackController,
            ICombatCommandGateway _combatCommandGateway)
        {
            ResetBeforeInitialization();

            if (_inputSource == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：输入源为空。",
                    this);
                return false;
            }

            if (_gameplayCamera == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：Gameplay Camera 为空。",
                    this);
                return false;
            }

            if (_basicAttackController == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：普通攻击执行器为空。",
                    this);
                return false;
            }

            if (
                mMovementConfig == null ||
                mMotor == null ||
                mFacingController == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：移动依赖未就绪。",
                    this);
                return false;
            }

            if (!_basicAttackController.Initialize())
            {
                Debug.LogError(
                    "[Gameplay] PlayerController 初始化失败：普通攻击配置无效。",
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

            mInputSource = _inputSource;
            mGameplayCamera = _gameplayCamera;
            mBasicAttackController = _basicAttackController;
            mBasicAttackDriver = new PlayerBasicAttackDriver(
                _basicAttackController,
                _combatCommandGateway);
            mControlStateMachine = new PlayerControlStateMachine(
                mMovementConfig,
                mBasicAttackDriver);
            IsInitialized = true;

            bool _shouldEnableControl = mIsControlEnabled && isActiveAndEnabled;
            mInputSource.SetInputEnabled(_shouldEnableControl);
            mControlStateMachine.SetEnabled(_shouldEnableControl);
            return true;
        }

        /// <summary>
        /// 设置当前玩家是否允许接收控制输入；禁用时完整重置状态和 Motor。
        /// </summary>
        /// <param name="_isEnabled">是否允许当前本地玩家控制角色。</param>
        public void SetControlEnabled(bool _isEnabled)
        {
            mIsControlEnabled = _isEnabled;

            if (!IsInitialized)
            {
                return;
            }

            bool _shouldEnableControl = _isEnabled && isActiveAndEnabled;
            mInputSource.SetInputEnabled(_shouldEnableControl);
            mControlStateMachine.SetEnabled(_shouldEnableControl);

            if (!_shouldEnableControl && mMotor != null)
            {
                mMotor.ResetMotion();
            }
        }

        /// <summary>
        /// 清理上一轮初始化拥有的输入和运行时对象，避免失败重试继续使用旧依赖。
        /// </summary>
        private void ResetBeforeInitialization()
        {
            if (mInputSource != null)
            {
                mInputSource.SetInputEnabled(false);
            }

            mControlStateMachine?.ForceReset();
            mBasicAttackDriver = null;
            mControlStateMachine = null;
            mInputSource = null;
            mGameplayCamera = null;
            IsInitialized = false;
        }
    }
}
