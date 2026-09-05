using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 根据玩家当前有效移动或冲刺方向平滑旋转指定朝向节点，并提供节点的实际当前朝向。
    /// 本组件不读取输入，也不决定玩家是否允许移动。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerFacingController : MonoBehaviour
    {
        private const float mMinimumFacingDirectionSqrMagnitude = 0.0001f;

        [SerializeField]
        [Tooltip("需要旋转的玩家朝向节点；为空时使用当前 GameObject。")]
        private Transform mFacingTransform;

        /// <summary>
        /// 获取朝向节点经过插值旋转后的实际世界空间 XZ 方向，供无移动输入时的冲刺使用。
        /// </summary>
        public Vector3 CurrentFacingDirection
        {
            get
            {
                Transform _facingTransform = mFacingTransform != null
                    ? mFacingTransform
                    : transform;
                Vector3 _facingDirection = _facingTransform.forward;
                _facingDirection.y = 0.0f;

                return _facingDirection.sqrMagnitude > mMinimumFacingDirectionSqrMagnitude
                    ? _facingDirection.normalized
                    : Vector3.forward;
            }
        }

        /// <summary>
        /// 缓存默认朝向节点，使朝向读取和旋转始终作用于同一个 Transform。
        /// </summary>
        private void Awake()
        {
            if (mFacingTransform == null)
            {
                mFacingTransform = transform;
            }
        }

        /// <summary>
        /// 在方向有效时按最大角速度旋转朝向节点；方向无效时保留节点的实际当前朝向。
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

            Vector3 _targetDirection = _worldDirection.normalized;
            Quaternion _targetRotation = Quaternion.LookRotation(_targetDirection, Vector3.up);
            float _maximumDegreesDelta = Mathf.Max(0.0f, _rotationSpeed) * Mathf.Max(0.0f, _deltaTime);

            mFacingTransform.rotation = Quaternion.RotateTowards(
                mFacingTransform.rotation,
                _targetRotation,
                _maximumDegreesDelta);
        }
    }
}
