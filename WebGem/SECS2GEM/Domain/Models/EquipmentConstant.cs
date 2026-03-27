using SECS2GEM.Core.Enums;

namespace SECS2GEM.Domain.Models
{
    /// <summary>
    /// 设备常量定义 (Equipment Constant)
    /// </summary>
    /// <remarks>
    /// 设备常量用于配置设备的参数。
    /// 通过S2F13/S2F14查询，通过S2F15/S2F16设置。
    /// </remarks>
    public sealed class EquipmentConstant
    {
        /// <summary>
        /// 设备常量ID (ECID)
        /// </summary>
        public uint ConstantId { get; set; }

        /// <summary>
        /// 常量名称 (ECNAME)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 常量单位
        /// </summary>
        public string Units { get; set; } = string.Empty;

        /// <summary>
        /// 数据格式
        /// </summary>
        public SecsFormat Format { get; set; } = SecsFormat.ASCII;

        /// <summary>
        /// 当前值
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 最小值（可选，用于数值类型）
        /// </summary>
        public object? MinValue { get; set; }

        /// <summary>
        /// 最大值（可选，用于数值类型）
        /// </summary>
        public object? MaxValue { get; set; }

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 值变化时的回调
        /// </summary>
        public Action<object?, object?>? OnValueChanged { get; set; }

        /// <summary>
        /// 获取当前值
        /// </summary>
        public object? GetValue()
        {
            return Value ?? DefaultValue;
        }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <returns>是否设置成功</returns>
        public bool TrySetValue(object? newValue)
        {
            if (IsReadOnly)
            {
                return false;
            }

            // 如果有范围限制，进行验证
            if (MinValue != null || MaxValue != null)
            {
                if (!ValidateRange(newValue))
                {
                    return false;
                }
            }

            var oldValue = Value;
            Value = newValue;
            OnValueChanged?.Invoke(oldValue, newValue);
            return true;
        }

        private bool ValidateRange(object? value)
        {
            if (value == null) return true;

            try
            {
                var comparable = value as IComparable;
                if (comparable == null) return true;

                if (MinValue != null && comparable.CompareTo(MinValue) < 0)
                    return false;

                if (MaxValue != null && comparable.CompareTo(MaxValue) > 0)
                    return false;

                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}
