using SECS2GEM.Core.Entities;

namespace SECS2GEM.Infrastructure.Logging
{
    /// <summary>
    /// 消息方向
    /// </summary>
    public enum MessageDirection
    {
        /// <summary>
        /// 发送（设备→主机）
        /// </summary>
        Send,

        /// <summary>
        /// 接收（主机→设备）
        /// </summary>
        Receive
    }

    /// <summary>
    /// 消息记录器接口
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 使用策略模式分离日志记录的实现，便于扩展不同的存储方式（文件、数据库等）。
    /// 
    /// 好处：
    /// 1. 解耦：HsmsConnection不需要关心日志如何存储
    /// 2. 可扩展：可以轻松添加新的日志存储方式
    /// 3. 可测试：可以使用Mock进行单元测试
    /// </remarks>
    public interface IMessageLogger : IAsyncDisposable
    {
        /// <summary>
        /// 是否已启用
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 初始化日志器
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口</param>
        /// <param name="deviceId">设备ID</param>
        Task InitializeAsync(string ipAddress, int port, ushort deviceId);

        /// <summary>
        /// 记录HSMS消息
        /// </summary>
        /// <param name="message">HSMS消息</param>
        /// <param name="rawBytes">原始字节</param>
        /// <param name="direction">消息方向</param>
        Task LogMessageAsync(HsmsMessage message, byte[] rawBytes, MessageDirection direction);

        /// <summary>
        /// 记录原始字节（用于控制消息）
        /// </summary>
        /// <param name="rawBytes">原始字节</param>
        /// <param name="direction">消息方向</param>
        /// <param name="description">描述</param>
        Task LogRawBytesAsync(byte[] rawBytes, MessageDirection direction, string? description = null);

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        Task FlushAsync();
    }
}
