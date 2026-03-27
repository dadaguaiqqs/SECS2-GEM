using SECS2GEM.Application.State;
using SECS2GEM.Core.Enums;
using SECS2GEM.Domain.Models;

namespace SECS2GEM.Tests;

/// <summary>
/// GemStateManager 单元测试
/// </summary>
public class GemStateManagerTests
{
    private readonly GemStateManager _stateManager;

    public GemStateManagerTests()
    {
        _stateManager = new GemStateManager("TestModel", "1.0.0");
    }

    #region 初始状态测试

    [Fact]
    public void Constructor_ShouldSetCorrectInitialValues()
    {
        Assert.Equal("TestModel", _stateManager.ModelName);
        Assert.Equal("1.0.0", _stateManager.SoftwareRevision);
        Assert.Equal(GemCommunicationState.Disabled, _stateManager.CommunicationState);
        Assert.Equal(GemControlState.EquipmentOffline, _stateManager.ControlState);
        Assert.Equal(GemProcessingState.Idle, _stateManager.ProcessingState);
        Assert.False(_stateManager.IsOnline);
        Assert.False(_stateManager.IsRemoteControl);
    }

    [Fact]
    public void Constructor_ShouldRegisterStandardStatusVariables()
    {
        // Clock variable (SVID 1) should be registered
        var clockValue = _stateManager.GetStatusVariable(1);
        Assert.NotNull(clockValue);
        Assert.IsType<string>(clockValue);

        // ControlState variable (SVID 2) should be registered
        var controlStateValue = _stateManager.GetStatusVariable(2);
        Assert.NotNull(controlStateValue);
    }

    #endregion

    #region 通信状态转换测试

    [Fact]
    public void SetCommunicationState_DisabledToEnabled_ShouldSucceed()
    {
        var result = _stateManager.SetCommunicationState(GemCommunicationState.Enabled);

        Assert.True(result);
        Assert.Equal(GemCommunicationState.Enabled, _stateManager.CommunicationState);
    }

    [Fact]
    public void SetCommunicationState_EnabledToWaitCommunicationRequest_ShouldSucceed()
    {
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);

        var result = _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);

        Assert.True(result);
        Assert.Equal(GemCommunicationState.WaitCommunicationRequest, _stateManager.CommunicationState);
    }

    [Fact]
    public void SetCommunicationState_ToCommunicating_ShouldSucceed()
    {
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
        _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);

        var result = _stateManager.SetCommunicationState(GemCommunicationState.Communicating);

        Assert.True(result);
        Assert.Equal(GemCommunicationState.Communicating, _stateManager.CommunicationState);
    }

    [Fact]
    public void SetCommunicationState_ShouldFireEvent()
    {
        GemCommunicationState? receivedState = null;
        _stateManager.CommunicationStateChanged += (s, state) => receivedState = state;

        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);

        Assert.Equal(GemCommunicationState.Enabled, receivedState);
    }

    #endregion

    #region 控制状态转换测试

    [Fact]
    public void RequestOnline_NotCommunicating_ShouldFail()
    {
        var result = _stateManager.RequestOnline();

        Assert.False(result);
        Assert.Equal(GemControlState.EquipmentOffline, _stateManager.ControlState);
    }

    [Fact]
    public void RequestOnline_Communicating_ShouldTransitionToAttemptOnline()
    {
        // Setup: Get to Communicating state
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
        _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);
        _stateManager.SetCommunicationState(GemCommunicationState.Communicating);

        var result = _stateManager.RequestOnline();

        Assert.True(result);
        Assert.Equal(GemControlState.AttemptOnline, _stateManager.ControlState);
    }

    [Fact]
    public void SwitchToRemote_FromAttemptOnline_ShouldSucceed()
    {
        // Setup
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
        _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);
        _stateManager.SetCommunicationState(GemCommunicationState.Communicating);
        _stateManager.RequestOnline();

        var result = _stateManager.SwitchToRemote();

        Assert.True(result);
        Assert.Equal(GemControlState.OnlineRemote, _stateManager.ControlState);
        Assert.True(_stateManager.IsOnline);
        Assert.True(_stateManager.IsRemoteControl);
    }

    [Fact]
    public void SwitchToLocal_FromOnlineRemote_ShouldSucceed()
    {
        // Setup
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
        _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);
        _stateManager.SetCommunicationState(GemCommunicationState.Communicating);
        _stateManager.RequestOnline();
        _stateManager.SwitchToRemote();

        var result = _stateManager.SwitchToLocal();

        Assert.True(result);
        Assert.Equal(GemControlState.OnlineLocal, _stateManager.ControlState);
        Assert.True(_stateManager.IsOnline);
        Assert.False(_stateManager.IsRemoteControl);
    }

    [Fact]
    public void RequestOffline_FromOnline_ShouldSucceed()
    {
        // Setup
        _stateManager.SetCommunicationState(GemCommunicationState.Enabled);
        _stateManager.SetCommunicationState(GemCommunicationState.WaitCommunicationRequest);
        _stateManager.SetCommunicationState(GemCommunicationState.Communicating);
        _stateManager.RequestOnline();
        _stateManager.SwitchToRemote();

        var result = _stateManager.RequestOffline();

        Assert.True(result);
        Assert.Equal(GemControlState.EquipmentOffline, _stateManager.ControlState);
        Assert.False(_stateManager.IsOnline);
    }

    #endregion

    #region 处理状态转换测试

    [Fact]
    public void SetProcessingState_IdleToSetup_ShouldSucceed()
    {
        var result = _stateManager.SetProcessingState(GemProcessingState.Setup);

        Assert.True(result);
        Assert.Equal(GemProcessingState.Setup, _stateManager.ProcessingState);
    }

    [Fact]
    public void SetProcessingState_SetupToReady_ShouldSucceed()
    {
        _stateManager.SetProcessingState(GemProcessingState.Setup);

        var result = _stateManager.SetProcessingState(GemProcessingState.Ready);

        Assert.True(result);
        Assert.Equal(GemProcessingState.Ready, _stateManager.ProcessingState);
    }

    [Fact]
    public void SetProcessingState_ReadyToExecuting_ShouldSucceed()
    {
        _stateManager.SetProcessingState(GemProcessingState.Ready);

        var result = _stateManager.SetProcessingState(GemProcessingState.Executing);

        Assert.True(result);
        Assert.Equal(GemProcessingState.Executing, _stateManager.ProcessingState);
    }

    [Fact]
    public void SetProcessingState_ExecutingToPaused_ShouldSucceed()
    {
        _stateManager.SetProcessingState(GemProcessingState.Ready);
        _stateManager.SetProcessingState(GemProcessingState.Executing);

        var result = _stateManager.SetProcessingState(GemProcessingState.Paused);

        Assert.True(result);
        Assert.Equal(GemProcessingState.Paused, _stateManager.ProcessingState);
    }

    #endregion

    #region 状态变量测试

    [Fact]
    public void RegisterStatusVariable_ShouldStoreVariable()
    {
        var sv = new StatusVariable
        {
            VariableId = 100,
            Name = "TestVar",
            Value = 42
        };

        _stateManager.RegisterStatusVariable(sv);
        var value = _stateManager.GetStatusVariable(100);

        Assert.Equal(42, value);
    }

    [Fact]
    public void SetStatusVariable_ShouldUpdateValue()
    {
        var sv = new StatusVariable
        {
            VariableId = 100,
            Name = "TestVar",
            Value = 42
        };
        _stateManager.RegisterStatusVariable(sv);

        _stateManager.SetStatusVariable(100, 100);
        var value = _stateManager.GetStatusVariable(100);

        Assert.Equal(100, value);
    }

    [Fact]
    public void GetStatusVariable_WithValueGetter_ShouldUseDynamicValue()
    {
        var counter = 0;
        var sv = new StatusVariable
        {
            VariableId = 100,
            Name = "Counter",
            ValueGetter = () => ++counter
        };
        _stateManager.RegisterStatusVariable(sv);

        var value1 = _stateManager.GetStatusVariable(100);
        var value2 = _stateManager.GetStatusVariable(100);

        Assert.Equal(1, value1);
        Assert.Equal(2, value2);
    }

    [Fact]
    public void GetAllStatusVariables_ShouldReturnAllRegistered()
    {
        var variables = _stateManager.GetAllStatusVariables();

        // Should have at least the standard variables (Clock, ControlState)
        Assert.True(variables.Count >= 2);
    }

    #endregion

    #region 设备常量测试

    [Fact]
    public void RegisterEquipmentConstant_ShouldStoreConstant()
    {
        var ec = new EquipmentConstant
        {
            ConstantId = 100,
            Name = "TestEC",
            Value = "TestValue"
        };

        _stateManager.RegisterEquipmentConstant(ec);
        var value = _stateManager.GetEquipmentConstant(100);

        Assert.Equal("TestValue", value);
    }

    [Fact]
    public void TrySetEquipmentConstant_ValidValue_ShouldSucceed()
    {
        var ec = new EquipmentConstant
        {
            ConstantId = 100,
            Name = "TestEC",
            Value = 50,
            MinValue = 0,
            MaxValue = 100
        };
        _stateManager.RegisterEquipmentConstant(ec);

        var result = _stateManager.TrySetEquipmentConstant(100, 75);
        var value = _stateManager.GetEquipmentConstant(100);

        Assert.True(result);
        Assert.Equal(75, value);
    }

    [Fact]
    public void TrySetEquipmentConstant_ReadOnly_ShouldFail()
    {
        var ec = new EquipmentConstant
        {
            ConstantId = 100,
            Name = "ReadOnlyEC",
            Value = "Original",
            IsReadOnly = true
        };
        _stateManager.RegisterEquipmentConstant(ec);

        var result = _stateManager.TrySetEquipmentConstant(100, "NewValue");
        var value = _stateManager.GetEquipmentConstant(100);

        Assert.False(result);
        Assert.Equal("Original", value);
    }

    [Fact]
    public void TrySetEquipmentConstant_OutOfRange_ShouldFail()
    {
        var ec = new EquipmentConstant
        {
            ConstantId = 100,
            Name = "RangeEC",
            Value = 50,
            MinValue = 0,
            MaxValue = 100
        };
        _stateManager.RegisterEquipmentConstant(ec);

        var result = _stateManager.TrySetEquipmentConstant(100, 150);
        var value = _stateManager.GetEquipmentConstant(100);

        Assert.False(result);
        Assert.Equal(50, value);
    }

    #endregion
}
