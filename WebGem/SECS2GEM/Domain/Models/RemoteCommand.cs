namespace SECS2GEM.Domain.Models
{
    /// <summary>
    /// 远程命令定义
    /// </summary>
    /// <remarks>
    /// 定义设备支持的远程命令（S2F41/S2F49）。
    /// </remarks>
    public sealed class RemoteCommandDefinition
    {
        /// <summary>
        /// 命令名称 (RCMD)
        /// </summary>
        public string CommandName { get; set; } = string.Empty;

        /// <summary>
        /// 命令描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 命令参数定义
        /// </summary>
        public List<CommandParameter> Parameters { get; set; } = new();

        /// <summary>
        /// 命令执行器
        /// </summary>
        public Func<RemoteCommandContext, Task<RemoteCommandResult>>? Handler { get; set; }
    }

    /// <summary>
    /// 命令参数定义
    /// </summary>
    public sealed class CommandParameter
    {
        /// <summary>
        /// 参数名称 (CPNAME)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 参数验证器
        /// </summary>
        public Func<object?, bool>? Validator { get; set; }
    }

    /// <summary>
    /// 远程命令执行上下文
    /// </summary>
    public sealed class RemoteCommandContext
    {
        /// <summary>
        /// 命令名称
        /// </summary>
        public string CommandName { get; set; } = string.Empty;

        /// <summary>
        /// 命令参数
        /// </summary>
        public Dictionary<string, object?> Parameters { get; set; } = new();

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// 获取参数值
        /// </summary>
        public T? GetParameter<T>(string name, T? defaultValue = default)
        {
            if (Parameters.TryGetValue(name, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// 远程命令执行结果
    /// </summary>
    public sealed class RemoteCommandResult
    {
        /// <summary>
        /// 确认码 (HCACK)
        /// </summary>
        public RemoteCommandAck AckCode { get; set; } = RemoteCommandAck.Acknowledge;

        /// <summary>
        /// 参数确认列表
        /// </summary>
        public List<ParameterAck> ParameterAcks { get; set; } = new();

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static RemoteCommandResult Success()
        {
            return new RemoteCommandResult { AckCode = RemoteCommandAck.Acknowledge };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static RemoteCommandResult Fail(RemoteCommandAck ack)
        {
            return new RemoteCommandResult { AckCode = ack };
        }
    }

    /// <summary>
    /// 参数确认
    /// </summary>
    public sealed class ParameterAck
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// 确认码
        /// </summary>
        public ParameterAckCode AckCode { get; set; } = ParameterAckCode.Accepted;
    }

    /// <summary>
    /// 远程命令确认码 (HCACK)
    /// </summary>
    public enum RemoteCommandAck : byte
    {
        /// <summary>确认，命令已执行</summary>
        Acknowledge = 0,

        /// <summary>无效命令</summary>
        InvalidCommand = 1,

        /// <summary>无法执行</summary>
        CannotPerform = 2,

        /// <summary>参数无效</summary>
        InvalidParameter = 3,

        /// <summary>确认，稍后执行</summary>
        AcknowledgeLater = 4,

        /// <summary>拒绝，已有活动命令</summary>
        Rejected = 5,

        /// <summary>无效对象</summary>
        InvalidObject = 6
    }

    /// <summary>
    /// 参数确认码 (CPACK)
    /// </summary>
    public enum ParameterAckCode : byte
    {
        /// <summary>接受</summary>
        Accepted = 0,

        /// <summary>参数名无效</summary>
        InvalidName = 1,

        /// <summary>参数值无效</summary>
        InvalidValue = 2,

        /// <summary>参数值超出范围</summary>
        OutOfRange = 3
    }
}
