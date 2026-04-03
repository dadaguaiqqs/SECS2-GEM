# SECS连接接口

<cite>
**本文档引用的文件**
- [ISecsConnection.cs](file://WebGem/SECS2GEM/Domain/Interfaces/ISecsConnection.cs)
- [HsmsConnection.cs](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs)
- [HsmsConfiguration.cs](file://WebGem/SECS2GEM/Infrastructure/Configuration/HsmsConfiguration.cs)
- [ConnectionState.cs](file://WebGem/SECS2GEM/Core/Enums/ConnectionState.cs)
- [MessageContext.cs](file://WebGem/SECS2GEM/Infrastructure/Connection/MessageContext.cs)
- [IMessageHandler.cs](file://WebGem/SECS2GEM/Domain/Interfaces/IMessageHandler.cs)
- [TransactionManager.cs](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs)
- [SecsCommunicationException.cs](file://WebGem/SECS2GEM/Core/Exceptions/SecsCommunicationException.cs)
- [SecsTimeoutException.cs](file://WebGem/SECS2GEM/Core/Exceptions/SecsTimeoutException.cs)
- [IntegrationTests.cs](file://WebGem/SECS2GEM.Tests/IntegrationTests.cs)
</cite>

## 目录
1. [简介](#简介)
2. [项目结构](#项目结构)
3. [核心组件](#核心组件)
4. [架构概览](#架构概览)
5. [详细组件分析](#详细组件分析)
6. [依赖关系分析](#依赖关系分析)
7. [性能考虑](#性能考虑)
8. [故障排除指南](#故障排除指南)
9. [结论](#结论)

## 简介

SECS连接接口是SEMI E37标准HSMS（High-Speed Machine Side）协议的.NET实现，提供了设备与主机之间的高速通信能力。本接口设计遵循异步编程模型，支持主动和被动两种连接模式，实现了完整的HSMS协议栈功能。

该接口的核心价值在于：
- **标准化协议支持**：完全符合SEMI E37标准的HSMS协议实现
- **异步非阻塞**：基于Task和Channel的异步消息处理模型
- **状态管理**：完整的连接生命周期状态管理
- **事务处理**：基于SystemBytes的事务跟踪和超时管理
- **事件驱动**：通过事件通知连接状态变化和消息接收

## 项目结构

SECS连接接口位于WebGem解决方案的SECS2GEM项目中，采用清晰的分层架构：

```mermaid
graph TB
subgraph "应用层"
App[应用程序]
Handlers[消息处理器]
end
subgraph "领域层"
ISecsConnection[ISecsConnection接口]
IMessageHandler[消息处理器接口]
IMessageContext[消息上下文接口]
end
subgraph "基础设施层"
HsmsConnection[HsmsConnection实现]
HsmsConfiguration[配置管理]
TransactionManager[事务管理]
MessageContext[消息上下文实现]
end
subgraph "核心层"
ConnectionState[连接状态枚举]
SecsMessage[SECS消息实体]
SecsException[异常体系]
end
App --> ISecsConnection
ISecsConnection --> HsmsConnection
HsmsConnection --> HsmsConfiguration
HsmsConnection --> TransactionManager
HsmsConnection --> MessageContext
IMessageHandler --> Handlers
IMessageContext --> MessageContext
```

**图表来源**
- [ISecsConnection.cs:59-142](file://WebGem/SECS2GEM/Domain/Interfaces/ISecsConnection.cs#L59-L142)
- [HsmsConnection.cs:30-139](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L30-L139)

**章节来源**
- [ISecsConnection.cs:1-144](file://WebGem/SECS2GEM/Domain/Interfaces/ISecsConnection.cs#L1-L144)
- [HsmsConnection.cs:1-906](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L1-L906)

## 核心组件

### ISecsConnection接口

ISecsConnection是SECS连接的核心抽象，定义了完整的连接管理API：

#### 连接管理方法
- **ConnectAsync()**：主动模式建立连接
- **StartListeningAsync()**：被动模式开始监听
- **DisconnectAsync()**：断开连接

#### 消息处理方法
- **SendAsync()**：发送消息并等待回复
- **SendOnlyAsync()**：发送消息（不等待回复）
- **SendLinktestAsync()**：发送Linktest心跳

#### 状态属性
- **State**：当前连接状态
- **IsSelected**：是否已选择（会话建立）
- **SessionId**：会话ID（设备ID）
- **RemoteEndpoint**：远程端点信息

#### 事件系统
- **StateChanged**：连接状态变化事件
- **PrimaryMessageReceived**：Primary消息接收事件

**章节来源**
- [ISecsConnection.cs:71-142](file://WebGem/SECS2GEM/Domain/Interfaces/ISecsConnection.cs#L71-L142)

### HsmsConnection实现

HsmsConnection是ISecsConnection接口的具体实现，采用状态模式管理连接生命周期：

#### 线程模型
- **接收任务**：持续读取TCP数据，解析消息
- **发送任务**：从Channel读取消息并发送
- **心跳任务**：定期发送Linktest

#### 状态转换
```mermaid
stateDiagram-v2
[*] --> 未连接
未连接 --> 正在连接 : ConnectAsync()
未连接 --> 已连接 : StartListeningAsync()
正在连接 --> 已连接 : 连接成功
已连接 --> 已选择 : Select请求成功
已选择 --> 正在断开 : DisconnectAsync()
正在断开 --> 未连接 : 资源清理完成
```

**图表来源**
- [ConnectionState.cs:10-41](file://WebGem/SECS2GEM/Core/Enums/ConnectionState.cs#L10-L41)
- [HsmsConnection.cs:64-78](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L64-L78)

**章节来源**
- [HsmsConnection.cs:30-418](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L30-L418)

## 架构概览

SECS连接架构采用分层设计，各层职责明确：

```mermaid
graph TB
subgraph "应用层"
GemEquipmentService[GemEquipmentService]
MessageDispatcher[MessageDispatcher]
end
subgraph "接口层"
ISecsConnection[ISecsConnection]
IMessageHandler[IMessageHandler]
IMessageContext[IMessageContext]
end
subgraph "实现层"
HsmsConnection[HsmsConnection]
TransactionManager[TransactionManager]
MessageContext[MessageContext]
end
subgraph "基础设施层"
HsmsConfiguration[HsmsConfiguration]
SecsSerializer[SecsSerializer]
MessageLogger[MessageLogger]
end
subgraph "核心层"
SecsMessage[SecsMessage]
HsmsMessage[HsmsMessage]
ConnectionState[ConnectionState]
end
GemEquipmentService --> ISecsConnection
MessageDispatcher --> IMessageHandler
ISecsConnection --> HsmsConnection
HsmsConnection --> TransactionManager
HsmsConnection --> MessageContext
HsmsConnection --> HsmsConfiguration
HsmsConnection --> SecsSerializer
MessageContext --> IMessageContext
```

**图表来源**
- [HsmsConnection.cs:122-139](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L122-L139)
- [TransactionManager.cs:24-119](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L24-L119)

## 详细组件分析

### 连接建立流程

#### 主动连接流程
```mermaid
sequenceDiagram
participant Client as 客户端
participant Connection as HsmsConnection
participant TCP as TCP套接字
participant Serializer as 序列化器
Client->>Connection : ConnectAsync()
Connection->>Connection : 设置状态=Connecting
Connection->>TCP : ConnectAsync(ip, port)
TCP-->>Connection : 连接成功
Connection->>Connection : 设置状态=Connected
Connection->>Serializer : 序列化Select请求
Connection->>TCP : 发送Select请求
TCP-->>Connection : Select响应
Connection->>Connection : 设置状态=Selected
Connection-->>Client : 连接建立完成
```

**图表来源**
- [HsmsConnection.cs:146-186](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L146-L186)
- [HsmsConnection.cs:520-541](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L520-L541)

#### 被动连接流程
```mermaid
sequenceDiagram
participant Server as 服务器
participant Listener as TcpListener
participant Connection as HsmsConnection
participant TCP as TCP套接字
participant Timer as T7定时器
Server->>Connection : StartListeningAsync()
Connection->>Listener : Start()
loop 等待连接
Listener->>Connection : AcceptTcpClientAsync()
Connection->>Connection : 设置状态=Connected
Connection->>Timer : 启动T7超时监控
Timer->>Connection : T7超时检查
alt 未收到Select请求
Connection->>Connection : DisconnectAsync()
else 收到Select请求
Connection->>Connection : 设置状态=Selected
end
end
```

**图表来源**
- [HsmsConnection.cs:191-296](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L191-L296)

**章节来源**
- [HsmsConnection.cs:146-296](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L146-L296)

### 消息发送与接收

#### 异步消息发送流程
```mermaid
flowchart TD
Start([开始发送]) --> CheckSelected{是否已选择?}
CheckSelected --> |否| ThrowError[抛出NotSelected异常]
CheckSelected --> |是| CreateTransaction[创建事务]
CreateTransaction --> Serialize[序列化消息]
Serialize --> Enqueue[入发送队列]
Enqueue --> WaitResponse{等待响应?}
WaitResponse --> |是| WaitForResponse[等待响应]
WaitResponse --> |否| CompleteSend[完成发送]
WaitForResponse --> CompleteSend
CompleteSend --> End([发送完成])
ThrowError --> End
```

**图表来源**
- [HsmsConnection.cs:427-453](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L427-L453)
- [TransactionManager.cs:46-72](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L46-L72)

#### 消息处理流程
```mermaid
flowchart TD
Receive([接收消息]) --> ParseMessage{解析消息}
ParseMessage --> |控制消息| HandleControl[处理控制消息]
ParseMessage --> |数据消息| HandleData[处理数据消息]
HandleControl --> ControlSwitch{消息类型切换}
ControlSwitch --> |SelectRequest| SendSelectResponse[发送Select响应]
ControlSwitch --> |DeselectRequest| SendDeselectResponse[发送Deselect响应]
ControlSwitch --> |LinktestRequest| SendLinktestResponse[发送Linktest响应]
ControlSwitch --> |SeparateRequest| Disconnect[断开连接]
HandleData --> CheckSecondary{Secondary消息?}
CheckSecondary --> |是| CompleteTransaction[完成事务]
CheckSecondary --> |否| TriggerEvent[触发消息事件]
SendSelectResponse --> UpdateState[更新状态=Selected]
SendDeselectResponse --> Disconnect
SendLinktestResponse --> UpdateState
CompleteTransaction --> End([处理完成])
TriggerEvent --> End
UpdateState --> End
```

**图表来源**
- [HsmsConnection.cs:732-814](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L732-L814)
- [HsmsConnection.cs:747-792](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L747-L792)

**章节来源**
- [HsmsConnection.cs:427-814](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L427-L814)

### 配置管理

HsmsConfiguration提供了全面的连接参数配置：

#### 超时参数配置
- **T3超时**：回复超时（默认45秒）
- **T5超时**：连接分离超时（默认10秒）
- **T6超时**：控制事务超时（默认5秒）
- **T7超时**：未选择超时（默认10秒）
- **T8超时**：网络字符间隔超时（默认5秒）

#### 心跳参数配置
- **LinktestInterval**：心跳间隔（默认30秒）
- **MaxLinktestFailures**：最大连续心跳失败次数（默认3次）

#### 缓冲区配置
- **ReceiveBufferSize**：接收缓冲区大小（默认64KB）
- **SendBufferSize**：发送缓冲区大小（默认64KB）

**章节来源**
- [HsmsConfiguration.cs:15-228](file://WebGem/SECS2GEM/Infrastructure/Configuration/HsmsConfiguration.cs#L15-L228)

## 依赖关系分析

### 类关系图
```mermaid
classDiagram
class ISecsConnection {
+ConnectionState State
+bool IsSelected
+ushort SessionId
+string RemoteEndpoint
+event StateChanged
+event PrimaryMessageReceived
+ConnectAsync(cancellationToken) Task
+StartListeningAsync(cancellationToken) Task
+DisconnectAsync(cancellationToken) Task
+SendAsync(message, cancellationToken) Task~SecsMessage~
+SendOnlyAsync(message, cancellationToken) Task
+SendLinktestAsync(cancellationToken) Task~bool~
}
class HsmsConnection {
-HsmsConfiguration _config
-ISecsSerializer _serializer
-ITransactionManager _transactionManager
-IMessageLogger _messageLogger
-TcpListener _listener
-TcpClient _tcpClient
-NetworkStream _stream
-ConnectionState _state
+ConnectAsync(cancellationToken) Task
+StartListeningAsync(cancellationToken) Task
+DisconnectAsync(cancellationToken) Task
+SendAsync(message, cancellationToken) Task~SecsMessage~
+SendOnlyAsync(message, cancellationToken) Task
+SendLinktestAsync(cancellationToken) Task~bool~
-InitializeChannelAndTasks() void
-HandleMessageAsync(message, cancellationToken) Task
}
class IMessageContext {
+ushort DeviceId
+uint SystemBytes
+DateTime ReceivedTime
+ReplyAsync(response, cancellationToken) Task
}
class MessageContext {
-Func~SecsMessage,uint,CancellationToken,Task~ _replyFunc
+ReplyAsync(response, cancellationToken) Task
}
class ITransactionManager {
+int ActiveTransactionCount
+uint GetNextTransactionId()
+BeginTransaction(systemBytes, messageName, timeout) ITransaction
+TryCompleteTransaction(systemBytes, response) bool
+CancelTransaction(systemBytes)
+CancelAllTransactions()
}
class TransactionManager {
-uint _transactionIdCounter
-ConcurrentDictionary~uint,Transaction~ _activeTransactions
+GetNextTransactionId() uint
+BeginTransaction(systemBytes, messageName, timeout) ITransaction
+TryCompleteTransaction(systemBytes, response) bool
}
ISecsConnection <|.. HsmsConnection
IMessageContext <|.. MessageContext
ITransactionManager <|.. TransactionManager
HsmsConnection --> IMessageContext : "创建"
HsmsConnection --> ITransactionManager : "使用"
HsmsConnection --> HsmsConfiguration : "依赖"
```

**图表来源**
- [ISecsConnection.cs:71-142](file://WebGem/SECS2GEM/Domain/Interfaces/ISecsConnection.cs#L71-L142)
- [HsmsConnection.cs:30-139](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L30-L139)
- [MessageContext.cs:12-63](file://WebGem/SECS2GEM/Infrastructure/Connection/MessageContext.cs#L12-L63)
- [TransactionManager.cs:24-119](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L24-L119)

### 依赖注入关系
```mermaid
graph LR
subgraph "外部依赖"
TcpClient[TcpClient]
Channel[Channel]
CancellationTokenSource[CancellationTokenSource]
end
subgraph "内部组件"
HsmsConnection[HsmsConnection]
TransactionManager[TransactionManager]
MessageContext[MessageContext]
HsmsConfiguration[HsmsConfiguration]
end
subgraph "核心服务"
ISecsSerializer[ISecsSerializer]
IMessageLogger[IMessageLogger]
end
HsmsConnection --> TcpClient
HsmsConnection --> Channel
HsmsConnection --> CancellationTokenSource
HsmsConnection --> TransactionManager
HsmsConnection --> MessageContext
HsmsConnection --> HsmsConfiguration
HsmsConnection --> ISecsSerializer
HsmsConnection --> IMessageLogger
```

**图表来源**
- [HsmsConnection.cs:32-36](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L32-L36)
- [TransactionManager.cs:24-28](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L24-L28)

**章节来源**
- [HsmsConnection.cs:30-418](file://WebGem/SECS2GEM/Infrastructure/Connection/HsmsConnection.cs#L30-L418)
- [TransactionManager.cs:24-201](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L24-L201)

## 性能考虑

### 并发连接处理最佳实践

#### 连接池管理
- **单连接单用途**：每个HsmsConnection实例管理单一连接
- **无连接池设计**：避免连接池复杂性，简化状态管理
- **资源隔离**：每个连接拥有独立的缓冲区和线程

#### 异步处理优化
- **Channel队列**：使用无界Channel实现消息队列
- **Task并行**：接收、发送、心跳任务并行执行
- **零拷贝优化**：直接使用byte[]避免额外复制

#### 内存管理
- **缓冲区复用**：接收缓冲区在循环中复用
- **及时释放**：连接断开时立即释放所有资源
- **弱引用**：避免循环引用导致的内存泄漏

### 性能调优建议

#### 网络参数优化
- **缓冲区大小**：根据消息大小调整ReceiveBufferSize和SendBufferSize
- **心跳间隔**：平衡心跳频率与CPU消耗
- **超时设置**：根据网络环境调整T3-T8超时参数

#### 事务管理优化
- **事务超时**：合理设置T3超时避免长时间占用资源
- **事务清理**：及时清理超时和已完成的事务
- **并发控制**：避免同时发送大量需要响应的消息

**章节来源**
- [HsmsConfiguration.cs:96-173](file://WebGem/SECS2GEM/Infrastructure/Configuration/HsmsConfiguration.cs#L96-L173)
- [TransactionManager.cs:124-201](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L124-L201)

## 故障排除指南

### 常见连接问题

#### 连接失败诊断
```mermaid
flowchart TD
ConnectFail[连接失败] --> CheckConfig{检查配置}
CheckConfig --> ConfigOK{配置正确?}
ConfigOK --> |否| FixConfig[修正配置]
ConfigOK --> |是| CheckNetwork{检查网络}
CheckNetwork --> NetworkOK{网络连通?}
NetworkOK --> |否| FixNetwork[修复网络]
NetworkOK --> |是| CheckFirewall{检查防火墙}
CheckFirewall --> FirewallOK{防火墙允许?}
FirewallOK --> |否| FixFirewall[配置防火墙]
FirewallOK --> |是| CheckServer{检查服务器状态}
CheckServer --> ServerOK{服务器正常?}
ServerOK --> |否| FixServer[修复服务器]
ServerOK --> |是| EnableLogging[启用详细日志]
```

**图表来源**
- [SecsCommunicationException.cs:114-151](file://WebGem/SECS2GEM/Core/Exceptions/SecsCommunicationException.cs#L114-L151)

#### 超时问题诊断
- **T3超时**：消息发送后未收到响应
- **T6超时**：控制消息（Select/Deselect/Linktest）响应超时
- **T7超时**：被动模式下未收到Select请求
- **T8超时**：消息传输中字符间隔超时

#### 心跳问题诊断
- **Linktest失败**：连续多次心跳请求无响应
- **连接中断**：心跳失败达到阈值后自动断开
- **恢复机制**：断开后按T5间隔自动重连

### 日志和监控

#### 消息日志配置
- **启用条件**：通过MessageLogging配置控制
- **日志级别**：区分详细和基本日志模式
- **文件管理**：自动轮转和清理旧日志

#### 异常处理策略
- **通信异常**：封装具体的错误类型
- **超时异常**：提供详细的超时信息
- **状态异常**：在不适当状态下调用方法时抛出

**章节来源**
- [SecsCommunicationException.cs:64-154](file://WebGem/SECS2GEM/Core/Exceptions/SecsCommunicationException.cs#L64-L154)
- [SecsTimeoutException.cs:57-162](file://WebGem/SECS2GEM/Core/Exceptions/SecsTimeoutException.cs#L57-L162)

## 结论

SECS连接接口提供了完整的HSMS协议实现，具有以下特点：

### 技术优势
- **标准兼容**：完全符合SEMI E37标准
- **异步设计**：基于现代.NET异步编程模型
- **状态管理**：清晰的连接生命周期管理
- **扩展性强**：模块化设计便于功能扩展

### 实现特色
- **事件驱动**：通过事件实现松耦合的消息处理
- **事务管理**：完善的事务跟踪和超时处理
- **错误处理**：详细的异常类型和诊断信息
- **性能优化**：多线程并发和内存优化

### 应用场景
该接口适用于各种SEMI E37标准的设备通信场景，包括：
- 半导体制造设备通信
- 显示面板生产设备集成
- 电子元器件测试设备
- 自动化生产线控制系统

通过合理的配置和使用，SECS连接接口能够为企业提供稳定可靠的设备通信解决方案。