using System.Net;
using System.Net.Sockets;
using SECS2GEM.Application.Services;
using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;
using SECS2GEM.Infrastructure.Configuration;
using SECS2GEM.Infrastructure.Serialization;

namespace SECS2GEM.Tests;

/// <summary>
/// 集成测试 - 测试完整的通信流程
/// </summary>
public class IntegrationTests : IAsyncLifetime
{
    private GemEquipmentService? _equipment;
    private TcpClient? _hostClient;
    private readonly SecsSerializer _serializer = new();
    private const int TestPort = 15000;

    public async Task InitializeAsync()
    {
        // 创建设备服务（Passive模式）
        var config = new GemConfiguration
        {
            ModelName = "IntegrationTestModel",
            SoftwareRevision = "1.0.0",
            AutoOnline = true,
            InitialRemoteMode = true,
            Hsms = HsmsConfiguration.CreatePassive(TestPort, 1)
        };

        _equipment = new GemEquipmentService(config);
        await _equipment.StartAsync();

        // 等待设备启动
        await Task.Delay(100);
    }

    public async Task DisposeAsync()
    {
        _hostClient?.Close();
        _hostClient?.Dispose();

        if (_equipment != null)
        {
            await _equipment.DisposeAsync();
        }
    }

    #region 连接测试

    [Fact]
    public async Task Equipment_ShouldAcceptConnection()
    {
        // Arrange & Act
        _hostClient = new TcpClient();
        await _hostClient.ConnectAsync(IPAddress.Loopback, TestPort);

        // Assert
        Assert.True(_hostClient.Connected);
    }

    [Fact]
    public async Task Equipment_ShouldRespondToSelectRequest()
    {
        // Arrange
        _hostClient = new TcpClient();
        await _hostClient.ConnectAsync(IPAddress.Loopback, TestPort);
        var stream = _hostClient.GetStream();

        // Act - Send Select.req
        var selectReq = HsmsMessage.CreateSelectRequest(1, 1);
        var requestBytes = _serializer.Serialize(selectReq);
        await stream.WriteAsync(requestBytes);

        // Read response
        var responseBuffer = new byte[14];
        var bytesRead = await stream.ReadAsync(responseBuffer);

        // Assert
        Assert.Equal(14, bytesRead);
        
        // Parse response
        _serializer.TryReadMessage(responseBuffer, out var response, out _);
        Assert.NotNull(response);
        Assert.Equal(HsmsMessageType.SelectResponse, response.MessageType);
    }

    #endregion

    #region 消息通信测试

    [Fact]
    public async Task Equipment_ShouldRespondToS1F1()
    {
        // Arrange - Connect and Select
        _hostClient = new TcpClient();
        await _hostClient.ConnectAsync(IPAddress.Loopback, TestPort);
        var stream = _hostClient.GetStream();

        // Send Select.req
        var selectReq = HsmsMessage.CreateSelectRequest(1, 1);
        await stream.WriteAsync(_serializer.Serialize(selectReq));
        await ReadResponseAsync(stream); // Discard Select.rsp

        // Act - Send S1F1
        var s1f1 = new SecsMessage(1, 1, true);
        var hsmsMsg = HsmsMessage.CreateDataMessage(1, s1f1, 2);
        await stream.WriteAsync(_serializer.Serialize(hsmsMsg));

        // Read response
        var response = await ReadResponseAsync(stream);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsDataMessage);
        Assert.Equal((byte)1, response.SecsMessage?.Stream);
        Assert.Equal((byte)2, response.SecsMessage?.Function);
        Assert.Equal("IntegrationTestModel", response.SecsMessage?.Item?[0].GetString());
    }

    [Fact]
    public async Task Equipment_ShouldRespondToS1F13()
    {
        // Arrange - Connect and Select
        _hostClient = new TcpClient();
        await _hostClient.ConnectAsync(IPAddress.Loopback, TestPort);
        var stream = _hostClient.GetStream();

        await SendSelectAsync(stream);

        // Act - Send S1F13 (Establish Communications)
        var s1f13 = new SecsMessage(1, 13, true);
        var hsmsMsg = HsmsMessage.CreateDataMessage(1, s1f13, 2);
        await stream.WriteAsync(_serializer.Serialize(hsmsMsg));

        var response = await ReadResponseAsync(stream);

        // Assert
        Assert.NotNull(response);
        Assert.Equal((byte)1, response.SecsMessage?.Stream);
        Assert.Equal((byte)14, response.SecsMessage?.Function);
        // COMMACK should be 0 (Accepted)
        Assert.Equal((byte)0, response.SecsMessage?.Item?[0].GetBytes()[0]);
    }

    [Fact]
    public async Task Equipment_ShouldRespondToLinktest()
    {
        // Arrange
        _hostClient = new TcpClient();
        await _hostClient.ConnectAsync(IPAddress.Loopback, TestPort);
        var stream = _hostClient.GetStream();

        await SendSelectAsync(stream);

        // Act - Send Linktest.req
        var linktestReq = HsmsMessage.CreateLinktestRequest(100);
        await stream.WriteAsync(_serializer.Serialize(linktestReq));

        var response = await ReadResponseAsync(stream);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(HsmsMessageType.LinktestResponse, response.MessageType);
        Assert.Equal(100u, response.SystemBytes);
    }

    #endregion

    #region Helper Methods

    private async Task SendSelectAsync(NetworkStream stream)
    {
        var selectReq = HsmsMessage.CreateSelectRequest(1, 1);
        await stream.WriteAsync(_serializer.Serialize(selectReq));
        await ReadResponseAsync(stream); // Discard response
    }

    private async Task<HsmsMessage?> ReadResponseAsync(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer);

        if (bytesRead == 0) return null;

        _serializer.TryReadMessage(buffer.AsSpan(0, bytesRead), out var message, out _);
        return message;
    }

    #endregion
}
