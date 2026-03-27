using SECS2GEM.Domain.Events;

namespace SECS2GEM.Domain.Interfaces
{
    /// <summary>
    /// 事件聚合器接口
    /// </summary>
    /// <remarks>
    /// 观察者模式：解耦事件发布者和订阅者。
    /// 
    /// 设计思路：
    /// 1. 提供统一的事件发布入口
    /// 2. 支持多个订阅者
    /// 3. 支持异步事件处理
    /// 4. 返回IDisposable用于取消订阅
    /// 
    /// 使用场景：
    /// - 报警事件通知
    /// - 状态变化通知
    /// - 消息接收通知
    /// </remarks>
    public interface IEventAggregator
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="event">事件实例</param>
        /// <returns>发布完成的任务</returns>
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : IGemEvent;

        /// <summary>
        /// 同步发布事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="event">事件实例</param>
        void Publish<TEvent>(TEvent @event) where TEvent : IGemEvent;

        /// <summary>
        /// 订阅事件（异步处理）
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理函数</param>
        /// <returns>订阅凭证，用于取消订阅</returns>
        IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IGemEvent;

        /// <summary>
        /// 订阅事件（同步处理）
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理函数</param>
        /// <returns>订阅凭证，用于取消订阅</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGemEvent;

        /// <summary>
        /// 清除指定类型的所有订阅
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        void ClearSubscriptions<TEvent>() where TEvent : IGemEvent;

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        void ClearAllSubscriptions();
    }
}
