using System.Collections.Concurrent;
using SECS2GEM.Domain.Events;
using SECS2GEM.Domain.Interfaces;

namespace SECS2GEM.Infrastructure.Services
{
    /// <summary>
    /// 事件聚合器实现
    /// </summary>
    /// <remarks>
    /// 观察者模式实现：
    /// 1. 使用ConcurrentDictionary按类型存储订阅者
    /// 2. 支持异步和同步事件处理
    /// 3. 返回IDisposable用于取消订阅
    /// 4. 异常隔离，一个订阅者异常不影响其他订阅者
    /// </remarks>
    public sealed class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<object>> _subscriptions = new();
        private readonly object _lock = new();

        /// <summary>
        /// 异步发布事件
        /// </summary>
        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IGemEvent
        {
            var handlers = GetHandlers<TEvent>();
            if (handlers.Count == 0) return;

            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                if (handler is Func<TEvent, Task> asyncHandler)
                {
                    tasks.Add(SafeInvokeAsync(asyncHandler, @event));
                }
                else if (handler is Action<TEvent> syncHandler)
                {
                    tasks.Add(Task.Run(() => SafeInvoke(syncHandler, @event)));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 同步发布事件
        /// </summary>
        public void Publish<TEvent>(TEvent @event) where TEvent : IGemEvent
        {
            var handlers = GetHandlers<TEvent>();
            if (handlers.Count == 0) return;

            foreach (var handler in handlers)
            {
                if (handler is Func<TEvent, Task> asyncHandler)
                {
                    // 对于异步处理器，启动任务但不等待
                    _ = SafeInvokeAsync(asyncHandler, @event);
                }
                else if (handler is Action<TEvent> syncHandler)
                {
                    SafeInvoke(syncHandler, @event);
                }
            }
        }

        /// <summary>
        /// 订阅事件（异步处理器）
        /// </summary>
        public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IGemEvent
        {
            return AddSubscription<TEvent>(handler);
        }

        /// <summary>
        /// 订阅事件（同步处理器）
        /// </summary>
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGemEvent
        {
            return AddSubscription<TEvent>(handler);
        }

        /// <summary>
        /// 清除指定类型的所有订阅
        /// </summary>
        public void ClearSubscriptions<TEvent>() where TEvent : IGemEvent
        {
            var type = typeof(TEvent);
            lock (_lock)
            {
                _subscriptions.TryRemove(type, out _);
            }
        }

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public void ClearAllSubscriptions()
        {
            lock (_lock)
            {
                _subscriptions.Clear();
            }
        }

        /// <summary>
        /// 添加订阅
        /// </summary>
        private IDisposable AddSubscription<TEvent>(object handler) where TEvent : IGemEvent
        {
            var type = typeof(TEvent);

            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(type, out var handlers))
                {
                    handlers = new List<object>();
                    _subscriptions[type] = handlers;
                }

                handlers.Add(handler);
            }

            return new Subscription(() => RemoveSubscription<TEvent>(handler));
        }

        /// <summary>
        /// 移除订阅
        /// </summary>
        private void RemoveSubscription<TEvent>(object handler) where TEvent : IGemEvent
        {
            var type = typeof(TEvent);

            lock (_lock)
            {
                if (_subscriptions.TryGetValue(type, out var handlers))
                {
                    handlers.Remove(handler);
                    if (handlers.Count == 0)
                    {
                        _subscriptions.TryRemove(type, out _);
                    }
                }
            }
        }

        /// <summary>
        /// 获取事件处理器列表（复制以避免并发修改）
        /// </summary>
        private List<object> GetHandlers<TEvent>() where TEvent : IGemEvent
        {
            var type = typeof(TEvent);

            lock (_lock)
            {
                if (_subscriptions.TryGetValue(type, out var handlers))
                {
                    return new List<object>(handlers);
                }
            }

            return new List<object>();
        }

        /// <summary>
        /// 安全调用异步处理器
        /// </summary>
        private async Task SafeInvokeAsync<TEvent>(Func<TEvent, Task> handler, TEvent @event)
        {
            try
            {
                await handler(@event);
            }
            catch (Exception)
            {
                // 记录日志但不抛出异常，避免影响其他订阅者
                // TODO: 添加日志记录
            }
        }

        /// <summary>
        /// 安全调用同步处理器
        /// </summary>
        private void SafeInvoke<TEvent>(Action<TEvent> handler, TEvent @event)
        {
            try
            {
                handler(@event);
            }
            catch (Exception)
            {
                // 记录日志但不抛出异常
                // TODO: 添加日志记录
            }
        }

        /// <summary>
        /// 订阅凭证
        /// </summary>
        private sealed class Subscription : IDisposable
        {
            private Action? _unsubscribe;

            public Subscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                _unsubscribe?.Invoke();
                _unsubscribe = null;
            }
        }
    }
}
