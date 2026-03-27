namespace SECS2GEM.Core.Exceptions
{
    /// <summary>
    /// 超时类型
    /// </summary>
    public enum TimeoutType
    {
        /// <summary>
        /// T3 - 回复超时
        /// 发送Primary消息后等待Secondary消息的超时
        /// </summary>
        T3Reply,

        /// <summary>
        /// T5 - 连接分离超时
        /// 连接断开后重新建立连接前的等待时间
        /// </summary>
        T5Separation,

        /// <summary>
        /// T6 - 控制事务超时
        /// 控制消息（Select/Deselect/Linktest）的响应超时
        /// </summary>
        T6Control,

        /// <summary>
        /// T7 - 未选择超时
        /// TCP连接建立后等待Select.req的超时
        /// </summary>
        T7NotSelected,

        /// <summary>
        /// T8 - 网络字符间隔超时
        /// 消息传输中字符之间的最大间隔
        /// </summary>
        T8Network,

        /// <summary>
        /// 连接超时
        /// TCP连接建立的超时
        /// </summary>
        Connect,

        /// <summary>
        /// 自定义超时
        /// </summary>
        Custom
    }

    /// <summary>
    /// SECS超时异常
    /// </summary>
    /// <remarks>
    /// 当操作超时时抛出。
    /// 包含超时类型和已等待时间信息。
    /// </remarks>
    public class SecsTimeoutException : SecsException
    {
        /// <summary>
        /// 超时类型
        /// </summary>
        public TimeoutType TimeoutType { get; }

        /// <summary>
        /// 已等待时间
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// 配置的超时时间
        /// </summary>
        public TimeSpan ConfiguredTimeout { get; }

        /// <summary>
        /// 相关的消息名称（如"S1F1"）
        /// </summary>
        public string? MessageName { get; }

        /// <summary>
        /// 事务ID
        /// </summary>
        public uint? SystemBytes { get; }

        /// <summary>
        /// 初始化SecsTimeoutException
        /// </summary>
        /// <param name="timeoutType">超时类型</param>
        /// <param name="elapsed">已等待时间</param>
        public SecsTimeoutException(TimeoutType timeoutType, TimeSpan elapsed)
            : base($"{timeoutType} timeout after {elapsed.TotalSeconds:F1}s")
        {
            TimeoutType = timeoutType;
            Elapsed = elapsed;
            ConfiguredTimeout = elapsed;
        }

        /// <summary>
        /// 初始化SecsTimeoutException
        /// </summary>
        /// <param name="timeoutType">超时类型</param>
        /// <param name="elapsed">已等待时间</param>
        /// <param name="configuredTimeout">配置的超时时间</param>
        public SecsTimeoutException(TimeoutType timeoutType, TimeSpan elapsed, TimeSpan configuredTimeout)
            : base($"{timeoutType} timeout after {elapsed.TotalSeconds:F1}s (configured: {configuredTimeout.TotalSeconds:F1}s)")
        {
            TimeoutType = timeoutType;
            Elapsed = elapsed;
            ConfiguredTimeout = configuredTimeout;
        }

        /// <summary>
        /// 初始化SecsTimeoutException（带消息信息）
        /// </summary>
        /// <param name="timeoutType">超时类型</param>
        /// <param name="elapsed">已等待时间</param>
        /// <param name="messageName">消息名称</param>
        /// <param name="systemBytes">事务ID</param>
        public SecsTimeoutException(TimeoutType timeoutType, TimeSpan elapsed, string messageName, uint systemBytes)
            : base($"{timeoutType} timeout for {messageName} (TxID={systemBytes}) after {elapsed.TotalSeconds:F1}s")
        {
            TimeoutType = timeoutType;
            Elapsed = elapsed;
            ConfiguredTimeout = elapsed;
            MessageName = messageName;
            SystemBytes = systemBytes;
        }

        /// <summary>
        /// 创建T3回复超时异常
        /// </summary>
        public static SecsTimeoutException T3Timeout(TimeSpan elapsed, string messageName, uint systemBytes)
        {
            return new SecsTimeoutException(TimeoutType.T3Reply, elapsed, messageName, systemBytes);
        }

        /// <summary>
        /// 创建T6控制超时异常
        /// </summary>
        public static SecsTimeoutException T6Timeout(TimeSpan elapsed, string controlMessage)
        {
            return new SecsTimeoutException(TimeoutType.T6Control, elapsed, controlMessage, 0);
        }

        /// <summary>
        /// 创建T7未选择超时异常
        /// </summary>
        public static SecsTimeoutException T7Timeout(TimeSpan elapsed)
        {
            return new SecsTimeoutException(TimeoutType.T7NotSelected, elapsed);
        }

        /// <summary>
        /// 创建连接超时异常
        /// </summary>
        public static SecsTimeoutException ConnectTimeout(TimeSpan elapsed, string endpoint)
        {
            var ex = new SecsTimeoutException(TimeoutType.Connect, elapsed);
            return ex;
        }
    }
}
