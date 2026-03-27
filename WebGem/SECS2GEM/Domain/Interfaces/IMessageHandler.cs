using SECS2GEM.Core.Entities;

namespace SECS2GEM.Domain.Interfaces
{
    /// <summary>
    /// 消息上下文接口
    /// </summary>
    /// <remarks>
    /// 提供消息处理所需的上下文信息，包括：
    /// - 设备标识
    /// - 当前连接
    /// - 设备状态
    /// - 回复消息的能力
    /// </remarks>
    public interface IMessageContext
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        ushort DeviceId { get; }

        /// <summary>
        /// 当前连接
        /// </summary>
        ISecsConnection Connection { get; }

        /// <summary>
        /// GEM状态
        /// </summary>
        IGemState GemState { get; }

        /// <summary>
        /// 事务ID（System Bytes）
        /// </summary>
        uint SystemBytes { get; }

        /// <summary>
        /// 消息接收时间
        /// </summary>
        DateTime ReceivedTime { get; }

        /// <summary>
        /// 发送响应消息
        /// </summary>
        /// <param name="response">响应消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task ReplyAsync(SecsMessage response, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 消息处理器接口
    /// </summary>
    /// <remarks>
    /// 策略模式：每个Stream/Function组合可以有独立的处理器。
    /// 开闭原则：添加新消息处理只需实现此接口，无需修改现有代码。
    /// 
    /// 执行流程：
    /// 1. MessageDispatcher收到消息
    /// 2. 遍历所有Handler，调用CanHandle判断
    /// 3. 找到能处理的Handler，调用HandleAsync
    /// 4. 返回响应消息（如果需要）
    /// </remarks>
    public interface IMessageHandler
    {
        /// <summary>
        /// 处理器优先级（数值越小优先级越高）
        /// </summary>
        int Priority => 0;

        /// <summary>
        /// 是否能处理该消息
        /// </summary>
        /// <param name="message">待处理的消息</param>
        /// <returns>是否能处理</returns>
        bool CanHandle(SecsMessage message);

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="message">待处理的消息</param>
        /// <param name="context">消息上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应消息，如果不需要响应则返回null</returns>
        Task<SecsMessage?> HandleAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 消息分发器接口
    /// </summary>
    /// <remarks>
    /// 责任链模式 + 策略模式组合：
    /// 1. 维护处理器列表
    /// 2. 按优先级排序查找能处理消息的处理器
    /// 3. 委托给找到的处理器执行
    /// 
    /// 好处：
    /// - 处理器之间解耦
    /// - 支持动态添加/移除处理器
    /// - 支持处理器优先级
    /// </remarks>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <param name="handler">处理器实例</param>
        void RegisterHandler(IMessageHandler handler);

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        /// <param name="handler">处理器实例</param>
        void UnregisterHandler(IMessageHandler handler);

        /// <summary>
        /// 分发消息到对应的处理器
        /// </summary>
        /// <param name="message">待分发的消息</param>
        /// <param name="context">消息上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果（响应消息），如果没有处理器能处理则返回null</returns>
        Task<SecsMessage?> DispatchAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken = default);
    }
}
