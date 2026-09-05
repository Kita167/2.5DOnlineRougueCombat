using UnityEngine;

namespace ProjectRelay.Presentation.Camera
{
    /// <summary>
    /// 在玩家完成当前帧移动后平滑跟随已绑定目标。
    /// 本组件只修改 CameraRig 的位置，不查找玩家，也不控制相机旋转。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("CameraRig 相对跟随目标的世界空间位置偏移。")]
        private Vector3 mOffset = new Vector3(0.0f, 10.0f, -8.0f);

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("CameraRig 到达目标位置使用的平滑时间；设为 0 时立即跟随。")]
        private float mSmoothTime = 0.08f;

        private Transform mTarget;
        private Vector3 mFollowVelocity;

        /// <summary>
        /// 在目标完成 Update 移动后更新 CameraRig，避免相机比玩家更早更新导致一帧抖动。
        /// </summary>
        private void LateUpdate()
        {
            if (mTarget == null)
            {
                mFollowVelocity = Vector3.zero;
                return;
            }

            Vector3 _targetPosition = mTarget.position + mOffset;

            if (mSmoothTime <= Mathf.Epsilon)
            {
                transform.position = _targetPosition;
                mFollowVelocity = Vector3.zero;
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                _targetPosition,
                ref mFollowVelocity,
                mSmoothTime);
        }

        /// <summary>
        /// 在编辑器修改参数时限制平滑时间，避免产生无效负值。
        /// </summary>
        private void OnValidate()
        {
            mSmoothTime = Mathf.Max(0.0f, mSmoothTime);
        }

        /// <summary>
        /// 绑定唯一跟随目标，并立即对齐 CameraRig，避免首次进入场景时从旧位置滑入。
        /// </summary>
        /// <param name="_target">需要跟随的玩家根节点。</param>
        /// <returns>目标有效并完成绑定时返回 true。</returns>
        public bool Bind(Transform _target)
        {
            if (_target == null)
            {
                Debug.LogError("[Gameplay] TopDownCameraController 绑定失败：跟随目标为空。", this);
                return false;
            }

            mTarget = _target;
            mFollowVelocity = Vector3.zero;
            transform.position = mTarget.position + mOffset;
            return true;
        }

        /// <summary>
        /// 解除当前跟随目标并清除平滑速度；重复调用不会产生副作用。
        /// </summary>
        public void Unbind()
        {
            mTarget = null;
            mFollowVelocity = Vector3.zero;
        }
    }
}
