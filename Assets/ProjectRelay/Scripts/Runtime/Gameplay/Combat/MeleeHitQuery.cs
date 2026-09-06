using System;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 使用固定复用缓冲区执行球形近战范围查询，并向权威攻击执行层暴露候选 Collider。
    /// 本类不判断阵营、死亡、自身层级或重复目标，也不应用伤害。
    /// </summary>
    public sealed class MeleeHitQuery
    {
        private readonly Collider[] mColliderBuffer;

        /// <summary>获取本查询实例可在单次查询中保存的候选数量。</summary>
        public int Capacity => mColliderBuffer.Length;

        /// <summary>获取最近一次查询写入缓冲区的候选数量。</summary>
        public int CandidateCount { get; private set; }

        /// <summary>
        /// 使用固定正容量创建可重复使用的非分配查询实例。
        /// </summary>
        /// <param name="_capacity">单次查询最多保留的候选 Collider 数量。</param>
        /// <exception cref="ArgumentOutOfRangeException">容量小于一时抛出。</exception>
        public MeleeHitQuery(int _capacity)
        {
            if (_capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(_capacity));
            }

            mColliderBuffer = new Collider[_capacity];
        }

        /// <summary>
        /// 在指定中心执行一次非分配球形查询，并保存结果数量供调用方顺序读取。
        /// </summary>
        /// <param name="_center">世界空间球形查询中心。</param>
        /// <param name="_radius">有限正查询半径。</param>
        /// <param name="_targetLayerMask">允许进入候选集合的物理层。</param>
        /// <returns>本次写入固定缓冲区的候选数量；参数非法时返回零。</returns>
        public int Query(
            Vector3 _center,
            float _radius,
            LayerMask _targetLayerMask)
        {
            if (!IsFinite(_center) || !IsFinitePositive(_radius))
            {
                CandidateCount = 0;
                return 0;
            }

            CandidateCount = Physics.OverlapSphereNonAlloc(
                _center,
                _radius,
                mColliderBuffer,
                _targetLayerMask,
                QueryTriggerInteraction.Collide);
            return CandidateCount;
        }

        /// <summary>
        /// 获取最近一次查询中的指定候选；越界位置返回 null。
        /// </summary>
        /// <param name="_index">从零开始且小于 CandidateCount 的候选位置。</param>
        /// <returns>对应 Collider，或索引越界时返回 null。</returns>
        public Collider GetCandidate(int _index)
        {
            return _index >= 0 && _index < CandidateCount
                ? mColliderBuffer[_index]
                : null;
        }

        /// <summary>
        /// 检查世界空间向量的三个分量是否全部有限。
        /// </summary>
        /// <param name="_value">需要验证的向量。</param>
        /// <returns>不存在 NaN 或无穷分量时返回 true。</returns>
        private static bool IsFinite(Vector3 _value)
        {
            return
                !float.IsNaN(_value.x) &&
                !float.IsInfinity(_value.x) &&
                !float.IsNaN(_value.y) &&
                !float.IsInfinity(_value.y) &&
                !float.IsNaN(_value.z) &&
                !float.IsInfinity(_value.z);
        }

        /// <summary>
        /// 检查浮点值是否有限且大于零。
        /// </summary>
        /// <param name="_value">需要验证的数值。</param>
        /// <returns>值可以作为球形查询半径时返回 true。</returns>
        private static bool IsFinitePositive(float _value)
        {
            return !float.IsNaN(_value) && !float.IsInfinity(_value) && _value > 0.0f;
        }
    }
}
