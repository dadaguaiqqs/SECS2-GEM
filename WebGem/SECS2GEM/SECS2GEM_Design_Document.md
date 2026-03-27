# SECS2GEM 模块设计文档

## 目录
1. [项目概述](#1-项目概述)
2. [架构设计](#2-架构设计)
3. [设计原则](#3-设计原则)
4. [核心模块设计](#4-核心模块设计)
5. [设计模式应用](#5-设计模式应用)
6. [接口定义](#6-接口定义)
7. [数据流程](#7-数据流程)
8. [状态管理](#8-状态管理)
9. [错误处理](#9-错误处理)
10. [扩展性设计](#10-扩展性设计)
11. [项目结构](#11-项目结构)

---

## 1. 项目概述

### 1.1 项目定位

SECS2GEM是一个半导体设备通信中间件模块，负责实现GEM（Generic Equipment Model）协议栈，提供设备与主机之间的标准化通信能力。

### 1.2 核心功能

```
┌─────────────────────────────────────────────────────────────────────┐
│                           WebGem 系统                               │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   ┌─────────────┐      ┌─────────────────┐      ┌─────────────┐   │
│   │   WebAPI    │◄────►│    SECS2GEM     │◄────►│    设备     │   │
│   │   客户端    │      │    中间件模块    │      │  (Equipment)│   │
│   └─────────────┘      └─────────────────┘      └─────────────┘   │
│                                                                     │
│   功能：                                                            │
│   • 消息格式转换（JSON ↔ SECS-II）                                 │
│   • HSMS通信管理                                                    │
│   • GEM状态机管理                                                   │
│   • 事件/报警处理                                                   │
│   • 配方管理                                                        │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.3 设计目标

| 目标 | 描述 |
|------|------|
| **高内聚** | 每个模块专注单一职责，模块内部紧密关联 |
| **低耦合** | 模块间通过抽象接口交互，降低依赖 |
| **可扩展** | 支持新消息类型、新设备类型的扩展 |
| **可测试** | 依赖注入支持单元测试和模拟 |
| **高性能** | 异步处理、连接池、消息缓冲 |

---

## 2. 架构设计

### 2.1 分层架构

采用**洋葱架构（Onion Architecture）**，核心业务逻辑位于中心，外层依赖内层。

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Infrastructure Layer                         │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │                      Application Layer                        │ │
│  │  ┌─────────────────────────────────────────────────────────┐ │ │
│  │  │                    Domain Layer                         │ │ │
│  │  │  ┌───────────────────────────────────────────────────┐ │ │ │
│  │  │  │              Core (Entities)                      │ │ │ │
│  │  │  │                                                   │ │ │ │
│  │  │  │  • SecsMessage    • SecsItem                     │ │ │ │
│  │  │  │  • HsmsMessage    • GemState                     │ │ │ │
│  │  │  │                                                   │ │ │ │
│  │  │  └───────────────────────────────────────────────────┘ │ │ │
│  │  │                                                         │ │ │
│  │  │  • ISecsConnection      • IMessageHandler              │ │ │
│  │  │  • ITransactionManager  • IStateManager                │ │ │
│  │  │                                                         │ │ │
│  │  └─────────────────────────────────────────────────────────┘ │ │
│  │                                                               │ │
│  │  • GemEquipmentService    • MessageDispatcher                │ │
│  │  • ConnectionManager      • EventReportService               │ │
│  │                                                               │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  • HsmsConnection    • TcpTransport    • JsonConverter             │
│  • SecsSerializer    • FileLogger      • Configuration             │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**为什么选择洋葱架构？**
1. **依赖反转**：外层依赖内层接口，内层不知道外层实现
2. **可测试性**：核心业务可独立测试，不依赖具体实现
3. **灵活性**：可轻松替换基础设施（如更换通信库、日志框架）
4. **清晰边界**：层次分明，职责明确

### 2.2 模块划分

```
SECS2GEM/
├── Core/                    # 核心层：实体和值对象
├── Domain/                  # 领域层：接口和领域服务
├── Application/             # 应用层：用例和业务逻辑
├── Infrastructure/          # 基础设施层：具体实现
└── Contracts/               # 契约层：DTO和API模型
```

---

## 3. 设计原则

### 3.1 SOLID原则应用

| 原则 | 应用场景 |
|------|----------|
| **S** - 单一职责 | SecsSerializer只负责序列化，HsmsConnection只负责通信 |
| **O** - 开闭原则 | 通过IMessageHandler扩展新消息处理，无需修改现有代码 |
| **L** - 里氏替换 | 任何ISecsConnection实现都可以替换使用 |
| **I** - 接口隔离 | 细粒度接口：IConnectable、ISendable、IReceivable |
| **D** - 依赖反转 | 高层模块依赖抽象接口，不依赖具体实现 |

### 3.2 依赖注入

所有模块通过依赖注入获取依赖，支持：
- 构造函数注入（推荐）
- 接口注入
- Microsoft.Extensions.DependencyInjection 集成

```csharp
// 服务注册示例
services.AddSingleton<ISecsSerializer, SecsSerializer>();
services.AddScoped<ITransactionManager, TransactionManager>();
services.AddTransient<IMessageHandler, Stream1Handler>();
```

---

## 4. 核心模块设计

### 4.1 Core 模块

核心实体定义，无外部依赖。

#### 4.1.1 SecsItem - 数据项

```csharp
/// <summary>
/// SECS-II数据项
/// 表示SECS协议中的基本数据单元
/// </summary>
/// <remarks>
/// 设计思路：
/// 1. 使用不可变设计，确保线程安全
/// 2. 支持递归结构（List可包含子项）
/// 3. 提供类型安全的值访问方法
/// </remarks>
public sealed class SecsItem
{
    /// <summary>数据格式</summary>
    public SecsFormat Format { get; }
    
    /// <summary>原始值</summary>
    public object Value { get; }
    
    /// <summary>子项（仅List类型有效）</summary>
    public IReadOnlyList<SecsItem> Items { get; }
    
    /// <summary>数据字节数（不含格式和长度字节）</summary>
    public int Count { get; }
}
```

#### 4.1.2 SecsMessage - SECS消息

```csharp
/// <summary>
/// SECS-II消息
/// </summary>
/// <remarks>
/// 设计思路：
/// 1. 封装Stream/Function/WBit等协议字段
/// 2. 提供消息构建的流畅API
/// 3. 不可变设计保证线程安全
/// </remarks>
public sealed class SecsMessage
{
    /// <summary>Stream号 (1-127)</summary>
    public byte Stream { get; }
    
    /// <summary>Function号</summary>
    public byte Function { get; }
    
    /// <summary>是否期望回复</summary>
    public bool WBit { get; }
    
    /// <summary>消息数据项</summary>
    public SecsItem? Item { get; }
    
    /// <summary>消息名称（如"S1F1"）</summary>
    public string Name => $"S{Stream}F{Function}";
    
    /// <summary>是否为Primary消息（奇数Function）</summary>
    public bool IsPrimary => Function % 2 == 1;
}
```

#### 4.1.3 HsmsMessage - HSMS消息

```csharp
/// <summary>
/// HSMS消息（包含Header和SECS消息）
/// </summary>
public sealed class HsmsMessage
{
    /// <summary>会话ID</summary>
    public ushort SessionId { get; }
    
    /// <summary>会话类型</summary>
    public HsmsMessageType MessageType { get; }
    
    /// <summary>系统字节（事务ID）</summary>
    public uint SystemBytes { get; }
    
    /// <summary>SECS消息（仅数据消息有效）</summary>
    public SecsMessage? SecsMessage { get; }
}

/// <summary>
/// HSMS消息类型
/// </summary>
public enum HsmsMessageType : byte
{
    DataMessage = 0,
    SelectRequest = 1,
    SelectResponse = 2,
    DeselectRequest = 3,
    DeselectResponse = 4,
    LinktestRequest = 5,
    LinktestResponse = 6,
    RejectRequest = 7,
    SeparateRequest = 9
}
```

### 4.2 Domain 模块

领域接口和领域服务定义。

#### 4.2.1 连接接口

```csharp
/// <summary>
/// SECS连接接口
/// </summary>
/// <remarks>
/// 接口隔离原则：将连接功能拆分为多个细粒度接口
/// </remarks>
public interface ISecsConnection : IAsyncDisposable
{
    /// <summary>连接状态</summary>
    ConnectionState State { get; }
    
    /// <summary>连接状态变化事件</summary>
    event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;
    
    /// <summary>收到消息事件</summary>
    event EventHandler<SecsMessageReceivedEventArgs>? MessageReceived;
    
    /// <summary>建立连接</summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>断开连接</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>发送消息并等待回复</summary>
    Task<SecsMessage?> SendAsync(
        SecsMessage message, 
        CancellationToken cancellationToken = default);
    
    /// <summary>发送消息（不等待回复）</summary>
    Task SendOnlyAsync(
        SecsMessage message, 
        CancellationToken cancellationToken = default);
}
```

#### 4.2.2 消息处理接口

```csharp
/// <summary>
/// 消息处理器接口
/// </summary>
/// <remarks>
/// 策略模式：每个Stream/Function组合可以有独立的处理器
/// 开闭原则：添加新消息处理只需实现此接口
/// </remarks>
public interface IMessageHandler
{
    /// <summary>是否能处理该消息</summary>
    bool CanHandle(SecsMessage message);
    
    /// <summary>处理消息并返回响应</summary>
    Task<SecsMessage?> HandleAsync(
        SecsMessage message, 
        IMessageContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 消息上下文
/// </summary>
public interface IMessageContext
{
    /// <summary>设备ID</summary>
    ushort DeviceId { get; }
    
    /// <summary>当前连接</summary>
    ISecsConnection Connection { get; }
    
    /// <summary>设备状态</summary>
    IGemState GemState { get; }
    
    /// <summary>发送响应</summary>
    Task ReplyAsync(SecsMessage response);
}
```

#### 4.2.3 事务管理接口

```csharp
/// <summary>
/// 事务管理器接口
/// </summary>
/// <remarks>
/// 职责：管理请求-响应的配对
/// 确保消息的可靠送达和超时处理
/// </remarks>
public interface ITransactionManager
{
    /// <summary>获取下一个事务ID</summary>
    uint GetNextTransactionId();
    
    /// <summary>开始事务</summary>
    ITransaction BeginTransaction(uint systemBytes, TimeSpan timeout);
    
    /// <summary>完成事务</summary>
    bool TryCompleteTransaction(uint systemBytes, SecsMessage response);
    
    /// <summary>取消事务</summary>
    void CancelTransaction(uint systemBytes);
}

/// <summary>
/// 事务接口
/// </summary>
public interface ITransaction : IDisposable
{
    /// <summary>事务ID</summary>
    uint SystemBytes { get; }
    
    /// <summary>等待响应</summary>
    Task<SecsMessage?> WaitForResponseAsync(CancellationToken cancellationToken = default);
}
```

### 4.3 Application 模块

应用服务和用例实现。

#### 4.3.1 GEM设备服务

```csharp
/// <summary>
/// GEM设备服务
/// 作为Equipment角色运行
/// </summary>
/// <remarks>
/// 外观模式：为复杂的GEM子系统提供统一入口
/// 执行流程：
/// 1. 启动时等待Host连接
/// 2. 处理Select.req建立会话
/// 3. 处理S1F13建立通信
/// 4. 进入正常消息处理循环
/// </remarks>
public interface IGemEquipmentService
{
    /// <summary>设备ID</summary>
    ushort DeviceId { get; }
    
    /// <summary>当前状态</summary>
    GemCommunicationState CommunicationState { get; }
    
    /// <summary>启动服务</summary>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>停止服务</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>发送事件报告</summary>
    Task SendEventAsync(uint ceid, CancellationToken cancellationToken = default);
    
    /// <summary>发送报警</summary>
    Task SendAlarmAsync(AlarmInfo alarm, CancellationToken cancellationToken = default);
}
```

#### 4.3.2 消息分发器

```csharp
/// <summary>
/// 消息分发器
/// </summary>
/// <remarks>
/// 责任链模式 + 策略模式组合：
/// 1. 维护处理器列表
/// 2. 按顺序查找能处理消息的处理器
/// 3. 委托给找到的处理器执行
/// 
/// 好处：
/// - 处理器之间解耦
/// - 支持动态添加/移除处理器
/// - 支持处理器优先级
/// </remarks>
public interface IMessageDispatcher
{
    /// <summary>注册处理器</summary>
    void RegisterHandler(IMessageHandler handler, int priority = 0);
    
    /// <summary>注销处理器</summary>
    void UnregisterHandler(IMessageHandler handler);
    
    /// <summary>分发消息</summary>
    Task<SecsMessage?> DispatchAsync(
        SecsMessage message,
        IMessageContext context,
        CancellationToken cancellationToken = default);
}
```

### 4.4 Infrastructure 模块

具体实现。

#### 4.4.1 HSMS连接实现

```csharp
/// <summary>
/// HSMS连接实现
/// </summary>
/// <remarks>
/// 执行流程（作为Passive端）：
/// 1. 启动TCP监听
/// 2. 接受连接 → 状态变为 Connected
/// 3. 接收Select.req → 发送Select.rsp → 状态变为 Selected
/// 4. 启动心跳检测
/// 5. 接收/发送数据消息
/// 
/// 线程模型：
/// - 接收线程：持续读取TCP数据
/// - 发送队列：使用Channel实现异步发送
/// - 心跳线程：定期发送Linktest
/// </remarks>
public class HsmsConnection : ISecsConnection
{
    private readonly ISecsSerializer _serializer;
    private readonly ITransactionManager _transactionManager;
    private readonly HsmsConfiguration _config;
    private readonly ILogger<HsmsConnection> _logger;
    
    // 状态机
    private HsmsConnectionState _state;
    
    // 网络
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    
    // 异步处理
    private readonly Channel<HsmsMessage> _sendChannel;
    private CancellationTokenSource? _cts;
}
```

#### 4.4.2 序列化器实现

```csharp
/// <summary>
/// SECS消息序列化器
/// </summary>
/// <remarks>
/// 职责：SECS消息与字节数组的相互转换
/// 
/// 序列化流程：
/// 1. 计算消息总长度
/// 2. 写入HSMS头（10字节）
/// 3. 递归序列化SecsItem
/// 
/// 反序列化流程：
/// 1. 读取并验证HSMS头
/// 2. 递归解析SecsItem
/// 3. 构造SecsMessage
/// </remarks>
public class SecsSerializer : ISecsSerializer
{
    /// <summary>序列化HSMS消息</summary>
    public byte[] Serialize(HsmsMessage message);
    
    /// <summary>反序列化HSMS消息</summary>
    public HsmsMessage Deserialize(ReadOnlySpan<byte> data);
    
    /// <summary>序列化数据项</summary>
    public void SerializeItem(SecsItem item, IBufferWriter<byte> writer);
    
    /// <summary>反序列化数据项</summary>
    public SecsItem DeserializeItem(ref SequenceReader<byte> reader);
}
```

---

## 5. 设计模式应用

### 5.1 工厂模式 - SecsItem创建

**为什么选择工厂模式？**
- SecsItem有多种类型，创建逻辑复杂
- 隐藏创建细节，提供流畅API
- 支持类型验证和转换

```csharp
/// <summary>
/// SecsItem工厂
/// </summary>
public static class SecsItem
{
    /// <summary>创建List</summary>
    public static SecsItem L(params SecsItem[] items) 
        => new(SecsFormat.List, items, items);
    
    /// <summary>创建ASCII字符串</summary>
    public static SecsItem A(string value) 
        => new(SecsFormat.ASCII, value, Encoding.ASCII.GetBytes(value));
    
    /// <summary>创建无符号整数</summary>
    public static SecsItem U4(uint value) 
        => new(SecsFormat.U4, value, BitConverter.GetBytes(value));
    
    /// <summary>创建布尔值</summary>
    public static SecsItem Boolean(bool value) 
        => new(SecsFormat.Boolean, value, new[] { (byte)(value ? 1 : 0) });
    
    // ... 其他类型
}

// 使用示例
var s1f2 = new SecsMessage(1, 2, false,
    SecsItem.L(
        SecsItem.A("MODEL-001"),    // MDLN
        SecsItem.A("1.0.0")         // SOFTREV
    )
);
```

### 5.2 策略模式 - 消息处理

**为什么选择策略模式？**
- 不同S/F消息需要不同处理逻辑
- 支持运行时动态切换处理策略
- 符合开闭原则，易于扩展

```csharp
/// <summary>
/// Stream 1 处理器
/// </summary>
public class Stream1Handler : IMessageHandler
{
    public bool CanHandle(SecsMessage message) 
        => message.Stream == 1;
    
    public async Task<SecsMessage?> HandleAsync(
        SecsMessage message, 
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        return message.Function switch
        {
            1 => HandleS1F1(message, context),   // Are You There
            3 => HandleS1F3(message, context),   // Status Request
            13 => HandleS1F13(message, context), // Establish Comm
            15 => HandleS1F15(message, context), // Request Offline
            17 => HandleS1F17(message, context), // Request Online
            _ => null
        };
    }
    
    /// <summary>
    /// 处理S1F1 - Are You There
    /// </summary>
    private SecsMessage HandleS1F1(SecsMessage request, IMessageContext context)
    {
        // 返回S1F2，包含设备型号和软件版本
        return new SecsMessage(1, 2, false,
            SecsItem.L(
                SecsItem.A(context.GemState.ModelName),
                SecsItem.A(context.GemState.SoftwareRevision)
            )
        );
    }
}
```

### 5.3 状态模式 - 连接状态管理

**为什么选择状态模式？**
- HSMS连接有明确的状态转换规则
- 不同状态下行为不同
- 避免大量if-else判断

```csharp
/// <summary>
/// HSMS连接状态基类
/// </summary>
public abstract class HsmsConnectionState
{
    protected HsmsConnection Connection { get; }
    
    /// <summary>处理接收到的消息</summary>
    public abstract Task HandleMessageAsync(HsmsMessage message);
    
    /// <summary>处理发送请求</summary>
    public abstract Task<bool> SendAsync(SecsMessage message);
    
    /// <summary>处理连接断开</summary>
    public abstract Task HandleDisconnectAsync();
}

/// <summary>
/// 未连接状态
/// </summary>
public class NotConnectedState : HsmsConnectionState
{
    public override Task<bool> SendAsync(SecsMessage message)
    {
        throw new InvalidOperationException("未建立连接，无法发送消息");
    }
}

/// <summary>
/// 已连接未选择状态
/// </summary>
public class ConnectedNotSelectedState : HsmsConnectionState
{
    public override async Task HandleMessageAsync(HsmsMessage message)
    {
        if (message.MessageType == HsmsMessageType.SelectRequest)
        {
            // 发送Select.rsp
            await SendSelectResponseAsync();
            // 转换到Selected状态
            Connection.TransitionTo(new SelectedState(Connection));
        }
    }
}

/// <summary>
/// 已选择状态
/// </summary>
public class SelectedState : HsmsConnectionState
{
    public override async Task HandleMessageAsync(HsmsMessage message)
    {
        switch (message.MessageType)
        {
            case HsmsMessageType.DataMessage:
                await HandleDataMessageAsync(message);
                break;
            case HsmsMessageType.LinktestRequest:
                await SendLinktestResponseAsync();
                break;
            case HsmsMessageType.SeparateRequest:
                Connection.TransitionTo(new NotConnectedState(Connection));
                break;
        }
    }
}
```

### 5.4 观察者模式 - 事件通知

**为什么选择观察者模式？**
- 解耦事件发布者和订阅者
- 支持多个订阅者
- 符合开闭原则

```csharp
/// <summary>
/// 事件聚合器
/// </summary>
public interface IEventAggregator
{
    /// <summary>发布事件</summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IGemEvent;
    
    /// <summary>订阅事件</summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IGemEvent;
}

/// <summary>
/// GEM事件基接口
/// </summary>
public interface IGemEvent
{
    DateTime Timestamp { get; }
}

/// <summary>
/// 报警事件
/// </summary>
public record AlarmEvent(
    uint AlarmId,
    string AlarmText,
    bool IsSet,
    DateTime Timestamp
) : IGemEvent;

/// <summary>
/// 状态变化事件
/// </summary>
public record StateChangedEvent(
    GemCommunicationState OldState,
    GemCommunicationState NewState,
    DateTime Timestamp
) : IGemEvent;
```

### 5.5 建造者模式 - 消息构建

**为什么选择建造者模式？**
- 消息结构复杂，参数多
- 提供流畅的构建API
- 支持分步骤构建

```csharp
/// <summary>
/// SECS消息建造器
/// </summary>
public class SecsMessageBuilder
{
    private byte _stream;
    private byte _function;
    private bool _wbit;
    private SecsItem? _item;
    
    public SecsMessageBuilder Stream(byte stream)
    {
        _stream = stream;
        return this;
    }
    
    public SecsMessageBuilder Function(byte function)
    {
        _function = function;
        return this;
    }
    
    public SecsMessageBuilder ExpectReply(bool expect = true)
    {
        _wbit = expect;
        return this;
    }
    
    public SecsMessageBuilder Item(SecsItem item)
    {
        _item = item;
        return this;
    }
    
    public SecsMessage Build()
    {
        return new SecsMessage(_stream, _function, _wbit, _item);
    }
}

// 使用示例
var message = new SecsMessageBuilder()
    .Stream(1)
    .Function(13)
    .ExpectReply()
    .Item(SecsItem.L(
        SecsItem.A(""),  // MDLN
        SecsItem.A("")   // SOFTREV
    ))
    .Build();
```

### 5.6 模板方法模式 - 消息处理基类

**为什么选择模板方法模式？**
- 消息处理有固定流程（验证→处理→响应）
- 不同消息的处理细节不同
- 复用通用逻辑

```csharp
/// <summary>
/// 消息处理器基类
/// </summary>
public abstract class MessageHandlerBase : IMessageHandler
{
    public abstract bool CanHandle(SecsMessage message);
    
    /// <summary>
    /// 处理消息（模板方法）
    /// </summary>
    public async Task<SecsMessage?> HandleAsync(
        SecsMessage message,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        // 1. 验证消息
        var validationResult = ValidateMessage(message, context);
        if (!validationResult.IsValid)
        {
            return CreateErrorResponse(message, validationResult.ErrorCode);
        }
        
        // 2. 执行业务处理（子类实现）
        var result = await ProcessMessageAsync(message, context, cancellationToken);
        
        // 3. 记录日志
        LogMessageProcessed(message, result);
        
        return result;
    }
    
    /// <summary>验证消息（可重写）</summary>
    protected virtual ValidationResult ValidateMessage(
        SecsMessage message, 
        IMessageContext context)
    {
        return ValidationResult.Success;
    }
    
    /// <summary>处理消息（子类实现）</summary>
    protected abstract Task<SecsMessage?> ProcessMessageAsync(
        SecsMessage message,
        IMessageContext context,
        CancellationToken cancellationToken);
    
    /// <summary>创建错误响应（可重写）</summary>
    protected virtual SecsMessage CreateErrorResponse(
        SecsMessage request, 
        int errorCode)
    {
        return new SecsMessage(
            request.Stream, 
            (byte)(request.Function + 1), 
            false,
            SecsItem.B((byte)errorCode)
        );
    }
}
```

---

## 6. 接口定义

### 6.1 序列化接口

```csharp
/// <summary>
/// SECS消息序列化接口
/// </summary>
public interface ISecsSerializer
{
    /// <summary>序列化HSMS消息为字节数组</summary>
    byte[] Serialize(HsmsMessage message);
    
    /// <summary>从字节数组反序列化HSMS消息</summary>
    HsmsMessage Deserialize(ReadOnlySpan<byte> data);
    
    /// <summary>尝试从流中读取完整消息</summary>
    bool TryReadMessage(
        ref ReadOnlySequence<byte> buffer, 
        out HsmsMessage? message);
}
```

### 6.2 配置接口

```csharp
/// <summary>
/// HSMS配置
/// </summary>
public class HsmsConfiguration
{
    /// <summary>设备ID</summary>
    public ushort DeviceId { get; set; } = 0;
    
    /// <summary>IP地址（Passive模式绑定地址）</summary>
    public string IpAddress { get; set; } = "0.0.0.0";
    
    /// <summary>端口号</summary>
    public int Port { get; set; } = 5000;
    
    /// <summary>连接模式</summary>
    public HsmsConnectionMode Mode { get; set; } = HsmsConnectionMode.Passive;
    
    /// <summary>T3 回复超时（秒）</summary>
    public int T3 { get; set; } = 45;
    
    /// <summary>T5 连接分离超时（秒）</summary>
    public int T5 { get; set; } = 10;
    
    /// <summary>T6 控制超时（秒）</summary>
    public int T6 { get; set; } = 5;
    
    /// <summary>T7 未选择超时（秒）</summary>
    public int T7 { get; set; } = 10;
    
    /// <summary>T8 网络超时（秒）</summary>
    public int T8 { get; set; } = 5;
    
    /// <summary>心跳间隔（秒）</summary>
    public int LinktestInterval { get; set; } = 30;
}

public enum HsmsConnectionMode
{
    /// <summary>主动模式（发起连接）</summary>
    Active,
    
    /// <summary>被动模式（等待连接）</summary>
    Passive
}
```

### 6.3 状态管理接口

```csharp
/// <summary>
/// GEM状态接口
/// </summary>
public interface IGemState
{
    /// <summary>设备型号</summary>
    string ModelName { get; }
    
    /// <summary>软件版本</summary>
    string SoftwareRevision { get; }
    
    /// <summary>通信状态</summary>
    GemCommunicationState CommunicationState { get; }
    
    /// <summary>控制状态</summary>
    GemControlState ControlState { get; }
    
    /// <summary>处理状态</summary>
    GemProcessingState ProcessingState { get; }
    
    /// <summary>获取状态变量值</summary>
    object? GetStatusVariable(uint svid);
    
    /// <summary>设置状态变量值</summary>
    void SetStatusVariable(uint svid, object value);
    
    /// <summary>获取设备常量值</summary>
    object? GetEquipmentConstant(uint ecid);
    
    /// <summary>设置设备常量值</summary>
    bool TrySetEquipmentConstant(uint ecid, object value);
}

public enum GemCommunicationState
{
    Disabled,
    Enabled,
    WaitCommunicationRequest,
    WaitCommunicationDelay,
    Communicating
}

public enum GemControlState
{
    EquipmentOffline,
    AttemptOnline,
    HostOffline,
    OnlineLocal,
    OnlineRemote
}

public enum GemProcessingState
{
    Idle,
    Setup,
    Ready,
    Executing,
    Paused
}
```

---

## 7. 数据流程

### 7.1 消息接收流程

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           消息接收流程                                       │
└─────────────────────────────────────────────────────────────────────────────┘

TCP接收 ──► 帧解析 ──► HSMS解析 ──► 消息分类 ──► 处理
   │          │          │           │           │
   │          │          │           │           │
   ▼          ▼          ▼           ▼           ▼
┌─────┐   ┌─────┐    ┌─────┐    ┌─────────┐  ┌─────────┐
│网络层│   │读取  │    │反序列│    │控制消息 │  │控制处理 │
│     │──►│长度  │───►│化    │───►│         │─►│         │
│     │   │+数据 │    │      │    │         │  │Select等│
└─────┘   └─────┘    └─────┘    └────┬────┘  └─────────┘
                                      │
                                      │数据消息
                                      ▼
                               ┌─────────┐
                               │事务匹配 │
                               │         │
                               └────┬────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
                    ▼               ▼               ▼
               ┌─────────┐    ┌─────────┐    ┌─────────┐
               │响应消息 │    │主动消息 │    │未知消息 │
               │完成事务 │    │分发处理 │    │S9Fx错误│
               └─────────┘    └─────────┘    └─────────┘
```

### 7.2 消息发送流程

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           消息发送流程                                       │
└─────────────────────────────────────────────────────────────────────────────┘

应用层 ──► 序列化 ──► 事务管理 ──► 发送队列 ──► TCP发送 ──► 等待响应
   │          │          │           │           │           │
   │          │          │           │           │           │
   ▼          ▼          ▼           ▼           ▼           ▼
┌─────┐   ┌─────┐    ┌─────┐    ┌─────────┐  ┌─────┐   ┌─────────┐
│构建 │   │SECS │    │生成 │    │Channel  │  │写入 │   │阻塞等待 │
│消息 │──►│序列化│───►│TxID │───►│异步队列 │─►│网络 │──►│或超时   │
│     │   │     │    │注册 │    │         │  │     │   │         │
└─────┘   └─────┘    └─────┘    └─────────┘  └─────┘   └─────────┘
                         │                                  │
                         │                                  │
                         └──────────────────────────────────┘
                                    响应到达时
                                    完成TaskCompletionSource
```

### 7.3 事件报告流程

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        事件报告配置与触发流程                                │
└─────────────────────────────────────────────────────────────────────────────┘

1. 报告定义 (S2F33)
   Host ─────────────────────────────────────────────► Equipment
         S2F33 Define Report
         L,2
           DATAID
           L,n
             L,2
               RPTID   ←─── 报告ID
               L,m
                 VID    ←─── 变量ID列表
                 ...

2. 事件关联 (S2F35)
   Host ─────────────────────────────────────────────► Equipment
         S2F35 Link Event Report
         L,2
           DATAID
           L,n
             L,2
               CEID   ←─── 事件ID
               L,m
                 RPTID ←─── 关联的报告ID列表
                 ...

3. 事件启用 (S2F37)
   Host ─────────────────────────────────────────────► Equipment
         S2F37 Enable/Disable Event Report
         L,2
           CEED   ←─── true=启用, false=禁用
           L,n
             CEID  ←─── 事件ID列表

4. 事件触发 (S6F11)
   Equipment ────────────────────────────────────────► Host
              S6F11 Event Report Send
              L,3
                DATAID
                CEID    ←─── 触发的事件ID
                L,n
                  L,2
                    RPTID  ←─── 报告ID
                    L,m
                      V     ←─── 变量值
                      ...
```

---

## 8. 状态管理

### 8.1 状态机实现

```csharp
/// <summary>
/// 状态机基类
/// </summary>
/// <remarks>
/// 设计思路：
/// 1. 泛型设计支持不同类型的状态枚举
/// 2. 支持状态转换验证
/// 3. 触发状态变化事件
/// </remarks>
public class StateMachine<TState> where TState : struct, Enum
{
    private TState _currentState;
    private readonly Dictionary<(TState From, TState To), Func<bool>> _transitions;
    private readonly object _lock = new();
    
    public TState CurrentState
    {
        get { lock (_lock) return _currentState; }
    }
    
    public event EventHandler<StateChangedEventArgs<TState>>? StateChanged;
    
    /// <summary>
    /// 定义允许的状态转换
    /// </summary>
    public void DefineTransition(TState from, TState to, Func<bool>? guard = null)
    {
        _transitions[(from, to)] = guard ?? (() => true);
    }
    
    /// <summary>
    /// 尝试转换状态
    /// </summary>
    public bool TryTransition(TState newState)
    {
        lock (_lock)
        {
            var key = (_currentState, newState);
            if (!_transitions.TryGetValue(key, out var guard) || !guard())
            {
                return false;
            }
            
            var oldState = _currentState;
            _currentState = newState;
            
            StateChanged?.Invoke(this, new StateChangedEventArgs<TState>(oldState, newState));
            return true;
        }
    }
}
```

### 8.2 GEM通信状态机

```csharp
/// <summary>
/// GEM通信状态机
/// </summary>
public class GemCommunicationStateMachine
{
    private readonly StateMachine<GemCommunicationState> _stateMachine;
    
    public GemCommunicationStateMachine()
    {
        _stateMachine = new StateMachine<GemCommunicationState>(
            GemCommunicationState.Disabled);
        
        // 定义状态转换规则
        _stateMachine.DefineTransition(
            GemCommunicationState.Disabled,
            GemCommunicationState.Enabled);
            
        _stateMachine.DefineTransition(
            GemCommunicationState.Enabled,
            GemCommunicationState.WaitCommunicationRequest);
            
        _stateMachine.DefineTransition(
            GemCommunicationState.WaitCommunicationRequest,
            GemCommunicationState.Communicating);
            
        _stateMachine.DefineTransition(
            GemCommunicationState.Communicating,
            GemCommunicationState.Disabled);
    }
    
    public GemCommunicationState CurrentState => _stateMachine.CurrentState;
    
    /// <summary>启用通信</summary>
    public bool Enable() 
        => _stateMachine.TryTransition(GemCommunicationState.Enabled);
    
    /// <summary>等待通信请求</summary>
    public bool WaitForCommunication() 
        => _stateMachine.TryTransition(GemCommunicationState.WaitCommunicationRequest);
    
    /// <summary>通信已建立</summary>
    public bool Communicate() 
        => _stateMachine.TryTransition(GemCommunicationState.Communicating);
    
    /// <summary>禁用通信</summary>
    public bool Disable() 
        => _stateMachine.TryTransition(GemCommunicationState.Disabled);
}
```

---

## 9. 错误处理

### 9.1 异常层次结构

```csharp
/// <summary>
/// SECS异常基类
/// </summary>
public class SecsException : Exception
{
    public SecsException(string message) : base(message) { }
    public SecsException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// 通信异常
/// </summary>
public class SecsCommunicationException : SecsException
{
    public CommunicationError ErrorType { get; }
    
    public SecsCommunicationException(CommunicationError errorType, string message) 
        : base(message)
    {
        ErrorType = errorType;
    }
}

/// <summary>
/// 超时异常
/// </summary>
public class SecsTimeoutException : SecsException
{
    public TimeoutType TimeoutType { get; }
    public TimeSpan Elapsed { get; }
    
    public SecsTimeoutException(TimeoutType type, TimeSpan elapsed)
        : base($"{type} timeout after {elapsed.TotalSeconds:F1}s")
    {
        TimeoutType = type;
        Elapsed = elapsed;
    }
}

/// <summary>
/// 消息格式异常
/// </summary>
public class SecsFormatException : SecsException
{
    public int Position { get; }
    
    public SecsFormatException(string message, int position)
        : base($"{message} at position {position}")
    {
        Position = position;
    }
}

public enum CommunicationError
{
    ConnectionFailed,
    ConnectionLost,
    SelectFailed,
    NotSelected
}

public enum TimeoutType
{
    T3Reply,
    T5Separation,
    T6Control,
    T7NotSelected,
    T8Network
}
```

### 9.2 S9错误响应

```csharp
/// <summary>
/// S9错误响应处理器
/// </summary>
public class Stream9ErrorHandler
{
    private readonly ISecsConnection _connection;
    
    /// <summary>发送S9F1 - 未识别的设备ID</summary>
    public Task SendUnrecognizedDeviceIdAsync(HsmsMessage message)
    {
        return SendS9ErrorAsync(1, message.Header);
    }
    
    /// <summary>发送S9F3 - 未识别的Stream</summary>
    public Task SendUnrecognizedStreamAsync(SecsMessage message)
    {
        return SendS9ErrorAsync(3, ExtractHeader(message));
    }
    
    /// <summary>发送S9F5 - 未识别的Function</summary>
    public Task SendUnrecognizedFunctionAsync(SecsMessage message)
    {
        return SendS9ErrorAsync(5, ExtractHeader(message));
    }
    
    /// <summary>发送S9F7 - 非法数据</summary>
    public Task SendIllegalDataAsync(SecsMessage message)
    {
        return SendS9ErrorAsync(7, ExtractHeader(message));
    }
    
    /// <summary>发送S9F9 - 事务超时</summary>
    public Task SendTransactionTimeoutAsync(SecsMessage message)
    {
        return SendS9ErrorAsync(9, ExtractHeader(message));
    }
    
    private Task SendS9ErrorAsync(byte function, byte[] mhead)
    {
        var errorMessage = new SecsMessage(9, function, false,
            SecsItem.B(mhead));
        return _connection.SendOnlyAsync(errorMessage);
    }
}
```

---

## 10. 扩展性设计

### 10.1 自定义消息处理器

```csharp
/// <summary>
/// 自定义Stream处理器基类
/// </summary>
public abstract class CustomStreamHandler : MessageHandlerBase
{
    protected abstract byte StreamNumber { get; }
    
    public override bool CanHandle(SecsMessage message) 
        => message.Stream == StreamNumber;
}

// 使用示例：自定义S100处理（厂商自定义Stream）
public class Stream100Handler : CustomStreamHandler
{
    protected override byte StreamNumber => 100;
    
    protected override Task<SecsMessage?> ProcessMessageAsync(
        SecsMessage message,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        return message.Function switch
        {
            1 => HandleS100F1Async(message, context),
            3 => HandleS100F3Async(message, context),
            _ => Task.FromResult<SecsMessage?>(null)
        };
    }
}
```

### 10.2 自定义数据类型转换

```csharp
/// <summary>
/// 数据类型转换器接口
/// </summary>
public interface ISecsDataConverter<T>
{
    SecsItem ToSecsItem(T value);
    T FromSecsItem(SecsItem item);
}

/// <summary>
/// 转换器注册表
/// </summary>
public class SecsConverterRegistry
{
    private readonly Dictionary<Type, object> _converters = new();
    
    public void Register<T>(ISecsDataConverter<T> converter)
    {
        _converters[typeof(T)] = converter;
    }
    
    public SecsItem Convert<T>(T value)
    {
        if (_converters.TryGetValue(typeof(T), out var converter))
        {
            return ((ISecsDataConverter<T>)converter).ToSecsItem(value);
        }
        throw new InvalidOperationException($"No converter for type {typeof(T)}");
    }
}

// 使用示例：自定义配方数据转换
public class RecipeConverter : ISecsDataConverter<Recipe>
{
    public SecsItem ToSecsItem(Recipe recipe)
    {
        return SecsItem.L(
            SecsItem.A(recipe.Id),
            SecsItem.A(recipe.Name),
            SecsItem.B(recipe.Body)
        );
    }
    
    public Recipe FromSecsItem(SecsItem item)
    {
        var list = item.Items;
        return new Recipe
        {
            Id = list[0].GetString(),
            Name = list[1].GetString(),
            Body = list[2].GetBytes()
        };
    }
}
```

### 10.3 插件架构

```csharp
/// <summary>
/// GEM插件接口
/// </summary>
public interface IGemPlugin
{
    /// <summary>插件名称</summary>
    string Name { get; }
    
    /// <summary>插件版本</summary>
    string Version { get; }
    
    /// <summary>初始化插件</summary>
    Task InitializeAsync(IGemPluginContext context);
    
    /// <summary>卸载插件</summary>
    Task ShutdownAsync();
}

/// <summary>
/// 插件上下文
/// </summary>
public interface IGemPluginContext
{
    /// <summary>注册消息处理器</summary>
    void RegisterHandler(IMessageHandler handler);
    
    /// <summary>注册事件监听</summary>
    IDisposable SubscribeEvent<T>(Func<T, Task> handler) where T : IGemEvent;
    
    /// <summary>获取服务</summary>
    T GetService<T>() where T : class;
}
```

---

## 11. 项目结构

### 11.1 完整目录结构

```
SECS2GEM/
│
├── Core/                                    # 核心实体层
│   ├── Entities/
│   │   ├── SecsItem.cs                     # SECS数据项
│   │   ├── SecsMessage.cs                  # SECS消息
│   │   ├── HsmsMessage.cs                  # HSMS消息
│   │   └── HsmsHeader.cs                   # HSMS头
│   │
│   ├── Enums/
│   │   ├── SecsFormat.cs                   # 数据格式枚举
│   │   ├── HsmsMessageType.cs              # HSMS消息类型
│   │   ├── ConnectionState.cs              # 连接状态
│   │   └── GemStates.cs                    # GEM状态枚举
│   │
│   └── Exceptions/
│       ├── SecsException.cs                # 异常基类
│       ├── SecsCommunicationException.cs   # 通信异常
│       ├── SecsTimeoutException.cs         # 超时异常
│       └── SecsFormatException.cs          # 格式异常
│
├── Domain/                                  # 领域层
│   ├── Interfaces/
│   │   ├── ISecsConnection.cs              # 连接接口
│   │   ├── ISecsSerializer.cs              # 序列化接口
│   │   ├── IMessageHandler.cs              # 消息处理接口
│   │   ├── IMessageDispatcher.cs           # 消息分发接口
│   │   ├── ITransactionManager.cs          # 事务管理接口
│   │   ├── IGemState.cs                    # GEM状态接口
│   │   └── IEventAggregator.cs             # 事件聚合接口
│   │
│   ├── Events/
│   │   ├── IGemEvent.cs                    # 事件基接口
│   │   ├── AlarmEvent.cs                   # 报警事件
│   │   ├── StateChangedEvent.cs            # 状态变化事件
│   │   └── MessageReceivedEvent.cs         # 消息接收事件
│   │
│   └── Models/
│       ├── AlarmInfo.cs                    # 报警信息
│       ├── EventReport.cs                  # 事件报告
│       ├── StatusVariable.cs               # 状态变量
│       └── EquipmentConstant.cs            # 设备常量
│
├── Application/                             # 应用层
│   ├── Services/
│   │   ├── GemEquipmentService.cs          # GEM设备服务
│   │   ├── MessageDispatcher.cs            # 消息分发器
│   │   ├── TransactionManager.cs           # 事务管理器
│   │   └── EventReportService.cs           # 事件报告服务
│   │
│   ├── Handlers/
│   │   ├── MessageHandlerBase.cs           # 处理器基类
│   │   ├── Stream1Handler.cs               # S1处理器
│   │   ├── Stream2Handler.cs               # S2处理器
│   │   ├── Stream5Handler.cs               # S5处理器
│   │   ├── Stream6Handler.cs               # S6处理器
│   │   ├── Stream7Handler.cs               # S7处理器
│   │   └── Stream9Handler.cs               # S9处理器
│   │
│   ├── StateMachines/
│   │   ├── StateMachine.cs                 # 状态机基类
│   │   ├── HsmsStateMachine.cs             # HSMS状态机
│   │   ├── CommunicationStateMachine.cs    # 通信状态机
│   │   └── ControlStateMachine.cs          # 控制状态机
│   │
│   └── Builders/
│       ├── SecsMessageBuilder.cs           # 消息建造器
│       └── SecsItemBuilder.cs              # 数据项建造器
│
├── Infrastructure/                          # 基础设施层
│   ├── Connection/
│   │   ├── HsmsConnection.cs               # HSMS连接实现
│   │   ├── HsmsConnectionState.cs          # 连接状态基类
│   │   ├── States/
│   │   │   ├── NotConnectedState.cs        # 未连接状态
│   │   │   ├── ConnectedState.cs           # 已连接状态
│   │   │   └── SelectedState.cs            # 已选择状态
│   │   └── HsmsConnectionFactory.cs        # 连接工厂
│   │
│   ├── Serialization/
│   │   ├── SecsSerializer.cs               # SECS序列化器
│   │   ├── SecsItemSerializer.cs           # 数据项序列化
│   │   └── HsmsHeaderSerializer.cs         # HSMS头序列化
│   │
│   ├── Transport/
│   │   ├── TcpTransport.cs                 # TCP传输
│   │   └── FrameDecoder.cs                 # 帧解码器
│   │
│   └── Logging/
│       └── SecsMessageLogger.cs            # 消息日志
│
├── Configuration/                           # 配置
│   ├── HsmsConfiguration.cs                # HSMS配置
│   ├── GemConfiguration.cs                 # GEM配置
│   └── ServiceCollectionExtensions.cs      # DI扩展
│
├── Contracts/                               # 契约/DTO
│   ├── Requests/
│   │   ├── SendMessageRequest.cs           # 发送消息请求
│   │   └── CommandRequest.cs               # 命令请求
│   │
│   └── Responses/
│       ├── MessageResponse.cs              # 消息响应
│       └── StatusResponse.cs               # 状态响应
│
├── Extensions/                              # 扩展方法
│   ├── SecsItemExtensions.cs               # SecsItem扩展
│   ├── SecsMessageExtensions.cs            # SecsMessage扩展
│   └── ByteExtensions.cs                   # 字节扩展
│
├── GEM_Protocol_Specification.md           # 协议规范文档
├── SECS2GEM_Design_Document.md             # 设计文档（本文档）
└── SECS2GEM.csproj                         # 项目文件
```

### 11.2 依赖关系图

```
┌─────────────────────────────────────────────────────────────────────┐
│                         依赖方向 (→ 表示依赖)                        │
└─────────────────────────────────────────────────────────────────────┘

                    ┌─────────────────┐
                    │   Contracts     │
                    │    (DTO)        │
                    └────────┬────────┘
                             │
                             ▼
┌─────────────┐      ┌─────────────────┐      ┌─────────────┐
│Infrastructure│ ───► │   Application   │ ◄─── │   WebAPI    │
│             │      │                 │      │  (外部)     │
└──────┬──────┘      └────────┬────────┘      └─────────────┘
       │                      │
       │                      ▼
       │             ┌─────────────────┐
       └───────────► │     Domain      │
                     │                 │
                     └────────┬────────┘
                              │
                              ▼
                     ┌─────────────────┐
                     │      Core       │
                     │   (Entities)    │
                     └─────────────────┘

规则：
• Core 不依赖任何其他层
• Domain 只依赖 Core
• Application 依赖 Domain 和 Core
• Infrastructure 依赖 Domain 和 Core（实现Domain接口）
• 外部模块（WebAPI）只依赖 Application 和 Contracts
```

### 11.3 NuGet依赖

```xml
<ItemGroup>
  <!-- 日志 -->
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
  
  <!-- 依赖注入 -->
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
  
  <!-- 配置 -->
  <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
  
  <!-- 高性能IO -->
  <PackageReference Include="System.IO.Pipelines" Version="8.0.0" />
  
  <!-- 异步集合 -->
  <PackageReference Include="System.Threading.Channels" Version="8.0.0" />
</ItemGroup>
```

---

## 附录：快速开始示例

### 启动GEM设备服务

```csharp
// 1. 配置服务
var services = new ServiceCollection();

services.AddSecs2Gem(options =>
{
    options.DeviceId = 1;
    options.Port = 5000;
    options.Mode = HsmsConnectionMode.Passive;
    options.ModelName = "SECS2GEM-Demo";
    options.SoftwareRevision = "1.0.0";
});

var provider = services.BuildServiceProvider();

// 2. 获取GEM服务
var gemService = provider.GetRequiredService<IGemEquipmentService>();

// 3. 订阅事件
gemService.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"收到消息: {e.Message.Name}");
};

gemService.StateChanged += (sender, e) =>
{
    Console.WriteLine($"状态变化: {e.OldState} → {e.NewState}");
};

// 4. 启动服务
await gemService.StartAsync();

Console.WriteLine("GEM设备服务已启动，等待Host连接...");
Console.ReadLine();

// 5. 停止服务
await gemService.StopAsync();
```

### 发送事件报告

```csharp
// 触发事件报告（假设CEID=100已配置）
await gemService.SendEventAsync(ceid: 100);
```

### 发送报警

```csharp
// 发送报警
await gemService.SendAlarmAsync(new AlarmInfo
{
    AlarmId = 1001,
    AlarmText = "温度超限警告",
    IsSet = true,
    AlarmCode = 0x81  // bit7=1(Set), bit0-6=1(Personal Safety)
});
```
