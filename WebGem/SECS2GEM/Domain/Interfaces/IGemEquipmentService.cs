using SECS2GEM.Domain.Events;
using SECS2GEM.Domain.Models;

namespace SECS2GEM.Domain.Interfaces
{
    /// <summary>
    /// GEM设备服务接口
    /// </summary>
    /// <remarks>
    /// 外观模式：为复杂的GEM子系统提供统一入口。
    /// 
    /// 作为Equipment角色时的执行流程：
    /// 1. 调用StartAsync启动服务，开始监听连接
    /// 2. Host连接后，等待Select.req
    /// 3. 收到Select.req后发送Select.rsp，进入Selected状态
    /// 4. 等待S1F13建立通信
    /// 5. 进入正常通信状态，处理消息
    /// 
    /// 主要职责：
    /// - 管理HSMS连接
    /// - 分发消息到处理器
    /// - 发送事件报告和报警
    /// - 管理GEM状态机
    /// </remarks>
    public interface IGemEquipmentService : IAsyncDisposable
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        ushort DeviceId { get; }

        /// <summary>
        /// 设备型号
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// 软件版本
        /// </summary>
        string SoftwareRevision { get; }

        /// <summary>
        /// GEM状态
        /// </summary>
        IGemState State { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 是否已连接（HSMS Selected状态）
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 是否正在通信（GEM Communicating状态）
        /// </summary>
        bool IsCommunicating { get; }

        #region 生命周期

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task StopAsync(CancellationToken cancellationToken = default);

        #endregion

        #region 消息发送

        /// <summary>
        /// 发送消息并等待响应
        /// </summary>
        /// <param name="message">SECS消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应消息</returns>
        Task<Core.Entities.SecsMessage?> SendAsync(
            Core.Entities.SecsMessage message,
            CancellationToken cancellationToken = default);

        #endregion

        #region 事件报告

        /// <summary>
        /// 发送事件报告 (S6F11)
        /// </summary>
        /// <param name="ceid">采集事件ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SendEventAsync(uint ceid, CancellationToken cancellationToken = default);

        /// <summary>
        /// 注册事件定义
        /// </summary>
        /// <param name="eventDefinition">事件定义</param>
        void RegisterEvent(CollectionEvent eventDefinition);

        /// <summary>
        /// 启用/禁用事件报告
        /// </summary>
        /// <param name="ceid">事件ID</param>
        /// <param name="enabled">是否启用</param>
        void SetEventEnabled(uint ceid, bool enabled);

        #endregion

        #region 报警

        /// <summary>
        /// 发送报警 (S5F1)
        /// </summary>
        /// <param name="alarm">报警信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SendAlarmAsync(AlarmInfo alarm, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清除报警
        /// </summary>
        /// <param name="alarmId">报警ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task ClearAlarmAsync(uint alarmId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 注册报警定义
        /// </summary>
        /// <param name="alarm">报警定义</param>
        void RegisterAlarm(AlarmDefinition alarm);

        #endregion

        #region 事件订阅

        /// <summary>
        /// 消息接收事件
        /// </summary>
        event EventHandler<MessageReceivedEvent>? MessageReceived;

        /// <summary>
        /// 状态变化事件
        /// </summary>
        event EventHandler<StateChangedEvent>? StateChanged;

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        #endregion
    }
}
