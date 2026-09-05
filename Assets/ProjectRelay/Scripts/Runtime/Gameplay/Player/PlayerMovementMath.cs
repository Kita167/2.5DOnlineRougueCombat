using UnityEngine;

namespace ProjectRelay.Gameplay.Player
{
    /// <summary>
    /// 提供玩家移动方向相关的无状态计算，使输入坐标转换可以脱离场景对象进行测试。
    /// </summary>
    public static class PlayerMovementMath
    {
        private const float mMinimumDirectionSqrMagnitude = 0.0001f;

        /// <summary>
        /// 将二维输入转换为相机相对的 XZ 世界方向，并将最终长度限制在 1 以内。
        /// </summary>
        /// <param name="_input">输入源提供的二维移动值。</param>
        /// <param name="_cameraForward">Gameplay Camera 的世界空间前方向。</param>
        /// <param name="_cameraRight">Gameplay Camera 的世界空间右方向。</param>
        /// <returns>相机相对的归一化世界移动方向。</returns>
        public static Vector3 GetCameraRelativeDirection(
            Vector2 _input,
            Vector3 _cameraForward,
            Vector3 _cameraRight)
        {
            Vector2 _clampedInput = Vector2.ClampMagnitude(_input, 1.0f);
            Vector3 _flatForward = Vector3.ProjectOnPlane(_cameraForward, Vector3.up);

            if (_flatForward.sqrMagnitude < mMinimumDirectionSqrMagnitude)
            {
                _flatForward = Vector3.forward;
            }

            _flatForward.Normalize();

            Vector3 _flatRight = Vector3.ProjectOnPlane(_cameraRight, Vector3.up);

            if (_flatRight.sqrMagnitude < mMinimumDirectionSqrMagnitude)
            {
                _flatRight = Vector3.Cross(Vector3.up, _flatForward);
            }

            _flatRight.Normalize();

            Vector3 _worldDirection =
                _flatForward * _clampedInput.y +
                _flatRight * _clampedInput.x;

            return Vector3.ClampMagnitude(_worldDirection, 1.0f);
        }
    }
}
