using SECS2GEM.Core.Entities;

namespace SECS2GEM.Domain.Interfaces
{
    /// <summary>
    /// 事务接口
    /// </summary>
    /// <remarks>
    /// 表示一个请求-响应的事务。
    /// 用于跟踪消息发送后等待响应的状态。
    /// </remarks>
    public interface ITransaction : IDisposable
    {
        /// <summary>
        /// 事务ID (System Bytes)
        /// </summary>
        uint SystemBytes { get; }

        /// <summary>
        /// 关联的消息名称
        /// </summary>
        string MessageName { get; }

        /// <summary>
        /// 事务创建时间
        /// </summary>
        DateTime CreatedTime { get; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 是否已超时
        /// </summary>
        bool IsTimedOut { get; }

        /// <summary>
        /// 等待响应
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应消息</returns>
        Task<SecsMessage?> WaitForResponseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置响应（当收到匹配的响应时调用）
        /// </summary>
        /// <param name="response">响应消息</param>
        void SetResponse(SecsMessage response);

        /// <summary>
        /// 设置超时
        /// </summary>
        void SetTimedOut();

        /// <summary>
        /// 取消事务
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// 事务管理器接口
    /// </summary>
    /// <remarks>
    /// 职责：
    /// 1. 生成唯一的事务ID（System Bytes）
    /// 2. 跟踪待响应的事务
    /// 3. 匹配响应消息与对应的请求
    /// 4. 处理超时
    /// 
    /// 设计思路：
    /// - 使用ConcurrentDictionary存储活跃事务
    /// - 通过System Bytes进行匹配
    /// - 支持超时自动清理
    /// </remarks>
    public interface ITransactionManager
    {
        /// <summary>
        /// 当前活跃的事务数量
        /// </summary>
        int ActiveTransactionCount { get; }

        /// <summary>
        /// 获取下一个事务ID
        /// </summary>
        /// <returns>唯一的事务ID</returns>
        uint GetNextTransactionId();

        /// <summary>
        /// 开始新事务
        /// </summary>
        /// <param name="systemBytes">事务ID</param>
        /// <param name="messageName">消息名称（用于日志和调试）</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>事务对象</returns>
        ITransaction BeginTransaction(uint systemBytes, string messageName, TimeSpan timeout);

        /// <summary>
        /// 尝试完成事务（收到响应时调用）
        /// </summary>
        /// <param name="systemBytes">事务ID</param>
        /// <param name="response">响应消息</param>
        /// <returns>是否找到并完成了对应的事务</returns>
        bool TryCompleteTransaction(uint systemBytes, SecsMessage response);

        /// <summary>
        /// 取消事务
        /// </summary>
        /// <param name="systemBytes">事务ID</param>
        void CancelTransaction(uint systemBytes);

        /// <summary>
        /// 取消所有事务
        /// </summary>
        void CancelAllTransactions();
    }
}
