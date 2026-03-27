using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;
using SECS2GEM.Domain.Events;
using SECS2GEM.Domain.Interfaces;
using SECS2GEM.Domain.Models;
using SECS2GEM.Application.Handlers;
using SECS2GEM.Application.Messaging;
using SECS2GEM.Application.State;
using SECS2GEM.Infrastructure.Configuration;
using SECS2GEM.Infrastructure.Connection;
using SECS2GEM.Infrastructure.Services;

namespace SECS2GEM.Application.Services
{
    /// <summary>
    /// GEM设备服务实现
    /// </summary>
    /// <remarks>
    /// 外观模式：为复杂的GEM子系统提供统一入口。
    /// 
    /// 设计思路：
    /// 1. 整合HSMS连接、消息分发、状态管理
    /// 2. 提供简洁的API供上层调用
    /// 3. 自动注册默认消息处理器
    /// 
    /// 执行流程（作为Equipment角色）：
    /// 1. 调用StartAsync启动服务，开始监听连接
    /// 2. Host连接后，等待Select.req
    /// 3. 收到Select.req后发送Select.rsp，进入Selected状态
    /// 4. 等待S1F13建立通信
    /// 5. 进入正常通信状态，处理消息
    /// </remarks>
    public sealed class GemEquipmentService : IGemEquipmentService
    {
        private readonly GemConfiguration _config;
        private readonly HsmsConnection _connection;
        private readonly GemStateManager _stateManager;
        private readonly MessageDispatcher _dispatcher;
        private readonly EventAggregator _eventAggregator;

        private readonly Dictionary<uint, CollectionEvent> _events = new();
        private readonly Dictionary<uint, AlarmDefinition> _alarms = new();
        private readonly Dictionary<uint, AlarmInfo> _activeAlarms = new();

        private bool _isRunning;
        private uint _dataId;

        #region Properties

        /// <summary>
        /// 设备ID
        /// </summary>
        public ushort DeviceId => _config.Hsms.DeviceId;

        /// <summary>
        /// 设备型号
        /// </summary>
        public string ModelName => _stateManager.ModelName;

        /// <summary>
        /// 软件版本
        /// </summary>
        public string SoftwareRevision => _stateManager.SoftwareRevision;

        /// <summary>
        /// GEM状态
        /// </summary>
        public IGemState State => _stateManager;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 是否已连接（HSMS Selected状态）
        /// </summary>
        public bool IsConnected => _connection.IsSelected;

        /// <summary>
        /// 是否正在通信（GEM Communicating状态）
        /// </summary>
        public bool IsCommunicating => _stateManager.CommunicationState == GemCommunicationState.Communicating;

        #endregion

        #region Events

        /// <summary>
        /// 消息接收事件
        /// </summary>
        public event EventHandler<MessageReceivedEvent>? MessageReceived;

        /// <summary>
        /// 状态变化事件
        /// </summary>
        public event EventHandler<StateChangedEvent>? StateChanged;

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        #endregion

        /// <summary>
        /// 创建GEM设备服务
        /// </summary>
        /// <param name="config">配置</param>
        public GemEquipmentService(GemConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // 创建组件
            _stateManager = new GemStateManager(config.ModelName, config.SoftwareRevision);
            _dispatcher = new MessageDispatcher();
            _eventAggregator = new EventAggregator();
            _connection = new HsmsConnection(config.Hsms);

            // 设置连接的GemState
            _connection.SetGemState(_stateManager);

            // 订阅连接事件
            _connection.StateChanged += OnConnectionStateChanged;
            _connection.PrimaryMessageReceived += OnPrimaryMessageReceived;

            // 订阅状态变化
            _stateManager.CommunicationStateChanged += OnCommunicationStateChanged;
            _stateManager.ControlStateChanged += OnControlStateChanged;

            // 注册默认处理器
            RegisterDefaultHandlers();
        }

        #region Lifecycle

        /// <summary>
        /// 启动服务
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning) return;

            _isRunning = true;

            // 启用通信状态
            _stateManager.SetCommunicationState(GemCommunicationState.Enabled);

            // 根据配置模式启动连接
            if (_config.Hsms.Mode == HsmsConnectionMode.Passive)
            {
                await _connection.StartListeningAsync(cancellationToken);
            }
            else
            {
                await _connection.ConnectAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!_isRunning) return;

            _isRunning = false;

            // 断开连接
            await _connection.DisconnectAsync(cancellationToken);

            // 重置状态
            _stateManager.SetCommunicationState(GemCommunicationState.Disabled);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            await _connection.DisposeAsync();
        }

        #endregion

        #region Message Sending

        /// <summary>
        /// 发送消息并等待响应
        /// </summary>
        public async Task<SecsMessage?> SendAsync(
            SecsMessage message,
            CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected.");
            }

            return await _connection.SendAsync(message, cancellationToken);
        }

        #endregion

        #region Event Reporting

        /// <summary>
        /// 发送事件报告 (S6F11)
        /// </summary>
        public async Task SendEventAsync(uint ceid, CancellationToken cancellationToken = default)
        {
            if (!IsCommunicating) return;

            if (!_events.TryGetValue(ceid, out var evt) || !evt.IsEnabled)
            {
                return;
            }

            var dataId = Interlocked.Increment(ref _dataId);

            // 构建报告数据
            var reports = new List<SecsItem>();
            foreach (var rptid in evt.LinkedReportIds)
            {
                // 简化实现：返回空报告数据
                reports.Add(SecsItem.L(
                    SecsItem.U4(rptid),
                    SecsItem.L()
                ));
            }

            // S6F11: DATAID, CEID, <L [n] <L [2] <RPTID> <L [m] <V1> ...>> ...>
            var message = new SecsMessage(6, 11, true, SecsItem.L(
                SecsItem.U4(dataId),
                SecsItem.U4(ceid),
                SecsItem.L(reports)
            ));

            await SendAsync(message, cancellationToken);

            // 发布事件
            await _eventAggregator.PublishAsync(new CollectionEventTriggeredEvent(
                "GemEquipmentService", dataId, ceid, evt.Name, new List<ReportData>()));
        }

        /// <summary>
        /// 注册事件定义
        /// </summary>
        public void RegisterEvent(CollectionEvent eventDefinition)
        {
            _events[eventDefinition.EventId] = eventDefinition;
        }

        /// <summary>
        /// 启用/禁用事件报告
        /// </summary>
        public void SetEventEnabled(uint ceid, bool enabled)
        {
            if (_events.TryGetValue(ceid, out var evt))
            {
                evt.IsEnabled = enabled;
            }
        }

        #endregion

        #region Alarm

        /// <summary>
        /// 发送报警 (S5F1)
        /// </summary>
        public async Task SendAlarmAsync(AlarmInfo alarm, CancellationToken cancellationToken = default)
        {
            if (!IsCommunicating) return;

            _activeAlarms[alarm.AlarmId] = alarm;

            // S5F1: ALCD, ALID, ALTX
            var alcd = (byte)(alarm.IsSet ? 0x80 : 0x00);
            alcd |= (byte)alarm.Category;

            var message = new SecsMessage(5, 1, true, SecsItem.L(
                SecsItem.B(alcd),
                SecsItem.U4(alarm.AlarmId),
                SecsItem.A(alarm.AlarmText)
            ));

            await SendAsync(message, cancellationToken);

            // 发布报警事件
            await _eventAggregator.PublishAsync(new AlarmEvent(
                "GemEquipmentService", alarm.AlarmId, alarm.AlarmCode, alarm.AlarmText));
        }

        /// <summary>
        /// 清除报警
        /// </summary>
        public async Task ClearAlarmAsync(uint alarmId, CancellationToken cancellationToken = default)
        {
            if (_activeAlarms.TryGetValue(alarmId, out var alarm))
            {
                alarm.IsSet = false;
                await SendAlarmAsync(alarm, cancellationToken);
                _activeAlarms.Remove(alarmId);
            }
        }

        /// <summary>
        /// 注册报警定义
        /// </summary>
        public void RegisterAlarm(AlarmDefinition alarm)
        {
            _alarms[alarm.AlarmId] = alarm;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 连接状态变化处理
        /// </summary>
        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, e);

            if (e.NewState == ConnectionState.Selected)
            {
                // 连接建立，等待S1F13
                _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);
            }
            else if (e.NewState == ConnectionState.NotConnected)
            {
                // 连接断开
                _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
            }
        }

        /// <summary>
        /// 收到Primary消息处理
        /// </summary>
        private async void OnPrimaryMessageReceived(object? sender, SecsMessageReceivedEventArgs e)
        {
            // 发布消息接收事件
            var msgEvent = new MessageReceivedEvent(
                "GemEquipmentService", e.Message, MessageDirection.Received, e.Context.SystemBytes, null);
            MessageReceived?.Invoke(this, msgEvent);

            // 分发到处理器
            var response = await _dispatcher.DispatchAsync(e.Message, e.Context);

            // 发送响应
            if (response != null)
            {
                await e.Context.ReplyAsync(response);
            }
        }

        /// <summary>
        /// 通信状态变化处理
        /// </summary>
        private void OnCommunicationStateChanged(object? sender, GemCommunicationState e)
        {
            StateChanged?.Invoke(this, new StateChangedEvent(
                "GemEquipmentService",
                StateType.Communication,
                _stateManager.CommunicationState,
                e,
                null));

            // 进入Communicating状态时，如果配置了自动上线
            if (e == GemCommunicationState.Communicating && _config.AutoOnline)
            {
                _stateManager.RequestOnline();
                if (_config.InitialRemoteMode)
                {
                    _stateManager.SwitchToRemote();
                }
                else
                {
                    _stateManager.SwitchToLocal();
                }
            }
        }

        /// <summary>
        /// 控制状态变化处理
        /// </summary>
        private void OnControlStateChanged(object? sender, GemControlState e)
        {
            StateChanged?.Invoke(this, new StateChangedEvent(
                "GemEquipmentService",
                StateType.Control,
                _stateManager.ControlState,
                e,
                null));
        }

        #endregion

        #region Default Handlers

        /// <summary>
        /// 注册默认消息处理器
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            // Stream 1 - Equipment Status
            _dispatcher.RegisterHandler(new S1F1Handler());
            _dispatcher.RegisterHandler(new S1F13Handler());
            _dispatcher.RegisterHandler(new S1F15Handler());
            _dispatcher.RegisterHandler(new S1F17Handler());

            // Stream 2 - Equipment Control
            _dispatcher.RegisterHandler(new S2F13Handler());
            _dispatcher.RegisterHandler(new S2F15Handler());
            _dispatcher.RegisterHandler(new S2F29Handler());
            _dispatcher.RegisterHandler(new S2F33Handler());
            _dispatcher.RegisterHandler(new S2F35Handler());
            _dispatcher.RegisterHandler(new S2F37Handler());
            _dispatcher.RegisterHandler(new S2F41Handler());

            // Stream 5 - Alarm Management
            _dispatcher.RegisterHandler(new S5F3Handler());
            _dispatcher.RegisterHandler(new S5F5Handler());
            _dispatcher.RegisterHandler(new S5F7Handler());

            // Stream 6 - Data Collection
            _dispatcher.RegisterHandler(new S6F15Handler());
            _dispatcher.RegisterHandler(new S6F19Handler());

            // Stream 7 - Process Program Management
            _dispatcher.RegisterHandler(new S7F1Handler());
            _dispatcher.RegisterHandler(new S7F3Handler());
            _dispatcher.RegisterHandler(new S7F5Handler());
            _dispatcher.RegisterHandler(new S7F17Handler());
            _dispatcher.RegisterHandler(new S7F19Handler());

            // Stream 10 - Terminal Services
            _dispatcher.RegisterHandler(new S10F3Handler());
            _dispatcher.RegisterHandler(new S10F5Handler());
        }

        /// <summary>
        /// 注册自定义消息处理器
        /// </summary>
        public void RegisterHandler(IMessageHandler handler)
        {
            _dispatcher.RegisterHandler(handler);
        }

        #endregion
    }
}
