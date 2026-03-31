using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;
using SECS2GEM.Core.Exceptions;
using SECS2GEM.Domain.Interfaces;

namespace SECS2GEM.Infrastructure.Serialization
{
    /// <summary>
    /// SECS消息序列化器实现
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 1. 序列化：递归遍历SecsItem树，构建字节数组
    /// 2. 反序列化：读取格式码和长度，递归解析数据项
    /// 3. 使用大端序（Big-Endian）进行数据编码
    /// 
    /// HSMS消息结构：
    /// ┌──────────────┬─────────────────────────────────────────┐
    /// │ Message      │              Message Body               │
    /// │ Length       │  (Header 10 bytes + SECS-II Data)      │
    /// │ (4 bytes)    │                                         │
    /// └──────────────┴─────────────────────────────────────────┘
    /// </remarks>
    public sealed class SecsSerializer : ISecsSerializer
    {
        /// <summary>
        /// 消息长度字段的大小
        /// </summary>
        private const int MessageLengthSize = 4;

        /// <summary>
        /// HSMS Header的大小
        /// </summary>
        private const int HeaderSize = 10;

        /// <summary>
        /// 最大消息大小（默认16MB）
        /// </summary>
        public int MaxMessageSize { get; set; } = 16 * 1024 * 1024;

        #region ISecsSerializer Implementation

        /// <summary>
        /// 序列化HSMS消息为字节数组
        /// </summary>
        public byte[] Serialize(HsmsMessage message)
        {
            // 计算消息体大小
            var bodySize = HeaderSize;
            if (message.IsDataMessage && message.SecsMessage?.Item != null)
            {
                bodySize += CalculateItemSize(message.SecsMessage.Item);
            }

            // 分配缓冲区：4字节长度 + 消息体
            var buffer = new byte[MessageLengthSize + bodySize];
            var span = buffer.AsSpan();

            // 写入消息长度（大端序）
            BinaryPrimitives.WriteInt32BigEndian(span[..4], bodySize);

            // 写入Header
            var headerBytes = message.Header.ToBytes();
            headerBytes.CopyTo(span.Slice(4, HeaderSize));

            // 写入数据项
            if (message.IsDataMessage && message.SecsMessage?.Item != null)
            {
                var itemSpan = span[(MessageLengthSize + HeaderSize)..];
                SerializeItemToSpan(message.SecsMessage.Item, itemSpan, out _);
            }

            return buffer;
        }

        /// <summary>
        /// 序列化数据项为字节数组
        /// </summary>
        public byte[] SerializeItem(SecsItem item)
        {
            var size = CalculateItemSize(item);
            var buffer = new byte[size];
            SerializeItemToSpan(item, buffer.AsSpan(), out _);
            return buffer;
        }

        /// <summary>
        /// 从字节数组反序列化HSMS消息
        /// </summary>
        public HsmsMessage Deserialize(ReadOnlySpan<byte> data)
        {
            if (data.Length < HeaderSize)
            {
                throw SecsFormatException.IncompleteData(HeaderSize, data.Length, 0);
            }

            // 解析Header
            var header = new HsmsHeader(data[..HeaderSize]);

            // 如果是数据消息，解析数据项
            SecsMessage? secsMessage = null;
            if (header.IsDataMessage && data.Length > HeaderSize)
            {
                var itemData = data[HeaderSize..];
                var item = DeserializeItemFromSpan(itemData, out _);

                secsMessage = new SecsMessage(
                    header.Stream,
                    header.Function,
                    header.WBit,
                    item);
            }
            else if (header.IsDataMessage)
            {
                // 数据消息但没有数据项
                secsMessage = new SecsMessage(
                    header.Stream,
                    header.Function,
                    header.WBit);
            }

            return new HsmsMessage(header, secsMessage, data.ToArray());
        }

        /// <summary>
        /// 反序列化数据项
        /// </summary>
        public SecsItem DeserializeItem(ReadOnlySpan<byte> data)
        {
            return DeserializeItemFromSpan(data, out _);
        }

        /// <summary>
        /// 尝试从缓冲区读取完整消息
        /// </summary>
        public bool TryReadMessage(ReadOnlySpan<byte> buffer, out HsmsMessage? message, out int bytesConsumed)
        {
            message = null;
            bytesConsumed = 0;

            // 至少需要4字节长度字段
            if (buffer.Length < MessageLengthSize)
            {
                return false;
            }

            // 读取消息长度
            var messageLength = BinaryPrimitives.ReadInt32BigEndian(buffer[..4]);

            // 验证消息长度
            if (messageLength < HeaderSize)
            {
                throw SecsFormatException.InvalidHeader($"Message length {messageLength} is less than header size");
            }

            if (messageLength > MaxMessageSize)
            {
                throw SecsFormatException.InvalidHeader($"Message length {messageLength} exceeds maximum {MaxMessageSize}");
            }

            // 检查是否有完整消息
            var totalLength = MessageLengthSize + messageLength;
            if (buffer.Length < totalLength)
            {
                return false;
            }

            // 反序列化消息
            var messageData = buffer.Slice(MessageLengthSize, messageLength);
            message = Deserialize(messageData);
            bytesConsumed = totalLength;

            return true;
        }

        #endregion

        #region Item Serialization

        /// <summary>
        /// 计算数据项的序列化大小
        /// </summary>
        private int CalculateItemSize(SecsItem item)
        {
            var dataLength = GetDataLength(item);
            var numLengthBytes = GetNumLengthBytes(dataLength);

            // 格式字节(1) + 长度字节(1-3) + 数据
            var size = 1 + numLengthBytes + dataLength;

            // 如果是List，加上子项大小
            if (item.Format == SecsFormat.List)
            {
                size = 1 + numLengthBytes; // List的dataLength是子项数量，不是字节数
                foreach (var subItem in item.Items)
                {
                    size += CalculateItemSize(subItem);
                }
            }

            return size;
        }

        /// <summary>
        /// 获取数据长度
        /// </summary>
        private int GetDataLength(SecsItem item)
        {
            return item.Format switch
            {
                SecsFormat.List => item.Count,
                SecsFormat.Binary => item.Count,
                SecsFormat.Boolean => item.Count,
                SecsFormat.ASCII => item.Count,
                SecsFormat.JIS8 => item.Count,
                SecsFormat.Unicode => item.Count * 2,
                SecsFormat.I1 => item.Count,
                SecsFormat.I2 => item.Count * 2,
                SecsFormat.I4 => item.Count * 4,
                SecsFormat.I8 => item.Count * 8,
                SecsFormat.U1 => item.Count,
                SecsFormat.U2 => item.Count * 2,
                SecsFormat.U4 => item.Count * 4,
                SecsFormat.U8 => item.Count * 8,
                SecsFormat.F4 => item.Count * 4,
                SecsFormat.F8 => item.Count * 8,
                _ => 0
            };
        }

        /// <summary>
        /// 获取长度字节数
        /// </summary>
        private int GetNumLengthBytes(int length)
        {
            if (length == 0) return 1; // 空数据仍需1字节长度
            if (length <= 255) return 1;
            if (length <= 65535) return 2;
            return 3;
        }

        /// <summary>
        /// 序列化数据项到Span
        /// </summary>
        private void SerializeItemToSpan(SecsItem item, Span<byte> span, out int bytesWritten)
        {
            var dataLength = GetDataLength(item);
            var numLengthBytes = GetNumLengthBytes(dataLength);

            // 写入格式字节：高6位是格式，低2位是长度字节数
            span[0] = (byte)((byte)item.Format | numLengthBytes);

            // 写入长度字节（大端序）
            WriteLengthBytes(span.Slice(1, numLengthBytes), dataLength, numLengthBytes);

            var offset = 1 + numLengthBytes;

            // 写入数据
            if (item.Format == SecsFormat.List)
            {
                // List类型：递归序列化子项
                foreach (var subItem in item.Items)
                {
                    SerializeItemToSpan(subItem, span[offset..], out var subBytesWritten);
                    offset += subBytesWritten;
                }
            }
            else
            {
                // 其他类型：写入数据值
                WriteItemData(item, span[offset..]);
                offset += dataLength;
            }

            bytesWritten = offset;
        }

        /// <summary>
        /// 写入长度字节
        /// </summary>
        private void WriteLengthBytes(Span<byte> span, int length, int numBytes)
        {
            switch (numBytes)
            {
                case 1:
                    span[0] = (byte)length;
                    break;
                case 2:
                    span[0] = (byte)(length >> 8);
                    span[1] = (byte)(length & 0xFF);
                    break;
                case 3:
                    span[0] = (byte)(length >> 16);
                    span[1] = (byte)((length >> 8) & 0xFF);
                    span[2] = (byte)(length & 0xFF);
                    break;
            }
        }

        /// <summary>
        /// 写入数据值
        /// </summary>
        private void WriteItemData(SecsItem item, Span<byte> span)
        {
            switch (item.Format)
            {
                case SecsFormat.ASCII:
                case SecsFormat.JIS8:
                    var str = item.GetString();
                    Encoding.ASCII.GetBytes(str, span);
                    break;

                case SecsFormat.Unicode:
                    var uniStr = item.GetString();
                    Encoding.BigEndianUnicode.GetBytes(uniStr, span);
                    break;

                case SecsFormat.Binary:
                    var bytes = item.GetBytes();
                    bytes.CopyTo(span);
                    break;

                case SecsFormat.Boolean:
                    var bools = item.GetBooleans();
                    for (int i = 0; i < bools.Length; i++)
                    {
                        span[i] = bools[i] ? (byte)1 : (byte)0;
                    }
                    break;

                case SecsFormat.I1:
                    var i1Arr = (sbyte[])item.Value;
                    for (int i = 0; i < i1Arr.Length; i++)
                    {
                        span[i] = (byte)i1Arr[i];
                    }
                    break;

                case SecsFormat.I2:
                    var i2Arr = (short[])item.Value;
                    for (int i = 0; i < i2Arr.Length; i++)
                    {
                        BinaryPrimitives.WriteInt16BigEndian(span[(i * 2)..], i2Arr[i]);
                    }
                    break;

                case SecsFormat.I4:
                    var i4Arr = (int[])item.Value;
                    for (int i = 0; i < i4Arr.Length; i++)
                    {
                        BinaryPrimitives.WriteInt32BigEndian(span[(i * 4)..], i4Arr[i]);
                    }
                    break;

                case SecsFormat.I8:
                    var i8Arr = (long[])item.Value;
                    for (int i = 0; i < i8Arr.Length; i++)
                    {
                        BinaryPrimitives.WriteInt64BigEndian(span[(i * 8)..], i8Arr[i]);
                    }
                    break;

                case SecsFormat.U1:
                    var u1Arr = (byte[])item.Value;
                    u1Arr.CopyTo(span);
                    break;

                case SecsFormat.U2:
                    var u2Arr = (ushort[])item.Value;
                    for (int i = 0; i < u2Arr.Length; i++)
                    {
                        BinaryPrimitives.WriteUInt16BigEndian(span[(i * 2)..], u2Arr[i]);
                    }
                    break;

                case SecsFormat.U4:
                    var u4Arr = (uint[])item.Value;
                    for (int i = 0; i < u4Arr.Length; i++)
                    {
                        BinaryPrimitives.WriteUInt32BigEndian(span[(i * 4)..], u4Arr[i]);
                    }
                    break;

                case SecsFormat.U8:
                    var u8Arr = (ulong[])item.Value;
                    for (int i = 0; i < u8Arr.Length; i++)
                    {
                        BinaryPrimitives.WriteUInt64BigEndian(span[(i * 8)..], u8Arr[i]);
                    }
                    break;

                case SecsFormat.F4:
                    var f4Arr = (float[])item.Value;
                    for (int i = 0; i < f4Arr.Length; i++)
                    {
                        BinaryPrimitives.WriteSingleBigEndian(span[(i * 4)..], f4Arr[i]);
                    }
                    break;

                case SecsFormat.F8:
                    var f8Arr = (double[])item.Value;
                    for (int i = 0; i < f8Arr.Length; i++)
                    {
                        BinaryPrimitives.WriteDoubleBigEndian(span[(i * 8)..], f8Arr[i]);
                    }
                    break;
            }
        }

        #endregion

        #region Item Deserialization

        /// <summary>
        /// 从Span反序列化数据项
        /// </summary>
        private SecsItem DeserializeItemFromSpan(ReadOnlySpan<byte> span, out int bytesRead)
        {
            if (span.Length < 1)
            {
                throw SecsFormatException.IncompleteData(1, span.Length, 0);
            }

            // 读取格式字节
            var formatByte = span[0];
            var format = (SecsFormat)(formatByte & 0xFC); // 高6位
            var numLengthBytes = formatByte & 0x03;        // 低2位

            if (numLengthBytes == 0)
            {
                // 空数据项
                bytesRead = 1;
                return CreateEmptyItem(format);
            }

            if (span.Length < 1 + numLengthBytes)
            {
                throw SecsFormatException.IncompleteData(1 + numLengthBytes, span.Length, 0);
            }

            // 读取长度
            var length = ReadLengthBytes(span.Slice(1, numLengthBytes), numLengthBytes);
            var headerSize = 1 + numLengthBytes;

            if (format == SecsFormat.List)
            {
                // List类型：length是子项数量
                var items = new List<SecsItem>(length);
                var offset = headerSize;

                for (int i = 0; i < length; i++)
                {
                    var subItem = DeserializeItemFromSpan(span[offset..], out var subBytesRead);
                    items.Add(subItem);
                    offset += subBytesRead;
                }

                bytesRead = offset;
                return SecsItem.L(items);
            }
            else
            {
                // 其他类型：length是数据字节数
                if (span.Length < headerSize + length)
                {
                    throw SecsFormatException.IncompleteData(headerSize + length, span.Length, 0);
                }

                var dataSpan = span.Slice(headerSize, length);
                var item = ReadItemData(format, dataSpan);
                bytesRead = headerSize + length;
                return item;
            }
        }

        /// <summary>
        /// 读取长度字节
        /// </summary>
        private int ReadLengthBytes(ReadOnlySpan<byte> span, int numBytes)
        {
            return numBytes switch
            {
                1 => span[0],
                2 => (span[0] << 8) | span[1],
                3 => (span[0] << 16) | (span[1] << 8) | span[2],
                _ => 0
            };
        }

        /// <summary>
        /// 创建空数据项
        /// </summary>
        private SecsItem CreateEmptyItem(SecsFormat format)
        {
            return format switch
            {
                SecsFormat.List => SecsItem.L(),
                SecsFormat.ASCII => SecsItem.A(""),
                SecsFormat.JIS8 => SecsItem.J(""),
                SecsFormat.Binary => SecsItem.B(Array.Empty<byte>()),
                SecsFormat.Boolean => SecsItem.Boolean(Array.Empty<bool>()),
                SecsFormat.I1 => SecsItem.I1(Array.Empty<sbyte>()),
                SecsFormat.I2 => SecsItem.I2(Array.Empty<short>()),
                SecsFormat.I4 => SecsItem.I4(Array.Empty<int>()),
                SecsFormat.I8 => SecsItem.I8(Array.Empty<long>()),
                SecsFormat.U1 => SecsItem.U1(Array.Empty<byte>()),
                SecsFormat.U2 => SecsItem.U2(Array.Empty<ushort>()),
                SecsFormat.U4 => SecsItem.U4(Array.Empty<uint>()),
                SecsFormat.U8 => SecsItem.U8(Array.Empty<ulong>()),
                SecsFormat.F4 => SecsItem.F4(Array.Empty<float>()),
                SecsFormat.F8 => SecsItem.F8(Array.Empty<double>()),
                _ => throw SecsFormatException.InvalidFormatCode((byte)format, 0)
            };
        }

        /// <summary>
        /// 读取数据项数据
        /// </summary>
        private SecsItem ReadItemData(SecsFormat format, ReadOnlySpan<byte> data)
        {
            return format switch
            {
                SecsFormat.ASCII => SecsItem.A(Encoding.ASCII.GetString(data)),
                SecsFormat.JIS8 => SecsItem.J(Encoding.ASCII.GetString(data)),
                SecsFormat.Unicode => SecsItem.U(Encoding.BigEndianUnicode.GetString(data)),
                SecsFormat.Binary => SecsItem.B(data.ToArray()),
                SecsFormat.Boolean => SecsItem.Boolean(ReadBooleans(data)),
                SecsFormat.I1 => SecsItem.I1(ReadSBytes(data)),
                SecsFormat.I2 => SecsItem.I2(ReadInt16Array(data)),
                SecsFormat.I4 => SecsItem.I4(ReadInt32Array(data)),
                SecsFormat.I8 => SecsItem.I8(ReadInt64Array(data)),
                SecsFormat.U1 => SecsItem.U1(data.ToArray()),
                SecsFormat.U2 => SecsItem.U2(ReadUInt16Array(data)),
                SecsFormat.U4 => SecsItem.U4(ReadUInt32Array(data)),
                SecsFormat.U8 => SecsItem.U8(ReadUInt64Array(data)),
                SecsFormat.F4 => SecsItem.F4(ReadSingleArray(data)),
                SecsFormat.F8 => SecsItem.F8(ReadDoubleArray(data)),
                _ => throw SecsFormatException.InvalidFormatCode((byte)format, 0)
            };
        }

        private bool[] ReadBooleans(ReadOnlySpan<byte> data)
        {
            var result = new bool[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                // SECS-II 标准：非零字节为 true，零字节为 false
                result[i] = data[i] != 0;
            }
            
            // 调试：记录原始字节和解析结果
            System.Diagnostics.Debug.WriteLine(
                $"[Boolean] Raw bytes: [{string.Join(" ", data.ToArray().Select(b => $"0x{b:X2}"))}] -> [{string.Join(", ", result)}]");
            
            return result;
        }

        private sbyte[] ReadSBytes(ReadOnlySpan<byte> data)
        {
            var result = new sbyte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (sbyte)data[i];
            }
            return result;
        }

        private short[] ReadInt16Array(ReadOnlySpan<byte> data)
        {
            var count = data.Length / 2;
            var result = new short[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadInt16BigEndian(data[(i * 2)..]);
            }
            return result;
        }

        private int[] ReadInt32Array(ReadOnlySpan<byte> data)
        {
            var count = data.Length / 4;
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadInt32BigEndian(data[(i * 4)..]);
            }
            return result;
        }

        private long[] ReadInt64Array(ReadOnlySpan<byte> data)
        {
            var count = data.Length / 8;
            var result = new long[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadInt64BigEndian(data[(i * 8)..]);
            }
            return result;
        }

        private ushort[] ReadUInt16Array(ReadOnlySpan<byte> data)
        {
            var count = data.Length / 2;
            var result = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt16BigEndian(data[(i * 2)..]);
            }
            return result;
        }

        private uint[] ReadUInt32Array(ReadOnlySpan<byte> data)
        {
            var count = data.Length / 4;
            var result = new uint[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt32BigEndian(data[(i * 4)..]);
            }
            return result;
        }

        private ulong[] ReadUInt64Array(ReadOnlySpan<byte> data)
        {
            var count = data.Length / 8;
            var result = new ulong[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadUInt64BigEndian(data[(i * 8)..]);
            }
            return result;
        }

        private float[] ReadSingleArray(ReadOnlySpan<byte> data)
        {
            var count = data.Length / 4;
            var result = new float[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadSingleBigEndian(data[(i * 4)..]);
            }
            return result;
        }

        private double[] ReadDoubleArray(ReadOnlySpan<byte> data)
        {
            var count = data.Length / 8;
            var result = new double[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadDoubleBigEndian(data[(i * 8)..]);
            }
            return result;
        }

        #endregion
    }
}
