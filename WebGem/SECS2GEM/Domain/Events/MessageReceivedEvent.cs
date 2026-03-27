using SECS2GEM.Core.Entities;

namespace SECS2GEM.Domain.Events
{
    /// <summary>
    /// 消息接收事件
    /// </summary>
    /// <remarks>
    /// 当收到SECS消息时触发。
    /// 可用于日志记录、消息拦截等。
    /// </remarks>
    public sealed class MessageReceivedEvent : GemEventBase
    {
        /// <summary>
        /// 接收到的消息
        /// </summary>
        public SecsMessage Message { get; }

        /// <summary>
        /// 消息方向
        /// </summary>
        public MessageDirection Direction { get; }

        /// <summary>
        /// 事务ID
        /// </summary>
        public uint SystemBytes { get; }

        /// <summary>
        /// 远程端点
        /// </summary>
        public string? RemoteEndpoint { get; }

        public MessageReceivedEvent(
            string source,
            SecsMessage message,
            MessageDirection direction,
            uint systemBytes,
            string? remoteEndpoint = null)
            : base(source)
        {
            Message = message;
            Direction = direction;
            SystemBytes = systemBytes;
            RemoteEndpoint = remoteEndpoint;
        }

        public override string ToString()
        {
            var arrow = Direction == MessageDirection.Received ? "←" : "→";
            return $"{arrow} {Message.Name} [TxID={SystemBytes}]";
        }
    }

    /// <summary>
    /// 消息方向
    /// </summary>
    public enum MessageDirection
    {
        /// <summary>接收</summary>
        Received,

        /// <summary>发送</summary>
        Sent
    }
}
