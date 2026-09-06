using System;

namespace ProjectRelay.Core
{
    /// <summary>
    /// 表达单局运行期间战斗单位的唯一身份。
    /// 该值不等同于 Prefab 或配置 ID，零值表示尚未分配身份。
    /// </summary>
    public readonly struct CombatantId : IEquatable<CombatantId>
    {
        /// <summary>
        /// 获取未分配的运行时身份。
        /// </summary>
        public static CombatantId None => default;

        /// <summary>
        /// 获取单局范围内的无符号身份值。
        /// </summary>
        public ulong Value { get; }

        /// <summary>
        /// 获取当前身份是否已分配。
        /// </summary>
        public bool IsValid => Value != 0UL;

        /// <summary>
        /// 使用非零运行时数值创建战斗单位身份。
        /// </summary>
        /// <param name="_value">单局范围内分配的身份值。</param>
        public CombatantId(ulong _value)
        {
            Value = _value;
        }

        /// <summary>
        /// 判断两个战斗单位身份是否相等。
        /// </summary>
        /// <param name="_other">需要比较的身份。</param>
        /// <returns>数值相同时返回 true。</returns>
        public bool Equals(CombatantId _other)
        {
            return Value == _other.Value;
        }

        /// <summary>
        /// 判断对象是否为相等的战斗单位身份。
        /// </summary>
        /// <param name="_obj">需要比较的对象。</param>
        /// <returns>对象类型和值均匹配时返回 true。</returns>
        public override bool Equals(object _obj)
        {
            return _obj is CombatantId _other && Equals(_other);
        }

        /// <summary>
        /// 获取身份数值对应的哈希值。
        /// </summary>
        /// <returns>当前身份的哈希值。</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// 返回用于日志和调试的身份数值。
        /// </summary>
        /// <returns>身份数值的十进制字符串。</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// 判断两个战斗单位身份是否相等。
        /// </summary>
        public static bool operator ==(CombatantId _left, CombatantId _right)
        {
            return _left.Equals(_right);
        }

        /// <summary>
        /// 判断两个战斗单位身份是否不同。
        /// </summary>
        public static bool operator !=(CombatantId _left, CombatantId _right)
        {
            return !_left.Equals(_right);
        }
    }
}
