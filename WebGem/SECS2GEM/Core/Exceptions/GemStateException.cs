namespace SECS2GEM.Core.Exceptions
{
    /// <summary>
    /// GEM状态错误类型
    /// </summary>
    public enum StateErrorType
    {
        /// <summary>
        /// 无效的状态转换
        /// </summary>
        InvalidTransition,

        /// <summary>
        /// 操作在当前状态下不允许
        /// </summary>
        OperationNotAllowed,

        /// <summary>
        /// 通信未建立
        /// </summary>
        NotCommunicating,

        /// <summary>
        /// 设备离线
        /// </summary>
        EquipmentOffline,

        /// <summary>
        /// 非远程控制模式
        /// </summary>
        NotRemoteControl
    }

    /// <summary>
    /// GEM状态异常
    /// </summary>
    /// <remarks>
    /// 当GEM状态机操作不符合规范时抛出。
    /// 例如：在离线状态下尝试执行远程命令。
    /// </remarks>
    public class GemStateException : SecsException
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        public StateErrorType ErrorType { get; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public string? CurrentState { get; }

        /// <summary>
        /// 目标状态（仅状态转换错误时有效）
        /// </summary>
        public string? TargetState { get; }

        /// <summary>
        /// 初始化GemStateException
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="message">错误消息</param>
        public GemStateException(StateErrorType errorType, string message)
            : base(message)
        {
            ErrorType = errorType;
        }

        /// <summary>
        /// 初始化GemStateException（带状态信息）
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="message">错误消息</param>
        /// <param name="currentState">当前状态</param>
        public GemStateException(StateErrorType errorType, string message, string currentState)
            : base(message)
        {
            ErrorType = errorType;
            CurrentState = currentState;
        }

        /// <summary>
        /// 初始化GemStateException（带状态转换信息）
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="currentState">当前状态</param>
        /// <param name="targetState">目标状态</param>
        public GemStateException(string message, string currentState, string targetState)
            : base(message)
        {
            ErrorType = StateErrorType.InvalidTransition;
            CurrentState = currentState;
            TargetState = targetState;
        }

        /// <summary>
        /// 创建无效状态转换异常
        /// </summary>
        public static GemStateException InvalidTransition<TState>(TState from, TState to)
            where TState : struct, Enum
        {
            return new GemStateException(
                $"Invalid state transition from {from} to {to}",
                from.ToString()!,
                to.ToString()!);
        }

        /// <summary>
        /// 创建操作不允许异常
        /// </summary>
        public static GemStateException OperationNotAllowed<TState>(string operation, TState currentState)
            where TState : struct, Enum
        {
            return new GemStateException(
                StateErrorType.OperationNotAllowed,
                $"Operation '{operation}' is not allowed in state {currentState}",
                currentState.ToString()!);
        }

        /// <summary>
        /// 创建通信未建立异常
        /// </summary>
        public static GemStateException NotCommunicating()
        {
            return new GemStateException(
                StateErrorType.NotCommunicating,
                "Communication has not been established.");
        }

        /// <summary>
        /// 创建设备离线异常
        /// </summary>
        public static GemStateException EquipmentOffline()
        {
            return new GemStateException(
                StateErrorType.EquipmentOffline,
                "Equipment is offline.");
        }

        /// <summary>
        /// 创建非远程控制异常
        /// </summary>
        public static GemStateException NotRemoteControl()
        {
            return new GemStateException(
                StateErrorType.NotRemoteControl,
                "Equipment is not in remote control mode.");
        }
    }
}
