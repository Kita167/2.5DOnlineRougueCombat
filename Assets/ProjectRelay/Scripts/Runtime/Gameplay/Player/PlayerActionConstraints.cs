using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存当前玩家动作状态对移动、朝向、冲刺和攻击施加的只读约束。
    /// 本值只描述动作仲裁结果，不包含伤害、命中范围或攻击阶段数据。
    /// </summary>
    public readonly struct PlayerActionConstraints
    {
        /// <summary>获取普通移动速度需要乘以的倍率。</summary>
        public float MovementSpeedMultiplier { get; }

        /// <summary>获取当前状态是否允许根据移动输入改变朝向。</summary>
        public bool CanTurn { get; }

        /// <summary>获取当前状态是否允许尝试进入冲刺。</summary>
        public bool CanDash { get; }

        /// <summary>获取当前状态是否允许尝试开始攻击。</summary>
        public bool CanAttack { get; }

        /// <summary>获取当前状态是否提供必须保持的世界空间平面朝向。</summary>
        public bool HasLockedFacingDirection { get; }

        /// <summary>获取冲刺或攻击开始时锁定的世界空间平面朝向。</summary>
        public Vector3 LockedFacingDirection { get; }

        /// <summary>
        /// 创建由 PlayerActionStateMachine 输出的一组动作约束。
        /// </summary>
        /// <param name="_movementSpeedMultiplier">普通移动速度倍率。</param>
        /// <param name="_canTurn">是否允许跟随移动输入转向。</param>
        /// <param name="_canDash">是否允许尝试冲刺。</param>
        /// <param name="_canAttack">是否允许尝试攻击。</param>
        /// <param name="_hasLockedFacingDirection">是否存在锁定朝向。</param>
        /// <param name="_lockedFacingDirection">已经归一化的世界空间平面朝向。</param>
        internal PlayerActionConstraints(
            float _movementSpeedMultiplier,
            bool _canTurn,
            bool _canDash,
            bool _canAttack,
            bool _hasLockedFacingDirection,
            Vector3 _lockedFacingDirection)
        {
            MovementSpeedMultiplier = _movementSpeedMultiplier;
            CanTurn = _canTurn;
            CanDash = _canDash;
            CanAttack = _canAttack;
            HasLockedFacingDirection = _hasLockedFacingDirection;
            LockedFacingDirection = _lockedFacingDirection;
        }
    }
}
