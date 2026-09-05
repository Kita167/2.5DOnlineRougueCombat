using ProjectRelay.Input;
using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 消费玩家输入并协调相机相对方向、移动状态、角色朝向与 PlayerMotor。
    /// 本组件不拥有 Input Actions，也不直接修改玩家 Transform。
    /// 后续只负责把技能输入和移动约束交给独立模块，不承载技能或伤害规则。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerFacingController))]
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

        private IPlayerInputSource mInputSource;
        private Camera mGameplayCamera;
        private PlayerActionStateMachine mActionStateMachine;
        private bool mIsControlEnabled;

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
        /// 每帧把本地移动输入转换为世界方向，更新角色朝向并让 PlayerMotor 处理实际移动。
        /// </summary>
        private void Update()
        {
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

            bool _canReadInput = mIsControlEnabled && mInputSource.IsEnabled;
            Vector2 _moveInput =
                _canReadInput
                    ? mInputSource.Move
                    : Vector2.zero;
            bool _dashPressed = _canReadInput && mInputSource.ConsumeDashPressed();

            Transform _cameraTransform = mGameplayCamera.transform;
            Vector3 _worldDirection = PlayerMovementMath.GetCameraRelativeDirection(
                _moveInput,
                _cameraTransform.forward,
                _cameraTransform.right);

            Vector3 _horizontalVelocity = mActionStateMachine.Tick(
                _worldDirection,
                mFacingController.CurrentFacingDirection,
                _dashPressed,
                Time.deltaTime);

            mFacingController.TickFacing(
                _horizontalVelocity,
                mMovementConfig.RotationSpeed,
                Time.deltaTime);

            mMotor.TickMovement(
                _horizontalVelocity,
                mMovementConfig.Gravity,
                mMovementConfig.MaximumFallSpeed,
                mMovementConfig.GroundedVerticalSpeed,
                Time.deltaTime);

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

            mInputSource = _inputSource;
            mGameplayCamera = _gameplayCamera;
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
            mActionStateMachine.SetEnabled(_shouldEnableControl);

            if (!_isEnabled && mMotor != null)
            {
                mMotor.ResetMotion();
            }
        }
    }
}
