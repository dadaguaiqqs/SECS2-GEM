namespace SECS2GEM.Core.Enums
{
    /// <summary>
    /// GEM通信状态
    /// </summary>
    /// <remarks>
    /// 定义于SEMI E30标准。
    /// 描述设备与主机之间的通信状态。
    /// </remarks>
    public enum GemCommunicationState
    {
        /// <summary>
        /// 通信禁用
        /// 设备不接受通信请求
        /// </summary>
        Disabled,

        /// <summary>
        /// 通信已启用
        /// 设备准备好接受通信
        /// </summary>
        Enabled,

        /// <summary>
        /// 等待通信请求
        /// 等待S1F13建立通信
        /// </summary>
        WaitCommunicationRequest,

        /// <summary>
        /// 等待通信延迟
        /// 通信请求被拒绝后的等待状态
        /// </summary>
        WaitCommunicationDelay,

        /// <summary>
        /// 通信中
        /// 正常通信状态
        /// </summary>
        Communicating
    }

    /// <summary>
    /// GEM控制状态
    /// </summary>
    /// <remarks>
    /// 定义于SEMI E30标准。
    /// 描述设备的在线/离线和控制模式。
    /// </remarks>
    public enum GemControlState
    {
        /// <summary>
        /// 设备离线
        /// 设备主动选择离线
        /// </summary>
        EquipmentOffline,

        /// <summary>
        /// 尝试上线
        /// 设备正在尝试建立在线状态
        /// </summary>
        AttemptOnline,

        /// <summary>
        /// 主机离线
        /// 主机请求设备离线
        /// </summary>
        HostOffline,

        /// <summary>
        /// 在线本地控制
        /// 设备在线，操作员控制
        /// </summary>
        OnlineLocal,

        /// <summary>
        /// 在线远程控制
        /// 设备在线，主机控制
        /// </summary>
        OnlineRemote
    }

    /// <summary>
    /// GEM处理状态
    /// </summary>
    /// <remarks>
    /// 描述设备的处理状态。
    /// </remarks>
    public enum GemProcessingState
    {
        /// <summary>
        /// 空闲
        /// 设备等待工作
        /// </summary>
        Idle,

        /// <summary>
        /// 准备中
        /// 设备正在准备
        /// </summary>
        Setup,

        /// <summary>
        /// 就绪
        /// 设备准备好执行
        /// </summary>
        Ready,

        /// <summary>
        /// 执行中
        /// 设备正在执行处理
        /// </summary>
        Executing,

        /// <summary>
        /// 已暂停
        /// 处理被暂停
        /// </summary>
        Paused
    }

    /// <summary>
    /// 报警类别
    /// </summary>
    /// <remarks>
    /// ALCD的低7位定义了报警类别。
    /// </remarks>
    public enum AlarmCategory : byte
    {
        /// <summary>
        /// 未知类别
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 人员安全报警
        /// </summary>
        PersonalSafety = 1,

        /// <summary>
        /// 设备安全报警
        /// </summary>
        EquipmentSafety = 2,

        /// <summary>
        /// 参数控制警告
        /// </summary>
        ParameterControlWarning = 3,

        /// <summary>
        /// 参数控制错误
        /// </summary>
        ParameterControlError = 4,

        /// <summary>
        /// 无法恢复的错误
        /// </summary>
        IrrecoverableError = 5,

        /// <summary>
        /// 设备状态警告
        /// </summary>
        EquipmentStatusWarning = 6,

        /// <summary>
        /// 注意标志
        /// </summary>
        AttentionFlags = 7,

        /// <summary>
        /// 数据完整性
        /// </summary>
        DataIntegrity = 8
    }
}
