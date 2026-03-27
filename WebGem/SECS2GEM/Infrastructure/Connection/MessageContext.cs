using SECS2GEM.Core.Entities;
using SECS2GEM.Domain.Interfaces;

namespace SECS2GEM.Infrastructure.Connection
{
    /// <summary>
    /// 消息上下文实现
    /// </summary>
    /// <remarks>
    /// 用于在处理Primary消息时提供发送回复的能力
    /// </remarks>
    internal sealed class MessageContext : IMessageContext
    {
        private readonly Func<SecsMessage, uint, CancellationToken, Task> _replyFunc;

        /// <summary>
        /// System Bytes
        /// </summary>
        public uint SystemBytes { get; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public ushort DeviceId { get; }

        /// <summary>
        /// 当前连接
        /// </summary>
        public ISecsConnection Connection { get; }

        /// <summary>
        /// GEM状态
        /// </summary>
        public IGemState GemState { get; }

        /// <summary>
        /// 消息接收时间
        /// </summary>
        public DateTime ReceivedTime { get; }

        public MessageContext(
            uint systemBytes,
            ushort deviceId,
            ISecsConnection connection,
            IGemState gemState,
            Func<SecsMessage, uint, CancellationToken, Task> replyFunc)
        {
            SystemBytes = systemBytes;
            DeviceId = deviceId;
            Connection = connection;
            GemState = gemState;
            _replyFunc = replyFunc;
            ReceivedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 发送回复消息
        /// </summary>
        public async Task ReplyAsync(SecsMessage reply, CancellationToken cancellationToken = default)
        {
            await _replyFunc(reply, SystemBytes, cancellationToken);
        }
    }
}
