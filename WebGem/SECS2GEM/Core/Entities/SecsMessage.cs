namespace SECS2GEM.Core.Entities
{
    /// <summary>
    /// SECS-II消息
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 1. 封装Stream/Function/WBit等协议字段
    /// 2. 不可变设计保证线程安全
    /// 3. 提供流畅的消息构建API
    /// 
    /// SECS消息格式：
    /// - Stream: 消息类别 (1-127)
    /// - Function: 具体功能（奇数为Primary，偶数为Secondary）
    /// - W-Bit: 是否期望回复
    /// - Item: 消息数据项
    /// </remarks>
    public sealed class SecsMessage
    {
        /// <summary>
        /// Stream号 (1-127)
        /// </summary>
        /// <remarks>
        /// Stream定义了消息的类别：
        /// - S1: 设备状态
        /// - S2: 设备控制
        /// - S5: 异常处理
        /// - S6: 数据采集
        /// - S7: 配方管理
        /// 等等
        /// </remarks>
        public byte Stream { get; }

        /// <summary>
        /// Function号
        /// </summary>
        /// <remarks>
        /// Function定义了具体功能：
        /// - 奇数: Primary消息（请求）
        /// - 偶数: Secondary消息（响应）
        /// - 0: 用于Abort
        /// </remarks>
        public byte Function { get; }

        /// <summary>
        /// 是否期望回复（W-Bit）
        /// </summary>
        /// <remarks>
        /// 位于Header Byte 2的最高位 (bit 7)：
        /// - true: 期望回复
        /// - false: 不期望回复
        /// 
        /// 通常Primary消息设置为true，Secondary消息设置为false。
        /// </remarks>
        public bool WBit { get; }

        /// <summary>
        /// 消息数据项
        /// </summary>
        /// <remarks>
        /// 包含SECS-II格式的数据内容。
        /// 某些消息可能没有数据项（如S1F1）。
        /// </remarks>
        public SecsItem? Item { get; }

        /// <summary>
        /// 消息名称（如"S1F1"）
        /// </summary>
        public string Name => $"S{Stream}F{Function}";

        /// <summary>
        /// 消息完整名称（含W-Bit信息）
        /// </summary>
        public string FullName => WBit ? $"S{Stream}F{Function} W" : $"S{Stream}F{Function}";

        /// <summary>
        /// 是否为Primary消息（奇数Function）
        /// </summary>
        public bool IsPrimary => Function % 2 == 1;

        /// <summary>
        /// 是否为Secondary消息（偶数Function，且不为0）
        /// </summary>
        public bool IsSecondary => Function % 2 == 0 && Function != 0;

        /// <summary>
        /// 初始化SecsMessage实例
        /// </summary>
        /// <param name="stream">Stream号</param>
        /// <param name="function">Function号</param>
        /// <param name="wbit">是否期望回复</param>
        /// <param name="item">消息数据项（可为null）</param>
        public SecsMessage(byte stream, byte function, bool wbit = true, SecsItem? item = null)
        {
            if (stream == 0 || stream > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(stream), "Stream must be between 1 and 127.");
            }

            Stream = stream;
            Function = function;
            WBit = wbit;
            Item = item;
        }

        /// <summary>
        /// 创建对应的响应消息（Function + 1，WBit = false）
        /// </summary>
        /// <param name="item">响应数据项</param>
        /// <returns>响应消息</returns>
        public SecsMessage CreateReply(SecsItem? item = null)
        {
            if (!IsPrimary)
            {
                throw new InvalidOperationException("Cannot create reply for non-primary message.");
            }

            return new SecsMessage(Stream, (byte)(Function + 1), false, item);
        }

        /// <summary>
        /// 返回消息的字符串表示
        /// </summary>
        public override string ToString()
        {
            return Item != null
                ? $"{FullName}\n{Item.ToSml()}"
                : FullName;
        }

        /// <summary>
        /// 返回消息的SML格式表示
        /// </summary>
        public string ToSml()
        {
            var itemSml = Item?.ToSml() ?? "";
            return $"{FullName}\n{itemSml}.";
        }

        #region 常用消息工厂方法

        /// <summary>
        /// 创建S1F1 Are You There Request
        /// </summary>
        public static SecsMessage S1F1()
        {
            return new SecsMessage(1, 1, true);
        }

        /// <summary>
        /// 创建S1F2 On Line Data
        /// </summary>
        /// <param name="modelName">设备型号</param>
        /// <param name="softwareRevision">软件版本</param>
        public static SecsMessage S1F2(string modelName, string softwareRevision)
        {
            return new SecsMessage(1, 2, false,
                SecsItem.L(
                    SecsItem.A(modelName),
                    SecsItem.A(softwareRevision)
                ));
        }

        /// <summary>
        /// 创建S1F13 Establish Communications Request
        /// </summary>
        /// <param name="modelName">设备型号（可为空）</param>
        /// <param name="softwareRevision">软件版本（可为空）</param>
        public static SecsMessage S1F13(string modelName = "", string softwareRevision = "")
        {
            return new SecsMessage(1, 13, true,
                SecsItem.L(
                    SecsItem.A(modelName),
                    SecsItem.A(softwareRevision)
                ));
        }

        /// <summary>
        /// 创建S1F14 Establish Communications Acknowledge
        /// </summary>
        /// <param name="commAck">通信确认码 (0=接受, 1=拒绝)</param>
        /// <param name="modelName">设备型号</param>
        /// <param name="softwareRevision">软件版本</param>
        public static SecsMessage S1F14(byte commAck, string modelName, string softwareRevision)
        {
            return new SecsMessage(1, 14, false,
                SecsItem.L(
                    SecsItem.B(commAck),
                    SecsItem.L(
                        SecsItem.A(modelName),
                        SecsItem.A(softwareRevision)
                    )
                ));
        }

        /// <summary>
        /// 创建S9Fx 错误消息
        /// </summary>
        /// <param name="function">错误类型 (1=设备ID, 3=Stream, 5=Function, 7=数据, 9=超时)</param>
        /// <param name="mhead">原消息头</param>
        public static SecsMessage S9Fx(byte function, byte[] mhead)
        {
            return new SecsMessage(9, function, false, SecsItem.B(mhead));
        }

        #endregion
    }
}
