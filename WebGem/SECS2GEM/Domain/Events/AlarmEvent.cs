using SECS2GEM.Core.Enums;

namespace SECS2GEM.Domain.Events
{
    /// <summary>
    /// 报警事件
    /// </summary>
    /// <remarks>
    /// 当设备发生报警或报警清除时触发。
    /// 对应S5F1消息。
    /// </remarks>
    public sealed class AlarmEvent : GemEventBase
    {
        /// <summary>
        /// 报警ID (ALID)
        /// </summary>
        public uint AlarmId { get; }

        /// <summary>
        /// 报警码 (ALCD)
        /// </summary>
        /// <remarks>
        /// bit 7: 1=Set, 0=Clear
        /// bit 0-6: 报警类别
        /// </remarks>
        public byte AlarmCode { get; }

        /// <summary>
        /// 报警文本 (ALTX)
        /// </summary>
        public string AlarmText { get; }

        /// <summary>
        /// 是否为报警触发（true）或报警清除（false）
        /// </summary>
        public bool IsSet => (AlarmCode & 0x80) != 0;

        /// <summary>
        /// 报警类别
        /// </summary>
        public AlarmCategory Category => (AlarmCategory)(AlarmCode & 0x7F);

        public AlarmEvent(string source, uint alarmId, byte alarmCode, string alarmText)
            : base(source)
        {
            AlarmId = alarmId;
            AlarmCode = alarmCode;
            AlarmText = alarmText;
        }

        public override string ToString()
        {
            return $"Alarm {(IsSet ? "SET" : "CLEAR")}: ID={AlarmId}, Category={Category}, Text={AlarmText}";
        }
    }
}
