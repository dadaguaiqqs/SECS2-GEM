using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;
using SECS2GEM.Core.Exceptions;
using SECS2GEM.Domain.Interfaces;
using SECS2GEM.Infrastructure.Configuration;
using SECS2GEM.Infrastructure.Logging;
using SECS2GEM.Infrastructure.Serialization;
using SECS2GEM.Infrastructure.Services;

namespace SECS2GEM.Infrastructure.Connection
{
    /// <summary>
    /// HSMS连接实现
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 1. 使用状态模式管理连接状态
    /// 2. 使用Channel实现异步消息队列
    /// 3. 支持Passive和Active两种连接模式
    /// 4. 自动心跳检测
    /// 
    /// 线程模型：
    /// - 接收任务：持续读取TCP数据，解析消息
    /// - 发送任务：从Channel读取消息并发送
    /// - 心跳任务：定期发送Linktest
    /// </remarks>
    public sealed class HsmsConnection : ISecsConnection
    {
        private readonly HsmsConfiguration _config;
        private readonly ISecsSerializer _serializer;
        private readonly ITransactionManager _transactionManager;
        private readonly IMessageLogger _messageLogger;
        private IGemState? _gemState;

        // 网络
        private TcpListener? _listener;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;

        // 状态
        private ConnectionState _state = ConnectionState.NotConnected;
        private readonly object _stateLock = new();

        // 异步任务
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;
        private Task? _sendTask;
        private Task? _linktestTask;

        // 发送队列
        private Channel<(byte[] Data, TaskCompletionSource<bool>? Completion)>? _sendChannel;

        // 心跳状态
        private int _linktestFailures;

        #region Properties

        /// <summary>
        /// 连接状态
        /// </summary>
        public ConnectionState State
        {
            get { lock (_stateLock) return _state; }
            private set
            {
                ConnectionState oldState;
                lock (_stateLock)
                {
                    if (_state == value) return;
                    oldState = _state;
                    _state = value;
                }
                StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(oldState, value, null));
            }
        }

        /// <summary>
        /// 是否已选择
        /// </summary>
        public bool IsSelected => State == ConnectionState.Selected;

        /// <summary>
        /// 会话ID
        /// </summary>
        public ushort SessionId => _config.DeviceId;

        /// <summary>
        /// 远程端点
        /// </summary>
        public string? RemoteEndpoint { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

        /// <summary>
        /// 收到Primary消息事件
        /// </summary>
        public event EventHandler<SecsMessageReceivedEventArgs>? PrimaryMessageReceived;

        #endregion

        /// <summary>
        /// 设置GEM状态（由Application层注入）
        /// </summary>
        public void SetGemState(IGemState gemState)
        {
            _gemState = gemState ?? throw new ArgumentNullException(nameof(gemState));
        }

        /// <summary>
        /// 创建HSMS连接
        /// </summary>
        public HsmsConnection(
            HsmsConfiguration config,
            ISecsSerializer? serializer = null,
            ITransactionManager? transactionManager = null,
            IMessageLogger? messageLogger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _config.Validate();

            _serializer = serializer ?? new SecsSerializer();
            _transactionManager = transactionManager ?? new Services.TransactionManager();
            _messageLogger = messageLogger ?? new MessageLogger(config.MessageLogging);

            if (_serializer is SecsSerializer secsSerializer)
            {
                secsSerializer.MaxMessageSize = _config.MaxMessageSize;
            }
        }

        #region Connection Management

        /// <summary>
        /// 建立连接（Active模式）
        /// </summary>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_config.Mode != HsmsConnectionMode.Active)
            {
                throw new InvalidOperationException("ConnectAsync is only for Active mode. Use StartListeningAsync for Passive mode.");
            }

            if (State != ConnectionState.NotConnected)
            {
                throw new InvalidOperationException($"Cannot connect in state {State}.");
            }

            State = ConnectionState.Connecting;

            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.ReceiveBufferSize = _config.ReceiveBufferSize;
                _tcpClient.SendBufferSize = _config.SendBufferSize;

                await _tcpClient.ConnectAsync(_config.IpAddress, _config.Port, cancellationToken);
                
                _stream = _tcpClient.GetStream();
                RemoteEndpoint = _tcpClient.Client.RemoteEndPoint?.ToString();

                State = ConnectionState.Connected;

                // 初始化消息日志记录器
                await _messageLogger.InitializeAsync(_config.IpAddress, _config.Port, _config.DeviceId);

                InitializeChannelAndTasks();

                // Active模式：发送Select.req
                await SendSelectRequestAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                State = ConnectionState.NotConnected;
                throw SecsCommunicationException.ConnectionFailed($"{_config.IpAddress}:{_config.Port}", ex);
            }
        }

        /// <summary>
        /// 开始监听（Passive模式）
        /// </summary>
        public Task StartListeningAsync(CancellationToken cancellationToken = default)
        {
            if (_config.Mode != HsmsConnectionMode.Passive)
            {
                throw new InvalidOperationException("StartListeningAsync is only for Passive mode. Use ConnectAsync for Active mode.");
            }

            if (State != ConnectionState.NotConnected)
            {
                throw new InvalidOperationException($"Cannot start listening in state {State}.");
            }

            var endpoint = new IPEndPoint(IPAddress.Parse(_config.IpAddress), _config.Port);
            _listener = new TcpListener(endpoint);
            _listener.Start();

            _cts = new CancellationTokenSource();

            // 等待连接（后台运行）
            _ = AcceptConnectionsAsync(_cts.Token);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 接受连接
        /// </summary>
        private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 检查 listener 是否有效
                    var listener = _listener;
                    if (listener == null)
                    {
                        break;
                    }
                    
                    var client = await listener.AcceptTcpClientAsync(cancellationToken);
                    
                    // 如果已有连接，拒绝新连接
                    if (State != ConnectionState.NotConnected)
                    {
                        client.Close();
                        continue;
                    }

                    _tcpClient = client;
                    _tcpClient.ReceiveBufferSize = _config.ReceiveBufferSize;
                    _tcpClient.SendBufferSize = _config.SendBufferSize;
                    _stream = _tcpClient.GetStream();
                    RemoteEndpoint = _tcpClient.Client.RemoteEndPoint?.ToString();

                    State = ConnectionState.Connected;

                    // 初始化消息日志记录器（Passive模式使用远程IP）
                    var remoteIp = (_tcpClient.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "unknown";
                    await _messageLogger.InitializeAsync(remoteIp, _config.Port, _config.DeviceId);

                    InitializeChannelAndTasks();

                    // Passive模式：启动T7超时监控
                    _ = StartT7TimeoutAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // listener 已被释放，正常退出
                    break;
                }
                catch (Exception)
                {
                    // 其他错误，检查是否应该继续
                    if (cancellationToken.IsCancellationRequested || _listener == null)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// T7超时监控
        /// </summary>
        private async Task StartT7TimeoutAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_config.T7Timeout, cancellationToken);
                
                if (State == ConnectionState.Connected)
                {
                    // T7超时，断开连接
                    await DisconnectAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (State == ConnectionState.NotConnected) return;

            State = ConnectionState.Disconnecting;

            try
            {
                // 发送Separate.req（带超时，防止阻塞）
                if (_stream != null && _tcpClient?.Connected == true)
                {
                    try
                    {
                        var separateMsg = HsmsMessage.CreateSeparateRequest(
                            SessionId, 
                            _transactionManager.GetNextTransactionId());
                        var data = _serializer.Serialize(separateMsg);
                        
                        // 使用2秒超时，防止网络异常时阻塞
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                            cancellationToken, timeoutCts.Token);
                        
                        await _stream.WriteAsync(data, linkedCts.Token).ConfigureAwait(false);
                    }
                    catch
                    {
                        // 忽略发送错误（包括超时）
                    }
                }
            }
            finally
            {
                Cleanup();
                State = ConnectionState.NotConnected;
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            // 1. 先取消所有后台任务
            try
            {
                _cts?.Cancel();
            }
            catch
            {
                // 忽略取消错误
            }

            // 2. 标记发送通道完成
            try
            {
                _sendChannel?.Writer.TryComplete();
            }
            catch
            {
                // 忽略
            }

            // 3. 关闭网络流（这会中断正在进行的读写操作）
            try
            {
                _stream?.Close();
                _stream?.Dispose();
            }
            catch
            {
                // 忽略关闭错误
            }
            _stream = null;

            // 4. 关闭TCP客户端
            try
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
            catch
            {
                // 忽略关闭错误
            }
            _tcpClient = null;

            // 5. 取消所有等待中的事务
            try
            {
                _transactionManager.CancelAllTransactions();
            }
            catch
            {
                // 忽略
            }
            
            _linktestFailures = 0;
            RemoteEndpoint = null;
        }

        /// <summary>
        /// 初始化Channel和异步任务
        /// </summary>
        private void InitializeChannelAndTasks()
        {
            _cts = new CancellationTokenSource();
            _sendChannel = Channel.CreateUnbounded<(byte[], TaskCompletionSource<bool>?)>();
            _linktestFailures = 0;

            _receiveTask = ReceiveLoopAsync(_cts.Token);
            _sendTask = SendLoopAsync(_cts.Token);

            if (_config.LinktestInterval > 0)
            {
                _linktestTask = LinktestLoopAsync(_cts.Token);
            }
        }

        #endregion

        #region Message Sending

        /// <summary>
        /// 发送消息并等待回复
        /// </summary>
        public async Task<SecsMessage?> SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            if (!IsSelected)
            {
                throw SecsCommunicationException.NotSelected();
            }

            var systemBytes = _transactionManager.GetNextTransactionId();
            var hsmsMsg = HsmsMessage.CreateDataMessage(SessionId, message, systemBytes);
            var data = _serializer.Serialize(hsmsMsg);

            if (message.WBit)
            {
                // 期望回复：创建事务
                using var transaction = _transactionManager.BeginTransaction(
                    systemBytes, message.Name, _config.T3Timeout);

                await EnqueueSendAsync(data, cancellationToken);
                return await transaction.WaitForResponseAsync(cancellationToken);
            }
            else
            {
                // 不期望回复
                await EnqueueSendAsync(data, cancellationToken);
                return null;
            }
        }

        /// <summary>
        /// 发送消息（不等待回复）
        /// </summary>
        public async Task SendOnlyAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            if (!IsSelected)
            {
                throw SecsCommunicationException.NotSelected();
            }

            var systemBytes = _transactionManager.GetNextTransactionId();
            var hsmsMsg = HsmsMessage.CreateDataMessage(SessionId, message, systemBytes);
            var data = _serializer.Serialize(hsmsMsg);

            await EnqueueSendAsync(data, cancellationToken);
        }

        /// <summary>
        /// 发送Linktest
        /// </summary>
        public async Task<bool> SendLinktestAsync(CancellationToken cancellationToken = default)
        {
            if (State != ConnectionState.Selected && State != ConnectionState.Connected)
            {
                return false;
            }

            var systemBytes = _transactionManager.GetNextTransactionId();
            var linktestMsg = HsmsMessage.CreateLinktestRequest(systemBytes);
            var data = _serializer.Serialize(linktestMsg);

            using var transaction = _transactionManager.BeginTransaction(
                systemBytes, "Linktest", _config.T6Timeout);

            try
            {
                await EnqueueSendAsync(data, cancellationToken);
                await transaction.WaitForResponseAsync(cancellationToken);
                Interlocked.Exchange(ref _linktestFailures, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将数据加入发送队列
        /// </summary>
        private async Task EnqueueSendAsync(byte[] data, CancellationToken cancellationToken)
        {
            if (_sendChannel == null)
            {
                throw new InvalidOperationException("Connection not initialized.");
            }

            var tcs = new TaskCompletionSource<bool>();
            await _sendChannel.Writer.WriteAsync((data, tcs), cancellationToken);
            await tcs.Task;
        }

        /// <summary>
        /// 发送Select.req
        /// </summary>
        private async Task SendSelectRequestAsync(CancellationToken cancellationToken)
        {
            var systemBytes = _transactionManager.GetNextTransactionId();
            var selectMsg = HsmsMessage.CreateSelectRequest(SessionId, systemBytes);
            var data = _serializer.Serialize(selectMsg);

            using var transaction = _transactionManager.BeginTransaction(
                systemBytes, "Select", _config.T6Timeout);

            await EnqueueSendAsync(data, cancellationToken);

            try
            {
                await transaction.WaitForResponseAsync(cancellationToken);
                State = ConnectionState.Selected;
            }
            catch (SecsTimeoutException)
            {
                await DisconnectAsync(cancellationToken);
                throw;
            }
        }

        #endregion

        #region Async Loops

        /// <summary>
        /// 接收循环
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[_config.ReceiveBufferSize];
            var receivedData = new List<byte>();

            try
            {
                while (!cancellationToken.IsCancellationRequested && _stream != null)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        // 连接关闭
                        break;
                    }

                    receivedData.AddRange(buffer.AsSpan(0, bytesRead).ToArray());

                    // 尝试解析消息
                    while (receivedData.Count >= 14) // 最小消息大小：4(长度) + 10(Header)
                    {
                        var span = receivedData.ToArray().AsSpan();
                        if (_serializer.TryReadMessage(span, out var message, out var consumed))
                        {
                            // 获取消息的原始字节用于日志记录
                            var messageBytes = receivedData.Take(consumed).ToArray();
                            
                            receivedData.RemoveRange(0, consumed);
                            
                            if (message != null)
                            {
                                // 记录接收的消息
                                await LogReceivedMessageAsync(message, messageBytes);
                                
                                await HandleMessageAsync(message, cancellationToken);
                            }
                        }
                        else
                        {
                            break; // 数据不完整，等待更多数据
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception)
            {
                // 连接异常
            }
            finally
            {
                if (State != ConnectionState.NotConnected && State != ConnectionState.Disconnecting)
                {
                    _ = DisconnectAsync();
                }
            }
        }

        /// <summary>
        /// 发送循环
        /// </summary>
        private async Task SendLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var (data, completion) in _sendChannel!.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        if (_stream != null)
                        {
                            await _stream.WriteAsync(data, cancellationToken);
                            
                            // 记录发送的消息
                            await LogSentMessageAsync(data);
                            
                            completion?.TrySetResult(true);
                        }
                        else
                        {
                            completion?.TrySetException(new InvalidOperationException("Stream is null."));
                        }
                    }
                    catch (Exception ex)
                    {
                        completion?.TrySetException(ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
        }

        /// <summary>
        /// 记录发送的消息
        /// </summary>
        private async Task LogSentMessageAsync(byte[] data)
        {
            if (!_messageLogger.IsEnabled) return;

            try
            {
                if (_serializer.TryReadMessage(data, out var message, out _) && message != null)
                {
                    await _messageLogger.LogMessageAsync(message, data, MessageDirection.Send);
                }
                else
                {
                    await _messageLogger.LogRawBytesAsync(data, MessageDirection.Send, "Raw data");
                }
            }
            catch
            {
                // 忽略日志错误
            }
        }

        /// <summary>
        /// 记录接收的消息
        /// </summary>
        private async Task LogReceivedMessageAsync(HsmsMessage message, byte[] data)
        {
            if (!_messageLogger.IsEnabled) return;

            try
            {
                await _messageLogger.LogMessageAsync(message, data, MessageDirection.Receive);
            }
            catch
            {
                // 忽略日志错误
            }
        }

        /// <summary>
        /// 心跳循环
        /// </summary>
        private async Task LinktestLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_config.LinktestIntervalTimeSpan, cancellationToken);

                    if (State == ConnectionState.Selected)
                    {
                        var success = await SendLinktestAsync(cancellationToken);
                        
                        if (!success)
                        {
                            var failures = Interlocked.Increment(ref _linktestFailures);
                            
                            if (failures >= _config.MaxLinktestFailures)
                            {
                                // 心跳失败次数过多，断开连接
                                await DisconnectAsync(cancellationToken);
                                break;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private async Task HandleMessageAsync(HsmsMessage message, CancellationToken cancellationToken)
        {
            if (message.IsControlMessage)
            {
                await HandleControlMessageAsync(message, cancellationToken);
            }
            else if (message.IsDataMessage)
            {
                HandleDataMessage(message);
            }
        }

        /// <summary>
        /// 处理控制消息
        /// </summary>
        private async Task HandleControlMessageAsync(HsmsMessage message, CancellationToken cancellationToken)
        {
            switch (message.MessageType)
            {
                case HsmsMessageType.SelectRequest:
                    // 发送Select.rsp
                    var selectRsp = HsmsMessage.CreateSelectResponse(
                        message.SessionId, message.SystemBytes);
                    var selectData = _serializer.Serialize(selectRsp);
                    await EnqueueSendAsync(selectData, cancellationToken);
                    State = ConnectionState.Selected;
                    break;

                case HsmsMessageType.SelectResponse:
                    // 完成事务
                    _transactionManager.TryCompleteTransaction(message.SystemBytes, 
                        new SecsMessage(0, 0, false));
                    break;

                case HsmsMessageType.DeselectRequest:
                    // 发送Deselect.rsp
                    var deselectRsp = HsmsMessage.CreateDeselectResponse(
                        message.SessionId, message.SystemBytes);
                    var deselectData = _serializer.Serialize(deselectRsp);
                    await EnqueueSendAsync(deselectData, cancellationToken);
                    await DisconnectAsync(cancellationToken);
                    break;

                case HsmsMessageType.LinktestRequest:
                    // 发送Linktest.rsp
                    var linktestRsp = HsmsMessage.CreateLinktestResponse(message.SystemBytes);
                    var linktestData = _serializer.Serialize(linktestRsp);
                    await EnqueueSendAsync(linktestData, cancellationToken);
                    break;

                case HsmsMessageType.LinktestResponse:
                    // 完成事务
                    _transactionManager.TryCompleteTransaction(message.SystemBytes,
                        new SecsMessage(0, 0, false));
                    break;

                case HsmsMessageType.SeparateRequest:
                    await DisconnectAsync(cancellationToken);
                    break;
            }
        }

        /// <summary>
        /// 处理数据消息
        /// </summary>
        private void HandleDataMessage(HsmsMessage message)
        {
            if (message.SecsMessage == null) return;

            if (message.SecsMessage.IsSecondary)
            {
                // Secondary消息：完成对应的事务
                _transactionManager.TryCompleteTransaction(
                    message.SystemBytes, message.SecsMessage);
            }
            else
            {
                // Primary消息：触发事件
                // 注意：这里需要实现IMessageContext，暂时用简化版本
                var context = CreateMessageContext(message.Header.SystemBytes, message.Header.SessionId);
                PrimaryMessageReceived?.Invoke(this, new SecsMessageReceivedEventArgs(message.SecsMessage, context));
            }
        }

        /// <summary>
        /// 创建消息上下文
        /// </summary>
        private IMessageContext CreateMessageContext(uint systemBytes, ushort sessionId)
        {
            if (_gemState == null)
            {
                throw new InvalidOperationException("GemState not initialized. Call SetGemState first.");
            }

            return new MessageContext(
                systemBytes,
                sessionId,
                this,
                _gemState,
                async (reply, sysBytes, ct) =>
                {
                    var hsmsMsg = HsmsMessage.CreateDataMessage(sessionId, reply, sysBytes);
                    var data = _serializer.Serialize(hsmsMsg);
                    await EnqueueSendAsync(data, ct);
                });
        }

        #endregion

        #region IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            // 先取消所有后台任务（包括AcceptConnectionsAsync）
            try
            {
                _cts?.Cancel();
            }
            catch
            {
                // 忽略取消时的错误
            }
            
            // 断开连接
            await DisconnectAsync().ConfigureAwait(false);
            
            // 停止监听（被动模式）
            try
            {
                _listener?.Stop();
            }
            catch
            {
                // 忽略停止监听时的错误
            }
            
            // 给后台任务一点时间退出
            await Task.Delay(50).ConfigureAwait(false);
            
            // 释放资源
            try
            {
                _listener?.Dispose();
                _listener = null;
            }
            catch
            {
                // 忽略
            }
            
            try
            {
                _cts?.Dispose();
                _cts = null;
            }
            catch
            {
                // 忽略
            }

            // 释放消息记录器
            try
            {
                await _messageLogger.DisposeAsync();
            }
            catch
            {
                // 忽略
            }
        }

        #endregion
    }
}
