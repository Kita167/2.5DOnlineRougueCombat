using ProjectRelay.Core;
using UnityEngine;

namespace ProjectRelay.Gameplay.Combat
{
    /// <summary>
    /// 将场景中的战斗单位映射到值类型运行时身份和阵营。
    /// 本组件不判断敌我关系，也不持有生命、攻击或表现状态。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatantIdentity : MonoBehaviour
    {
        private static ulong mNextLocalId;

        [SerializeField]
        [Tooltip("该战斗单位在本局规则中的阵营。")]
        private Faction mFaction = Faction.Neutral;

        private CombatantId mId;

        /// <summary>
        /// 获取本战斗单位的单局运行时身份。
        /// </summary>
        public CombatantId Id => mId;

        /// <summary>
        /// 获取本战斗单位当前配置的阵营。
        /// </summary>
        public Faction Faction => mFaction;

        /// <summary>
        /// 在场景组合根装配前为本地对象分配临时运行时身份。
        /// 网络接入后可由 Initialize 在首个战斗命令前替换为权威身份。
        /// </summary>
        private void Awake()
        {
            if (!mId.IsValid)
            {
                mId = CreateLocalId();
            }
        }

        /// <summary>
        /// 在对象参与战斗前设置由场景或网络权威分配的身份和阵营。
        /// </summary>
        /// <param name="_id">非零的单局运行时身份。</param>
        /// <param name="_faction">非 None 的权威阵营。</param>
        /// <returns>参数有效并成功写入时返回 true。</returns>
        public bool Initialize(CombatantId _id, Faction _faction)
        {
            if (!_id.IsValid || _faction == Faction.None)
            {
                Debug.LogError(
                    "[Combat] CombatantIdentity 初始化失败：身份必须非零且阵营不能为 None。",
                    this);
                return false;
            }

            mId = _id;
            mFaction = _faction;
            return true;
        }

        /// <summary>
        /// 在编辑器修改组件时阻止未配置阵营进入运行时 Prefab。
        /// </summary>
        private void OnValidate()
        {
            if (mFaction == Faction.None)
            {
                mFaction = Faction.Neutral;
            }
        }

        /// <summary>
        /// 为尚无权威身份的本地对象生成进程内唯一的非零身份。
        /// </summary>
        /// <returns>本次运行期间单调递增的身份。</returns>
        private static CombatantId CreateLocalId()
        {
            unchecked
            {
                mNextLocalId++;

                if (mNextLocalId == 0UL)
                {
                    mNextLocalId++;
                }
            }

            return new CombatantId(mNextLocalId);
        }
    }
}
