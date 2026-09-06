using ProjectRelay.Core;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 保存玩家提交一次普通攻击命令所需的不可变值类型数据。
    /// 请求不持有目标、场景对象或表现引用，便于后续通过网络边界传递和校验。
    /// </summary>
    public readonly struct BasicAttackRequest
    {
        private const float mMinimumDirectionSqrMagnitude = 0.0001f;

        /// <summary>获取提交命令的战斗单位运行时身份。</summary>
        public CombatantId SourceId { get; }

        /// <summary>获取请求使用的稳定攻击定义标识。</summary>
        public StableId AttackId { get; }

        /// <summary>获取请求提交时的世界空间攻击方向。</summary>
        public Vector3 AttackDirection { get; }

        /// <summary>获取本地玩家在当前运行期间递增的非零请求序号。</summary>
        public ulong RequestSequence { get; }

        /// <summary>获取攻击方向投影到 XZ 平面后是否为有限有效方向。</summary>
        public bool HasValidDirection => IsValidPlanarDirection(AttackDirection);

        /// <summary>获取请求的身份、攻击标识、方向和序号是否全部有效。</summary>
        public bool IsValid =>
            SourceId.IsValid &&
            AttackId.IsValid &&
            HasValidDirection &&
            RequestSequence != 0UL;

        /// <summary>
        /// 创建一份不依赖 GameObject 的普通攻击命令请求。
        /// 参数合法性由 Command Gateway 统一验证并返回明确拒绝原因。
        /// </summary>
        /// <param name="_sourceId">提交请求的战斗单位运行时身份。</param>
        /// <param name="_attackId">请求使用的稳定攻击定义标识。</param>
        /// <param name="_attackDirection">请求提交时的世界空间攻击方向。</param>
        /// <param name="_requestSequence">本地运行期间递增的非零请求序号。</param>
        public BasicAttackRequest(
            CombatantId _sourceId,
            StableId _attackId,
            Vector3 _attackDirection,
            ulong _requestSequence)
        {
            SourceId = _sourceId;
            AttackId = _attackId;
            AttackDirection = _attackDirection;
            RequestSequence = _requestSequence;
        }

        /// <summary>
        /// 验证方向的 XZ 分量是否有限且足以形成单位方向。
        /// </summary>
        /// <param name="_direction">需要验证的世界空间方向。</param>
        /// <returns>方向可以安全归一化时返回 true。</returns>
        private static bool IsValidPlanarDirection(Vector3 _direction)
        {
            if (
                float.IsNaN(_direction.x) ||
                float.IsInfinity(_direction.x) ||
                float.IsNaN(_direction.z) ||
                float.IsInfinity(_direction.z))
            {
                return false;
            }

            _direction.y = 0.0f;
            return _direction.sqrMagnitude > mMinimumDirectionSqrMagnitude;
        }
    }
}
