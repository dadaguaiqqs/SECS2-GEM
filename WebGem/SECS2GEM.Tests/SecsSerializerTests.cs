using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;
using SECS2GEM.Infrastructure.Serialization;

namespace SECS2GEM.Tests;

/// <summary>
/// SecsSerializer 单元测试
/// </summary>
public class SecsSerializerTests
{
    private readonly SecsSerializer _serializer = new();

    #region SecsItem 序列化测试

    [Fact]
    public void SerializeItem_ASCII_ShouldReturnCorrectBytes()
    {
        // Arrange
        var item = SecsItem.A("TEST");

        // Act
        var bytes = _serializer.SerializeItem(item);

        // Assert
        // Format: 0x41 (ASCII=0x10, 1 length byte=0x01) => 0x10 | 0x01 = 0x11
        // Wait, ASCII format code is 0x10, with 1 length byte: 0x10 | 0x01 = 0x11
        Assert.Equal(0x41, bytes[0]); // Format byte: ASCII (0x40) + 1 length byte
        Assert.Equal(4, bytes[1]);    // Length: 4 bytes
        Assert.Equal((byte)'T', bytes[2]);
        Assert.Equal((byte)'E', bytes[3]);
        Assert.Equal((byte)'S', bytes[4]);
        Assert.Equal((byte)'T', bytes[5]);
    }

    [Fact]
    public void SerializeItem_EmptyList_ShouldReturnCorrectBytes()
    {
        // Arrange
        var item = SecsItem.L();

        // Act
        var bytes = _serializer.SerializeItem(item);

        // Assert
        Assert.Equal(0x01, bytes[0]); // List format (0x00) + 1 length byte
        Assert.Equal(0, bytes[1]);    // 0 items
    }

    [Fact]
    public void SerializeItem_ListWithItems_ShouldReturnCorrectBytes()
    {
        // Arrange
        var item = SecsItem.L(
            SecsItem.A("A"),
            SecsItem.U4(123)
        );

        // Act
        var bytes = _serializer.SerializeItem(item);

        // Assert
        Assert.Equal(0x01, bytes[0]); // List format + 1 length byte
        Assert.Equal(2, bytes[1]);    // 2 items
    }

    [Fact]
    public void SerializeItem_U4_ShouldUseBigEndian()
    {
        // Arrange
        var item = SecsItem.U4(0x12345678);

        // Act
        var bytes = _serializer.SerializeItem(item);

        // Assert
        // U4 format = 0xB0, with 1 length byte = 0xB1
        Assert.Equal(0xB1, bytes[0]);
        Assert.Equal(4, bytes[1]);    // 4 bytes
        Assert.Equal(0x12, bytes[2]); // Big-endian: MSB first
        Assert.Equal(0x34, bytes[3]);
        Assert.Equal(0x56, bytes[4]);
        Assert.Equal(0x78, bytes[5]);
    }

    [Fact]
    public void SerializeItem_Binary_ShouldPreserveData()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03, 0xFF };
        var item = SecsItem.B(data);

        // Act
        var bytes = _serializer.SerializeItem(item);

        // Assert
        Assert.Equal(0x21, bytes[0]); // Binary format (0x20) + 1 length byte
        Assert.Equal(4, bytes[1]);
        Assert.Equal(data, bytes[2..6]);
    }

    #endregion

    #region SecsItem 反序列化测试

    [Fact]
    public void DeserializeItem_ASCII_ShouldReturnCorrectValue()
    {
        // Arrange
        var bytes = new byte[] { 0x41, 0x04, (byte)'T', (byte)'E', (byte)'S', (byte)'T' };

        // Act
        var item = _serializer.DeserializeItem(bytes);

        // Assert
        Assert.Equal(SecsFormat.ASCII, item.Format);
        Assert.Equal("TEST", item.GetString());
    }

    [Fact]
    public void DeserializeItem_U4_ShouldParseBigEndian()
    {
        // Arrange
        var bytes = new byte[] { 0xB1, 0x04, 0x12, 0x34, 0x56, 0x78 };

        // Act
        var item = _serializer.DeserializeItem(bytes);

        // Assert
        Assert.Equal(SecsFormat.U4, item.Format);
        Assert.Equal(0x12345678u, (uint)item.GetUInt64());
    }

    [Fact]
    public void DeserializeItem_NestedList_ShouldParseCorrectly()
    {
        // Arrange - Create nested list and serialize
        var original = SecsItem.L(
            SecsItem.A("MSG"),
            SecsItem.L(
                SecsItem.U4(1),
                SecsItem.U4(2)
            )
        );
        var bytes = _serializer.SerializeItem(original);

        // Act
        var item = _serializer.DeserializeItem(bytes);

        // Assert
        Assert.Equal(SecsFormat.List, item.Format);
        Assert.Equal(2, item.Count);
        Assert.Equal("MSG", item[0].GetString());
        Assert.Equal(SecsFormat.List, item[1].Format);
        Assert.Equal(2, item[1].Count);
    }

    #endregion

    #region 往返测试 (Round-trip)

    [Theory]
    [InlineData("")]
    [InlineData("Hello")]
    [InlineData("Test Message 123")]
    public void RoundTrip_ASCII_ShouldPreserveValue(string value)
    {
        // Arrange
        var original = SecsItem.A(value);

        // Act
        var bytes = _serializer.SerializeItem(original);
        var result = _serializer.DeserializeItem(bytes);

        // Assert
        Assert.Equal(value, result.GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void RoundTrip_I4_ShouldPreserveValue(int value)
    {
        // Arrange
        var original = SecsItem.I4(value);

        // Act
        var bytes = _serializer.SerializeItem(original);
        var result = _serializer.DeserializeItem(bytes);

        // Assert
        Assert.Equal(value, (int)result.GetInt64());
    }

    [Fact]
    public void RoundTrip_ComplexMessage_ShouldPreserveStructure()
    {
        // Arrange - S1F14 like message
        var original = SecsItem.L(
            SecsItem.B(0),  // COMMACK
            SecsItem.L(
                SecsItem.A("MDLN"),
                SecsItem.A("1.0.0")
            )
        );

        // Act
        var bytes = _serializer.SerializeItem(original);
        var result = _serializer.DeserializeItem(bytes);

        // Assert
        Assert.Equal(SecsFormat.List, result.Format);
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].GetBytes()[0]);
        Assert.Equal("MDLN", result[1][0].GetString());
        Assert.Equal("1.0.0", result[1][1].GetString());
    }

    #endregion

    #region HsmsMessage 序列化测试

    [Fact]
    public void Serialize_DataMessage_ShouldIncludeHeader()
    {
        // Arrange
        var secsMsg = new SecsMessage(1, 1, true);
        var hsmsMsg = HsmsMessage.CreateDataMessage(1, secsMsg, 12345);

        // Act
        var bytes = _serializer.Serialize(hsmsMsg);

        // Assert
        // First 4 bytes: message length (big-endian)
        Assert.True(bytes.Length >= 14); // 4 + 10 header minimum
        
        // Length should be 10 (header only, no data item)
        var length = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        Assert.Equal(10, length);
    }

    [Fact]
    public void Serialize_SelectRequest_ShouldBeControlMessage()
    {
        // Arrange
        var hsmsMsg = HsmsMessage.CreateSelectRequest(1, 100);

        // Act
        var bytes = _serializer.Serialize(hsmsMsg);

        // Assert
        Assert.Equal(14, bytes.Length); // 4 length + 10 header
        Assert.Equal(HsmsMessageType.SelectRequest, hsmsMsg.MessageType);
    }

    #endregion

    #region TryReadMessage 测试

    [Fact]
    public void TryReadMessage_IncompleteData_ShouldReturnFalse()
    {
        // Arrange - Only 2 bytes (incomplete length field)
        var buffer = new byte[] { 0x00, 0x00 };

        // Act
        var result = _serializer.TryReadMessage(buffer, out var message, out var consumed);

        // Assert
        Assert.False(result);
        Assert.Null(message);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void TryReadMessage_CompleteMessage_ShouldReturnTrue()
    {
        // Arrange - Create and serialize a message
        var hsmsMsg = HsmsMessage.CreateLinktestRequest(1);
        var bytes = _serializer.Serialize(hsmsMsg);

        // Act
        var result = _serializer.TryReadMessage(bytes, out var message, out var consumed);

        // Assert
        Assert.True(result);
        Assert.NotNull(message);
        Assert.Equal(bytes.Length, consumed);
        Assert.Equal(HsmsMessageType.LinktestRequest, message.MessageType);
    }

    #endregion
}
