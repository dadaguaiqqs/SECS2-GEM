using SECS2GEM.Core.Entities;
using SECS2GEM.Domain.Interfaces;

namespace SECS2GEM.Application.Messaging
{
    /// <summary>
    /// 消息分发器
    /// </summary>
    /// <remarks>
    /// 责任链 + 策略模式组合：
    /// 1. 维护处理器列表，按优先级排序
    /// 2. 收到消息时遍历处理器，找到能处理的
    /// 3. 委托给处理器执行
    /// 
    /// 好处：
    /// - 处理器之间解耦，互不干扰
    /// - 支持动态注册/注销处理器
    /// - 按优先级匹配，支持覆盖默认行为
    /// 
    /// 执行流程：
    /// 1. DispatchAsync收到Primary消息
    /// 2. 按Priority排序遍历所有Handler
    /// 3. 调用CanHandle找到第一个能处理的Handler
    /// 4. 调用HandleAsync获取响应
    /// 5. 如果没有Handler能处理，返回S9F7错误
    /// </remarks>
    public sealed class MessageDispatcher : IMessageDispatcher
    {
        private readonly List<IMessageHandler> _handlers = new();
        private readonly object _lock = new();
        private bool _sorted = false;

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <param name="handler">处理器实例</param>
        public void RegisterHandler(IMessageHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            lock (_lock)
            {
                _handlers.Add(handler);
                _sorted = false;
            }
        }

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        /// <param name="handler">处理器实例</param>
        public void UnregisterHandler(IMessageHandler handler)
        {
            lock (_lock)
            {
                _handlers.Remove(handler);
            }
        }

        /// <summary>
        /// 分发消息到对应的处理器
        /// </summary>
        /// <param name="message">待分发的消息</param>
        /// <param name="context">消息上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果（响应消息），如果没有处理器能处理则返回null</returns>
        public async Task<SecsMessage?> DispatchAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken = default)
        {
            var handlers = GetSortedHandlers();

            foreach (var handler in handlers)
            {
                if (handler.CanHandle(message))
                {
                    var response = await handler.HandleAsync(message, context, cancellationToken);
                    return response;
                }
            }

            // 没有处理器能处理该消息
            // 返回S9F7 (Illegal Data) 或 null
            if (message.WBit)
            {
                return CreateS9F7Response(message);
            }

            return null;
        }

        /// <summary>
        /// 获取排序后的处理器列表
        /// </summary>
        private List<IMessageHandler> GetSortedHandlers()
        {
            lock (_lock)
            {
                if (!_sorted)
                {
                    _handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                    _sorted = true;
                }

                return new List<IMessageHandler>(_handlers);
            }
        }

        /// <summary>
        /// 创建S9F7响应（非法数据）
        /// </summary>
        private static SecsMessage CreateS9F7Response(SecsMessage request)
        {
            // S9F7包含原始消息的Header
            return new SecsMessage(9, 7, false, SecsItem.B(
                (byte)request.Stream,
                (byte)request.Function
            ));
        }
    }
}
