# SECS2GEM 类图

## 1. 整体架构类图

```mermaid
classDiagram
    direction TB
    
    %% Application Layer
    class GemEquipmentService {
        -GemStateManager _stateManager
        -MessageDispatcher _dispatcher
        -HsmsConnection _connection
        -EventAggregator _eventAggregator
        +ushort DeviceId
        +IGemState State
        +bool IsRunning
        +bool IsConnected
        +StartAsync()
        +StopAsync()
        +SendAsync()
        +SendEventAsync()
        +SendAlarmAsync()
    }
    
    class GemStateManager {
        -ConcurrentDictionary~uint,StatusVariable~ _statusVariables
        -ConcurrentDictionary~uint,EquipmentConstant~ _equipmentConstants
        -GemCommunicationState _communicationState
        -GemControlState _controlState
        -GemProcessingState _processingState
        +string ModelName
        +string SoftwareRevision
        +GetStatusVariable()
        +SetStatusVariable()
        +GetEquipmentConstant()
        +TrySetEquipmentConstant()
        +SetCommunicationState()
        +SetControlState()
    }
    
    class MessageDispatcher {
        -List~IMessageHandler~ _handlers
        +RegisterHandler()
        +UnregisterHandler()
        +DispatchAsync()
    }
    
    %% Infrastructure Layer
    class HsmsConnection {
        -HsmsConfiguration _config
        -ISecsSerializer _serializer
        -ITransactionManager _transactionManager
        -TcpClient _tcpClient
        -NetworkStream _stream
        +ConnectionState State
        +bool IsSelected
        +ConnectAsync()
        +StartListeningAsync()
        +DisconnectAsync()
        +SendAsync()
    }
    
    class SecsSerializer {
        +int MaxMessageSize
        +Serialize()
        +SerializeItem()
        +Deserialize()
        +DeserializeItem()
        +TryReadMessage()
    }
    
    class TransactionManager {
        -ConcurrentDictionary~uint,Transaction~ _activeTransactions
        +GetNextTransactionId()
        +BeginTransaction()
        +TryCompleteTransaction()
        +CancelTransaction()
    }
    
    class EventAggregator {
        -ConcurrentDictionary~Type,List~ _subscriptions
        +PublishAsync()
        +Publish()
        +Subscribe()
        +ClearSubscriptions()
    }
    
    %% Domain Interfaces
    class IGemEquipmentService {
        <<interface>>
        +DeviceId
        +State
        +IsRunning
        +StartAsync()
        +StopAsync()
        +SendAsync()
    }
    
    class IGemState {
        <<interface>>
        +CommunicationState
        +ControlState
        +ProcessingState
        +GetStatusVariable()
        +SetStatusVariable()
    }
    
    class ISecsConnection {
        <<interface>>
        +State
        +IsSelected
        +ConnectAsync()
        +SendAsync()
    }
    
    class IMessageHandler {
        <<interface>>
        +Priority
        +CanHandle()
        +HandleAsync()
    }
    
    class IMessageDispatcher {
        <<interface>>
        +RegisterHandler()
        +DispatchAsync()
    }
    
    class ISecsSerializer {
        <<interface>>
        +Serialize()
        +Deserialize()
    }
    
    class ITransactionManager {
        <<interface>>
        +GetNextTransactionId()
        +BeginTransaction()
    }
    
    class IEventAggregator {
        <<interface>>
        +PublishAsync()
        +Subscribe()
    }
    
    %% Relationships
    GemEquipmentService ..|> IGemEquipmentService
    GemStateManager ..|> IGemState
    HsmsConnection ..|> ISecsConnection
    MessageDispatcher ..|> IMessageDispatcher
    SecsSerializer ..|> ISecsSerializer
    TransactionManager ..|> ITransactionManager
    EventAggregator ..|> IEventAggregator
    
    GemEquipmentService --> GemStateManager
    GemEquipmentService --> MessageDispatcher
    GemEquipmentService --> HsmsConnection
    GemEquipmentService --> EventAggregator
    
    HsmsConnection --> SecsSerializer
    HsmsConnection --> TransactionManager
    
    MessageDispatcher --> IMessageHandler
```

## 2. Core层 - 实体类图

```mermaid
classDiagram
    direction LR
    
    class SecsItem {
        -object _value
        -ReadOnlyCollection~SecsItem~ _items
        +SecsFormat Format
        +int Count
        +ReadOnlyCollection~SecsItem~ Items
        +object Value
        +L()$ SecsItem
        +A()$ SecsItem
        +B()$ SecsItem
        +U4()$ SecsItem
        +I4()$ SecsItem
        +F8()$ SecsItem
        +GetString()
        +GetBytes()
        +GetInt64()
        +ToSml()
    }
    
    class SecsMessage {
        +byte Stream
        +byte Function
        +bool WBit
        +SecsItem Item
        +string Name
        +bool IsPrimary
        +bool IsSecondary
    }
    
    class HsmsHeader {
        +ushort SessionId
        +byte Stream
        +byte Function
        +bool WBit
        +byte SType
        +uint SystemBytes
        +bool IsDataMessage
        +bool IsControlMessage
        +ToBytes()
        +CreateDataMessage()$
        +CreateSelectRequest()$
        +CreateLinktestRequest()$
    }
    
    class HsmsMessage {
        +HsmsHeader Header
        +SecsMessage SecsMessage
        +byte[] RawBytes
        +ushort SessionId
        +HsmsMessageType MessageType
        +uint SystemBytes
        +bool IsDataMessage
        +CreateDataMessage()$
        +CreateSelectRequest()$
        +CreateLinktestRequest()$
    }
    
    HsmsMessage --> HsmsHeader
    HsmsMessage --> SecsMessage
    SecsMessage --> SecsItem
    SecsItem --> SecsItem : contains
```

## 3. Core层 - 枚举类图

```mermaid
classDiagram
    direction TB
    
    class SecsFormat {
        <<enumeration>>
        List = 0x00
        Binary = 0x08
        Boolean = 0x09
        ASCII = 0x10
        JIS8 = 0x11
        Unicode = 0x12
        I1 = 0x19
        I2 = 0x1A
        I4 = 0x1C
        I8 = 0x18
        U1 = 0x29
        U2 = 0x2A
        U4 = 0x2C
        U8 = 0x28
        F4 = 0x24
        F8 = 0x20
    }
    
    class HsmsMessageType {
        <<enumeration>>
        DataMessage = 0
        SelectRequest = 1
        SelectResponse = 2
        DeselectRequest = 3
        DeselectResponse = 4
        LinktestRequest = 5
        LinktestResponse = 6
        RejectRequest = 7
        SeparateRequest = 9
    }
    
    class ConnectionState {
        <<enumeration>>
        NotConnected
        Connecting
        Connected
        Selected
        Disconnecting
    }
    
    class GemCommunicationState {
        <<enumeration>>
        Disabled
        Enabled
        WaitCommunicationRequest
        WaitCommunicationDelay
        Communicating
    }
    
    class GemControlState {
        <<enumeration>>
        EquipmentOffline
        AttemptOnline
        HostOffline
        OnlineLocal
        OnlineRemote
    }
    
    class GemProcessingState {
        <<enumeration>>
        Idle
        Setup
        Ready
        Executing
        Paused
    }
```

## 4. Domain层 - 模型类图

```mermaid
classDiagram
    direction TB
    
    class StatusVariable {
        +uint VariableId
        +string Name
        +string Units
        +SecsFormat Format
        +object Value
        +Func~object~ ValueGetter
        +GetValue()
        +SetValue()
    }
    
    class EquipmentConstant {
        +uint ConstantId
        +string Name
        +string Units
        +SecsFormat Format
        +object Value
        +object DefaultValue
        +object MinValue
        +object MaxValue
        +bool IsReadOnly
        +GetValue()
        +TrySetValue()
    }
    
    class CollectionEvent {
        +uint EventId
        +string Name
        +string Description
        +bool IsEnabled
        +List~uint~ LinkedReportIds
    }
    
    class ReportDefinition {
        +uint ReportId
        +string Name
        +List~uint~ VariableIds
    }
    
    class AlarmInfo {
        +uint AlarmId
        +string AlarmText
        +bool IsSet
        +AlarmCategory Category
        +byte AlarmCode
        +DateTime Timestamp
    }
    
    class AlarmDefinition {
        +uint AlarmId
        +string Name
        +string Description
        +AlarmCategory Category
        +bool IsEnabled
        +uint AssociatedEventId
    }
    
    class RemoteCommandDefinition {
        +string CommandName
        +string Description
        +List~CommandParameter~ Parameters
        +Func Handler
    }
    
    class RemoteCommandResult {
        +RemoteCommandAck AckCode
        +List~ParameterAck~ ParameterAcks
        +Success()$
        +Fail()$
    }
    
    CollectionEvent --> ReportDefinition : links
    AlarmDefinition --> CollectionEvent : triggers
    RemoteCommandDefinition --> RemoteCommandResult : returns
```

## 5. Application层 - 消息处理器类图

```mermaid
classDiagram
    direction TB
    
    class MessageHandlerBase {
        <<abstract>>
        +int Priority
        #byte Stream
        #byte Function
        +CanHandle()
        +HandleAsync()
        #HandleCoreAsync()*
        #CreateErrorResponse()
    }
    
    class S1F1Handler {
        #byte Stream = 1
        #byte Function = 1
        #HandleCoreAsync()
    }
    
    class S1F13Handler {
        #byte Stream = 1
        #byte Function = 13
        #HandleCoreAsync()
    }
    
    class S1F15Handler {
        #byte Stream = 1
        #byte Function = 15
        #HandleCoreAsync()
    }
    
    class S1F17Handler {
        #byte Stream = 1
        #byte Function = 17
        #HandleCoreAsync()
    }
    
    class S2F13Handler {
        #byte Stream = 2
        #byte Function = 13
        #HandleCoreAsync()
    }
    
    class S2F15Handler {
        #byte Stream = 2
        #byte Function = 15
        #HandleCoreAsync()
    }
    
    class S2F41Handler {
        -Dictionary _commands
        #byte Stream = 2
        #byte Function = 41
        +RegisterCommand()
        #HandleCoreAsync()
    }
    
    class S5F3Handler {
        #byte Stream = 5
        #byte Function = 3
        #HandleCoreAsync()
    }
    
    class S7F3Handler {
        #byte Stream = 7
        #byte Function = 3
        #HandleCoreAsync()
    }
    
    MessageHandlerBase ..|> IMessageHandler
    S1F1Handler --|> MessageHandlerBase
    S1F13Handler --|> MessageHandlerBase
    S1F15Handler --|> MessageHandlerBase
    S1F17Handler --|> MessageHandlerBase
    S2F13Handler --|> MessageHandlerBase
    S2F15Handler --|> MessageHandlerBase
    S2F41Handler --|> MessageHandlerBase
    S5F3Handler --|> MessageHandlerBase
    S7F3Handler --|> MessageHandlerBase
```

## 6. Domain层 - 事件类图

```mermaid
classDiagram
    direction TB
    
    class IGemEvent {
        <<interface>>
        +string Source
        +DateTime Timestamp
        +string EventId
    }
    
    class GemEventBase {
        <<abstract>>
        +string Source
        +DateTime Timestamp
        +string EventId
    }
    
    class AlarmEvent {
        +uint AlarmId
        +byte AlarmCode
        +string AlarmText
        +bool IsSet
        +AlarmCategory Category
    }
    
    class StateChangedEvent {
        +StateType StateType
        +object OldState
        +object NewState
        +string Reason
    }
    
    class MessageReceivedEvent {
        +SecsMessage Message
        +MessageDirection Direction
        +uint SystemBytes
        +string RemoteEndpoint
    }
    
    class CollectionEventTriggeredEvent {
        +uint DataId
        +uint CollectionEventId
        +string EventName
        +IReadOnlyList~ReportData~ Reports
    }
    
    GemEventBase ..|> IGemEvent
    AlarmEvent --|> GemEventBase
    StateChangedEvent --|> GemEventBase
    MessageReceivedEvent --|> GemEventBase
    CollectionEventTriggeredEvent --|> GemEventBase
```

## 7. 异常类图

```mermaid
classDiagram
    direction TB
    
    class SecsException {
        +string Code
        +SecsException(message, code)
        +SecsException(message, code, inner)
    }
    
    class SecsCommunicationException {
        +ConnectionFailed()$
        +NotConnected()$
        +NotSelected()$
        +SendFailed()$
    }
    
    class SecsTimeoutException {
        +TimeSpan Elapsed
        +string TimeoutType
        +T3Timeout()$
        +T6Timeout()$
    }
    
    class SecsFormatException {
        +InvalidFormatCode()$
        +InvalidHeader()$
        +IncompleteData()$
    }
    
    class SecsStateException {
        +InvalidState()$
        +InvalidTransition()$
    }
    
    class SecsTransactionException {
        +TransactionNotFound()$
        +DuplicateTransaction()$
        +TransactionCancelled()$
    }
    
    SecsException --|> Exception
    SecsCommunicationException --|> SecsException
    SecsTimeoutException --|> SecsException
    SecsFormatException --|> SecsException
    SecsStateException --|> SecsException
    SecsTransactionException --|> SecsException
```

## 8. 配置类图

```mermaid
classDiagram
    direction LR
    
    class GemConfiguration {
        +HsmsConfiguration Hsms
        +string ModelName
        +string SoftwareRevision
        +GemControlState InitialControlState
        +bool AutoOnline
        +bool InitialRemoteMode
    }
    
    class HsmsConfiguration {
        +ushort DeviceId
        +string IpAddress
        +int Port
        +HsmsConnectionMode Mode
        +int T3
        +int T5
        +int T6
        +int T7
        +int T8
        +int LinktestInterval
        +int MaxMessageSize
        +TimeSpan T3Timeout
        +Validate()
        +CreatePassive()$
        +CreateActive()$
    }
    
    class HsmsConnectionMode {
        <<enumeration>>
        Passive
        Active
    }
    
    GemConfiguration --> HsmsConfiguration
    HsmsConfiguration --> HsmsConnectionMode
```

## 9. 层次依赖关系图

```mermaid
graph TB
    subgraph Application["Application Layer"]
        GemEquipmentService
        GemStateManager
        MessageDispatcher
        Handlers["Message Handlers"]
    end
    
    subgraph Infrastructure["Infrastructure Layer"]
        HsmsConnection
        SecsSerializer
        TransactionManager
        EventAggregator
        Configuration
    end
    
    subgraph Domain["Domain Layer"]
        Interfaces["Interfaces"]
        Events["Events"]
        Models["Models"]
    end
    
    subgraph Core["Core Layer"]
        Entities["Entities"]
        Enums["Enums"]
        Exceptions["Exceptions"]
    end
    
    Application --> Infrastructure
    Application --> Domain
    Infrastructure --> Domain
    Infrastructure --> Core
    Domain --> Core
```

## 10. 消息处理流程图

```mermaid
sequenceDiagram
    participant Host
    participant HsmsConnection
    participant MessageDispatcher
    participant Handler as IMessageHandler
    participant GemState as GemStateManager
    
    Host->>HsmsConnection: Primary Message (SxFy)
    HsmsConnection->>HsmsConnection: Deserialize
    HsmsConnection->>MessageDispatcher: DispatchAsync(message, context)
    
    loop Find Handler
        MessageDispatcher->>Handler: CanHandle(message)
        Handler-->>MessageDispatcher: true/false
    end
    
    MessageDispatcher->>Handler: HandleAsync(message, context)
    Handler->>GemState: GetStatusVariable() / SetControlState()
    GemState-->>Handler: result
    Handler-->>MessageDispatcher: Response Message
    MessageDispatcher-->>HsmsConnection: Response
    HsmsConnection->>HsmsConnection: Serialize
    HsmsConnection-->>Host: Secondary Message (SxFy+1)
```
