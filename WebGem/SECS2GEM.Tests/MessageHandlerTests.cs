using SECS2GEM.Application.Handlers;
using SECS2GEM.Application.Messaging;
using SECS2GEM.Application.State;
using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;
using SECS2GEM.Domain.Interfaces;

namespace SECS2GEM.Tests;

/// <summary>
/// 消息处理器单元测试
/// </summary>
public class MessageHandlerTests
{
    private readonly GemStateManager _stateManager;
    private readonly MockMessageContext _context;

    public MessageHandlerTests()
    {
        _stateManager = new GemStateManager("TestModel", "1.0.0");
        _context = new MockMessageContext(_stateManager);
    }

    #region S1F1 Handler Tests

    [Fact]
    public async Task S1F1Handler_ShouldReturnS1F2WithModelAndVersion()
    {
        // Arrange
        var handler = new S1F1Handler();
        var message = new SecsMessage(1, 1, true);

        // Act
        var response = await handler.HandleAsync(message, _context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, response.Stream);
        Assert.Equal(2, response.Function);
        Assert.NotNull(response.Item);
        Assert.Equal(SecsFormat.List, response.Item.Format);
        Assert.Equal(2, response.Item.Count);
        Assert.Equal("TestModel", response.Item[0].GetString());
        Assert.Equal("1.0.0", response.Item[1].GetString());
    }

    [Fact]
    public void S1F1Handler_CanHandle_ShouldReturnTrueForS1F1()
    {
        var handler = new S1F1Handler();
        var message = new SecsMessage(1, 1, true);

        Assert.True(handler.CanHandle(message));
    }

    [Fact]
    public void S1F1Handler_CanHandle_ShouldReturnFalseForOtherMessages()
    {
        var handler = new S1F1Handler();
        var message = new SecsMessage(1, 13, true);

        Assert.False(handler.CanHandle(message));
    }

    #endregion

    #region S1F13 Handler Tests

    [Fact]
    public async Task S1F13Handler_ShouldSetCommunicatingState()
    {
        // Arrange - 状态转换需要遵循：Disabled → Enabled → WaitCommunicationRequest → Communicating
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
        _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);
        
        var handler = new S1F13Handler();
        var message = new SecsMessage(1, 13, true);

        // Act
        var response = await handler.HandleAsync(message, _context);

        // Assert
        Assert.Equal(GemCommunicationState.Communicating, _stateManager.CommunicationState);
    }

    [Fact]
    public async Task S1F13Handler_ShouldReturnS1F14WithAccepted()
    {
        // Arrange、
        var handler = new S1F13Handler();
        var message = new SecsMessage(1, 13, true);

        // Act
        var response = await handler.HandleAsync(message, _context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, response.Stream);
        Assert.Equal(14, response.Function);
        Assert.NotNull(response.Item);
        Assert.Equal(SecsFormat.List, response.Item.Format);
        Assert.Equal(2, response.Item.Count);
        Assert.Equal(0, response.Item[0].GetBytes()[0]); // COMMACK = 0 (Accepted)
    }

    #endregion

    #region S1F15 Handler Tests

    [Fact]
    public async Task S1F15Handler_Online_ShouldGoOffline()
    {
        // Arrange - Get to Online state
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
        _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);
        _stateManager.SetCommunicationState(GemCommunicationState.Communicating);
        _stateManager.RequestOnline();
        _stateManager.SwitchToRemote();

        var handler = new S1F15Handler();
        var message = new SecsMessage(1, 15, true);

        // Act
        var response = await handler.HandleAsync(message, _context);

        // Assert
        Assert.Equal(GemControlState.EquipmentOffline, _stateManager.ControlState);
        Assert.NotNull(response);
        Assert.Equal(1, response.Stream);
        Assert.Equal(16, response.Function);
        Assert.Equal(0, response.Item!.GetBytes()[0]); // OFLACK = 0 (Success)
    }

    #endregion

    #region S1F17 Handler Tests

    [Fact]
    public async Task S1F17Handler_Communicating_ShouldGoOnline()
    {
        // Arrange
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
        _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);
        _stateManager.SetCommunicationState(GemCommunicationState.Communicating);

        var handler = new S1F17Handler();
        var message = new SecsMessage(1, 17, true);

        // Act
        var response = await handler.HandleAsync(message, _context);

        // Assert
        Assert.Equal(GemControlState.OnlineRemote, _stateManager.ControlState);
        Assert.True(_stateManager.IsOnline);
        Assert.NotNull(response);
        Assert.Equal(1, response.Stream);
        Assert.Equal(18, response.Function);
        Assert.Equal(0, response.Item!.GetBytes()[0]); // ONLACK = 0 (Accepted)
    }

    #endregion

    #region MessageDispatcher Tests

    [Fact]
    public async Task MessageDispatcher_ShouldRouteToCorrectHandler()
    {
        // Arrange
        var dispatcher = new MessageDispatcher();
        dispatcher.RegisterHandler(new S1F1Handler());
        dispatcher.RegisterHandler(new S1F13Handler());

        var message = new SecsMessage(1, 1, true);

        // Act
        var response = await dispatcher.DispatchAsync(message, _context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.Function); // S1F2
    }

    [Fact]
    public async Task MessageDispatcher_NoHandler_ShouldReturnS9F7()
    {
        // Arrange
        var dispatcher = new MessageDispatcher();
        var message = new SecsMessage(99, 99, true); // Unknown message

        // Act
        var response = await dispatcher.DispatchAsync(message, _context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(9, response.Stream);
        Assert.Equal(7, response.Function);
    }

    [Fact]
    public async Task MessageDispatcher_ShouldRespectPriority()
    {
        // Arrange
        var dispatcher = new MessageDispatcher();
        var lowPriorityHandler = new MockHandler(1, 1, 200, "low");
        var highPriorityHandler = new MockHandler(1, 1, 50, "high");
        
        dispatcher.RegisterHandler(lowPriorityHandler);
        dispatcher.RegisterHandler(highPriorityHandler);

        var message = new SecsMessage(1, 1, true);

        // Act
        var response = await dispatcher.DispatchAsync(message, _context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("high", response.Item?.GetString());
    }

    #endregion
}

#region Test Helpers

/// <summary>
/// 模拟消息上下文
/// </summary>
internal class MockMessageContext : IMessageContext
{
    public uint SystemBytes => 1;
    public ushort DeviceId => 1;
    public ISecsConnection Connection => null!;
    public IGemState GemState { get; }
    public DateTime ReceivedTime => DateTime.UtcNow;

    public MockMessageContext(IGemState gemState)
    {
        GemState = gemState;
    }

    public Task ReplyAsync(SecsMessage response, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 模拟处理器（用于测试优先级）
/// </summary>
internal class MockHandler : IMessageHandler
{
    private readonly byte _stream;
    private readonly byte _function;
    private readonly string _response;

    public int Priority { get; }

    public MockHandler(byte stream, byte function, int priority, string response)
    {
        _stream = stream;
        _function = function;
        Priority = priority;
        _response = response;
    }

    public bool CanHandle(SecsMessage message)
    {
        return message.Stream == _stream && message.Function == _function;
    }

    public Task<SecsMessage?> HandleAsync(SecsMessage message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<SecsMessage?>(new SecsMessage(
            _stream, (byte)(_function + 1), false, SecsItem.A(_response)));
    }
}

#endregion
