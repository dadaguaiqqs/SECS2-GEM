# Stream消息处理

<cite>
**本文档引用的文件**
- [StreamOneHandlers.cs](file://WebGem/SECS2GEM/Application/Handlers/StreamOneHandlers.cs)
- [StreamTwoHandlers.cs](file://WebGem/SECS2GEM/Application/Handlers/StreamTwoHandlers.cs)
- [OtherStreamHandlers.cs](file://WebGem/SECS2GEM/Application/Handlers/OtherStreamHandlers.cs)
- [MessageDispatcher.cs](file://WebGem/SECS2GEM/Application/Messaging/MessageDispatcher.cs)
- [IMessageHandler.cs](file://WebGem/SECS2GEM/Domain/Interfaces/IMessageHandler.cs)
- [SecsMessage.cs](file://WebGem/SECS2GEM/Core/Entities/SecsMessage.cs)
- [SecsItem.cs](file://WebGem/SECS2GEM/Core/Entities/SecsItem.cs)
- [EquipmentConstant.cs](file://WebGem/SECS2GEM/Domain/Models/EquipmentConstant.cs)
- [IEventAggregator.cs](file://WebGem/SECS2GEM/Domain/Interfaces/IEventAggregator.cs)
- [TransactionManager.cs](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs)
- [HsmsConfiguration.cs](file://WebGem/SECS2GEM/Infrastructure/Configuration/HsmsConfiguration.cs)
- [GemStateManager.cs](file://WebGem/SECS2GEM/Application/State/GemStateManager.cs)
- [IGemState.cs](file://WebGem/SECS2GEM/Domain/Interfaces/IGemState.cs)
- [MessageHandlerTests.cs](file://WebGem/SECS2GEM.Tests/MessageHandlerTests.cs)
- [GEM_Protocol_Specification.md](file://WebGem/SECS2GEM/GEM_Protocol_Specification.md)
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

本文档专注于SECS-II协议中Stream 1-10的消息处理机制，详细阐述主从消息配对规则、响应时机以及不同类型Stream的用途和处理流程。SECS-II（Semiconductor Equipment and Materials International Organization）是半导体制造设备与主机系统之间的标准通信协议，基于HSMS（High-Speed Message Service）实现。

在本系统中，Stream 1-10分别对应不同的设备管理功能：
- **Stream 1**: 设备状态管理（连接建立、设备状态查询）
- **Stream 2**: 设备控制（设备常量管理、事件报告、远程命令）
- **Stream 5**: 异常处理（报警管理）
- **Stream 6**: 数据采集（事件报告）
- **Stream 7**: 配方管理（工艺程序管理）
- **Stream 10**: 终端服务（显示控制）

## 项目结构

系统采用分层架构设计，主要分为以下层次：

```mermaid
graph TB
subgraph "应用层"
Handlers[消息处理器]
State[状态管理器]
Messaging[消息处理]
end
subgraph "领域层"
Interfaces[接口定义]
Models[业务模型]
Events[事件系统]
end
subgraph "基础设施层"
Services[服务组件]
Configuration[配置管理]
Logging[日志系统]
end
subgraph "核心实体层"
SecsMessage[SECS消息]
SecsItem[数据项]
end
Handlers --> SecsMessage
State --> SecsMessage
Messaging --> SecsMessage
Interfaces --> Handlers
Models --> State
Services --> State
Configuration --> Services
Logging --> Services
```

**图表来源**
- [MessageDispatcher.cs:1-123](file://WebGem/SECS2GEM/Application/Messaging/MessageDispatcher.cs#L1-123)
- [IMessageHandler.cs:1-131](file://WebGem/SECS2GEM/Domain/Interfaces/IMessageHandler.cs#L1-131)

**章节来源**
- [MessageDispatcher.cs:1-123](file://WebGem/SECS2GEM/Application/Messaging/MessageDispatcher.cs#L1-123)
- [IMessageHandler.cs:1-131](file://WebGem/SECS2GEM/Domain/Interfaces/IMessageHandler.cs#L1-131)

## 核心组件

### 消息处理器基类

系统实现了模板方法模式的消息处理器基类，提供统一的处理框架：

```mermaid
classDiagram
class MessageHandlerBase {
+int Priority
#byte Stream
#byte Function
+bool CanHandle(message) bool
+Task HandleAsync(message, context, cancellationToken) SecsMessage?
#Task HandleCoreAsync(message, context, cancellationToken) SecsMessage?
#SecsMessage CreateErrorResponse(request) SecsMessage
}
class S1F1Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S1F13Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S2F13Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
MessageHandlerBase <|-- S1F1Handler
MessageHandlerBase <|-- S1F13Handler
MessageHandlerBase <|-- S2F13Handler
```

**图表来源**
- [StreamOneHandlers.cs:20-86](file://WebGem/SECS2GEM/Application/Handlers/StreamOneHandlers.cs#L20-86)
- [StreamOneHandlers.cs:94-114](file://WebGem/SECS2GEM/Application/Handlers/StreamOneHandlers.cs#L94-114)

### 消息分发器

消息分发器采用责任链+策略模式，负责将消息路由到相应的处理器：

```mermaid
sequenceDiagram
participant Client as 客户端
participant Dispatcher as 消息分发器
participant Handler as 消息处理器
participant State as 状态管理器
Client->>Dispatcher : 接收SecsMessage
Dispatcher->>Dispatcher : GetSortedHandlers()
loop 遍历处理器
Dispatcher->>Handler : CanHandle(message)
alt 找到匹配处理器
Handler->>State : 访问设备状态
Handler->>Handler : HandleCoreAsync()
Handler-->>Dispatcher : 返回响应消息
Dispatcher-->>Client : 发送响应
end
end
alt 无匹配处理器
Dispatcher->>Dispatcher : CreateS9F7Response()
Dispatcher-->>Client : 返回错误响应
end
```

**图表来源**
- [MessageDispatcher.cs:67-91](file://WebGem/SECS2GEM/Application/Messaging/MessageDispatcher.cs#L67-91)
- [IMessageHandler.cs:63-88](file://WebGem/SECS2GEM/Domain/Interfaces/IMessageHandler.cs#L63-88)

**章节来源**
- [MessageDispatcher.cs:1-123](file://WebGem/SECS2GEM/Application/Messaging/MessageDispatcher.cs#L1-123)
- [IMessageHandler.cs:1-131](file://WebGem/SECS2GEM/Domain/Interfaces/IMessageHandler.cs#L1-131)

## 架构概览

系统采用模块化设计，各组件职责明确：

```mermaid
graph TD
subgraph "外部系统"
Host[主机/MES系统]
Simulator[模拟器]
end
subgraph "SECS-II处理层"
HSMS[HSMS连接]
SecsParser[SECS-II解析器]
MessageRouter[消息路由器]
end
subgraph "应用处理层"
StateMgr[GEM状态管理]
MsgHandlers[消息处理器]
EventAggregator[事件聚合器]
end
subgraph "设备接口层"
Equipment[设备硬件]
Sensors[传感器]
Actuators[执行器]
end
Host --> HSMS
Simulator --> HSMS
HSMS --> SecsParser
SecsParser --> MessageRouter
MessageRouter --> StateMgr
MessageRouter --> MsgHandlers
MsgHandlers --> EventAggregator
StateMgr --> Equipment
EventAggregator --> Equipment
Equipment --> Sensors
Equipment --> Actuators
```

**图表来源**
- [GemStateManager.cs:22-492](file://WebGem/SECS2GEM/Application/State/GemStateManager.cs#L22-492)
- [TransactionManager.cs:24-201](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L24-201)

## 详细组件分析

### Stream 1 - 设备状态管理

Stream 1负责设备状态相关的消息处理，包括连接建立、设备状态查询等功能。

#### S1F1 - Are You There

S1F1是设备存在查询消息，用于检测设备是否在线：

```mermaid
sequenceDiagram
participant Host as 主机
participant Equipment as 设备
participant Handler as S1F1处理器
participant State as 状态管理器
Host->>Equipment : S1F1 Are You There
Equipment->>Handler : 接收消息
Handler->>State : 获取设备型号和版本
Handler->>Handler : 创建S1F2响应
Handler-->>Equipment : 返回S1F2
Equipment-->>Host : 设备在线数据
```

**图表来源**
- [StreamOneHandlers.cs:99-113](file://WebGem/SECS2GEM/Application/Handlers/StreamOneHandlers.cs#L99-113)

#### S1F13 - Establish Communications Request

S1F13用于建立通信连接请求：

```mermaid
flowchart TD
Start([接收S1F13]) --> ValidateState["验证通信状态"]
ValidateState --> StateValid{"状态有效?"}
StateValid --> |否| ReturnDenied["返回拒绝(CommAck=1)"]
StateValid --> |是| SetCommunicating["设置通信状态为Communicating"]
SetCommunicating --> CreateResponse["创建S1F14响应"]
CreateResponse --> ReturnAccepted["返回接受(CommAck=0)"]
ReturnDenied --> End([结束])
ReturnAccepted --> End
```

**图表来源**
- [StreamOneHandlers.cs:127-148](file://WebGem/SECS2GEM/Application/Handlers/StreamOneHandlers.cs#L127-148)

**章节来源**
- [StreamOneHandlers.cs:89-210](file://WebGem/SECS2GEM/Application/Handlers/StreamOneHandlers.cs#L89-210)

### Stream 2 - 设备控制

Stream 2处理设备控制相关消息，包括设备常量管理、事件报告和远程命令。

#### S2F13 - Equipment Constant Request

设备常量查询处理器：

```mermaid
sequenceDiagram
participant Host as 主机
participant Handler as S2F13处理器
participant State as 状态管理器
participant Equipment as 设备常量
Host->>Handler : S2F13 Equipment Constant Request
Handler->>State : 获取设备常量列表
alt 请求特定常量
Handler->>State : 查询指定ECID
State->>Equipment : 获取常量值
Equipment-->>State : 返回常量值
State-->>Handler : 返回常量值
else 请求所有常量
Handler->>State : 获取所有设备常量
State-->>Handler : 返回常量列表
end
Handler->>Handler : 创建S2F14响应
Handler-->>Host : 返回设备常量数据
```

**图表来源**
- [StreamTwoHandlers.cs:18-57](file://WebGem/SECS2GEM/Application/Handlers/StreamTwoHandlers.cs#L18-57)

#### S2F41 - Host Command Send

远程命令处理处理器：

```mermaid
flowchart TD
Start([接收S2F41]) --> ParseCommand["解析RCMD参数"]
ParseCommand --> ValidateParams{"验证参数"}
ValidateParams --> |无效| SetHcackInvalid["设置HCACK=1"]
ValidateParams --> |有效| FindHandler["查找命令处理器"]
FindHandler --> HandlerFound{"找到处理器?"}
HandlerFound --> |否| SetHcackInvalid
HandlerFound --> |是| ExecuteCommand["执行命令"]
ExecuteCommand --> CollectResults["收集参数确认"]
CollectResults --> SetHcackSuccess["设置HCACK=0"]
SetHcackInvalid --> CreateResponse["创建S2F42响应"]
SetHcackSuccess --> CreateResponse
CreateResponse --> End([发送响应])
```

**图表来源**
- [StreamTwoHandlers.cs:285-328](file://WebGem/SECS2GEM/Application/Handlers/StreamTwoHandlers.cs#L285-328)

**章节来源**
- [StreamTwoHandlers.cs:1-331](file://WebGem/SECS2GEM/Application/Handlers/StreamTwoHandlers.cs#L1-331)

### 其他Stream处理

#### Stream 5 - 异常处理

Stream 5处理报警相关的消息：

```mermaid
classDiagram
class S5F3Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S5F5Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S5F7Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
MessageHandlerBase <|-- S5F3Handler
MessageHandlerBase <|-- S5F5Handler
MessageHandlerBase <|-- S5F7Handler
```

**图表来源**
- [OtherStreamHandlers.cs:9-27](file://WebGem/SECS2GEM/Application/Handlers/OtherStreamHandlers.cs#L9-27)

#### Stream 6 - 数据采集

Stream 6处理事件报告相关消息：

```mermaid
classDiagram
class S6F15Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S6F19Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
MessageHandlerBase <|-- S6F15Handler
MessageHandlerBase <|-- S6F19Handler
```

**图表来源**
- [OtherStreamHandlers.cs:72-113](file://WebGem/SECS2GEM/Application/Handlers/OtherStreamHandlers.cs#L72-113)

#### Stream 7 - 配方管理

Stream 7处理工艺程序管理：

```mermaid
classDiagram
class S7F1Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S7F3Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S7F5Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S7F17Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
MessageHandlerBase <|-- S7F1Handler
MessageHandlerBase <|-- S7F3Handler
MessageHandlerBase <|-- S7F5Handler
MessageHandlerBase <|-- S7F17Handler
```

**图表来源**
- [OtherStreamHandlers.cs:118-208](file://WebGem/SECS2GEM/Application/Handlers/OtherStreamHandlers.cs#L118-208)

#### Stream 10 - 终端服务

Stream 10处理终端显示相关消息：

```mermaid
classDiagram
class S10F3Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
class S10F5Handler {
+HandleCoreAsync(message, context, cancellationToken) SecsMessage?
}
MessageHandlerBase <|-- S10F3Handler
MessageHandlerBase <|-- S10F5Handler
```

**图表来源**
- [OtherStreamHandlers.cs:233-274](file://WebGem/SECS2GEM/Application/Handlers/OtherStreamHandlers.cs#L233-274)

**章节来源**
- [OtherStreamHandlers.cs:1-276](file://WebGem/SECS2GEM/Application/Handlers/OtherStreamHandlers.cs#L1-276)

## 依赖关系分析

系统采用松耦合设计，通过接口隔离实现依赖注入：

```mermaid
graph TB
subgraph "接口层"
IMessageHandler[IMessageHandler]
IMessageContext[IMessageContext]
IGemState[IGemState]
IEventAggregator[IEventAggregator]
ITransactionManager[ITransactionManager]
end
subgraph "实现层"
MessageHandlerBase[MessageHandlerBase]
MessageDispatcher[MessageDispatcher]
GemStateManager[GemStateManager]
TransactionManager[TransactionManager]
EventAggregator[EventAggregator]
end
subgraph "实体层"
SecsMessage[SecsMessage]
SecsItem[SecsItem]
EquipmentConstant[EquipmentConstant]
end
IMessageHandler --> MessageHandlerBase
IMessageContext --> MessageDispatcher
IGemState --> GemStateManager
IEventAggregator --> EventAggregator
ITransactionManager --> TransactionManager
MessageHandlerBase --> IMessageHandler
MessageDispatcher --> IMessageContext
GemStateManager --> IGemState
TransactionManager --> ITransactionManager
MessageHandlerBase --> SecsMessage
MessageDispatcher --> SecsMessage
GemStateManager --> EquipmentConstant
SecsMessage --> SecsItem
```

**图表来源**
- [IMessageHandler.cs:63-131](file://WebGem/SECS2GEM/Domain/Interfaces/IMessageHandler.cs#L63-131)
- [GemStateManager.cs:22-492](file://WebGem/SECS2GEM/Application/State/GemStateManager.cs#L22-492)

**章节来源**
- [IMessageHandler.cs:1-131](file://WebGem/SECS2GEM/Domain/Interfaces/IMessageHandler.cs#L1-131)
- [GemStateManager.cs:1-492](file://WebGem/SECS2GEM/Application/State/GemStateManager.cs#L1-492)

## 性能考虑

### 事务处理机制

系统实现了完整的事务管理机制，确保消息处理的可靠性和一致性：

```mermaid
sequenceDiagram
participant Client as 客户端
participant TM as 事务管理器
participant Handler as 消息处理器
participant Device as 设备
Client->>TM : BeginTransaction()
TM->>TM : 生成SystemBytes
TM-->>Client : 返回事务句柄
Client->>Handler : 发送消息
Handler->>Device : 处理请求
Device-->>Handler : 返回响应
Handler->>TM : TryCompleteTransaction()
TM-->>Client : 返回响应结果
Note over TM : 自动超时处理
```

**图表来源**
- [TransactionManager.cs:46-72](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L46-72)

### 超时参数配置

系统提供了灵活的超时参数配置，支持不同场景的需求：

| 参数 | 名称 | 描述 | 默认值 |
|------|------|------|--------|
| T3 | Reply Timeout | 等待回复超时 | 45秒 |
| T5 | Connection Separation Timeout | 连接分离超时 | 10秒 |
| T6 | Control Transaction Timeout | 控制事务超时 | 5秒 |
| T7 | Not Selected Timeout | 未选择超时 | 10秒 |
| T8 | Network Intercharacter Timeout | 网络字符间隔超时 | 5秒 |

**章节来源**
- [TransactionManager.cs:1-201](file://WebGem/SECS2GEM/Infrastructure/Services/TransactionManager.cs#L1-201)
- [HsmsConfiguration.cs:15-266](file://WebGem/SECS2GEM/Infrastructure/Configuration/HsmsConfiguration.cs#L15-266)

## 故障排除指南

### 常见问题及解决方案

#### 消息处理失败

当消息无法被任何处理器处理时，系统会返回S9F7错误响应：

```mermaid
flowchart TD
ReceiveMsg[接收消息] --> CheckHandlers{检查处理器}
CheckHandlers --> HandlersFound{"找到处理器?"}
HandlersFound --> |是| ProcessMsg[处理消息]
HandlersFound --> |否| CheckWBit{W-Bit=true?}
CheckWBit --> |是| ReturnS9F7[返回S9F7错误]
CheckWBit --> |否| ReturnNull[返回null]
ProcessMsg --> SendResponse[发送响应]
ReturnS9F7 --> End([结束])
ReturnNull --> End
SendResponse --> End
```

**图表来源**
- [MessageDispatcher.cs:83-91](file://WebGem/SECS2GEM/Application/Messaging/MessageDispatcher.cs#L83-91)

#### 状态转换异常

设备状态转换需要遵循严格的规则，违反规则会导致状态转换失败：

```mermaid
stateDiagram-v2
[*] --> Disabled
Disabled --> Enabled : Establish Communication
Enabled --> WaitCommunicationRequest : Request Communication
WaitCommunicationRequest --> WaitCommunicationDelay : Delay
WaitCommunicationDelay --> WaitCommunicationRequest : Retry
WaitCommunicationRequest --> Communicating : Success
Communicating --> Disabled : Disconnect
Communicating --> Enabled : Re-establish
```

**图表来源**
- [GemStateManager.cs:357-387](file://WebGem/SECS2GEM/Application/State/GemStateManager.cs#L357-387)

### 测试验证

系统提供了完整的单元测试，验证各种消息处理场景：

**章节来源**
- [MessageHandlerTests.cs:1-279](file://WebGem/SECS2GEM.Tests/MessageHandlerTests.cs#L1-279)

## 结论

本系统为SECS-II协议中的Stream 1-10消息处理提供了完整的解决方案，具有以下特点：

1. **模块化设计**: 采用分层架构，各组件职责明确，便于维护和扩展
2. **标准化实现**: 严格遵循GEM协议规范，确保与主机系统的兼容性
3. **可靠性保障**: 实现了完整的事务管理、超时处理和错误恢复机制
4. **灵活性**: 支持动态注册处理器，可根据需求扩展新的消息类型
5. **可测试性**: 提供完善的单元测试，确保代码质量

通过合理利用这些组件和机制，可以构建稳定可靠的SECS-II设备通信系统，满足半导体制造设备与主机系统之间的数据交换需求。