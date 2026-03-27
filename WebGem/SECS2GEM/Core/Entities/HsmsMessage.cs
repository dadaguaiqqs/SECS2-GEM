using SECS2GEM.Core.Enums;

namespace SECS2GEM.Core.Entities
{
    /// <summary>
    /// HSMS消息（完整消息，包含Header和SECS消息体）
    /// </summary>
    /// <remarks>
    /// HSMS消息结构：
    /// ┌────────────────────────────────────────────────────────────┐
    /// │                      HSMS Message                          │
    /// ├──────────────┬─────────────────────────────────────────────┤
    /// │ Message      │              Message Body                   │
    /// │ Length       │  (Header + SECS-II Message Data)           │
    /// │ (4 bytes)    │                                             │
    /// └──────────────┴─────────────────────────────────────────────┘
    /// 
    /// 设计思路：
    /// 1. 封装HSMS协议的完整消息
    /// 2. 区分数据消息和控制消息
    /// 3. 不可变设计保证线程安全
    /// </remarks>
    public sealed class HsmsMessage
    {
        /// <summary>
        /// HSMS消息头
        /// </summary>
        public HsmsHeader Header { get; }

        /// <summary>
        /// SECS消息体（仅数据消息有效）
        /// </summary>
        public SecsMessage? SecsMessage { get; }

        /// <summary>
        /// 原始字节数据（用于调试和日志）
        /// </summary>
        public byte[]? RawBytes { get; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public ushort SessionId => Header.SessionId;

        /// <summary>
        /// 消息类型
        /// </summary>
        public HsmsMessageType MessageType => (HsmsMessageType)Header.SType;

        /// <summary>
        /// System Bytes (事务ID)
        /// </summary>
        public uint SystemBytes => Header.SystemBytes;

        /// <summary>
        /// 是否为数据消息
        /// </summary>
        public bool IsDataMessage => Header.IsDataMessage;

        /// <summary>
        /// 是否为控制消息
        /// </summary>
        public bool IsControlMessage => Header.IsControlMessage;

        /// <summary>
        /// 消息接收/创建时间
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 初始化HSMS消息
        /// </summary>
        /// <param name="header">HSMS头</param>
        /// <param name="secsMessage">SECS消息（可为null）</param>
        /// <param name="rawBytes">原始字节（可为null）</param>
        public HsmsMessage(HsmsHeader header, SecsMessage? secsMessage = null, byte[]? rawBytes = null)
        {
            Header = header;
            SecsMessage = secsMessage;
            RawBytes = rawBytes;
            Timestamp = DateTime.UtcNow;
        }

        #region Static Factory Methods - Data Message

        /// <summary>
        /// 创建数据消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="secsMessage">SECS消息</param>
        /// <param name="systemBytes">事务ID</param>
        /// <returns>HSMS数据消息</returns>
        public static HsmsMessage CreateDataMessage(
            ushort sessionId,
            SecsMessage secsMessage,
            uint systemBytes)
        {
            var header = HsmsHeader.CreateDataMessage(
                sessionId,
                secsMessage.Stream,
                secsMessage.Function,
                secsMessage.WBit,
                systemBytes);

            return new HsmsMessage(header, secsMessage);
        }

        #endregion

        #region Static Factory Methods - Control Messages

        /// <summary>
        /// 创建Select.req消息
        /// </summary>
        public static HsmsMessage CreateSelectRequest(ushort sessionId, uint systemBytes)
        {
            var header = HsmsHeader.CreateSelectRequest(sessionId, systemBytes);
            return new HsmsMessage(header);
        }

        /// <summary>
        /// 创建Select.rsp消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="systemBytes">事务ID（必须与请求匹配）</param>
        /// <param name="selectStatus">选择状态 (0=成功)</param>
        public static HsmsMessage CreateSelectResponse(ushort sessionId, uint systemBytes, byte selectStatus = 0)
        {
            var header = HsmsHeader.CreateSelectResponse(sessionId, systemBytes, selectStatus);
            return new HsmsMessage(header);
        }

        /// <summary>
        /// 创建Deselect.req消息
        /// </summary>
        public static HsmsMessage CreateDeselectRequest(ushort sessionId, uint systemBytes)
        {
            var header = HsmsHeader.CreateDeselectRequest(sessionId, systemBytes);
            return new HsmsMessage(header);
        }

        /// <summary>
        /// 创建Deselect.rsp消息
        /// </summary>
        public static HsmsMessage CreateDeselectResponse(ushort sessionId, uint systemBytes, byte deselectStatus = 0)
        {
            var header = HsmsHeader.CreateDeselectResponse(sessionId, systemBytes, deselectStatus);
            return new HsmsMessage(header);
        }

        /// <summary>
        /// 创建Linktest.req消息
        /// </summary>
        public static HsmsMessage CreateLinktestRequest(uint systemBytes)
        {
            var header = HsmsHeader.CreateLinktestRequest(systemBytes);
            return new HsmsMessage(header);
        }

        /// <summary>
        /// 创建Linktest.rsp消息
        /// </summary>
        public static HsmsMessage CreateLinktestResponse(uint systemBytes)
        {
            var header = HsmsHeader.CreateLinktestResponse(systemBytes);
            return new HsmsMessage(header);
        }

        /// <summary>
        /// 创建Separate.req消息
        /// </summary>
        public static HsmsMessage CreateSeparateRequest(ushort sessionId, uint systemBytes)
        {
            var header = HsmsHeader.CreateSeparateRequest(sessionId, systemBytes);
            return new HsmsMessage(header);
        }

        /// <summary>
        /// 创建Reject.req消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="reasonCode">拒绝原因代码</param>
        /// <param name="systemBytes">事务ID</param>
        public static HsmsMessage CreateRejectRequest(ushort sessionId, byte reasonCode, uint systemBytes)
        {
            var header = HsmsHeader.CreateRejectRequest(sessionId, reasonCode, systemBytes);
            return new HsmsMessage(header);
        }

        #endregion

        #region Response Creation

        /// <summary>
        /// 为当前消息创建响应（用于控制消息）
        /// </summary>
        public HsmsMessage? CreateResponse(byte status = 0)
        {
            return MessageType switch
            {
                HsmsMessageType.SelectRequest => CreateSelectResponse(SessionId, SystemBytes, status),
                HsmsMessageType.DeselectRequest => CreateDeselectResponse(SessionId, SystemBytes, status),
                HsmsMessageType.LinktestRequest => CreateLinktestResponse(SystemBytes),
                _ => null
            };
        }

        #endregion

        public override string ToString()
        {
            if (IsDataMessage && SecsMessage != null)
            {
                return $"HSMS Data: {SecsMessage.Name} [Session={SessionId}, TxID={SystemBytes}]";
            }
            return $"HSMS Control: {Header}";
        }
    }
}
