using UnityEngine;

namespace ProjectRelay.Input
{
    /// <summary>
    /// 为玩家控制器提供与具体输入设备无关的连续移动输入和一次性动作意图。
    /// 输入源只负责采集与缓存意图，不直接执行移动、交互或战斗逻辑。
    /// </summary>
    public interface IPlayerInputSource
    {
        /// <summary>
        /// 获取当前二维移动输入，返回值长度不会超过 1。
        /// </summary>
        Vector2 Move { get; }

        /// <summary>
        /// 获取当前输入源是否正在接收 Gameplay Action Map 的输入。
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 消费一次普通攻击意图；成功消费后，同一意图不能再次读取。
        /// </summary>
        /// <returns>存在尚未消费的攻击意图时返回 true。</returns>
        bool ConsumeAttackPressed();

        /// <summary>
        /// 消费一次已经满足 Input Action Interaction 条件的交互意图。
        /// </summary>
        /// <returns>存在尚未消费的交互意图时返回 true。</returns>
        bool ConsumeInteractPerformed();

        /// <summary>
        /// 消费一次冲刺意图；成功消费后，同一意图不能再次读取。
        /// </summary>
        /// <returns>存在尚未消费的冲刺意图时返回 true。</returns>
        bool ConsumeDashPressed();

        /// <summary>
        /// 设置 Gameplay Action Map 是否接收输入；禁用时同步清空已有输入缓存。
        /// </summary>
        /// <param name="_isEnabled">为 true 时启用输入，为 false 时禁用并清空输入。</param>
        void SetInputEnabled(bool _isEnabled);

        /// <summary>
        /// 清除连续移动值与所有尚未消费的一次性动作意图。
        /// </summary>
        void Clear();
    }
}
