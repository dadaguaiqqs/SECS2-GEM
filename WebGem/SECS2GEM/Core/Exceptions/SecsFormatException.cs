namespace SECS2GEM.Core.Exceptions
{
    /// <summary>
    /// 格式错误类型
    /// </summary>
    public enum FormatErrorType
    {
        /// <summary>
        /// 无效的格式码
        /// </summary>
        InvalidFormatCode,

        /// <summary>
        /// 无效的长度字节数
        /// </summary>
        InvalidLengthBytes,

        /// <summary>
        /// 数据长度不匹配
        /// </summary>
        LengthMismatch,

        /// <summary>
        /// 数据不完整
        /// </summary>
        IncompleteData,

        /// <summary>
        /// Stream号无效
        /// </summary>
        InvalidStream,

        /// <summary>
        /// Function号无效
        /// </summary>
        InvalidFunction,

        /// <summary>
        /// 消息结构无效
        /// </summary>
        InvalidMessageStructure,

        /// <summary>
        /// HSMS头无效
        /// </summary>
        InvalidHeader
    }

    /// <summary>
    /// SECS格式异常
    /// </summary>
    /// <remarks>
    /// 当消息格式不符合SECS-II/HSMS规范时抛出。
    /// 包含错误位置和类型信息。
    /// </remarks>
    public class SecsFormatException : SecsException
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        public FormatErrorType ErrorType { get; }

        /// <summary>
        /// 错误位置（字节偏移）
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// 期望的值
        /// </summary>
        public string? ExpectedValue { get; }

        /// <summary>
        /// 实际的值
        /// </summary>
        public string? ActualValue { get; }

        /// <summary>
        /// 初始化SecsFormatException
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="position">错误位置</param>
        public SecsFormatException(string message, int position)
            : base($"{message} at position {position}")
        {
            ErrorType = FormatErrorType.InvalidMessageStructure;
            Position = position;
        }

        /// <summary>
        /// 初始化SecsFormatException
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="message">错误消息</param>
        /// <param name="position">错误位置</param>
        public SecsFormatException(FormatErrorType errorType, string message, int position)
            : base($"{message} at position {position}")
        {
            ErrorType = errorType;
            Position = position;
        }

        /// <summary>
        /// 初始化SecsFormatException（带期望/实际值）
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="message">错误消息</param>
        /// <param name="position">错误位置</param>
        /// <param name="expected">期望值</param>
        /// <param name="actual">实际值</param>
        public SecsFormatException(
            FormatErrorType errorType,
            string message,
            int position,
            string expected,
            string actual)
            : base($"{message} at position {position}. Expected: {expected}, Actual: {actual}")
        {
            ErrorType = errorType;
            Position = position;
            ExpectedValue = expected;
            ActualValue = actual;
        }

        /// <summary>
        /// 创建无效格式码异常
        /// </summary>
        public static SecsFormatException InvalidFormatCode(byte formatByte, int position)
        {
            return new SecsFormatException(
                FormatErrorType.InvalidFormatCode,
                $"Invalid format code 0x{formatByte:X2}",
                position);
        }

        /// <summary>
        /// 创建数据不完整异常
        /// </summary>
        public static SecsFormatException IncompleteData(int expected, int actual, int position)
        {
            return new SecsFormatException(
                FormatErrorType.IncompleteData,
                "Incomplete data",
                position,
                $"{expected} bytes",
                $"{actual} bytes");
        }

        /// <summary>
        /// 创建长度不匹配异常
        /// </summary>
        public static SecsFormatException LengthMismatch(int expectedLength, int actualLength, int position)
        {
            return new SecsFormatException(
                FormatErrorType.LengthMismatch,
                "Data length mismatch",
                position,
                $"{expectedLength} bytes",
                $"{actualLength} bytes");
        }

        /// <summary>
        /// 创建无效Stream号异常
        /// </summary>
        public static SecsFormatException InvalidStream(byte stream)
        {
            return new SecsFormatException(
                FormatErrorType.InvalidStream,
                $"Invalid stream number {stream}. Must be between 1 and 127.",
                2);
        }

        /// <summary>
        /// 创建无效HSMS头异常
        /// </summary>
        public static SecsFormatException InvalidHeader(string reason)
        {
            return new SecsFormatException(
                FormatErrorType.InvalidHeader,
                $"Invalid HSMS header: {reason}",
                0);
        }
    }
}
