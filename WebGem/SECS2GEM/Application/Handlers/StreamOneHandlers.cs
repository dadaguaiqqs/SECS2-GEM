using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;
using SECS2GEM.Domain.Interfaces;

namespace SECS2GEM.Application.Handlers
{
    /// <summary>
    /// 消息处理器基类
    /// </summary>
    /// <remarks>
    /// 模板方法模式：
    /// 1. 定义处理流程骨架
    /// 2. 子类只需实现具体处理逻辑
    /// 
    /// 好处：
    /// - 统一的异常处理
    /// - 统一的日志记录
    /// - 减少重复代码
    /// </remarks>
    public abstract class MessageHandlerBase : IMessageHandler
    {
        /// <summary>
        /// 处理器优先级（数值越小优先级越高）
        /// </summary>
        public virtual int Priority => 100;

        /// <summary>
        /// 目标Stream
        /// </summary>
        protected abstract byte Stream { get; }

        /// <summary>
        /// 目标Function
        /// </summary>
        protected abstract byte Function { get; }

        /// <summary>
        /// 是否能处理该消息
        /// </summary>
        public virtual bool CanHandle(SecsMessage message)
        {
            return message.Stream == Stream && message.Function == Function;
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        public async Task<SecsMessage?> HandleAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await HandleCoreAsync(message, context, cancellationToken);
            }
            catch (Exception)
            {
                // 返回S9F7（非法数据）或适当的错误响应
                if (message.WBit)
                {
                    return CreateErrorResponse(message);
                }
                return null;
            }
        }

        /// <summary>
        /// 核心处理逻辑（由子类实现）
        /// </summary>
        protected abstract Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken);

        /// <summary>
        /// 创建错误响应
        /// </summary>
        protected virtual SecsMessage CreateErrorResponse(SecsMessage request)
        {
            return new SecsMessage(9, 7, false, SecsItem.B(
                (byte)request.Stream,
                (byte)request.Function
            ));
        }
    }

    /// <summary>
    /// S1F1 处理器 - Are You There
    /// </summary>
    /// <remarks>
    /// 处理Host的连接检测请求，返回设备型号和软件版本
    /// </remarks>
    public sealed class S1F1Handler : MessageHandlerBase
    {
        protected override byte Stream => 1;
        protected override byte Function => 1;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            var gemState = context.GemState;

            // S1F2: MDLN, SOFTREV
            var response = new SecsMessage(1, 2, false, SecsItem.L(
                SecsItem.A(gemState.ModelName),
                SecsItem.A(gemState.SoftwareRevision)
            ));

            return Task.FromResult<SecsMessage?>(response);
        }
    }

    /// <summary>
    /// S1F13 处理器 - Establish Communications Request
    /// </summary>
    /// <remarks>
    /// 建立通信请求，返回通信确认
    /// </remarks>
    public sealed class S1F13Handler : MessageHandlerBase
    {
        protected override byte Stream => 1;
        protected override byte Function => 13;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            var gemState = context.GemState;

            // 设置通信状态为Communicating
            gemState.SetCommunicationState(GemCommunicationState.Communicating);

            // S1F14: COMMACK, <MDLN, SOFTREV>
            // COMMACK: 0 = Accepted, 1 = Denied
            var response = new SecsMessage(1, 14, false, SecsItem.L(
                SecsItem.B(0), // Accepted
                SecsItem.L(
                    SecsItem.A(gemState.ModelName),
                    SecsItem.A(gemState.SoftwareRevision)
                )
            ));

            return Task.FromResult<SecsMessage?>(response);
        }
    }

    /// <summary>
    /// S1F15 处理器 - Request OFF-LINE
    /// </summary>
    public sealed class S1F15Handler : MessageHandlerBase
    {
        protected override byte Stream => 1;
        protected override byte Function => 15;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            var gemState = context.GemState;

            // 尝试切换到离线
            byte oflack = gemState.RequestOffline() ? (byte)0 : (byte)1;

            // S1F16: OFLACK
            var response = new SecsMessage(1, 16, false, SecsItem.B(oflack));

            return Task.FromResult<SecsMessage?>(response);
        }
    }

    /// <summary>
    /// S1F17 处理器 - Request ON-LINE
    /// </summary>
    public sealed class S1F17Handler : MessageHandlerBase
    {
        protected override byte Stream => 1;
        protected override byte Function => 17;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            var gemState = context.GemState;

            // 尝试切换到在线
            byte onlack;
            if (gemState.RequestOnline())
            {
                // 默认进入Remote模式
                gemState.SwitchToRemote();
                onlack = 0; // Accepted
            }
            else
            {
                onlack = 1; // Denied
            }

            // S1F18: ONLACK
            var response = new SecsMessage(1, 18, false, SecsItem.B(onlack));

            return Task.FromResult<SecsMessage?>(response);
        }
    }
}
