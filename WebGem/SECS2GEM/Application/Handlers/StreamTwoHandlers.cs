using SECS2GEM.Core.Entities;
using SECS2GEM.Domain.Interfaces;
using SECS2GEM.Domain.Models;

namespace SECS2GEM.Application.Handlers
{
    /// <summary>
    /// S2F13 处理器 - Equipment Constant Request
    /// </summary>
    /// <remarks>
    /// 查询设备常量值
    /// </remarks>
    public sealed class S2F13Handler : MessageHandlerBase
    {
        protected override byte Stream => 2;
        protected override byte Function => 13;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            var gemState = context.GemState;
            var items = new List<SecsItem>();

            // 请求格式：<L [n] <ECID1> <ECID2> ...>
            // 如果是空列表，返回所有EC
            if (message.Item == null || message.Item.Count == 0)
            {
                foreach (var ec in gemState.GetAllEquipmentConstants())
                {
                    items.Add(CreateEcvItem(ec));
                }
            }
            else
            {
                foreach (var ecidItem in message.Item.Items)
                {
                    var ecid = (uint)ecidItem.GetUInt64();
                    var value = gemState.GetEquipmentConstant(ecid);
                    if (value != null)
                    {
                        items.Add(CreateValueItem(value));
                    }
                    else
                    {
                        // 不存在的EC返回空
                        items.Add(SecsItem.L());
                    }
                }
            }

            // S2F14: <L [n] <ECV1> <ECV2> ...>
            var response = new SecsMessage(2, 14, false, SecsItem.L(items));

            return Task.FromResult<SecsMessage?>(response);
        }

        private static SecsItem CreateEcvItem(EquipmentConstant ec)
        {
            return CreateValueItem(ec.Value);
        }

        private static SecsItem CreateValueItem(object value)
        {
            return value switch
            {
                string s => SecsItem.A(s),
                int i => SecsItem.I4(i),
                uint u => SecsItem.U4(u),
                float f => SecsItem.F4(f),
                double d => SecsItem.F8(d),
                bool b => SecsItem.Boolean(b),
                byte[] bytes => SecsItem.B(bytes),
                _ => SecsItem.A(value.ToString() ?? "")
            };
        }
    }

    /// <summary>
    /// S2F15 处理器 - New Equipment Constant Send
    /// </summary>
    /// <remarks>
    /// 设置设备常量值
    /// </remarks>
    public sealed class S2F15Handler : MessageHandlerBase
    {
        protected override byte Stream => 2;
        protected override byte Function => 15;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            var gemState = context.GemState;
            byte eac = 0; // 0 = OK

            // 请求格式：<L [n] <L [2] <ECID> <ECV>> ...>
            if (message.Item != null)
            {
                foreach (var pair in message.Item.Items)
                {
                    if (pair.Count >= 2)
                    {
                        var ecid = (uint)pair[0].GetUInt64();
                        var value = ExtractValue(pair[1]);

                        if (!gemState.TrySetEquipmentConstant(ecid, value))
                        {
                            eac = 1; // At least one EC not set
                        }
                    }
                }
            }

            // S2F16: EAC
            var response = new SecsMessage(2, 16, false, SecsItem.B(eac));

            return Task.FromResult<SecsMessage?>(response);
        }

        private static object ExtractValue(SecsItem item)
        {
            return item.Format switch
            {
                Core.Enums.SecsFormat.ASCII or Core.Enums.SecsFormat.JIS8 => item.GetString(),
                Core.Enums.SecsFormat.I1 or Core.Enums.SecsFormat.I2 or 
                Core.Enums.SecsFormat.I4 or Core.Enums.SecsFormat.I8 => item.GetInt64(),
                Core.Enums.SecsFormat.U1 or Core.Enums.SecsFormat.U2 or 
                Core.Enums.SecsFormat.U4 or Core.Enums.SecsFormat.U8 => item.GetUInt64(),
                Core.Enums.SecsFormat.F4 or Core.Enums.SecsFormat.F8 => item.GetDouble(),
                Core.Enums.SecsFormat.Boolean => item.GetBoolean(),
                Core.Enums.SecsFormat.Binary => item.GetBytes(),
                _ => item.Value
            };
        }
    }

    /// <summary>
    /// S2F29 处理器 - Equipment Constant Namelist Request
    /// </summary>
    public sealed class S2F29Handler : MessageHandlerBase
    {
        protected override byte Stream => 2;
        protected override byte Function => 29;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            var gemState = context.GemState;
            var items = new List<SecsItem>();

            var constants = message.Item == null || message.Item.Count == 0
                ? gemState.GetAllEquipmentConstants()
                : GetRequestedConstants(gemState, message.Item);

            foreach (var ec in constants)
            {
                items.Add(SecsItem.L(
                    SecsItem.U4(ec.ConstantId),
                    SecsItem.A(ec.Name),
                    SecsItem.A(ec.MinValue?.ToString() ?? ""),
                    SecsItem.A(ec.MaxValue?.ToString() ?? ""),
                    SecsItem.A(ec.DefaultValue?.ToString() ?? ""),
                    SecsItem.A(ec.Units)
                ));
            }

            // S2F30
            var response = new SecsMessage(2, 30, false, SecsItem.L(items));

            return Task.FromResult<SecsMessage?>(response);
        }

        private static IEnumerable<EquipmentConstant> GetRequestedConstants(
            IGemState gemState, SecsItem requestItem)
        {
            var allConstants = gemState.GetAllEquipmentConstants();
            var dict = allConstants.ToDictionary(ec => ec.ConstantId);

            foreach (var ecidItem in requestItem.Items)
            {
                var ecid = (uint)ecidItem.GetUInt64();
                if (dict.TryGetValue(ecid, out var ec))
                {
                    yield return ec;
                }
            }
        }
    }

    /// <summary>
    /// S2F33 处理器 - Define Report
    /// </summary>
    public sealed class S2F33Handler : MessageHandlerBase
    {
        protected override byte Stream => 2;
        protected override byte Function => 33;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            // 简化实现：接受所有报告定义
            byte drack = 0; // Accepted

            // S2F34: DRACK
            var response = new SecsMessage(2, 34, false, SecsItem.B(drack));

            return Task.FromResult<SecsMessage?>(response);
        }
    }

    /// <summary>
    /// S2F35 处理器 - Link Event Report
    /// </summary>
    public sealed class S2F35Handler : MessageHandlerBase
    {
        protected override byte Stream => 2;
        protected override byte Function => 35;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            // 简化实现：接受所有事件报告链接
            byte lrack = 0; // Accepted

            // S2F36: LRACK
            var response = new SecsMessage(2, 36, false, SecsItem.B(lrack));

            return Task.FromResult<SecsMessage?>(response);
        }
    }

    /// <summary>
    /// S2F37 处理器 - Enable/Disable Event Report
    /// </summary>
    public sealed class S2F37Handler : MessageHandlerBase
    {
        protected override byte Stream => 2;
        protected override byte Function => 37;

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            // 简化实现：接受所有启用/禁用请求
            byte erack = 0; // Accepted

            // S2F38: ERACK
            var response = new SecsMessage(2, 38, false, SecsItem.B(erack));

            return Task.FromResult<SecsMessage?>(response);
        }
    }

    /// <summary>
    /// S2F41 处理器 - Host Command Send
    /// </summary>
    /// <remarks>
    /// 处理Host远程命令
    /// </remarks>
    public sealed class S2F41Handler : MessageHandlerBase
    {
        private readonly Dictionary<string, Func<IMessageContext, RemoteCommandResult>> _commands = new();

        protected override byte Stream => 2;
        protected override byte Function => 41;

        /// <summary>
        /// 注册远程命令处理函数
        /// </summary>
        public void RegisterCommand(string rcmd, Func<IMessageContext, RemoteCommandResult> handler)
        {
            _commands[rcmd.ToUpperInvariant()] = handler;
        }

        protected override Task<SecsMessage?> HandleCoreAsync(
            SecsMessage message,
            IMessageContext context,
            CancellationToken cancellationToken)
        {
            byte hcack;
            var cpacks = new List<SecsItem>();

            // 请求格式：<L [2] <RCMD> <L [n] <L [2] <CPNAME> <CPVAL>> ...>>
            if (message.Item != null && message.Item.Count >= 1)
            {
                var rcmd = message.Item[0].GetString().ToUpperInvariant();

                if (_commands.TryGetValue(rcmd, out var handler))
                {
                    var result = handler(context);
                    hcack = (byte)result.AckCode;

                    foreach (var param in result.ParameterAcks)
                    {
                        cpacks.Add(SecsItem.L(
                            SecsItem.A(param.ParameterName),
                            SecsItem.B((byte)param.AckCode)
                        ));
                    }
                }
                else
                {
                    hcack = 1; // Invalid command
                }
            }
            else
            {
                hcack = 3; // Invalid parameter
            }

            // S2F42: HCACK, <L [n] <L [2] <CPNAME> <CPACK>> ...>
            var response = new SecsMessage(2, 42, false, SecsItem.L(
                SecsItem.B(hcack),
                SecsItem.L(cpacks)
            ));

            return Task.FromResult<SecsMessage?>(response);
        }
    }
}
