using SECS2GEM.Core.Enums;

namespace SECS2GEM.Domain.Events
{
    /// <summary>
    /// 状态变化事件
    /// </summary>
    /// <remarks>
    /// 当GEM状态机状态发生变化时触发。
    /// </remarks>
    public sealed class StateChangedEvent : GemEventBase
    {
        /// <summary>
        /// 状态类型
        /// </summary>
        public StateType StateType { get; }

        /// <summary>
        /// 旧状态值
        /// </summary>
        public object OldState { get; }

        /// <summary>
        /// 新状态值
        /// </summary>
        public object NewState { get; }

        /// <summary>
        /// 变化原因
        /// </summary>
        public string? Reason { get; }

        public StateChangedEvent(
            string source,
            StateType stateType,
            object oldState,
            object newState,
            string? reason = null)
            : base(source)
        {
            StateType = stateType;
            OldState = oldState;
            NewState = newState;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"State Changed: {StateType} {OldState} → {NewState}" +
                   (Reason != null ? $" ({Reason})" : "");
        }
    }

    /// <summary>
    /// 通信状态变化事件
    /// </summary>
    public sealed class CommunicationStateChangedEvent : GemEventBase
    {
        public GemCommunicationState OldState { get; }
        public GemCommunicationState NewState { get; }

        public CommunicationStateChangedEvent(
            string source,
            GemCommunicationState oldState,
            GemCommunicationState newState)
            : base(source)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// 控制状态变化事件
    /// </summary>
    public sealed class ControlStateChangedEvent : GemEventBase
    {
        public GemControlState OldState { get; }
        public GemControlState NewState { get; }

        public ControlStateChangedEvent(
            string source,
            GemControlState oldState,
            GemControlState newState)
            : base(source)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// 状态类型枚举
    /// </summary>
    public enum StateType
    {
        /// <summary>通信状态</summary>
        Communication,

        /// <summary>控制状态</summary>
        Control,

        /// <summary>处理状态</summary>
        Processing,

        /// <summary>连接状态</summary>
        Connection
    }
}
