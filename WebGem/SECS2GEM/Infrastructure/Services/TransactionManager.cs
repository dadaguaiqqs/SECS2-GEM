using System.Collections.Concurrent;
using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Exceptions;
using SECS2GEM.Domain.Interfaces;

namespace SECS2GEM.Infrastructure.Services
{
    /// <summary>
    /// 事务管理器实现
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 1. 使用Interlocked确保事务ID生成的原子性
    /// 2. 使用ConcurrentDictionary存储活跃事务
    /// 3. 使用TaskCompletionSource实现异步等待响应
    /// 4. 支持超时自动清理
    /// 
    /// 执行流程：
    /// 1. 发送消息前调用BeginTransaction创建事务
    /// 2. 消息发送，等待响应
    /// 3. 收到响应后调用TryCompleteTransaction完成事务
    /// 4. 如果超时，事务会被自动取消
    /// </remarks>
    public sealed class TransactionManager : ITransactionManager, IDisposable
    {
        private uint _transactionIdCounter;
        private readonly ConcurrentDictionary<uint, Transaction> _activeTransactions = new();
        private bool _disposed;

        /// <summary>
        /// 当前活跃的事务数量
        /// </summary>
        public int ActiveTransactionCount => _activeTransactions.Count;

        /// <summary>
        /// 获取下一个事务ID
        /// </summary>
        public uint GetNextTransactionId()
        {
            return Interlocked.Increment(ref _transactionIdCounter);
        }

        /// <summary>
        /// 开始新事务
        /// </summary>
        public ITransaction BeginTransaction(uint systemBytes, string messageName, TimeSpan timeout)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var transaction = new Transaction(systemBytes, messageName, timeout, OnTransactionTimeout);
            
            if (!_activeTransactions.TryAdd(systemBytes, transaction))
            {
                transaction.Dispose();
                throw new InvalidOperationException($"Transaction with SystemBytes {systemBytes} already exists.");
            }

            return transaction;
        }

        /// <summary>
        /// 尝试完成事务
        /// </summary>
        public bool TryCompleteTransaction(uint systemBytes, SecsMessage response)
        {
            if (_activeTransactions.TryRemove(systemBytes, out var transaction))
            {
                transaction.SetResponse(response);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 取消事务
        /// </summary>
        public void CancelTransaction(uint systemBytes)
        {
            if (_activeTransactions.TryRemove(systemBytes, out var transaction))
            {
                transaction.Cancel();
                transaction.Dispose();
            }
        }

        /// <summary>
        /// 取消所有事务
        /// </summary>
        public void CancelAllTransactions()
        {
            foreach (var kvp in _activeTransactions)
            {
                if (_activeTransactions.TryRemove(kvp.Key, out var transaction))
                {
                    transaction.Cancel();
                    transaction.Dispose();
                }
            }
        }

        /// <summary>
        /// 事务超时回调
        /// </summary>
        private void OnTransactionTimeout(uint systemBytes)
        {
            if (_activeTransactions.TryRemove(systemBytes, out var transaction))
            {
                transaction.SetTimedOut();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            CancelAllTransactions();
        }
    }

    /// <summary>
    /// 事务实现
    /// </summary>
    internal sealed class Transaction : ITransaction
    {
        private readonly TaskCompletionSource<SecsMessage?> _tcs;
        private readonly CancellationTokenSource _timeoutCts;
        private readonly Action<uint> _onTimeout;
        private bool _disposed;

        public uint SystemBytes { get; }
        public string MessageName { get; }
        public DateTime CreatedTime { get; }
        public bool IsCompleted => _tcs.Task.IsCompleted;
        public bool IsTimedOut { get; private set; }

        public Transaction(uint systemBytes, string messageName, TimeSpan timeout, Action<uint> onTimeout)
        {
            SystemBytes = systemBytes;
            MessageName = messageName;
            CreatedTime = DateTime.UtcNow;
            _onTimeout = onTimeout;
            _tcs = new TaskCompletionSource<SecsMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _timeoutCts = new CancellationTokenSource();

            // 设置超时
            if (timeout > TimeSpan.Zero)
            {
                _timeoutCts.CancelAfter(timeout);
                _timeoutCts.Token.Register(() =>
                {
                    if (!IsCompleted)
                    {
                        _onTimeout(SystemBytes);
                    }
                });
            }
        }

        public async Task<SecsMessage?> WaitForResponseAsync(CancellationToken cancellationToken = default)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _timeoutCts.Token);

            try
            {
                return await _tcs.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (IsTimedOut)
            {
                var elapsed = DateTime.UtcNow - CreatedTime;
                throw SecsTimeoutException.T3Timeout(elapsed, MessageName, SystemBytes);
            }
        }

        public void SetResponse(SecsMessage response)
        {
            _tcs.TrySetResult(response);
        }

        public void SetTimedOut()
        {
            IsTimedOut = true;
            _tcs.TrySetCanceled();
        }

        public void Cancel()
        {
            _tcs.TrySetCanceled();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timeoutCts.Dispose();
        }
    }
}
