using System;

namespace STTech.BytesIO.Mqtt
{
    /// <summary>
    /// MQTT v5 用户自定义属性键值对。
    /// </summary>
    public sealed class MqttUserProperty : IEquatable<MqttUserProperty>
    {
        /// <summary>
        /// 属性名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 属性值。
        /// </summary>
        public string Value { get; }

        public MqttUserProperty(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool Equals(MqttUserProperty other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                   string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as MqttUserProperty);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? StringComparer.Ordinal.GetHashCode(Name) : 0) * 397) ^
                       (Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0);
            }
        }

        public override string ToString() => $"{Name}: {Value}";
    }
}
