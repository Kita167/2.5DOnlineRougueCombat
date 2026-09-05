using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 保存玩家普通移动、旋转、垂直运动和冲刺使用的只读设计参数。
    /// 运行时状态不得写回该资源。
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerMovement_Default",
        menuName = "Project Relay/Gameplay/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Horizontal Movement")]
        [SerializeField]
        [Min(0.0f)]
        [Tooltip("玩家在地面进行普通移动时的水平速度，单位为米每秒。")]
        private float mMoveSpeed = 5.0f;

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("玩家朝目标方向旋转时允许的最大角速度，单位为度每秒。")]
        private float mRotationSpeed = 720.0f;

        [Header("Vertical Movement")]
        [SerializeField]
        [Tooltip("玩家离开地面后使用的向下加速度，必须小于或等于零。")]
        private float mGravity = -25.0f;

        [SerializeField]
        [Tooltip("玩家允许达到的最大向下速度，必须小于或等于零。")]
        private float mMaximumFallSpeed = -40.0f;

        [SerializeField]
        [Tooltip("玩家接地时保留的小幅向下速度，用于维持 CharacterController 贴地。")]
        private float mGroundedVerticalSpeed = -2.0f;

        [Header("Dash")]
        [SerializeField]
        [Min(0.0f)]
        [Tooltip("冲刺期间使用的水平速度，单位为米每秒。")]
        private float mDashSpeed = 12.0f;

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("一次冲刺保持的时间，单位为秒。")]
        private float mDashDuration = 0.18f;

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("冲刺结束后再次允许冲刺前需要等待的时间，单位为秒。")]
        private float mDashCooldown = 0.80f;

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("冲刺输入在暂时不能执行时允许保留的时间，单位为秒。")]
        private float mDashInputBuffer = 0.10f;

        [SerializeField]
        [Tooltip("开启后，冲刺受到明显侧面阻挡时允许提前结束。")]
        private bool mEndDashWhenBlocked = true;

        /// <summary>
        /// 获取玩家普通移动的水平速度。
        /// </summary>
        public float MoveSpeed => mMoveSpeed;

        /// <summary>
        /// 获取玩家朝目标方向旋转时使用的最大角速度。
        /// </summary>
        public float RotationSpeed => mRotationSpeed;

        /// <summary>
        /// 获取玩家离地后使用的向下加速度。
        /// </summary>
        public float Gravity => mGravity;

        /// <summary>
        /// 获取玩家允许达到的最大向下速度。
        /// </summary>
        public float MaximumFallSpeed => mMaximumFallSpeed;

        /// <summary>
        /// 获取玩家接地时用于保持贴地的垂直速度。
        /// </summary>
        public float GroundedVerticalSpeed => mGroundedVerticalSpeed;

        /// <summary>
        /// 获取冲刺期间使用的水平速度。
        /// </summary>
        public float DashSpeed => mDashSpeed;

        /// <summary>
        /// 获取一次冲刺的持续时间。
        /// </summary>
        public float DashDuration => mDashDuration;

        /// <summary>
        /// 获取冲刺结束后再次允许冲刺前的等待时间。
        /// </summary>
        public float DashCooldown => mDashCooldown;

        /// <summary>
        /// 获取暂时无法执行冲刺时保留冲刺意图的时间。
        /// </summary>
        public float DashInputBuffer => mDashInputBuffer;

        /// <summary>
        /// 获取冲刺受到明显侧面阻挡时是否允许提前结束。
        /// </summary>
        public bool EndDashWhenBlocked => mEndDashWhenBlocked;

        /// <summary>
        /// 在编辑器修改资源时约束参数范围，避免产生向上重力或无效速度。
        /// </summary>
        private void OnValidate()
        {
            mMoveSpeed = Mathf.Max(0.0f, mMoveSpeed);
            mRotationSpeed = Mathf.Max(0.0f, mRotationSpeed);
            mGravity = Mathf.Min(0.0f, mGravity);
            mMaximumFallSpeed = Mathf.Min(0.0f, mMaximumFallSpeed);
            mGroundedVerticalSpeed = Mathf.Clamp(mGroundedVerticalSpeed, mMaximumFallSpeed, 0.0f);
            mDashSpeed = Mathf.Max(0.0f, mDashSpeed);
            mDashDuration = Mathf.Max(0.0f, mDashDuration);
            mDashCooldown = Mathf.Max(0.0f, mDashCooldown);
            mDashInputBuffer = Mathf.Max(0.0f, mDashInputBuffer);
        }
    }
}
