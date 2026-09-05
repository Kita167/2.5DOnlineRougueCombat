using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 将玩家实际水平速度和冲刺状态写入 Animator 参数。
    /// 本组件只负责动画表现，不修改玩家移动或移动状态。
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationPresenter : MonoBehaviour
    {
        private static readonly int mSpeedParameterId = Animator.StringToHash("Speed");
        private static readonly int mIsDashingParameterId = Animator.StringToHash("IsDashing");

        [SerializeField]
        [Tooltip("接收 Speed 和 IsDashing 参数的玩家 Animator；为空时从子节点获取。")]
        private Animator mAnimator;

        [SerializeField]
        [Tooltip("提供实际移动速度和移动状态的玩家控制器；为空时从父节点获取。")]
        private PlayerController mPlayerController;

        /// <summary>
        /// 缓存 Animator 和玩家控制器。
        /// 没有 Animator 时安静停用，使尚未接入动画的玩家仍可正常控制。
        /// </summary>
        private void Awake()
        {
            if (mAnimator == null)
            {
                mAnimator = GetComponentInChildren<Animator>(true);
            }

            if (mAnimator == null)
            {
                enabled = false;
                return;
            }

            if (mPlayerController == null)
            {
                mPlayerController = GetComponentInParent<PlayerController>();
            }

            if (mPlayerController == null)
            {
                Debug.LogError(
                    "[Gameplay] PlayerAnimationPresenter 缺少 PlayerController 引用。",
                    this);
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// 在 PlayerController 完成当前帧更新后，把只读玩法状态提交给 Animator。
        /// </summary>
        private void Update()
        {
            if (mAnimator == null || mPlayerController == null)
            {
                return;
            }

            mAnimator.SetFloat(mSpeedParameterId, mPlayerController.NormalizedHorizontalSpeed);
            mAnimator.SetBool(
                mIsDashingParameterId,
                mPlayerController.CurrentActionState == PlayerActionState.Dashing);
        }
    }
}
