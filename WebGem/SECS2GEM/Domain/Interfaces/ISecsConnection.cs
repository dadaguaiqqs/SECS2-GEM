using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;

namespace SECS2GEM.Domain.Interfaces
{
    /// <summary>
    /// 连接状态变化事件参数
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧状态
        /// </summary>
        public ConnectionState OldState { get; }

        /// <summary>
        /// 新状态
        /// </summary>
        public ConnectionState NewState { get; }

        /// <summary>
        /// 状态变化原因
        /// </summary>
        public string? Reason { get; }

        public ConnectionStateChangedEventArgs(ConnectionState oldState, ConnectionState newState, string? reason = null)
        {
            OldState = oldState;
            NewState = newState;
            Reason = reason;
        }
    }

    /// <summary>
    /// SECS消息接收事件参数
    /// </summary>
    public class SecsMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的消息
        /// </summary>
        public SecsMessage Message { get; }

        /// <summary>
        /// 消息上下文
        /// </summary>
        public IMessageContext Context { get; }

        public SecsMessageReceivedEventArgs(SecsMessage message, IMessageContext context)
        {
            Message = message;
            Context = context;
        }
    }

    /// <summary>
    /// SECS连接接口
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 1. 定义连接的生命周期管理（连接、断开、状态）
    /// 2. 支持异步消息发送和接收
    /// 3. 通过事件通知状态变化和消息到达
    /// 
    /// 执行流程（作为Passive端）：
    /// 1. 调用StartListeningAsync开始监听
    /// 2. 收到TCP连接后触发StateChanged → Connected
    /// 3. 收到Select.req并响应后 → Selected
    /// 4. 可以开始SendAsync/ReceiveAsync
    /// </remarks>
    public interface ISecsConnection : IAsyncDisposable
    {
        /// <summary>
        /// 连接状态
        /// </summary>
        ConnectionState State { get; }

        /// <summary>
        /// 是否已连接并选择
        /// </summary>
        bool IsSelected { get; }

        /// <summary>
        /// 会话ID（设备ID）
        /// </summary>
        ushort SessionId { get; }

        /// <summary>
        /// 远程端点信息
        /// </summary>
        string? RemoteEndpoint { get; }

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

        /// <summary>
        /// 收到Primary消息事件（需要处理并响应的消息）
        /// </summary>
        event EventHandler<SecsMessageReceivedEventArgs>? PrimaryMessageReceived;

        /// <summary>
        /// 建立连接（Active模式）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 开始监听（Passive模式）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task StartListeningAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送消息并等待回复
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应消息，如果不期望回复则返回null</returns>
        Task<SecsMessage?> SendAsync(SecsMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送消息（不等待回复）
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SendOnlyAsync(SecsMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送Linktest心跳
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> SendLinktestAsync(CancellationToken cancellationToken = default);
    }
}
