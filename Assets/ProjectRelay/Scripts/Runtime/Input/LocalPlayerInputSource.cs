using ProjectRelay.Input.Generated;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectRelay.Input
{
    /// <summary>
    /// 使用 Input System 生成类采集本地玩家输入，并将输入转换为可轮询、可消费的玩法意图。
    /// 本组件拥有生成类实例及 Player Action Map 的启停和事件订阅生命周期。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayerInputSource : MonoBehaviour, IPlayerInputSource
    {
        private ProjectRelayInputActions mInputActions;
        private Vector2 mMove;
        private bool mIsInputRequested = true;
        private bool mIsSubscribed;
        private bool mAttackPressed;
        private bool mInteractPerformed;
        private bool mDashPressed;

        /// <summary>
        /// 获取当前二维移动输入，返回值长度不会超过 1。
        /// </summary>
        public Vector2 Move => mMove;

        /// <summary>
        /// 获取 Player Action Map 当前是否处于启用状态。
        /// </summary>
        public bool IsEnabled => mInputActions != null && mInputActions.Gameplay.enabled;

        /// <summary>
        /// 创建由本组件独占的 Input Actions 运行时实例。
        /// </summary>
        private void Awake()
        {
            mInputActions = new ProjectRelayInputActions();
        }

        /// <summary>
        /// 订阅 Player Action 回调，并按外部请求恢复输入启用状态。
        /// </summary>
        private void OnEnable()
        {
            SubscribeCallbacks();

            if (mIsInputRequested)
            {
                mInputActions.Gameplay.Enable();
            }
        }

        /// <summary>
        /// 停止接收输入、清除缓存并退订回调，防止对象重复启用后重复响应。
        /// </summary>
        private void OnDisable()
        {
            if (mInputActions != null)
            {
                mInputActions.Gameplay.Disable();
            }

            Clear();
            UnsubscribeCallbacks();
        }

        /// <summary>
        /// 释放由生成类创建的 InputActionAsset 运行时实例。
        /// </summary>
        private void OnDestroy()
        {
            if (mInputActions == null)
            {
                return;
            }

            UnsubscribeCallbacks();
            mInputActions.Dispose();
            mInputActions = null;
        }

        /// <summary>
        /// 消费一次普通攻击意图；成功消费后，同一意图不能再次读取。
        /// </summary>
        /// <returns>存在尚未消费的攻击意图时返回 true。</returns>
        public bool ConsumeAttackPressed()
        {
            bool _wasPressed = mAttackPressed && IsEnabled;
            mAttackPressed = false;
            return _wasPressed;
        }

        /// <summary>
        /// 消费一次已经满足 Input Action Interaction 条件的交互意图。
        /// </summary>
        /// <returns>存在尚未消费的交互意图时返回 true。</returns>
        public bool ConsumeInteractPerformed()
        {
            bool _wasPerformed = mInteractPerformed && IsEnabled;
            mInteractPerformed = false;
            return _wasPerformed;
        }

        /// <summary>
        /// 消费一次冲刺意图；成功消费后，同一意图不能再次读取。
        /// </summary>
        /// <returns>存在尚未消费的冲刺意图时返回 true。</returns>
        public bool ConsumeDashPressed()
        {
            bool _wasPressed = mDashPressed && IsEnabled;
            mDashPressed = false;
            return _wasPressed;
        }

        /// <summary>
        /// 设置 Player Action Map 是否接收输入；禁用时同步清空已有输入缓存。
        /// </summary>
        /// <param name="_isEnabled">为 true 时启用输入，为 false 时禁用并清空输入。</param>
        public void SetInputEnabled(bool _isEnabled)
        {
            mIsInputRequested = _isEnabled;

            if (mInputActions == null || !isActiveAndEnabled)
            {
                if (!_isEnabled)
                {
                    Clear();
                }

                return;
            }

            if (_isEnabled)
            {
                mInputActions.Gameplay.Enable();
                return;
            }

            mInputActions.Gameplay.Disable();
            Clear();
        }

        /// <summary>
        /// 清除连续移动值与所有尚未消费的一次性动作意图。
        /// </summary>
        public void Clear()
        {
            mMove = Vector2.zero;
            mAttackPressed = false;
            mInteractPerformed = false;
            mDashPressed = false;
        }

        /// <summary>
        /// 注册当前输入模块使用的 Action 回调；重复调用不会重复注册。
        /// </summary>
        private void SubscribeCallbacks()
        {
            if (mIsSubscribed || mInputActions == null)
            {
                return;
            }

            mInputActions.Gameplay.Move.performed += HandleMovePerformed;
            mInputActions.Gameplay.Move.canceled += HandleMoveCanceled;
            mInputActions.Gameplay.Attack.performed += HandleAttackPerformed;
            mInputActions.Gameplay.Interact.performed += HandleInteractPerformed;
            mInputActions.Gameplay.Dash.performed += HandleDashPerformed;
            mIsSubscribed = true;
        }

        /// <summary>
        /// 解除当前输入模块注册的全部 Action 回调；未订阅时安全返回。
        /// </summary>
        private void UnsubscribeCallbacks()
        {
            if (!mIsSubscribed || mInputActions == null)
            {
                return;
            }

            mInputActions.Gameplay.Move.performed -= HandleMovePerformed;
            mInputActions.Gameplay.Move.canceled -= HandleMoveCanceled;
            mInputActions.Gameplay.Attack.performed -= HandleAttackPerformed;
            mInputActions.Gameplay.Interact.performed -= HandleInteractPerformed;
            mInputActions.Gameplay.Dash.performed -= HandleDashPerformed;
            mIsSubscribed = false;
        }

        /// <summary>
        /// 缓存 Move Action 当前产生的二维输入，并限制斜向输入长度不超过 1。
        /// </summary>
        /// <param name="_context">Input System 提供的 Move Action 回调上下文。</param>
        private void HandleMovePerformed(InputAction.CallbackContext _context)
        {
            if (!IsEnabled)
            {
                return;
            }

            Vector2 _move = _context.ReadValue<Vector2>();
            mMove = Vector2.ClampMagnitude(_move, 1.0f);
        }

        /// <summary>
        /// 在 Move Action 取消时清除移动输入，避免松键后角色继续移动。
        /// </summary>
        /// <param name="_context">Input System 提供的 Move Action 回调上下文。</param>
        private void HandleMoveCanceled(InputAction.CallbackContext _context)
        {
            mMove = Vector2.zero;
        }

        /// <summary>
        /// 将一次 Attack performed 回调记录为待消费的普通攻击意图。
        /// </summary>
        /// <param name="_context">Input System 提供的 Attack Action 回调上下文。</param>
        private void HandleAttackPerformed(InputAction.CallbackContext _context)
        {
            if (IsEnabled)
            {
                mAttackPressed = true;
            }
        }

        /// <summary>
        /// 将一次满足 Hold 等 Interaction 条件的 Interact 回调记录为待消费意图。
        /// </summary>
        /// <param name="_context">Input System 提供的 Interact Action 回调上下文。</param>
        private void HandleInteractPerformed(InputAction.CallbackContext _context)
        {
            if (IsEnabled)
            {
                mInteractPerformed = true;
            }
        }

        /// <summary>
        /// 将一次 Dash performed 回调记录为待消费的冲刺意图。
        /// </summary>
        /// <param name="_context">Input System 提供的 Dash Action 回调上下文。</param>
        private void HandleDashPerformed(InputAction.CallbackContext _context)
        {
            if (IsEnabled)
            {
                mDashPressed = true;
            }
        }
    }
}
