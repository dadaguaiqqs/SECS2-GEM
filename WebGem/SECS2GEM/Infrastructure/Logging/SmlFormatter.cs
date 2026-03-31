using System.Text;
using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;

namespace SECS2GEM.Infrastructure.Logging
{
    /// <summary>
    /// SML格式化器
    /// </summary>
    /// <remarks>
    /// SML (SECS Message Language) 是SECS协议的标准文本表示格式。
    /// 格式示例：
    /// S1F1 W
    /// .
    /// 
    /// S1F2
    /// &lt;L [2]
    ///   &lt;A "MDLN"&gt;
    ///   &lt;A "1.0.0"&gt;
    /// &gt;
    /// .
    /// </remarks>
    public static class SmlFormatter
    {
        /// <summary>
        /// 将HSMS消息转换为SML格式
        /// </summary>
        public static string Format(HsmsMessage message, MessageDirection direction, bool includeTimestamp = true)
        {
            var sb = new StringBuilder();

            // 时间戳和方向
            if (includeTimestamp)
            {
                sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");
            }
            sb.AppendLine(direction == MessageDirection.Send ? ">> SEND" : "<< RECV");

            if (message.IsControlMessage)
            {
                // 控制消息
                FormatControlMessage(sb, message);
            }
            else if (message.SecsMessage != null)
            {
                // 数据消息
                FormatDataMessage(sb, message);
            }

            sb.AppendLine(".");
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// 将SecsMessage转换为SML格式
        /// </summary>
        public static string Format(SecsMessage message)
        {
            var sb = new StringBuilder();
            FormatSecsMessage(sb, message, 0);
            return sb.ToString();
        }

        /// <summary>
        /// 将SecsItem转换为SML格式
        /// </summary>
        public static string Format(SecsItem item)
        {
            var sb = new StringBuilder();
            FormatItem(sb, item, 0);
            return sb.ToString();
        }

        private static void FormatControlMessage(StringBuilder sb, HsmsMessage message)
        {
            var typeName = message.MessageType switch
            {
                HsmsMessageType.SelectRequest => "Select.req",
                HsmsMessageType.SelectResponse => "Select.rsp",
                HsmsMessageType.DeselectRequest => "Deselect.req",
                HsmsMessageType.DeselectResponse => "Deselect.rsp",
                HsmsMessageType.LinktestRequest => "Linktest.req",
                HsmsMessageType.LinktestResponse => "Linktest.rsp",
                HsmsMessageType.RejectRequest => "Reject.req",
                HsmsMessageType.SeparateRequest => "Separate.req",
                _ => $"Control({message.MessageType})"
            };

            sb.AppendLine($"  {typeName}");
            sb.AppendLine($"  SessionId={message.SessionId} SystemBytes={message.SystemBytes}");
        }

        private static void FormatDataMessage(StringBuilder sb, HsmsMessage message)
        {
            sb.AppendLine($"  SessionId={message.SessionId} SystemBytes={message.SystemBytes}");
            
            if (message.SecsMessage != null)
            {
                FormatSecsMessage(sb, message.SecsMessage, 2);
            }
        }

        private static void FormatSecsMessage(StringBuilder sb, SecsMessage message, int indent)
        {
            var prefix = new string(' ', indent);
            
            // Stream/Function和W位
            sb.Append($"{prefix}S{message.Stream}F{message.Function}");
            if (message.WBit)
            {
                sb.Append(" W");
            }
            sb.AppendLine();

            // 数据项
            if (message.Item != null)
            {
                FormatItem(sb, message.Item, indent);
            }
        }

        private static void FormatItem(StringBuilder sb, SecsItem item, int indent)
        {
            var prefix = new string(' ', indent);

            if (item.Format == SecsFormat.List)
            {
                sb.AppendLine($"{prefix}<L [{item.Count}]");
                foreach (var child in item.Items)
                {
                    FormatItem(sb, child, indent + 2);
                }
                sb.AppendLine($"{prefix}>");
            }
            else
            {
                sb.AppendLine($"{prefix}{FormatSingleItem(item)}");
            }
        }

        private static string FormatSingleItem(SecsItem item)
        {
            var formatCode = GetFormatCode(item.Format);

            try
            {
                return item.Format switch
                {
                    SecsFormat.ASCII => $"<A \"{EscapeString(item.GetString())}\">",
                    SecsFormat.JIS8 => $"<J \"{EscapeString(item.GetString())}\">",
                    SecsFormat.Unicode => $"<U \"{EscapeString(item.GetString())}\">",
                    SecsFormat.Binary => FormatBinary(item.GetBytes()),
                    SecsFormat.Boolean => FormatBoolean(item),
                    SecsFormat.I1 or SecsFormat.I2 or SecsFormat.I4 or SecsFormat.I8 =>
                        FormatSignedIntegers(formatCode, item.GetInt64Array()),
                    SecsFormat.U1 or SecsFormat.U2 or SecsFormat.U4 or SecsFormat.U8 =>
                        FormatUnsignedIntegers(formatCode, item.GetUInt64Array()),
                    SecsFormat.F4 or SecsFormat.F8 =>
                        FormatFloats(formatCode, item.GetDoubleArray()),
                    _ => $"<{formatCode} ?>"
                };
            }
            catch
            {
                return $"<{formatCode} (格式化失败)>";
            }
        }

        private static string FormatSignedIntegers(string formatCode, long[] values)
        {
            if (values.Length == 0) return $"<{formatCode}>";
            if (values.Length == 1) return $"<{formatCode} {values[0]}>";
            return $"<{formatCode} [{values.Length}] {string.Join(" ", values)}>";
        }

        private static string FormatUnsignedIntegers(string formatCode, ulong[] values)
        {
            if (values.Length == 0) return $"<{formatCode}>";
            if (values.Length == 1) return $"<{formatCode} {values[0]}>";
            return $"<{formatCode} [{values.Length}] {string.Join(" ", values)}>";
        }

        private static string FormatFloats(string formatCode, double[] values)
        {
            if (values.Length == 0) return $"<{formatCode}>";
            if (values.Length == 1) return $"<{formatCode} {values[0]:G}>";
            return $"<{formatCode} [{values.Length}] {string.Join(" ", values.Select(v => v.ToString("G")))}>";
        }

        private static string GetFormatCode(SecsFormat format)
        {
            return format switch
            {
                SecsFormat.List => "L",
                SecsFormat.Binary => "B",
                SecsFormat.Boolean => "BOOLEAN",
                SecsFormat.ASCII => "A",
                SecsFormat.JIS8 => "J",
                SecsFormat.Unicode => "U",
                SecsFormat.I1 => "I1",
                SecsFormat.I2 => "I2",
                SecsFormat.I4 => "I4",
                SecsFormat.I8 => "I8",
                SecsFormat.U1 => "U1",
                SecsFormat.U2 => "U2",
                SecsFormat.U4 => "U4",
                SecsFormat.U8 => "U8",
                SecsFormat.F4 => "F4",
                SecsFormat.F8 => "F8",
                _ => "?"
            };
        }

        private static string FormatBinary(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return "<B>";
            }
            
            if (bytes.Length <= 16)
            {
                return $"<B 0x{BitConverter.ToString(bytes).Replace("-", " 0x")}>";
            }
            
            // 长数据显示部分
            var preview = bytes.Take(8).ToArray();
            return $"<B [{bytes.Length}] 0x{BitConverter.ToString(preview).Replace("-", " 0x")} ...>";
        }

        private static string FormatBoolean(SecsItem item)
        {
            try
            {
                var values = item.GetBooleans();
                if (values.Length == 0) return "<BOOLEAN>";
                if (values.Length == 1) return $"<BOOLEAN {(values[0] ? "TRUE" : "FALSE")}>";
                return $"<BOOLEAN [{values.Length}] {string.Join(" ", values.Select(b => b ? "T" : "F"))}>";
            }
            catch
            {
                return $"<BOOLEAN ?>";
            }
        }

        private static string EscapeString(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }

        /// <summary>
        /// 格式化原始字节为HEX格式
        /// </summary>
        public static string FormatHex(byte[] bytes, MessageDirection direction, bool includeTimestamp = true)
        {
            var sb = new StringBuilder();

            // 时间戳和方向
            if (includeTimestamp)
            {
                sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");
            }
            sb.AppendLine(direction == MessageDirection.Send ? ">> SEND" : "<< RECV");

            // 格式化字节
            sb.AppendLine(FormatHexDump(bytes));
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// 格式化字节为HEX dump格式
        /// </summary>
        private static string FormatHexDump(byte[] bytes)
        {
            var sb = new StringBuilder();
            const int bytesPerLine = 16;

            for (int i = 0; i < bytes.Length; i += bytesPerLine)
            {
                // 偏移地址
                sb.Append($"  {i:X8}  ");

                // HEX部分
                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (i + j < bytes.Length)
                    {
                        sb.Append($"{bytes[i + j]:X2} ");
                    }
                    else
                    {
                        sb.Append("   ");
                    }

                    if (j == 7) sb.Append(" ");
                }

                sb.Append(" |");

                // ASCII部分
                for (int j = 0; j < bytesPerLine && i + j < bytes.Length; j++)
                {
                    var b = bytes[i + j];
                    sb.Append(b >= 32 && b < 127 ? (char)b : '.');
                }

                sb.AppendLine("|");
            }

            return sb.ToString();
        }
    }
}
