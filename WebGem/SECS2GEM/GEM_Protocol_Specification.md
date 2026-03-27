# GEM协议规范文档

## 目录
1. [概述](#1-概述)
2. [协议栈结构](#2-协议栈结构)
3. [HSMS通信协议](#3-hsms通信协议)
4. [SECS-II消息格式](#4-secs-ii消息格式)
5. [数据项类型](#5-数据项类型)
6. [GEM状态模型](#6-gem状态模型)
7. [核心消息流程](#7-核心消息流程)
8. [常用Stream/Function定义](#8-常用streamfunction定义)
9. [实现要点](#9-实现要点)

---

## 1. 概述

### 1.1 什么是GEM协议

GEM (Generic Equipment Model) 是SEMI E30标准定义的通用设备模型，用于半导体制造设备与主机（Host）之间的通信。GEM协议建立在以下标准之上：

| 标准 | 名称 | 描述 |
|------|------|------|
| SEMI E4 | SECS-I | 串行通信物理层（已较少使用） |
| SEMI E5 | SECS-II | 消息内容和结构定义 |
| SEMI E30 | GEM | 通用设备模型和行为 |
| SEMI E37 | HSMS | 高速消息服务（TCP/IP通信） |

### 1.2 通信角色

```
┌─────────────────┐                    ┌─────────────────┐
│                 │                    │                 │
│      Host       │◄──── HSMS/TCP ────►│    Equipment    │
│   (主机/MES)    │                    │   (设备/EAP)    │
│                 │                    │                 │
└─────────────────┘                    └─────────────────┘
      主动方                                  被动方
    (Active)                               (Passive)
```

- **Host（主机）**: 通常是MES系统或工厂自动化系统，发起连接
- **Equipment（设备）**: 半导体制造设备，等待连接

---

## 2. 协议栈结构

```
┌─────────────────────────────────────┐
│            GEM (E30)                │  ← 应用层：设备行为模型
├─────────────────────────────────────┤
│          SECS-II (E5)               │  ← 表示层：消息格式定义
├─────────────────────────────────────┤
│           HSMS (E37)                │  ← 会话层：消息传输控制
├─────────────────────────────────────┤
│            TCP/IP                   │  ← 传输层：网络通信
└─────────────────────────────────────┘
```

---

## 3. HSMS通信协议

### 3.1 HSMS消息结构

HSMS消息由消息头（Header）和消息体（Message Body）组成：

```
┌────────────────────────────────────────────────────────────┐
│                      HSMS Message                          │
├──────────────┬─────────────────────────────────────────────┤
│ Message      │              Message Body                   │
│ Length       │  (SECS-II Message Data)                     │
│ (4 bytes)    │                                             │
├──────────────┼──────────────────────────────────────────────┤
│              │ ┌─────────────────────────────────────────┐ │
│              │ │         Message Header (10 bytes)       │ │
│              │ ├─────────────────────────────────────────┤ │
│              │ │         Message Text (Variable)         │ │
│              │ └─────────────────────────────────────────┘ │
└──────────────┴──────────────────────────────────────────────┘
```

### 3.2 消息长度字段 (4 Bytes)

```
Byte 0-3: Message Length (Big-Endian)
          表示后续消息体的总字节数（Header + Text）
          最小值：10（仅有Header）
          最大值：由系统配置决定
```

### 3.3 消息头结构 (10 Bytes)

```
┌─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
│ Byte 0  │ Byte 1  │ Byte 2  │ Byte 3  │ Byte 4  │ Byte 5  │ Byte 6  │ Byte 7  │ Byte 8  │ Byte 9  │
├─────────┴─────────┼─────────┴─────────┼─────────┼─────────┼─────────┴─────────┴─────────┴─────────┤
│   Session ID      │   Header Byte 2-3 │  PType  │  SType  │           System Bytes                │
│   (Device ID)     │  Stream/Function  │         │         │     (Transaction ID)                  │
└───────────────────┴───────────────────┴─────────┴─────────┴───────────────────────────────────────┘
```

#### 3.3.1 字段详解

| 字段 | 字节位置 | 描述 |
|------|----------|------|
| Session ID | 0-1 | 会话标识符，通常为设备ID（高位字节在前） |
| Header Byte 2 | 2 | bit7: W-Bit (等待回复标志), bit0-6: Stream号 |
| Header Byte 3 | 3 | Function号 |
| PType | 4 | 表示类型，SECS-II消息固定为0x00 |
| SType | 5 | 会话类型，数据消息为0x00 |
| System Bytes | 6-9 | 事务ID，用于匹配请求和响应 |

#### 3.3.2 W-Bit（等待位）

```
Header Byte 2 的最高位 (bit 7):
  1 = 期望回复（Primary Message）
  0 = 不期望回复（Secondary Message 或 无需回复的消息）
```

#### 3.3.3 SType（会话类型）定义

| SType | 值 | 描述 |
|-------|-----|------|
| Data Message | 0 | SECS-II数据消息 |
| Select.req | 1 | 选择请求（建立连接） |
| Select.rsp | 2 | 选择响应 |
| Deselect.req | 3 | 取消选择请求 |
| Deselect.rsp | 4 | 取消选择响应 |
| Linktest.req | 5 | 链路测试请求 |
| Linktest.rsp | 6 | 链路测试响应 |
| Reject.req | 7 | 拒绝请求 |
| Separate.req | 9 | 分离请求（断开连接） |

### 3.4 HSMS连接状态机

```
                              ┌─────────────┐
                              │   NOT      │
                              │ CONNECTED  │
                              └──────┬──────┘
                                     │ TCP Connected
                                     ▼
                              ┌─────────────┐
              ┌───────────────│  CONNECTED  │───────────────┐
              │               │ NOT SELECTED│               │
              │               └──────┬──────┘               │
              │                      │                      │
    Separate.req                     │ Select.req/rsp       │ T7 Timeout
              │                      ▼                      │
              │               ┌─────────────┐               │
              └───────────────│  SELECTED   │───────────────┘
                              │             │
                              └─────────────┘
                                     │
                              Data Messages
                              (SECS-II)
```

### 3.5 HSMS超时参数

| 参数 | 名称 | 描述 | 典型值 |
|------|------|------|--------|
| T3 | Reply Timeout | 等待回复超时 | 45秒 |
| T5 | Connection Separation Timeout | 连接分离超时 | 10秒 |
| T6 | Control Transaction Timeout | 控制事务超时 | 5秒 |
| T7 | Not Selected Timeout | 未选择超时 | 10秒 |
| T8 | Network Intercharacter Timeout | 网络字符间隔超时 | 5秒 |

### 3.6 HSMS连接流程

```
    Host (Active)                           Equipment (Passive)
         │                                         │
         │         TCP Connection Request          │
         │────────────────────────────────────────►│
         │         TCP Connection Accept           │
         │◄────────────────────────────────────────│
         │                                         │
         │         Select.req (SType=1)            │
         │────────────────────────────────────────►│
         │         Select.rsp (SType=2)            │
         │◄────────────────────────────────────────│
         │                                         │
         │      ═══════ SELECTED STATE ═══════     │
         │                                         │
         │     Data Messages (SECS-II, SType=0)    │
         │◄───────────────────────────────────────►│
         │                                         │
         │         Linktest.req (SType=5)          │
         │────────────────────────────────────────►│
         │         Linktest.rsp (SType=6)          │
         │◄────────────────────────────────────────│
         │                                         │
```

---

## 4. SECS-II消息格式

### 4.1 Stream和Function概念

SECS-II消息使用Stream（流）和Function（功能）来标识消息类型：

- **Stream (S)**: 消息类别，范围1-127
- **Function (F)**: 具体功能，奇数为Primary（请求），偶数为Secondary（响应）

常用Stream定义：

| Stream | 描述 |
|--------|------|
| S1 | 设备状态（Equipment Status） |
| S2 | 设备控制（Equipment Control） |
| S3 | 物料状态（Material Status） |
| S5 | 异常处理（Exception Handling） |
| S6 | 数据采集（Data Collection） |
| S7 | 配方管理（Process Program Management） |
| S9 | 系统错误（System Errors） |
| S10 | 终端服务（Terminal Services） |
| S14 | 对象服务（Object Services） |

### 4.2 消息文本结构

SECS-II消息文本由数据项（Item）组成，采用TLV（Type-Length-Value）格式：

```
┌─────────────────────────────────────────────────────────────┐
│                    SECS-II Data Item                        │
├──────────────────┬──────────────────┬───────────────────────┤
│   Format Code    │     Length       │        Data           │
│    (1 byte)      │   (1-3 bytes)    │     (Variable)        │
└──────────────────┴──────────────────┴───────────────────────┘
```

#### 4.2.1 格式码（Format Code）定义

```
Format Code 结构（1字节）:
  ┌───┬───┬───┬───┬───┬───┬───┬───┐
  │ b7│ b6│ b5│ b4│ b3│ b2│ b1│ b0│
  └───┴───┴───┴───┴───┴───┴───┴───┘
    │   │   │   │   │   │   └───┴───── Number of Length Bytes (低2位, 值0-3)
    └───┴───┴───┴───┴───┴───────────── Format Type (高6位)
```

#### 4.2.2 数据格式类型

| Format | 二进制码 | 描述 | C#类型 |
|--------|----------|------|--------|
| L (List) | 000000 | 列表（容器） | List<SecsItem> |
| B (Binary) | 001000 | 二进制数据 | byte[] |
| BOOLEAN | 001001 | 布尔值 | bool |
| A (ASCII) | 010000 | ASCII字符串 | string |
| J (JIS-8) | 010001 | JIS-8字符串 | string |
| I8 | 011000 | 8字节有符号整数 | long |
| I1 | 011001 | 1字节有符号整数 | sbyte |
| I2 | 011010 | 2字节有符号整数 | short |
| I4 | 011100 | 4字节有符号整数 | int |
| F8 | 100000 | 8字节浮点数 | double |
| F4 | 100100 | 4字节浮点数 | float |
| U8 | 101000 | 8字节无符号整数 | ulong |
| U1 | 101001 | 1字节无符号整数 | byte |
| U2 | 101010 | 2字节无符号整数 | ushort |
| U4 | 101100 | 4字节无符号整数 | uint |

#### 4.2.3 长度字段

长度字节数由格式码的低2位决定：

| 低2位值 | 长度字节数 | 最大数据长度 |
|---------|------------|--------------|
| 01 | 1 | 255 bytes (2^8 - 1) |
| 10 | 2 | 65,535 bytes (2^16 - 1) |
| 11 | 3 | 16,777,215 bytes (2^24 - 1) |

> 注意：低2位值为 00 表示长度为0（空数据项），此时没有长度字节和数据字节。

#### 4.2.4 长度字节计算详解

**读取规则**：长度字节采用**大端序（Big-Endian）**，高位字节在前，低位字节在后。

```
数据项结构：
┌────────────┐┌────────────────────┐┌────────────────────┐
│ Format Byte  ││   Length Bytes     ││     Data Bytes     │
│   (1字节)    ││   (1-3字节)        ││   (可变长度)        │
└────────────┘└────────────────────┘└────────────────────┘
      │                   │
      │                   └─── 长度值 = 实际数据的字节数
      │
      └─── 高6位: 数据类型
           低2位: 长度字节数
```

**计算公式**：

```
1字节长度 (NumLenBytes=1):
  Length = Byte[0]
  范围: 0 ~ 255

2字节长度 (NumLenBytes=2):
  Length = (Byte[0] << 8) | Byte[1]
  范围: 0 ~ 65,535

3字节长度 (NumLenBytes=3):
  Length = (Byte[0] << 16) | (Byte[1] << 8) | Byte[2]
  范围: 0 ~ 16,777,215
```

**示例1：1字节长度**

```
原始字节: 41 05 48 45 4C 4C 4F

解析:
  41 = 0100 0001
       ││││ ││││
       ││││ └┴─── NumLenBytes = 01 (1字节)
       └┴┴┴─────── Format = 010000 (ASCII)

  05 = 长度字节
       Length = 5

  48 45 4C 4C 4F = "HELLO" (5字节 ASCII)

结果: A "HELLO"
```

**示例2：2字节长度**

```
原始字节: 42 01 00 [256字节的数据...]

解析:
  42 = 0100 0010
       ││││ ││││
       ││││ └┴─── NumLenBytes = 10 (2字节)
       └┴┴┴─────── Format = 010000 (ASCII)

  01 00 = 长度字节 (大端序)
          Length = (0x01 << 8) | 0x00 = 256

结果: A (包含256字节的ASCII字符串)
```

**示例3：3字节长度**

```
原始字节: 43 01 86 A0 [数据...]

解析:
  43 = 0100 0011
       ││││ ││││
       ││││ └┴─── NumLenBytes = 11 (3字节)
       └┴┴┴─────── Format = 010000 (ASCII)

  01 86 A0 = 长度字节 (大端序)
             Length = (0x01 << 16) | (0x86 << 8) | 0xA0
                    = 65536 + 34304 + 160
                    = 100,000

结果: A (包含100,000字节的ASCII字符串)
```

**示例4：List类型的长度含义**

```
对于List类型，长度表示的是子项的数量，而非字节数！

原始字节: 01 03

解析:
  01 = 0000 0001
       ││││ ││││
       ││││ └┴─── NumLenBytes = 01 (1字节)
       └┴┴┴─────── Format = 000000 (List)

  03 = 长度字节
       Length = 3 (表示包含3个子项)

结果: L,3 (包含3个子项的列表)
```

**长度字节数选择策略**：

```csharp
/// <summary>
/// 根据数据长度确定需要的长度字节数
/// </summary>
public static int GetNumLengthBytes(int dataLength)
{
    if (dataLength == 0)
        return 0;  // 空数据，低2位 = 00
    if (dataLength <= 255)
        return 1;  // 低2位 = 01
    if (dataLength <= 65535)
        return 2;  // 低2位 = 10
    if (dataLength <= 16777215)
        return 3;  // 低2位 = 11
    
    throw new ArgumentException("数据长度超出最大限制");
}
```

### 4.3 消息示例解析

#### 示例：S1F1 Are You There Request

```
原始字节: 00 00 00 0A 00 01 81 01 00 00 00 00 00 01

解析:
┌─────────────────────────────────────────────────────────────┐
│ Message Length: 00 00 00 0A (10 bytes)                      │
├─────────────────────────────────────────────────────────────┤
│ Header:                                                      │
│   Session ID:    00 01 (Device ID = 1)                      │
│   Header Byte 2: 81 (W-Bit=1, Stream=1)                     │
│   Header Byte 3: 01 (Function=1)                            │
│   PType:         00                                          │
│   SType:         00 (Data Message)                          │
│   System Bytes:  00 00 00 01 (Transaction ID = 1)           │
├─────────────────────────────────────────────────────────────┤
│ Message Text: (empty - no data items)                       │
└─────────────────────────────────────────────────────────────┘
```

#### 示例：S1F2 On Line Data

```
原始字节: 00 00 00 1E 00 01 01 02 00 00 00 00 00 01 
          01 02 41 06 45 51 55 49 50 31 41 08 53 4F 46 54 56 45 52 31

解析:
┌─────────────────────────────────────────────────────────────┐
│ Header: S1F2, Device=1, TxID=1                              │
├─────────────────────────────────────────────────────────────┤
│ Message Text:                                                │
│   L,2 (List with 2 items)                                   │
│     ├─ A "EQUIP1" (MDLN - Model Name)                       │
│     └─ A "SOFTVER1" (SOFTREV - Software Revision)           │
└─────────────────────────────────────────────────────────────┘

详细解析:
  01 02       → L,2 (Format=0x00, NumLenBytes=1, Length=2)
  41 06       → A,6 (Format=0x10, NumLenBytes=1, Length=6)
  45 51 55 49 50 31 → "EQUIP1" (ASCII)
  41 08       → A,8 (Format=0x10, NumLenBytes=1, Length=8)
  53 4F 46 54 56 45 52 31 → "SOFTVER1" (ASCII)
```

---

## 5. 数据项类型

### 5.1 基本数据类型编码

```csharp
/// <summary>
/// SECS-II数据格式定义
/// </summary>
public enum SecsFormat : byte
{
    /// <summary>列表类型</summary>
    List = 0x00,      // 000000 00
    
    /// <summary>二进制数据</summary>
    Binary = 0x20,    // 001000 00
    
    /// <summary>布尔类型</summary>
    Boolean = 0x24,   // 001001 00
    
    /// <summary>ASCII字符串</summary>
    ASCII = 0x40,     // 010000 00
    
    /// <summary>JIS-8字符串</summary>
    JIS8 = 0x44,      // 010001 00
    
    /// <summary>8字节有符号整数</summary>
    I8 = 0x60,        // 011000 00
    
    /// <summary>1字节有符号整数</summary>
    I1 = 0x64,        // 011001 00
    
    /// <summary>2字节有符号整数</summary>
    I2 = 0x68,        // 011010 00
    
    /// <summary>4字节有符号整数</summary>
    I4 = 0x70,        // 011100 00
    
    /// <summary>8字节浮点数</summary>
    F8 = 0x80,        // 100000 00
    
    /// <summary>4字节浮点数</summary>
    F4 = 0x90,        // 100100 00
    
    /// <summary>8字节无符号整数</summary>
    U8 = 0xA0,        // 101000 00
    
    /// <summary>1字节无符号整数</summary>
    U1 = 0xA4,        // 101001 00
    
    /// <summary>2字节无符号整数</summary>
    U2 = 0xA8,        // 101010 00
    
    /// <summary>4字节无符号整数</summary>
    U4 = 0xB0,        // 101100 00
}
```

### 5.2 数据项编码规则

```
编码流程:
1. 确定数据类型对应的Format Code
2. 计算数据长度，确定需要的长度字节数
3. 组合Format Code和长度字节数
4. 写入长度值（大端序）
5. 写入数据值（大端序）
```

示例 - 编码整数值 12345 (U2类型):

```
数据: 12345 (0x3039)
Format Code: U2 = 0xA8 (101010 00)
长度字节数: 1 (数据长度=2 < 256)
最终Format Byte: 0xA8 | 0x01 = 0xA9

编码结果: A9 02 30 39
          │  │  └───── 数据值 (大端序)
          │  └──────── 长度 = 2
          └─────────── Format Code + 长度字节数
```

---

## 6. GEM状态模型

### 6.1 通信状态模型

```
                    ┌─────────────────────────┐
                    │    COMMUNICATING        │
                    │      通信中              │
        ┌───────────┤                         ├───────────┐
        │           └───────────┬─────────────┘           │
        │                       │                         │
        │ S1F13                 │ S1F17                   │ 通信失败
        │ Establish             │ Request                 │
        │ Communication         │ ON-LINE                 │
        │                       │                         │
        ▼                       ▼                         ▼
┌───────────────┐       ┌───────────────┐       ┌───────────────┐
│   DISABLED    │       │   ENABLED     │       │    ENABLED    │
│               │◄──────│    Local      │◄─────►│    Remote     │
│   通信禁用    │       │   本地控制    │       │   远程控制     │
└───────────────┘       └───────────────┘       └───────────────┘
                              ▲                       ▲
                              │      S1F15/S1F17      │
                              └───────────────────────┘
```

### 6.2 控制状态模型

```
                         ┌─────────────┐
                         │  OFF-LINE   │
                         │   离线      │
                         └──────┬──────┘
                                │
                    ┌───────────┼───────────┐
                    │           │           │
                    ▼           ▼           ▼
            ┌───────────┐ ┌───────────┐ ┌───────────┐
            │ Equipment │ │   Host    │ │  Attempt  │
            │ Off-line  │ │ Off-line  │ │  ON-LINE  │
            │ 设备离线  │ │ 主机离线  │ │ 尝试上线   │
            └───────────┘ └───────────┘ └─────┬─────┘
                                              │
                         ┌────────────────────┘
                         │
                         ▼
                    ┌─────────────┐
                    │   ON-LINE   │
                    │    在线     │
                    └──────┬──────┘
                           │
                    ┌──────┴──────┐
                    ▼             ▼
            ┌───────────┐ ┌───────────┐
            │   Local   │ │  Remote   │
            │  本地控制 │ │  远程控制 │
            └───────────┘ └───────────┘
```

### 6.3 处理状态模型

```
┌─────────────────────────────────────────────────────────────────┐
│                      Processing State                           │
├───────────────────────┬────────────────────┬────────────────────┤
│       IDLE            │    EXECUTING       │   PAUSED           │
│       空闲            │      执行中        │    暂停            │
├───────────────────────┼────────────────────┼────────────────────┤
│  等待工作指令         │  正在处理工件      │  处理已暂停        │
│  设备准备就绪         │  配方正在执行      │  等待恢复指令      │
└───────────────────────┴────────────────────┴────────────────────┘
```

---

## 7. 核心消息流程

### 7.1 建立通信流程

```
    Host                                    Equipment
      │                                         │
      │         TCP Connection                  │
      │────────────────────────────────────────►│
      │                                         │
      │         Select.req (SType=1)            │
      │────────────────────────────────────────►│
      │         Select.rsp (SType=2)            │
      │◄────────────────────────────────────────│
      │                                         │
      │         S1F13 Establish Communication   │
      │────────────────────────────────────────►│
      │         S1F14 Establish Ack             │
      │◄────────────────────────────────────────│
      │                                         │
      │      ═══════ Communication Enabled ════ │
      │                                         │
```

### 7.2 状态查询流程

```
    Host                                    Equipment
      │                                         │
      │         S1F1 Are You There              │
      │────────────────────────────────────────►│
      │         S1F2 On Line Data               │
      │◄────────────────────────────────────────│
      │                                         │
      │         S1F3 Selected Equipment Status  │
      │────────────────────────────────────────►│
      │         S1F4 Selected Equipment Status  │
      │◄────────────────────────────────────────│
      │                                         │
```

### 7.3 报警处理流程

```
    Host                                    Equipment
      │                                         │
      │                                         │ (Alarm Occurred)
      │         S5F1 Alarm Report               │
      │◄────────────────────────────────────────│
      │         S5F2 Alarm Ack                  │
      │────────────────────────────────────────►│
      │                                         │
      │         S5F3 Enable/Disable Alarm       │
      │────────────────────────────────────────►│
      │         S5F4 Enable/Disable Ack         │
      │◄────────────────────────────────────────│
      │                                         │
```

### 7.4 事件报告流程

```
    Host                                    Equipment
      │                                         │
      │         S2F33 Define Report             │
      │────────────────────────────────────────►│
      │         S2F34 Define Report Ack         │
      │◄────────────────────────────────────────│
      │                                         │
      │         S2F35 Link Event Report         │
      │────────────────────────────────────────►│
      │         S2F36 Link Event Report Ack     │
      │◄────────────────────────────────────────│
      │                                         │
      │         S2F37 Enable/Disable Event      │
      │────────────────────────────────────────►│
      │         S2F38 Enable/Disable Event Ack  │
      │◄────────────────────────────────────────│
      │                                         │
      │                                         │ (Event Triggered)
      │         S6F11 Event Report              │
      │◄────────────────────────────────────────│
      │         S6F12 Event Report Ack          │
      │────────────────────────────────────────►│
      │                                         │
```

### 7.5 配方管理流程

```
    Host                                    Equipment
      │                                         │
      │         S7F1 Process Program Load       │
      │────────────────────────────────────────►│
      │         S7F2 Process Program Load Ack   │
      │◄────────────────────────────────────────│
      │                                         │
      │         S7F3 Process Program Send       │
      │────────────────────────────────────────►│
      │         S7F4 Process Program Send Ack   │
      │◄────────────────────────────────────────│
      │                                         │
      │         S7F5 Process Program Request    │
      │────────────────────────────────────────►│
      │         S7F6 Process Program Data       │
      │◄────────────────────────────────────────│
      │                                         │
      │         S7F17 Delete Process Program    │
      │────────────────────────────────────────►│
      │         S7F18 Delete Process Program Ack│
      │◄────────────────────────────────────────│
      │                                         │
```

### 7.6 远程命令流程

```
    Host                                    Equipment
      │                                         │
      │         S2F41 Host Command Send         │
      │────────────────────────────────────────►│
      │         S2F42 Host Command Ack          │
      │◄────────────────────────────────────────│
      │                                         │
      │         S2F49 Enhanced Remote Command   │
      │────────────────────────────────────────►│
      │         S2F50 Enhanced Remote Cmd Ack   │
      │◄────────────────────────────────────────│
      │                                         │
```

---

## 8. 常用Stream/Function定义

### 8.1 Stream 1 - 设备状态

| S/F | W-Bit | 名称 | 方向 | 描述 |
|-----|-------|------|------|------|
| S1F1 | W | Are You There | H→E | 设备存在查询 |
| S1F2 | - | On Line Data | E→H | 设备在线数据响应 |
| S1F3 | W | Selected Equipment Status Request | H→E | 状态变量查询 |
| S1F4 | - | Selected Equipment Status | E→H | 状态变量响应 |
| S1F11 | W | Status Variable Namelist Request | H→E | 状态变量名称查询 |
| S1F12 | - | Status Variable Namelist | E→H | 状态变量名称响应 |
| S1F13 | W | Establish Communications Request | H↔E | 建立通信请求 |
| S1F14 | - | Establish Communications Ack | E↔H | 建立通信确认 |
| S1F15 | W | Request OFF-LINE | H→E | 请求离线 |
| S1F16 | - | OFF-LINE Acknowledge | E→H | 离线确认 |
| S1F17 | W | Request ON-LINE | H→E | 请求上线 |
| S1F18 | - | ON-LINE Acknowledge | E→H | 上线确认 |

#### S1F1/S1F2 消息格式

```
S1F1 Are You There Request
───────────────────────────
Header Only (no data items)

S1F2 On Line Data
─────────────────
L,2
  <MDLN A>      设备型号
  <SOFTREV A>   软件版本
```

#### S1F13/S1F14 消息格式

```
S1F13 Establish Communications Request
──────────────────────────────────────
L,2
  <MDLN A>      设备型号（可为空）
  <SOFTREV A>   软件版本（可为空）

S1F14 Establish Communications Acknowledge
──────────────────────────────────────────
L,2
  <COMMACK B:1> 通信确认码
                  0 = 接受
                  1 = 拒绝，已通信
                  2 = 拒绝，设备离线
  L,2
    <MDLN A>    设备型号
    <SOFTREV A> 软件版本
```

### 8.2 Stream 2 - 设备控制

| S/F | W-Bit | 名称 | 方向 | 描述 |
|-----|-------|------|------|------|
| S2F13 | W | Equipment Constant Request | H→E | 设备常量查询 |
| S2F14 | - | Equipment Constant Data | E→H | 设备常量响应 |
| S2F15 | W | New Equipment Constant Send | H→E | 设置设备常量 |
| S2F16 | - | New Equipment Constant Ack | E→H | 设置确认 |
| S2F17 | W | Date and Time Request | H→E | 日期时间查询 |
| S2F18 | - | Date and Time Data | E→H | 日期时间响应 |
| S2F31 | W | Date and Time Set Request | H→E | 设置日期时间 |
| S2F32 | - | Date and Time Set Ack | E→H | 设置确认 |
| S2F33 | W | Define Report | H→E | 定义报告 |
| S2F34 | - | Define Report Acknowledge | E→H | 定义报告确认 |
| S2F35 | W | Link Event Report | H→E | 关联事件报告 |
| S2F36 | - | Link Event Report Acknowledge | E→H | 关联事件报告确认 |
| S2F37 | W | Enable/Disable Event Report | H→E | 启用/禁用事件报告 |
| S2F38 | - | Enable/Disable Event Report Ack | E→H | 启用/禁用确认 |
| S2F41 | W | Host Command Send | H→E | 远程命令 |
| S2F42 | - | Host Command Acknowledge | E→H | 远程命令确认 |

#### S2F41/S2F42 消息格式

```
S2F41 Host Command Send
───────────────────────
L,2
  <RCMD A>      远程命令名称
  L,n           参数列表
    L,2
      <CPNAME A>  参数名
      <CPVAL A>   参数值
    ...

S2F42 Host Command Acknowledge
──────────────────────────────
L,2
  <HCACK B:1>   命令确认码
                  0 = 确认，命令已执行
                  1 = 参数名无效
                  2 = 至少一个参数值无效
                  3 = 无法执行
                  4 = 命令名无效
                  5 = 已被拒绝
  L,n           参数确认列表
    L,2
      <CPNAME A>  参数名
      <CPACK B:1> 参数确认码
    ...
```

### 8.3 Stream 5 - 异常处理

| S/F | W-Bit | 名称 | 方向 | 描述 |
|-----|-------|------|------|------|
| S5F1 | W | Alarm Report Send | E→H | 报警报告 |
| S5F2 | - | Alarm Report Acknowledge | H→E | 报警确认 |
| S5F3 | W | Enable/Disable Alarm Send | H→E | 启用/禁用报警 |
| S5F4 | - | Enable/Disable Alarm Acknowledge | E→H | 启用/禁用确认 |
| S5F5 | W | List Alarms Request | H→E | 报警列表查询 |
| S5F6 | - | List Alarm Data | E→H | 报警列表响应 |
| S5F7 | W | List Enabled Alarm Request | H→E | 已启用报警查询 |
| S5F8 | - | List Enabled Alarm Data | E→H | 已启用报警响应 |

#### S5F1/S5F2 消息格式

```
S5F1 Alarm Report Send
──────────────────────
L,3
  <ALCD B:1>    报警码
                  bit 7: 1=Set, 0=Clear
                  bit 0-6: 报警类别
  <ALID U4:1>   报警ID
  <ALTX A>      报警文本

S5F2 Alarm Report Acknowledge
─────────────────────────────
<ACKC5 B:1>     确认码
                  0 = 接受
                  非0 = 错误
```

### 8.4 Stream 6 - 数据采集

| S/F | W-Bit | 名称 | 方向 | 描述 |
|-----|-------|------|------|------|
| S6F1 | W | Trace Data Send | E→H | 跟踪数据发送 |
| S6F2 | - | Trace Data Acknowledge | H→E | 跟踪数据确认 |
| S6F11 | W | Event Report Send | E→H | 事件报告发送 |
| S6F12 | - | Event Report Acknowledge | H→E | 事件报告确认 |
| S6F15 | W | Event Report Request | H→E | 事件报告请求 |
| S6F16 | - | Event Report Data | E→H | 事件报告数据 |
| S6F19 | W | Individual Report Request | H→E | 单独报告请求 |
| S6F20 | - | Individual Report Data | E→H | 单独报告数据 |

#### S6F11/S6F12 消息格式

```
S6F11 Event Report Send
───────────────────────
L,3
  <DATAID U4:1>   数据ID
  <CEID U4:1>     采集事件ID
  L,n             报告列表
    L,2
      <RPTID U4:1>  报告ID
      L,m           变量列表
        <V>         变量值
        ...
    ...

S6F12 Event Report Acknowledge
──────────────────────────────
<ACKC6 B:1>       确认码
                    0 = 接受
                    非0 = 错误
```

### 8.5 Stream 7 - 配方管理

| S/F | W-Bit | 名称 | 方向 | 描述 |
|-----|-------|------|------|------|
| S7F1 | W | Process Program Load Inquire | H→E | 加载配方询问 |
| S7F2 | - | Process Program Load Grant | E→H | 加载配方授权 |
| S7F3 | W | Process Program Send | H→E | 发送配方 |
| S7F4 | - | Process Program Acknowledge | E→H | 配方确认 |
| S7F5 | W | Process Program Request | H→E | 请求配方 |
| S7F6 | - | Process Program Data | E→H | 配方数据 |
| S7F17 | W | Delete Process Program Send | H→E | 删除配方 |
| S7F18 | - | Delete Process Program Ack | E→H | 删除配方确认 |
| S7F19 | W | Current EPPD Request | H→E | 当前配方查询 |
| S7F20 | - | Current EPPD Data | E→H | 当前配方数据 |

#### S7F3/S7F4 消息格式

```
S7F3 Process Program Send
─────────────────────────
L,2
  <PPID A>        配方ID
  <PPBODY B>      配方内容（二进制）

S7F4 Process Program Acknowledge
────────────────────────────────
<ACKC7 B:1>       确认码
                    0 = 接受
                    1 = 权限不足
                    2 = 空间不足
                    3 = 格式无效
                    4 = PPID已存在
                    5 = 不允许
```

### 8.6 Stream 9 - 系统错误

| S/F | W-Bit | 名称 | 描述 |
|-----|-------|------|------|
| S9F1 | - | Unrecognized Device ID | 无法识别的设备ID |
| S9F3 | - | Unrecognized Stream Type | 无法识别的Stream |
| S9F5 | - | Unrecognized Function Type | 无法识别的Function |
| S9F7 | - | Illegal Data | 非法数据 |
| S9F9 | - | Transaction Timer Timeout | 事务超时 |
| S9F11 | - | Data Too Long | 数据过长 |
| S9F13 | - | Conversation Timeout | 会话超时 |

### 8.7 Stream 10 - 终端服务

| S/F | W-Bit | 名称 | 方向 | 描述 |
|-----|-------|------|------|------|
| S10F1 | W | Terminal Request | H→E | 终端请求 |
| S10F2 | - | Terminal Request Acknowledge | E→H | 终端请求确认 |
| S10F3 | W | Terminal Display, Single | H→E | 单行显示 |
| S10F4 | - | Terminal Display Ack | E→H | 显示确认 |
| S10F5 | W | Terminal Display, Multi-Block | H→E | 多行显示 |
| S10F6 | - | Terminal Display Ack | E→H | 显示确认 |

---

## 9. 实现要点

### 9.1 字节序

SECS-II协议使用**大端序（Big-Endian）**进行数据传输：

```csharp
/// <summary>
/// 将整数转换为大端序字节数组
/// </summary>
public static byte[] ToBigEndian(int value)
{
    byte[] bytes = BitConverter.GetBytes(value);
    if (BitConverter.IsLittleEndian)
    {
        Array.Reverse(bytes);
    }
    return bytes;
}

/// <summary>
/// 从大端序字节数组读取整数
/// </summary>
public static int FromBigEndian(byte[] bytes)
{
    if (BitConverter.IsLittleEndian)
    {
        Array.Reverse(bytes);
    }
    return BitConverter.ToInt32(bytes, 0);
}
```

### 9.2 事务管理

```csharp
/// <summary>
/// 事务管理器接口
/// 用于管理HSMS消息的请求-响应匹配
/// </summary>
public interface ITransactionManager
{
    /// <summary>获取新的事务ID</summary>
    uint GetNextTransactionId();
    
    /// <summary>注册等待响应的事务</summary>
    void RegisterTransaction(uint transactionId, TaskCompletionSource<SecsMessage> tcs);
    
    /// <summary>完成事务（收到响应时调用）</summary>
    bool CompleteTransaction(uint transactionId, SecsMessage response);
    
    /// <summary>取消超时的事务</summary>
    void CancelTransaction(uint transactionId);
}
```

### 9.3 消息解析流程

```
接收数据 → 读取消息长度 → 读取完整消息 → 解析Header → 判断SType
                                                          │
            ┌─────────────────────────────────────────────┼────────────────┐
            │                                             │                │
            ▼                                             ▼                ▼
     SType = 0                                   SType = 1-9        SType = 其他
    (Data Message)                             (Control Message)      (错误)
            │                                             │
            ▼                                             ▼
    解析SECS-II数据                               处理控制消息
            │                                   (Select/Deselect/
            ▼                                    Linktest/Separate)
    匹配事务或触发事件
```

### 9.4 错误处理策略

```csharp
/// <summary>
/// GEM协议错误类型
/// </summary>
public enum GemErrorType
{
    /// <summary>通信错误</summary>
    CommunicationError,
    
    /// <summary>超时错误</summary>
    TimeoutError,
    
    /// <summary>消息格式错误</summary>
    MessageFormatError,
    
    /// <summary>未知的Stream/Function</summary>
    UnknownStreamFunction,
    
    /// <summary>参数错误</summary>
    ParameterError,
    
    /// <summary>状态错误</summary>
    StateError
}

/// <summary>
/// 错误处理建议：
/// 1. 通信错误 → 尝试重连
/// 2. 超时错误 → 发送S9F9，记录日志
/// 3. 消息格式错误 → 发送S9F7
/// 4. 未知S/F → 发送S9F3或S9F5
/// </summary>
```

### 9.5 心跳机制

```csharp
/// <summary>
/// 心跳配置
/// </summary>
public class LinktestConfiguration
{
    /// <summary>心跳间隔（毫秒）</summary>
    public int Interval { get; set; } = 30000;
    
    /// <summary>心跳超时（毫秒）</summary>
    public int Timeout { get; set; } = 5000;
    
    /// <summary>最大连续失败次数</summary>
    public int MaxFailures { get; set; } = 3;
}

/// <summary>
/// 心跳流程：
/// 1. 定期发送Linktest.req
/// 2. 等待Linktest.rsp
/// 3. 超时或失败超过阈值 → 断开连接
/// </summary>
```

### 9.6 线程安全

```csharp
/// <summary>
/// 关键的线程安全考虑：
/// 1. 事务ID生成 - 使用Interlocked操作
/// 2. 事务字典 - 使用ConcurrentDictionary
/// 3. 消息发送 - 使用SemaphoreSlim序列化
/// 4. 状态机转换 - 使用锁保护
/// </summary>
```

### 9.7 建议的项目结构

```
SECS2GEM/
├── Core/
│   ├── SecsMessage.cs          # SECS消息定义
│   ├── SecsItem.cs             # 数据项定义
│   ├── SecsFormat.cs           # 格式枚举
│   └── SecsException.cs        # 异常定义
├── Hsms/
│   ├── HsmsConnection.cs       # HSMS连接管理
│   ├── HsmsHeader.cs           # HSMS头定义
│   ├── HsmsMessage.cs          # HSMS消息定义
│   └── HsmsState.cs            # 连接状态机
├── Gem/
│   ├── GemEquipment.cs         # GEM设备实现
│   ├── GemHost.cs              # GEM主机实现
│   ├── StateModels/            # 状态模型
│   │   ├── CommunicationState.cs
│   │   ├── ControlState.cs
│   │   └── ProcessingState.cs
│   └── Handlers/               # 消息处理器
│       ├── Stream1Handler.cs
│       ├── Stream2Handler.cs
│       └── ...
├── Serialization/
│   ├── SecsSerializer.cs       # 消息序列化
│   └── SecsDeserializer.cs     # 消息反序列化
├── Configuration/
│   ├── GemConfiguration.cs     # GEM配置
│   └── HsmsConfiguration.cs    # HSMS配置
└── Interfaces/
    ├── ISecsConnection.cs      # 连接接口
    ├── IMessageHandler.cs      # 消息处理接口
    └── ITransactionManager.cs  # 事务管理接口
```

---

## 附录A：常用SECS变量类型

| 缩写 | 全称 | 描述 |
|------|------|------|
| MDLN | Model Name | 设备型号 |
| SOFTREV | Software Revision | 软件版本 |
| SVID | Status Variable ID | 状态变量ID |
| SV | Status Variable | 状态变量值 |
| ECID | Equipment Constant ID | 设备常量ID |
| ECV | Equipment Constant Value | 设备常量值 |
| CEID | Collection Event ID | 采集事件ID |
| RPTID | Report ID | 报告ID |
| VID | Variable ID | 变量ID |
| ALID | Alarm ID | 报警ID |
| ALCD | Alarm Code | 报警码 |
| ALTX | Alarm Text | 报警文本 |
| PPID | Process Program ID | 配方ID |
| PPBODY | Process Program Body | 配方内容 |
| RCMD | Remote Command | 远程命令 |
| CPNAME | Command Parameter Name | 命令参数名 |
| CPVAL | Command Parameter Value | 命令参数值 |
| DATAID | Data ID | 数据ID |
| OBJSPEC | Object Specifier | 对象标识符 |
| OBJTYPE | Object Type | 对象类型 |
| OBJID | Object ID | 对象ID |

---

## 附录B：响应代码定义

### COMMACK (通信确认码)
| 值 | 描述 |
|---|----|
| 0 | 接受 |
| 1 | 拒绝，已建立通信 |
| 2 | 拒绝，设备未准备好 |

### HCACK (主机命令确认码)
| 值 | 描述 |
|---|----|
| 0 | 确认，命令已执行 |
| 1 | 无效命令 |
| 2 | 无法执行 |
| 3 | 参数无效 |
| 4 | 确认，命令将稍后执行 |
| 5 | 被拒绝，已存在活动命令 |
| 6 | 无效对象 |

### ACKC5 (报警确认码)
| 值 | 描述 |
|---|----|
| 0 | 接受 |
| 其他 | 错误 |

### ACKC6 (数据采集确认码)
| 值 | 描述 |
|---|----|
| 0 | 接受 |
| 其他 | 错误 |

### ACKC7 (配方管理确认码)
| 值 | 描述 |
|---|----|
| 0 | 接受 |
| 1 | 权限错误 |
| 2 | 长度错误 |
| 3 | 矩阵溢出 |
| 4 | PPID不存在 |
| 5 | 模式错误 |
| 6 | 命令执行中 |

---

## 附录C：参考标准

| 标准编号 | 名称 | 描述 |
|----------|------|------|
| SEMI E4 | SEMI Equipment Communications Standard 1 | SECS-I串行通信 |
| SEMI E5 | SEMI Equipment Communications Standard 2 | SECS-II消息格式 |
| SEMI E30 | Generic Model for Communications and Control | GEM通用模型 |
| SEMI E37 | High-Speed SECS Message Services | HSMS协议 |
| SEMI E39 | Object Services Standard | 对象服务 |
| SEMI E40 | Processing Management Standard | 处理管理 |
| SEMI E87 | Carrier Management | 载具管理 |
| SEMI E90 | Substrate Tracking | 基板追踪 |
| SEMI E94 | Control Job Management | 控制作业管理 |
| SEMI E116 | Equipment Performance Tracking | 设备性能追踪 |
