namespace SECS2GEM.Domain.Models
{
    /// <summary>
    /// 采集事件定义 (Collection Event)
    /// </summary>
    /// <remarks>
    /// 采集事件用于S6F11事件报告。
    /// 事件触发时，收集关联报告中的变量值并上报。
    /// </remarks>
    public sealed class CollectionEvent
    {
        /// <summary>
        /// 采集事件ID (CEID)
        /// </summary>
        public uint EventId { get; set; }

        /// <summary>
        /// 事件名称 (CENAME)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 事件描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 关联的报告ID列表 (RPTID)
        /// </summary>
        public List<uint> LinkedReportIds { get; set; } = new();
    }

    /// <summary>
    /// 报告定义 (Report)
    /// </summary>
    /// <remarks>
    /// 报告定义通过S2F33配置，包含要上报的变量列表。
    /// </remarks>
    public sealed class ReportDefinition
    {
        /// <summary>
        /// 报告ID (RPTID)
        /// </summary>
        public uint ReportId { get; set; }

        /// <summary>
        /// 报告名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 包含的变量ID列表 (VID)
        /// </summary>
        public List<uint> VariableIds { get; set; } = new();
    }

    /// <summary>
    /// 事件报告配置
    /// </summary>
    /// <remarks>
    /// 管理事件、报告、变量之间的关联关系。
    /// </remarks>
    public sealed class EventReportConfiguration
    {
        /// <summary>
        /// 所有采集事件
        /// </summary>
        public Dictionary<uint, CollectionEvent> Events { get; } = new();

        /// <summary>
        /// 所有报告定义
        /// </summary>
        public Dictionary<uint, ReportDefinition> Reports { get; } = new();

        /// <summary>
        /// 添加事件
        /// </summary>
        public void AddEvent(CollectionEvent @event)
        {
            Events[@event.EventId] = @event;
        }

        /// <summary>
        /// 添加报告
        /// </summary>
        public void AddReport(ReportDefinition report)
        {
            Reports[report.ReportId] = report;
        }

        /// <summary>
        /// 关联事件与报告
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="reportIds">报告ID列表</param>
        public bool LinkEventReport(uint eventId, IEnumerable<uint> reportIds)
        {
            if (!Events.TryGetValue(eventId, out var @event))
            {
                return false;
            }

            @event.LinkedReportIds.Clear();
            @event.LinkedReportIds.AddRange(reportIds);
            return true;
        }

        /// <summary>
        /// 启用/禁用事件
        /// </summary>
        public bool SetEventEnabled(uint eventId, bool enabled)
        {
            if (!Events.TryGetValue(eventId, out var @event))
            {
                return false;
            }

            @event.IsEnabled = enabled;
            return true;
        }

        /// <summary>
        /// 清除所有报告定义
        /// </summary>
        public void ClearAllReports()
        {
            Reports.Clear();
            foreach (var @event in Events.Values)
            {
                @event.LinkedReportIds.Clear();
            }
        }

        /// <summary>
        /// 删除指定报告
        /// </summary>
        public bool DeleteReport(uint reportId)
        {
            if (!Reports.Remove(reportId))
            {
                return false;
            }

            // 从所有事件中移除该报告的关联
            foreach (var @event in Events.Values)
            {
                @event.LinkedReportIds.Remove(reportId);
            }

            return true;
        }
    }
}
