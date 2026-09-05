using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 根据玩家当前有效移动方向平滑旋转指定朝向节点，并保存最后一次有效朝向。
    /// 本组件不读取输入，也不决定玩家是否允许移动。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerFacingController : MonoBehaviour
    {
        private const float mMinimumFacingDirectionSqrMagnitude = 0.0001f;

        [SerializeField]
        [Tooltip("需要旋转的玩家朝向节点；为空时使用当前 GameObject。")]
        private Transform mFacingTransform;

        private Vector3 mLastFacingDirection;

        /// <summary>
        /// 获取最近一次有效的世界空间 XZ 平面朝向，供后续无输入冲刺等功能复用。
        /// </summary>
        public Vector3 LastFacingDirection => mLastFacingDirection;

        /// <summary>
        /// 缓存默认朝向节点，并用场景中已有旋转初始化最后有效朝向。
        /// </summary>
        private void Awake()
        {
            if (mFacingTransform == null)
            {
                mFacingTransform = transform;
            }

            Vector3 _initialDirection = mFacingTransform.forward;
            _initialDirection.y = 0.0f;

            mLastFacingDirection =
                _initialDirection.sqrMagnitude > mMinimumFacingDirectionSqrMagnitude
                    ? _initialDirection.normalized
                    : Vector3.forward;
        }

        /// <summary>
        /// 在方向有效时更新最后朝向并按最大角速度旋转；方向无效时保留当前朝向。
        /// </summary>
        /// <param name="_worldDirection">期望面向的世界空间方向，只使用 XZ 平面分量。</param>
        /// <param name="_rotationSpeed">每秒允许旋转的最大角度。</param>
        /// <param name="_deltaTime">当前玩法帧使用的时间增量。</param>
        public void TickFacing(Vector3 _worldDirection, float _rotationSpeed, float _deltaTime)
        {
            _worldDirection.y = 0.0f;

            if (_worldDirection.sqrMagnitude <= mMinimumFacingDirectionSqrMagnitude)
            {
                return;
            }

            mLastFacingDirection = _worldDirection.normalized;

            Quaternion _targetRotation = Quaternion.LookRotation(mLastFacingDirection, Vector3.up);
            float _maximumDegreesDelta = Mathf.Max(0.0f, _rotationSpeed) * Mathf.Max(0.0f, _deltaTime);

            mFacingTransform.rotation = Quaternion.RotateTowards(
                mFacingTransform.rotation,
                _targetRotation,
                _maximumDegreesDelta);
        }
    }
}
