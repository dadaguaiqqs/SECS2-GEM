namespace SECS2GEM.Core.Exceptions
{
    /// <summary>
    /// 通信错误类型
    /// </summary>
    public enum CommunicationError
    {
        /// <summary>
        /// 连接失败
        /// TCP连接无法建立
        /// </summary>
        ConnectionFailed,

        /// <summary>
        /// 连接丢失
        /// TCP连接意外断开
        /// </summary>
        ConnectionLost,

        /// <summary>
        /// 选择失败
        /// HSMS Select请求被拒绝
        /// </summary>
        SelectFailed,

        /// <summary>
        /// 未选择
        /// 尝试在未建立HSMS会话时发送数据
        /// </summary>
        NotSelected,

        /// <summary>
        /// 取消选择失败
        /// HSMS Deselect请求失败
        /// </summary>
        DeselectFailed,

        /// <summary>
        /// 发送失败
        /// 消息发送过程中发生错误
        /// </summary>
        SendFailed,

        /// <summary>
        /// 接收失败
        /// 消息接收过程中发生错误
        /// </summary>
        ReceiveFailed,

        /// <summary>
        /// 链路测试失败
        /// Linktest请求未收到响应
        /// </summary>
        LinktestFailed
    }

    /// <summary>
    /// SECS通信异常
    /// </summary>
    /// <remarks>
    /// 当发生通信相关错误时抛出。
    /// 包括连接、发送、接收等操作的失败。
    /// </remarks>
    public class SecsCommunicationException : SecsException
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        public CommunicationError ErrorType { get; }

        /// <summary>
        /// 远程端点信息（如IP:Port）
        /// </summary>
        public string? RemoteEndpoint { get; }

        /// <summary>
        /// 初始化SecsCommunicationException
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="message">错误消息</param>
        public SecsCommunicationException(CommunicationError errorType, string message)
            : base(message)
        {
            ErrorType = errorType;
        }

        /// <summary>
        /// 初始化SecsCommunicationException
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="message">错误消息</param>
        /// <param name="remoteEndpoint">远程端点</param>
        public SecsCommunicationException(CommunicationError errorType, string message, string remoteEndpoint)
            : base(message)
        {
            ErrorType = errorType;
            RemoteEndpoint = remoteEndpoint;
        }

        /// <summary>
        /// 初始化SecsCommunicationException
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public SecsCommunicationException(CommunicationError errorType, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorType = errorType;
        }

        /// <summary>
        /// 创建连接失败异常
        /// </summary>
        public static SecsCommunicationException ConnectionFailed(string endpoint, Exception? inner = null)
        {
            var message = $"Failed to connect to {endpoint}.";
            return inner != null
                ? new SecsCommunicationException(CommunicationError.ConnectionFailed, message, inner)
                : new SecsCommunicationException(CommunicationError.ConnectionFailed, message, endpoint);
        }

        /// <summary>
        /// 创建连接丢失异常
        /// </summary>
        public static SecsCommunicationException ConnectionLost(string? endpoint = null)
        {
            var message = endpoint != null
                ? $"Connection to {endpoint} was lost."
                : "Connection was lost.";
            return new SecsCommunicationException(CommunicationError.ConnectionLost, message);
        }

        /// <summary>
        /// 创建选择失败异常
        /// </summary>
        public static SecsCommunicationException SelectFailed(byte status)
        {
            var message = $"Select request failed with status {status}.";
            return new SecsCommunicationException(CommunicationError.SelectFailed, message);
        }

        /// <summary>
        /// 创建未选择异常
        /// </summary>
        public static SecsCommunicationException NotSelected()
        {
            return new SecsCommunicationException(
                CommunicationError.NotSelected,
                "Cannot send message: HSMS session not established.");
        }
    }
}
