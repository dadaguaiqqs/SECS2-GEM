namespace SECS2GEM.Core.Enums
{
    /// <summary>
    /// HSMS消息类型 (SType)
    /// </summary>
    /// <remarks>
    /// 定义于HSMS Header的第6字节（Byte 5）。
    /// 用于区分数据消息和控制消息。
    /// </remarks>
    public enum HsmsMessageType : byte
    {
        /// <summary>
        /// 数据消息 (SType = 0)
        /// 携带SECS-II消息内容
        /// </summary>
        DataMessage = 0,

        /// <summary>
        /// 选择请求 (SType = 1)
        /// 用于建立HSMS会话
        /// </summary>
        SelectRequest = 1,

        /// <summary>
        /// 选择响应 (SType = 2)
        /// 对Select.req的响应
        /// </summary>
        SelectResponse = 2,

        /// <summary>
        /// 取消选择请求 (SType = 3)
        /// 用于终止HSMS会话
        /// </summary>
        DeselectRequest = 3,

        /// <summary>
        /// 取消选择响应 (SType = 4)
        /// 对Deselect.req的响应
        /// </summary>
        DeselectResponse = 4,

        /// <summary>
        /// 链路测试请求 (SType = 5)
        /// 用于心跳检测
        /// </summary>
        LinktestRequest = 5,

        /// <summary>
        /// 链路测试响应 (SType = 6)
        /// 对Linktest.req的响应
        /// </summary>
        LinktestResponse = 6,

        /// <summary>
        /// 拒绝请求 (SType = 7)
        /// 用于拒绝无效的控制消息
        /// </summary>
        RejectRequest = 7,

        /// <summary>
        /// 分离请求 (SType = 9)
        /// 用于立即断开连接
        /// </summary>
        SeparateRequest = 9
    }
}
