using SECS2GEM.Core.Enums;

namespace SECS2GEM.Domain.Models
{
    /// <summary>
    /// 报警信息（用于发送S5F1）
    /// </summary>
    public sealed class AlarmInfo
    {
        /// <summary>
        /// 报警ID (ALID)
        /// </summary>
        public uint AlarmId { get; set; }

        /// <summary>
        /// 报警文本 (ALTX)
        /// </summary>
        public string AlarmText { get; set; } = string.Empty;

        /// <summary>
        /// 是否为报警触发（true=Set, false=Clear）
        /// </summary>
        public bool IsSet { get; set; } = true;

        /// <summary>
        /// 报警类别
        /// </summary>
        public AlarmCategory Category { get; set; } = AlarmCategory.Unknown;

        /// <summary>
        /// 获取报警码 (ALCD)
        /// </summary>
        /// <remarks>
        /// bit 7: 1=Set, 0=Clear
        /// bit 0-6: 报警类别
        /// </remarks>
        public byte AlarmCode => (byte)((IsSet ? 0x80 : 0x00) | ((byte)Category & 0x7F));

        /// <summary>
        /// 报警发生时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 报警定义（设备支持的报警配置）
    /// </summary>
    public sealed class AlarmDefinition
    {
        /// <summary>
        /// 报警ID (ALID)
        /// </summary>
        public uint AlarmId { get; set; }

        /// <summary>
        /// 报警名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 报警描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 报警类别
        /// </summary>
        public AlarmCategory Category { get; set; } = AlarmCategory.Unknown;

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 关联的事件ID（报警触发时发送的事件）
        /// </summary>
        public uint? AssociatedEventId { get; set; }
    }
}
