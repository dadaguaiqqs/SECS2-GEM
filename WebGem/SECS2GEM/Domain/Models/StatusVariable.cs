using SECS2GEM.Core.Enums;

namespace SECS2GEM.Domain.Models
{
    /// <summary>
    /// 状态变量定义 (Status Variable)
    /// </summary>
    /// <remarks>
    /// 状态变量用于表示设备的实时状态信息。
    /// 通过S1F3/S1F4查询，通过S6F11事件报告上报。
    /// </remarks>
    public sealed class StatusVariable
    {
        /// <summary>
        /// 状态变量ID (SVID)
        /// </summary>
        public uint VariableId { get; set; }

        /// <summary>
        /// 变量名称 (SVNAME)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 变量单位
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
        /// 值获取器（可选，用于动态获取值）
        /// </summary>
        public Func<object?>? ValueGetter { get; set; }

        /// <summary>
        /// 获取当前值
        /// </summary>
        public object? GetValue()
        {
            return ValueGetter != null ? ValueGetter() : Value;
        }

        /// <summary>
        /// 设置当前值
        /// </summary>
        public void SetValue(object? value)
        {
            Value = value;
        }
    }
}
