namespace SECS2GEM.Core.Enums
{
    /// <summary>
    /// HSMS连接状态
    /// </summary>
    /// <remarks>
    /// 定义HSMS连接的生命周期状态。
    /// 状态转换遵循SEMI E37标准。
    /// </remarks>
    public enum ConnectionState
    {
        /// <summary>
        /// 未连接
        /// TCP连接未建立
        /// </summary>
        NotConnected,

        /// <summary>
        /// 正在连接
        /// TCP连接建立中（仅Active模式）
        /// </summary>
        Connecting,

        /// <summary>
        /// 已连接但未选择
        /// TCP连接已建立，但HSMS会话未建立
        /// </summary>
        Connected,

        /// <summary>
        /// 已选择
        /// HSMS会话已建立，可以进行数据通信
        /// </summary>
        Selected,

        /// <summary>
        /// 正在断开
        /// 连接正在关闭中
        /// </summary>
        Disconnecting
    }

    /// <summary>
    /// HSMS连接模式
    /// </summary>
    public enum HsmsConnectionMode
    {
        /// <summary>
        /// 主动模式
        /// 主动发起TCP连接（通常为Host）
        /// </summary>
        Active,

        /// <summary>
        /// 被动模式
        /// 等待TCP连接（通常为Equipment）
        /// </summary>
        Passive
    }
}
