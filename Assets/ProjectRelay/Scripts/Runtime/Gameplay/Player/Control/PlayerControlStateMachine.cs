using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 统一管理 Player 互斥控制状态的注册、逐帧推进和合法转移。
    /// 本类不读取设备输入、不执行位移，也不包含攻击、命中或表现规则。
    /// </summary>
    public sealed class PlayerControlStateMachine
    {
        private const int mMaximumTransitionsPerTick = 4;

        private readonly Dictionary<PlayerControlStateId, PlayerControlState> mStates;
        private readonly PlayerControlContext mContext;

        private PlayerControlState mCurrentState;

        /// <summary>获取当前唯一激活的 Player 控制状态标识。</summary>
        public PlayerControlStateId CurrentStateId => mCurrentState.Id;

        /// <summary>获取当前状态为最近一次 Tick 生成的控制输出。</summary>
        public PlayerControlOutput CurrentOutput => mContext.CurrentOutput;

        /// <summary>获取控制状态机是否已经离开 Disabled。</summary>
        public bool IsEnabled => CurrentStateId != PlayerControlStateId.Disabled;

        /// <summary>获取最近一次完整重置后累计的安全运行时间。</summary>
        public float ElapsedTime => mContext.ElapsedTime;

        /// <summary>
        /// 在状态切换已经完成后发布不可变转移结果，供表现和调试代码只读观察。
        /// </summary>
        public event Action<PlayerControlTransition> StateChanged;

        /// <summary>
        /// 使用移动配置创建完整控制状态机；基础攻击桥接器为空时攻击请求会被安全拒绝。
        /// </summary>
        /// <param name="_movementConfig">普通移动输出使用的只读配置。</param>
        /// <exception cref="ArgumentNullException">移动配置为空时抛出。</exception>
        public PlayerControlStateMachine(PlayerMovementConfig _movementConfig)
            : this(_movementConfig, null)
        {
        }

        /// <summary>
        /// 使用移动配置和基础攻击桥接器创建完整控制状态机，并从 Disabled 开始运行。
        /// </summary>
        /// <param name="_movementConfig">普通移动和 Dash 输出使用的只读配置。</param>
        /// <param name="_attackDriver">提交并观察基础攻击运行时的桥接器。</param>
        /// <exception cref="ArgumentNullException">移动配置为空时抛出。</exception>
        public PlayerControlStateMachine(
            PlayerMovementConfig _movementConfig,
            PlayerBasicAttackDriver _attackDriver)
        {
            mContext = new PlayerControlContext(
                _movementConfig ?? throw new ArgumentNullException(nameof(_movementConfig)),
                _attackDriver);
            mStates = new Dictionary<PlayerControlStateId, PlayerControlState>(5);

            RegisterState(new PlayerDisabledState());
            RegisterState(new PlayerIdleState());
            RegisterState(new PlayerMoveState());
            RegisterState(new PlayerAttackState());
            RegisterState(new PlayerDashState());

            mCurrentState = mStates[PlayerControlStateId.Disabled];

            PlayerControlTransition _initialTransition = new PlayerControlTransition(
                PlayerControlStateId.Disabled,
                PlayerControlStateId.Disabled,
                PlayerControlTransitionReason.Initialize,
                false,
                Vector3.zero);
            mCurrentState.Enter(mContext, _initialTransition);
            mContext.SetOutput(
                mCurrentState.CreateOutput(mContext, default));
        }

        /// <summary>
        /// 推进当前状态、处理同帧连续转移，并生成可直接交给 Facing 和 Motor 的输出。
        /// </summary>
        /// <param name="_input">本帧已经转换到世界空间的不可变控制输入。</param>
        /// <param name="_deltaTime">当前玩法帧时间；非法或负值按零处理。</param>
        /// <returns>当前状态为本帧生成的最终控制输出。</returns>
        public PlayerControlOutput Tick(
            in PlayerControlInput _input,
            float _deltaTime)
        {
            float _safeDeltaTime = GetSafeDeltaTime(_deltaTime);
            mContext.SetInput(_input);

            if (IsEnabled)
            {
                mContext.AdvanceTime(_safeDeltaTime);
                mContext.AttackDriver?.Tick(_safeDeltaTime);
            }

            int _transitionCount = 0;
            PlayerControlInput _stateInput = _input;

            while (_transitionCount < mMaximumTransitionsPerTick)
            {
                PlayerControlTransitionRequest _request =
                    mCurrentState.Tick(mContext, _stateInput, _safeDeltaTime);

                if (!_request.HasRequest)
                {
                    break;
                }

                if (!TryChangeState(_request, false))
                {
                    break;
                }

                _transitionCount++;
                _stateInput = new PlayerControlInput(
                    _input.MoveDirection,
                    _input.FacingDirection,
                    false,
                    false);
            }

            if (_transitionCount >= mMaximumTransitionsPerTick)
            {
                Debug.LogError(
                    "[Gameplay] Player 控制状态单帧转移超过安全上限，已强制重置。");
                ForceReset();
            }

            PlayerControlOutput _output =
                mCurrentState.CreateOutput(mContext, _input);
            mContext.SetOutput(_output);
            return _output;
        }

        /// <summary>
        /// 设置状态机是否接受控制；启用时从 Disabled 进入 Idle，禁用时完整重置。
        /// </summary>
        /// <param name="_isEnabled">是否允许 Player 控制状态运行。</param>
        public void SetEnabled(bool _isEnabled)
        {
            if (!_isEnabled)
            {
                ResetToDisabled(PlayerControlTransitionReason.Disable);
                return;
            }

            if (CurrentStateId != PlayerControlStateId.Disabled)
            {
                return;
            }

            PlayerControlTransitionRequest _request =
                PlayerControlTransitionRequest.Create(
                    PlayerControlStateId.Idle,
                    PlayerControlTransitionReason.Enable);
            TryChangeState(_request, false);
            mContext.SetOutput(
                mCurrentState.CreateOutput(mContext, default));
        }

        /// <summary>
        /// 强制经过当前状态 Exit 进入 Disabled，并清除累计时间与本帧输出。
        /// </summary>
        public void ForceReset()
        {
            ResetToDisabled(PlayerControlTransitionReason.ForceReset);
        }

        /// <summary>
        /// 将 Motor 完成位移后的实际结果转交给当前状态处理。
        /// </summary>
        /// <param name="_movementResult">本帧不可变移动结果。</param>
        public void ReportMovementResult(in PlayerMovementResult _movementResult)
        {
            PlayerControlTransitionRequest _request =
                mCurrentState.ReportMovementResult(mContext, _movementResult);

            if (TryChangeState(_request, false))
            {
                mContext.SetOutput(
                    mCurrentState.CreateOutput(
                        mContext,
                        mContext.CurrentInput));
            }
        }

        /// <summary>
        /// 查询当前状态是否允许进入指定目标，供调试和测试读取转移图。
        /// </summary>
        /// <param name="_targetStateId">需要查询的目标状态。</param>
        /// <returns>目标已注册且转移图允许时返回 true。</returns>
        public bool CanTransitionTo(PlayerControlStateId _targetStateId)
        {
            return
                mStates.ContainsKey(_targetStateId) &&
                IsTransitionAllowed(CurrentStateId, _targetStateId);
        }

        /// <summary>
        /// 注册一个唯一状态实例；重复 ID 表示状态机构造错误并立即抛出。
        /// </summary>
        /// <param name="_state">需要加入本状态机的状态实例。</param>
        private void RegisterState(PlayerControlState _state)
        {
            if (_state == null)
            {
                throw new ArgumentNullException(nameof(_state));
            }

            mStates.Add(_state.Id, _state);
        }

        /// <summary>
        /// 校验请求并以固定 Exit、写入、Enter、通知顺序执行唯一状态切换。
        /// </summary>
        /// <param name="_request">当前状态返回的不可变转移请求。</param>
        /// <param name="_force">是否仅跳过普通合法边校验。</param>
        /// <returns>目标存在且实际完成切换时返回 true。</returns>
        private bool TryChangeState(
            in PlayerControlTransitionRequest _request,
            bool _force)
        {
            if (
                !_request.HasRequest ||
                _request.TargetStateId == CurrentStateId ||
                !mStates.TryGetValue(
                    _request.TargetStateId,
                    out PlayerControlState _nextState) ||
                (!_force &&
                    !IsTransitionAllowed(CurrentStateId, _request.TargetStateId)))
            {
                return false;
            }

            PlayerControlTransition _transition = new PlayerControlTransition(
                CurrentStateId,
                _request.TargetStateId,
                _request.Reason,
                _request.HasDirection,
                _request.Direction);

            mCurrentState.Exit(mContext, _transition);
            mCurrentState = _nextState;
            mCurrentState.Enter(mContext, _transition);
            StateChanged?.Invoke(_transition);
            return true;
        }

        /// <summary>
        /// 从任意已启用状态进入 Disabled，并在完成 Exit 后清除共享运行时数据。
        /// </summary>
        /// <param name="_reason">Disable 或 ForceReset 原因。</param>
        private void ResetToDisabled(PlayerControlTransitionReason _reason)
        {
            if (CurrentStateId != PlayerControlStateId.Disabled)
            {
                PlayerControlTransitionRequest _request =
                    PlayerControlTransitionRequest.Create(
                        PlayerControlStateId.Disabled,
                        _reason);
                TryChangeState(_request, true);
            }
            else
            {
                mContext.ResetRuntime();
            }

            mContext.SetOutput(
                mCurrentState.CreateOutput(mContext, default));
        }

        /// <summary>
        /// 定义完整 Player 控制状态机允许的显式转移边。
        /// </summary>
        private static bool IsTransitionAllowed(
            PlayerControlStateId _from,
            PlayerControlStateId _to)
        {
            if (_to == PlayerControlStateId.Disabled)
            {
                return _from != PlayerControlStateId.Disabled;
            }

            switch (_from)
            {
                case PlayerControlStateId.Disabled:
                    return _to == PlayerControlStateId.Idle;

                case PlayerControlStateId.Idle:
                    return
                        _to == PlayerControlStateId.Move ||
                        _to == PlayerControlStateId.Attack ||
                        _to == PlayerControlStateId.Dash;

                case PlayerControlStateId.Move:
                    return
                        _to == PlayerControlStateId.Idle ||
                        _to == PlayerControlStateId.Attack ||
                        _to == PlayerControlStateId.Dash;

                case PlayerControlStateId.Attack:
                case PlayerControlStateId.Dash:
                    return
                        _to == PlayerControlStateId.Idle ||
                        _to == PlayerControlStateId.Move;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 将非法或负帧时间转换为零，避免运行时累计值传播 NaN 或无穷。
        /// </summary>
        private static float GetSafeDeltaTime(float _deltaTime)
        {
            return float.IsNaN(_deltaTime) || float.IsInfinity(_deltaTime)
                ? 0.0f
                : Mathf.Max(0.0f, _deltaTime);
        }
    }
}
