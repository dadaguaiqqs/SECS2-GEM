namespace SECS2GEM.Domain.Events
{
    /// <summary>
    /// GEM事件基接口
    /// </summary>
    /// <remarks>
    /// 所有GEM相关事件的基接口。
    /// 用于事件聚合器的类型约束。
    /// </remarks>
    public interface IGemEvent
    {
        /// <summary>
        /// 事件发生时间
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// 事件源标识
        /// </summary>
        string Source { get; }
    }

    /// <summary>
    /// GEM事件基类
    /// </summary>
    public abstract class GemEventBase : IGemEvent
    {
        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 事件源标识
        /// </summary>
        public string Source { get; }

        protected GemEventBase(string source)
        {
            Timestamp = DateTime.UtcNow;
            Source = source;
        }

        protected GemEventBase(string source, DateTime timestamp)
        {
            Timestamp = timestamp;
            Source = source;
        }
    }
}
