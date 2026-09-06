using System;

namespace ProjectRelay.Core
{
    /// <summary>
    /// 表达可跨资源、存档和网络消息传递的稳定字符串标识。
    /// 该值不持有 Unity 对象引用，默认值表示无效标识。
    /// </summary>
    public readonly struct StableId : IEquatable<StableId>
    {
        private readonly string mValue;

        /// <summary>
        /// 获取空的无效标识。
        /// </summary>
        public static StableId None => default;

        /// <summary>
        /// 获取标识原始值；默认结构返回空字符串而不是 null。
        /// </summary>
        public string Value => mValue ?? string.Empty;

        /// <summary>
        /// 获取当前标识是否包含非空白内容。
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(mValue);

        /// <summary>
        /// 从持久化字符串创建稳定标识，不改变调用方提供的内容。
        /// </summary>
        /// <param name="_value">资源或协议中保存的稳定字符串。</param>
        public StableId(string _value)
        {
            mValue = _value ?? string.Empty;
        }

        /// <summary>
        /// 使用序号比较判断两个稳定标识是否相等。
        /// </summary>
        /// <param name="_other">需要比较的稳定标识。</param>
        /// <returns>原始字符串完全一致时返回 true。</returns>
        public bool Equals(StableId _other)
        {
            return string.Equals(Value, _other.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断对象是否为相等的稳定标识。
        /// </summary>
        /// <param name="_obj">需要比较的对象。</param>
        /// <returns>对象类型和值均匹配时返回 true。</returns>
        public override bool Equals(object _obj)
        {
            return _obj is StableId _other && Equals(_other);
        }

        /// <summary>
        /// 获取与序号字符串比较一致的哈希值。
        /// </summary>
        /// <returns>当前稳定标识的哈希值。</returns>
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        /// <summary>
        /// 返回用于日志和调试的原始标识字符串。
        /// </summary>
        /// <returns>原始标识字符串。</returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// 判断两个稳定标识是否相等。
        /// </summary>
        public static bool operator ==(StableId _left, StableId _right)
        {
            return _left.Equals(_right);
        }

        /// <summary>
        /// 判断两个稳定标识是否不同。
        /// </summary>
        public static bool operator !=(StableId _left, StableId _right)
        {
            return !_left.Equals(_right);
        }
    }
}
