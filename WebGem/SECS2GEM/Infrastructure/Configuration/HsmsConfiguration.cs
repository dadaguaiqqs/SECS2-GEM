using SECS2GEM.Core.Enums;

namespace SECS2GEM.Infrastructure.Configuration
{
    /// <summary>
    /// HSMS连接配置
    /// </summary>
    /// <remarks>
    /// 配置HSMS连接的各项参数，包括：
    /// - 网络参数（IP、端口、连接模式）
    /// - 超时参数（T3-T8）
    /// - 心跳参数
    /// </remarks>
    public sealed class HsmsConfiguration
    {
        /// <summary>
        /// 设备ID (Session ID)
        /// </summary>
        public ushort DeviceId { get; set; } = 0;

        /// <summary>
        /// IP地址
        /// - Passive模式：绑定地址（默认0.0.0.0监听所有网卡）
        /// - Active模式：目标地址
        /// </summary>
        public string IpAddress { get; set; } = "0.0.0.0";

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 5000;

        /// <summary>
        /// 连接模式
        /// </summary>
        public HsmsConnectionMode Mode { get; set; } = HsmsConnectionMode.Passive;

        #region 超时参数 (单位：秒)

        /// <summary>
        /// T3 - 回复超时
        /// 发送Primary消息后等待Secondary消息的超时时间
        /// 默认：45秒
        /// </summary>
        public int T3 { get; set; } = 45;

        /// <summary>
        /// T5 - 连接分离超时
        /// 连接断开后重新建立连接前的等待时间
        /// 默认：10秒
        /// </summary>
        public int T5 { get; set; } = 10;

        /// <summary>
        /// T6 - 控制事务超时
        /// 控制消息（Select/Deselect/Linktest）的响应超时
        /// 默认：5秒
        /// </summary>
        public int T6 { get; set; } = 5;

        /// <summary>
        /// T7 - 未选择超时
        /// TCP连接建立后等待Select.req的超时时间
        /// 默认：10秒
        /// </summary>
        public int T7 { get; set; } = 10;

        /// <summary>
        /// T8 - 网络字符间隔超时
        /// 消息传输中字符之间的最大间隔
        /// 默认：5秒
        /// </summary>
        public int T8 { get; set; } = 5;

        #endregion

        #region 心跳参数

        /// <summary>
        /// 心跳间隔（秒）
        /// 0表示禁用心跳
        /// 默认：30秒
        /// </summary>
        public int LinktestInterval { get; set; } = 30;

        /// <summary>
        /// 最大连续心跳失败次数
        /// 超过此次数将断开连接
        /// 默认：3次
        /// </summary>
        public int MaxLinktestFailures { get; set; } = 3;

        #endregion

        #region 其他参数

        /// <summary>
        /// 最大消息大小（字节）
        /// 默认：16MB
        /// </summary>
        public int MaxMessageSize { get; set; } = 16 * 1024 * 1024;

        /// <summary>
        /// 接收缓冲区大小（字节）
        /// 默认：64KB
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 64 * 1024;

        /// <summary>
        /// 发送缓冲区大小（字节）
        /// 默认：64KB
        /// </summary>
        public int SendBufferSize { get; set; } = 64 * 1024;

        /// <summary>
        /// 是否自动重连
        /// 默认：true
        /// </summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// 重连延迟（秒）
        /// 默认：使用T5
        /// </summary>
        public int ReconnectDelay { get; set; } = 0;

        #endregion

        #region TimeSpan Helpers

        /// <summary>
        /// T3超时时间
        /// </summary>
        public TimeSpan T3Timeout => TimeSpan.FromSeconds(T3);

        /// <summary>
        /// T5超时时间
        /// </summary>
        public TimeSpan T5Timeout => TimeSpan.FromSeconds(T5);

        /// <summary>
        /// T6超时时间
        /// </summary>
        public TimeSpan T6Timeout => TimeSpan.FromSeconds(T6);

        /// <summary>
        /// T7超时时间
        /// </summary>
        public TimeSpan T7Timeout => TimeSpan.FromSeconds(T7);

        /// <summary>
        /// T8超时时间
        /// </summary>
        public TimeSpan T8Timeout => TimeSpan.FromSeconds(T8);

        /// <summary>
        /// 心跳间隔时间
        /// </summary>
        public TimeSpan LinktestIntervalTimeSpan => TimeSpan.FromSeconds(LinktestInterval);

        /// <summary>
        /// 重连延迟时间
        /// </summary>
        public TimeSpan ReconnectDelayTimeSpan => 
            TimeSpan.FromSeconds(ReconnectDelay > 0 ? ReconnectDelay : T5);

        #endregion

        /// <summary>
        /// 验证配置
        /// </summary>
        public void Validate()
        {
            if (Port <= 0 || Port > 65535)
            {
                throw new ArgumentException("Port must be between 1 and 65535.", nameof(Port));
            }

            if (T3 <= 0)
            {
                throw new ArgumentException("T3 must be greater than 0.", nameof(T3));
            }

            if (T6 <= 0)
            {
                throw new ArgumentException("T6 must be greater than 0.", nameof(T6));
            }

            if (T7 <= 0)
            {
                throw new ArgumentException("T7 must be greater than 0.", nameof(T7));
            }
        }

        /// <summary>
        /// 创建默认的Passive模式配置
        /// </summary>
        public static HsmsConfiguration CreatePassive(int port, ushort deviceId = 0)
        {
            return new HsmsConfiguration
            {
                Mode = HsmsConnectionMode.Passive,
                Port = port,
                DeviceId = deviceId,
                IpAddress = "0.0.0.0"
            };
        }

        /// <summary>
        /// 创建Active模式配置
        /// </summary>
        public static HsmsConfiguration CreateActive(string ipAddress, int port, ushort deviceId = 0)
        {
            return new HsmsConfiguration
            {
                Mode = HsmsConnectionMode.Active,
                IpAddress = ipAddress,
                Port = port,
                DeviceId = deviceId
            };
        }
    }

    /// <summary>
    /// GEM设备配置
    /// </summary>
    public sealed class GemConfiguration
    {
        /// <summary>
        /// HSMS连接配置
        /// </summary>
        public HsmsConfiguration Hsms { get; set; } = new();

        /// <summary>
        /// 设备型号 (MDLN)
        /// </summary>
        public string ModelName { get; set; } = "SECS2GEM";

        /// <summary>
        /// 软件版本 (SOFTREV)
        /// </summary>
        public string SoftwareRevision { get; set; } = "1.0.0";

        /// <summary>
        /// 初始控制状态
        /// </summary>
        public GemControlState InitialControlState { get; set; } = GemControlState.EquipmentOffline;

        /// <summary>
        /// 是否自动进入在线状态
        /// </summary>
        public bool AutoOnline { get; set; } = true;

        /// <summary>
        /// 初始在线模式（Local/Remote）
        /// </summary>
        public bool InitialRemoteMode { get; set; } = true;
    }
}
