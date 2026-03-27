using System.Runtime.InteropServices;

namespace SECS2GEM.Core.Entities
{
    /// <summary>
    /// HSMS消息头 (10字节)
    /// </summary>
    /// <remarks>
    /// HSMS Header结构：
    /// ┌─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
    /// │ Byte 0  │ Byte 1  │ Byte 2  │ Byte 3  │ Byte 4  │ Byte 5  │ Byte 6  │ Byte 7  │ Byte 8  │ Byte 9  │
    /// ├─────────┴─────────┼─────────┴─────────┼─────────┼─────────┼─────────┴─────────┴─────────┴─────────┤
    /// │   Session ID      │   Header Byte 2-3 │  PType  │  SType  │           System Bytes                │
    /// │   (Device ID)     │  Stream/Function  │         │         │     (Transaction ID)                  │
    /// └───────────────────┴───────────────────┴─────────┴─────────┴───────────────────────────────────────┘
    /// 
    /// Header Byte 2 结构：
    /// - bit 7: W-Bit (等待回复标志)
    /// - bit 0-6: Stream号
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HsmsHeader : IEquatable<HsmsHeader>
    {
        /// <summary>
        /// HSMS头的固定长度
        /// </summary>
        public const int HeaderLength = 10;

        /// <summary>
        /// 会话ID的高字节
        /// </summary>
        public byte SessionIdHigh { get; }

        /// <summary>
        /// 会话ID的低字节
        /// </summary>
        public byte SessionIdLow { get; }

        /// <summary>
        /// Header Byte 2 (W-Bit + Stream)
        /// </summary>
        public byte HeaderByte2 { get; }

        /// <summary>
        /// Header Byte 3 (Function)
        /// </summary>
        public byte HeaderByte3 { get; }

        /// <summary>
        /// PType (表示类型，SECS-II消息固定为0x00)
        /// </summary>
        public byte PType { get; }

        /// <summary>
        /// SType (会话类型)
        /// </summary>
        public byte SType { get; }

        /// <summary>
        /// System Bytes (事务ID) - 字节1
        /// </summary>
        public byte SystemByte1 { get; }

        /// <summary>
        /// System Bytes (事务ID) - 字节2
        /// </summary>
        public byte SystemByte2 { get; }

        /// <summary>
        /// System Bytes (事务ID) - 字节3
        /// </summary>
        public byte SystemByte3 { get; }

        /// <summary>
        /// System Bytes (事务ID) - 字节4
        /// </summary>
        public byte SystemByte4 { get; }

        /// <summary>
        /// 会话ID (设备ID)
        /// </summary>
        public ushort SessionId => (ushort)((SessionIdHigh << 8) | SessionIdLow);

        /// <summary>
        /// W-Bit (等待回复标志)
        /// </summary>
        public bool WBit => (HeaderByte2 & 0x80) != 0;

        /// <summary>
        /// Stream号 (1-127)
        /// </summary>
        public byte Stream => (byte)(HeaderByte2 & 0x7F);

        /// <summary>
        /// Function号
        /// </summary>
        public byte Function => HeaderByte3;

        /// <summary>
        /// System Bytes (事务ID)
        /// </summary>
        public uint SystemBytes =>
            ((uint)SystemByte1 << 24) |
            ((uint)SystemByte2 << 16) |
            ((uint)SystemByte3 << 8) |
            SystemByte4;

        /// <summary>
        /// 是否为数据消息
        /// </summary>
        public bool IsDataMessage => SType == 0;

        /// <summary>
        /// 是否为控制消息
        /// </summary>
        public bool IsControlMessage => SType != 0;

        /// <summary>
        /// 初始化HsmsHeader
        /// </summary>
        public HsmsHeader(
            ushort sessionId,
            byte stream,
            byte function,
            bool wbit,
            byte ptype,
            byte stype,
            uint systemBytes)
        {
            SessionIdHigh = (byte)(sessionId >> 8);
            SessionIdLow = (byte)(sessionId & 0xFF);
            HeaderByte2 = (byte)((wbit ? 0x80 : 0x00) | (stream & 0x7F));
            HeaderByte3 = function;
            PType = ptype;
            SType = stype;
            SystemByte1 = (byte)(systemBytes >> 24);
            SystemByte2 = (byte)(systemBytes >> 16);
            SystemByte3 = (byte)(systemBytes >> 8);
            SystemByte4 = (byte)(systemBytes & 0xFF);
        }

        /// <summary>
        /// 从字节数组创建HsmsHeader
        /// </summary>
        public HsmsHeader(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < HeaderLength)
            {
                throw new ArgumentException($"Header requires at least {HeaderLength} bytes.", nameof(bytes));
            }

            SessionIdHigh = bytes[0];
            SessionIdLow = bytes[1];
            HeaderByte2 = bytes[2];
            HeaderByte3 = bytes[3];
            PType = bytes[4];
            SType = bytes[5];
            SystemByte1 = bytes[6];
            SystemByte2 = bytes[7];
            SystemByte3 = bytes[8];
            SystemByte4 = bytes[9];
        }

        /// <summary>
        /// 将Header转换为字节数组
        /// </summary>
        public byte[] ToBytes()
        {
            return
            [
                SessionIdHigh,
                SessionIdLow,
                HeaderByte2,
                HeaderByte3,
                PType,
                SType,
                SystemByte1,
                SystemByte2,
                SystemByte3,
                SystemByte4
            ];
        }

        /// <summary>
        /// 创建数据消息的Header
        /// </summary>
        public static HsmsHeader CreateDataMessage(
            ushort sessionId,
            byte stream,
            byte function,
            bool wbit,
            uint systemBytes)
        {
            return new HsmsHeader(
                sessionId,
                stream,
                function,
                wbit,
                ptype: 0,
                stype: 0,
                systemBytes);
        }

        /// <summary>
        /// 创建Select.req的Header
        /// </summary>
        public static HsmsHeader CreateSelectRequest(ushort sessionId, uint systemBytes)
        {
            return new HsmsHeader(
                sessionId,
                stream: 0,
                function: 0,
                wbit: false,
                ptype: 0,
                stype: 1,
                systemBytes);
        }

        /// <summary>
        /// 创建Select.rsp的Header
        /// </summary>
        public static HsmsHeader CreateSelectResponse(ushort sessionId, uint systemBytes, byte selectStatus = 0)
        {
            return new HsmsHeader(
                sessionId,
                stream: 0,
                function: selectStatus,
                wbit: false,
                ptype: 0,
                stype: 2,
                systemBytes);
        }

        /// <summary>
        /// 创建Linktest.req的Header
        /// </summary>
        public static HsmsHeader CreateLinktestRequest(uint systemBytes)
        {
            return new HsmsHeader(
                sessionId: 0xFFFF,
                stream: 0,
                function: 0,
                wbit: false,
                ptype: 0,
                stype: 5,
                systemBytes);
        }

        /// <summary>
        /// 创建Linktest.rsp的Header
        /// </summary>
        public static HsmsHeader CreateLinktestResponse(uint systemBytes)
        {
            return new HsmsHeader(
                sessionId: 0xFFFF,
                stream: 0,
                function: 0,
                wbit: false,
                ptype: 0,
                stype: 6,
                systemBytes);
        }

        /// <summary>
        /// 创建Separate.req的Header
        /// </summary>
        public static HsmsHeader CreateSeparateRequest(ushort sessionId, uint systemBytes)
        {
            return new HsmsHeader(
                sessionId,
                stream: 0,
                function: 0,
                wbit: false,
                ptype: 0,
                stype: 9,
                systemBytes);
        }

        /// <summary>
        /// 创建Deselect.req的Header
        /// </summary>
        public static HsmsHeader CreateDeselectRequest(ushort sessionId, uint systemBytes)
        {
            return new HsmsHeader(
                sessionId,
                stream: 0,
                function: 0,
                wbit: false,
                ptype: 0,
                stype: 3,
                systemBytes);
        }

        /// <summary>
        /// 创建Deselect.rsp的Header
        /// </summary>
        public static HsmsHeader CreateDeselectResponse(ushort sessionId, uint systemBytes, byte deselectStatus = 0)
        {
            return new HsmsHeader(
                sessionId,
                stream: 0,
                function: deselectStatus,
                wbit: false,
                ptype: 0,
                stype: 4,
                systemBytes);
        }

        /// <summary>
        /// 创建Reject.req的Header
        /// </summary>
        public static HsmsHeader CreateRejectRequest(ushort sessionId, byte reasonCode, uint systemBytes)
        {
            return new HsmsHeader(
                sessionId,
                stream: 0,
                function: reasonCode,
                wbit: false,
                ptype: 0,
                stype: 7,
                systemBytes);
        }

        public override string ToString()
        {
            if (IsDataMessage)
            {
                return $"S{Stream}F{Function}{(WBit ? " W" : "")} [Session={SessionId}, TxID={SystemBytes}]";
            }

            var sTypeName = SType switch
            {
                1 => "Select.req",
                2 => "Select.rsp",
                3 => "Deselect.req",
                4 => "Deselect.rsp",
                5 => "Linktest.req",
                6 => "Linktest.rsp",
                7 => "Reject.req",
                9 => "Separate.req",
                _ => $"Unknown({SType})"
            };

            return $"{sTypeName} [Session={SessionId}, TxID={SystemBytes}]";
        }

        public bool Equals(HsmsHeader other)
        {
            return SessionIdHigh == other.SessionIdHigh &&
                   SessionIdLow == other.SessionIdLow &&
                   HeaderByte2 == other.HeaderByte2 &&
                   HeaderByte3 == other.HeaderByte3 &&
                   PType == other.PType &&
                   SType == other.SType &&
                   SystemByte1 == other.SystemByte1 &&
                   SystemByte2 == other.SystemByte2 &&
                   SystemByte3 == other.SystemByte3 &&
                   SystemByte4 == other.SystemByte4;
        }

        public override bool Equals(object? obj)
        {
            return obj is HsmsHeader other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                SessionId,
                HeaderByte2,
                HeaderByte3,
                PType,
                SType,
                SystemBytes);
        }

        public static bool operator ==(HsmsHeader left, HsmsHeader right) => left.Equals(right);
        public static bool operator !=(HsmsHeader left, HsmsHeader right) => !left.Equals(right);
    }
}
