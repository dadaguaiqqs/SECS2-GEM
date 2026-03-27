namespace SECS2GEM.Domain.Events
{
    /// <summary>
    /// 采集事件报告事件
    /// </summary>
    /// <remarks>
    /// 当需要发送S6F11事件报告时触发。
    /// </remarks>
    public sealed class CollectionEventTriggeredEvent : GemEventBase
    {
        /// <summary>
        /// 数据ID (DATAID)
        /// </summary>
        public uint DataId { get; }

        /// <summary>
        /// 采集事件ID (CEID)
        /// </summary>
        public uint CollectionEventId { get; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// 关联的报告数据
        /// </summary>
        public IReadOnlyList<ReportData> Reports { get; }

        public CollectionEventTriggeredEvent(
            string source,
            uint dataId,
            uint ceid,
            string eventName,
            IReadOnlyList<ReportData> reports)
            : base(source)
        {
            DataId = dataId;
            CollectionEventId = ceid;
            EventName = eventName;
            Reports = reports;
        }

        public override string ToString()
        {
            return $"Event Triggered: CEID={CollectionEventId} ({EventName}), Reports={Reports.Count}";
        }
    }

    /// <summary>
    /// 报告数据
    /// </summary>
    public sealed class ReportData
    {
        /// <summary>
        /// 报告ID (RPTID)
        /// </summary>
        public uint ReportId { get; }

        /// <summary>
        /// 变量值列表
        /// </summary>
        public IReadOnlyList<VariableValue> Variables { get; }

        public ReportData(uint reportId, IReadOnlyList<VariableValue> variables)
        {
            ReportId = reportId;
            Variables = variables;
        }
    }

    /// <summary>
    /// 变量值
    /// </summary>
    public sealed class VariableValue
    {
        /// <summary>
        /// 变量ID (VID)
        /// </summary>
        public uint VariableId { get; }

        /// <summary>
        /// 变量名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 变量值
        /// </summary>
        public object? Value { get; }

        public VariableValue(uint variableId, string name, object? value)
        {
            VariableId = variableId;
            Name = name;
            Value = value;
        }
    }
}
