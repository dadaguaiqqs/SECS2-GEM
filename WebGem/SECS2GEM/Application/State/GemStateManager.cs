using System.Collections.Concurrent;
using SECS2GEM.Core.Enums;
using SECS2GEM.Domain.Interfaces;
using SECS2GEM.Domain.Models;

namespace SECS2GEM.Application.State
{
    /// <summary>
    /// GEM状态管理器
    /// </summary>
    /// <remarks>
    /// 状态模式实现：
    /// 1. 封装GEM协议定义的三种状态机（通信/控制/处理）
    /// 2. 管理状态变量（SV）和设备常量（EC）
    /// 3. 实现状态转换的验证逻辑
    /// 
    /// 状态转换规则：
    /// - 通信状态：Disabled ↔ Enabled (WaitCRA ↔ WaitDelay ↔ Communicating)
    /// - 控制状态：EquipmentOffline ↔ (AttemptOnline → OnlineLocal/OnlineRemote)
    /// - 处理状态：Init → Idle ↔ (Setup/Ready/Executing/Pause)
    /// </remarks>
    public sealed class GemStateManager : IGemState
    {
        private readonly object _stateLock = new();
        private readonly ConcurrentDictionary<uint, StatusVariable> _statusVariables = new();
        private readonly ConcurrentDictionary<uint, EquipmentConstant> _equipmentConstants = new();

        // 状态
        private GemCommunicationState _communicationState = GemCommunicationState.Disabled;
        private GemControlState _controlState = GemControlState.EquipmentOffline;
        private GemProcessingState _processingState = GemProcessingState.Idle;

        #region Properties

        /// <summary>
        /// 设备型号 (MDLN)
        /// </summary>
        public string ModelName { get; }

        /// <summary>
        /// 软件版本 (SOFTREV)
        /// </summary>
        public string SoftwareRevision { get; }

        /// <summary>
        /// 通信状态
        /// </summary>
        public GemCommunicationState CommunicationState
        {
            get { lock (_stateLock) return _communicationState; }
        }

        /// <summary>
        /// 控制状态
        /// </summary>
        public GemControlState ControlState
        {
            get { lock (_stateLock) return _controlState; }
        }

        /// <summary>
        /// 处理状态
        /// </summary>
        public GemProcessingState ProcessingState
        {
            get { lock (_stateLock) return _processingState; }
        }

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool IsOnline => ControlState is GemControlState.OnlineLocal or GemControlState.OnlineRemote;

        /// <summary>
        /// 是否远程控制模式
        /// </summary>
        public bool IsRemoteControl => ControlState == GemControlState.OnlineRemote;

        #endregion

        #region Events

        /// <summary>
        /// 通信状态变化事件
        /// </summary>
        public event EventHandler<GemCommunicationState>? CommunicationStateChanged;

        /// <summary>
        /// 控制状态变化事件
        /// </summary>
        public event EventHandler<GemControlState>? ControlStateChanged;

        #endregion

        /// <summary>
        /// 创建GEM状态管理器
        /// </summary>
        /// <param name="modelName">设备型号</param>
        /// <param name="softwareRevision">软件版本</param>
        public GemStateManager(string modelName = "SECS2GEM", string softwareRevision = "1.0.0")
        {
            ModelName = modelName;
            SoftwareRevision = softwareRevision;

            // 注册标准状态变量
            RegisterStandardStatusVariables();
        }

        #region Status Variables

        /// <summary>
        /// 获取状态变量值
        /// </summary>
        public object? GetStatusVariable(uint svid)
        {
            if (_statusVariables.TryGetValue(svid, out var sv))
            {
                return sv.GetValue();
            }
            return null;
        }

        /// <summary>
        /// 设置状态变量值
        /// </summary>
        public void SetStatusVariable(uint svid, object value)
        {
            if (_statusVariables.TryGetValue(svid, out var sv))
            {
                sv.SetValue(value);
            }
        }

        /// <summary>
        /// 获取所有状态变量定义
        /// </summary>
        public IReadOnlyCollection<StatusVariable> GetAllStatusVariables()
        {
            return _statusVariables.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// 注册状态变量
        /// </summary>
        public void RegisterStatusVariable(StatusVariable variable)
        {
            _statusVariables[variable.VariableId] = variable;
        }

        #endregion

        #region Equipment Constants

        /// <summary>
        /// 获取设备常量值
        /// </summary>
        public object? GetEquipmentConstant(uint ecid)
        {
            if (_equipmentConstants.TryGetValue(ecid, out var ec))
            {
                return ec.Value;
            }
            return null;
        }

        /// <summary>
        /// 设置设备常量值
        /// </summary>
        public bool TrySetEquipmentConstant(uint ecid, object value)
        {
            if (_equipmentConstants.TryGetValue(ecid, out var ec))
            {
                return ec.TrySetValue(value);
            }
            return false;
        }

        /// <summary>
        /// 获取所有设备常量定义
        /// </summary>
        public IReadOnlyCollection<EquipmentConstant> GetAllEquipmentConstants()
        {
            return _equipmentConstants.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// 注册设备常量
        /// </summary>
        public void RegisterEquipmentConstant(EquipmentConstant constant)
        {
            _equipmentConstants[constant.ConstantId] = constant;
        }

        #endregion

        #region State Transitions

        /// <summary>
        /// 设置通信状态
        /// </summary>
        public bool SetCommunicationState(GemCommunicationState state)
        {
            lock (_stateLock)
            {
                if (!IsValidCommunicationTransition(_communicationState, state))
                {
                    return false;
                }

                var oldState = _communicationState;
                _communicationState = state;

                // 离开通信状态时重置控制状态
                if (oldState == GemCommunicationState.Communicating && 
                    state != GemCommunicationState.Communicating)
                {
                    _controlState = GemControlState.EquipmentOffline;
                }

                CommunicationStateChanged?.Invoke(this, state);
                return true;
            }
        }

        /// <summary>
        /// 设置控制状态
        /// </summary>
        public bool SetControlState(GemControlState state)
        {
            lock (_stateLock)
            {
                if (!IsValidControlTransition(_controlState, state))
                {
                    return false;
                }

                _controlState = state;
                ControlStateChanged?.Invoke(this, state);
                return true;
            }
        }

        /// <summary>
        /// 设置处理状态
        /// </summary>
        public bool SetProcessingState(GemProcessingState state)
        {
            lock (_stateLock)
            {
                if (!IsValidProcessingTransition(_processingState, state))
                {
                    return false;
                }

                _processingState = state;
                return true;
            }
        }

        /// <summary>
        /// 请求上线
        /// </summary>
        public bool RequestOnline()
        {
            lock (_stateLock)
            {
                if (_communicationState != GemCommunicationState.Communicating)
                {
                    return false;
                }

                if (_controlState == GemControlState.EquipmentOffline)
                {
                    _controlState = GemControlState.AttemptOnline;
                    ControlStateChanged?.Invoke(this, _controlState);
                }

                return true;
            }
        }

        /// <summary>
        /// 请求离线
        /// </summary>
        public bool RequestOffline()
        {
            lock (_stateLock)
            {
                if (_controlState is GemControlState.OnlineLocal or GemControlState.OnlineRemote)
                {
                    _controlState = GemControlState.EquipmentOffline;
                    ControlStateChanged?.Invoke(this, _controlState);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 切换到本地控制
        /// </summary>
        public bool SwitchToLocal()
        {
            lock (_stateLock)
            {
                if (_controlState == GemControlState.OnlineRemote)
                {
                    _controlState = GemControlState.OnlineLocal;
                    ControlStateChanged?.Invoke(this, _controlState);
                    return true;
                }

                if (_controlState == GemControlState.AttemptOnline)
                {
                    _controlState = GemControlState.OnlineLocal;
                    ControlStateChanged?.Invoke(this, _controlState);
                    return true;
                }

                return _controlState == GemControlState.OnlineLocal;
            }
        }

        /// <summary>
        /// 切换到远程控制
        /// </summary>
        public bool SwitchToRemote()
        {
            lock (_stateLock)
            {
                if (_controlState == GemControlState.OnlineLocal)
                {
                    _controlState = GemControlState.OnlineRemote;
                    ControlStateChanged?.Invoke(this, _controlState);
                    return true;
                }

                if (_controlState == GemControlState.AttemptOnline)
                {
                    _controlState = GemControlState.OnlineRemote;
                    ControlStateChanged?.Invoke(this, _controlState);
                    return true;
                }

                return _controlState == GemControlState.OnlineRemote;
            }
        }

        #endregion

        #region Transition Validation

        /// <summary>
        /// 验证通信状态转换是否有效
        /// </summary>
        private static bool IsValidCommunicationTransition(GemCommunicationState from, GemCommunicationState to)
        {
            // 允许的转换
            return (from, to) switch
            {
                // Disabled状态可以进入Enabled
                (GemCommunicationState.Disabled, GemCommunicationState.Enabled) => true,
                (GemCommunicationState.Disabled, GemCommunicationState.WaitCommunicationRequest) => true,

                // Enabled状态可以回到Disabled或进入WaitCRA
                (GemCommunicationState.Enabled, GemCommunicationState.Disabled) => true,
                (GemCommunicationState.Enabled, GemCommunicationState.WaitCommunicationRequest) => true,

                // WaitCRA可以进入WaitDelay、Communicating或回到Disabled
                (GemCommunicationState.WaitCommunicationRequest, GemCommunicationState.WaitCommunicationDelay) => true,
                (GemCommunicationState.WaitCommunicationRequest, GemCommunicationState.Communicating) => true,
                (GemCommunicationState.WaitCommunicationRequest, GemCommunicationState.Disabled) => true,

                // WaitDelay可以重新进入WaitCRA或回到Disabled
                (GemCommunicationState.WaitCommunicationDelay, GemCommunicationState.WaitCommunicationRequest) => true,
                (GemCommunicationState.WaitCommunicationDelay, GemCommunicationState.Disabled) => true,

                // Communicating可以回到任何状态
                (GemCommunicationState.Communicating, _) => true,

                // 相同状态允许
                _ when from == to => true,

                _ => false
            };
        }

        /// <summary>
        /// 验证控制状态转换是否有效
        /// </summary>
        private static bool IsValidControlTransition(GemControlState from, GemControlState to)
        {
            return (from, to) switch
            {
                // EquipmentOffline可以进入AttemptOnline
                (GemControlState.EquipmentOffline, GemControlState.AttemptOnline) => true,

                // AttemptOnline可以进入OnlineLocal或OnlineRemote，或回到EquipmentOffline
                (GemControlState.AttemptOnline, GemControlState.OnlineLocal) => true,
                (GemControlState.AttemptOnline, GemControlState.OnlineRemote) => true,
                (GemControlState.AttemptOnline, GemControlState.EquipmentOffline) => true,

                // OnlineLocal和OnlineRemote可以互相切换或回到EquipmentOffline
                (GemControlState.OnlineLocal, GemControlState.OnlineRemote) => true,
                (GemControlState.OnlineLocal, GemControlState.EquipmentOffline) => true,
                (GemControlState.OnlineRemote, GemControlState.OnlineLocal) => true,
                (GemControlState.OnlineRemote, GemControlState.EquipmentOffline) => true,

                // HostOffline只能从在线状态进入
                (GemControlState.OnlineLocal, GemControlState.HostOffline) => true,
                (GemControlState.OnlineRemote, GemControlState.HostOffline) => true,
                (GemControlState.HostOffline, GemControlState.EquipmentOffline) => true,

                // 相同状态允许
                _ when from == to => true,

                _ => false
            };
        }

        /// <summary>
        /// 验证处理状态转换是否有效
        /// </summary>
        private static bool IsValidProcessingTransition(GemProcessingState from, GemProcessingState to)
        {
            return (from, to) switch
            {
                // Idle可以进入Setup或直接Ready
                (GemProcessingState.Idle, GemProcessingState.Setup) => true,
                (GemProcessingState.Idle, GemProcessingState.Ready) => true,

                // Setup完成后进入Ready
                (GemProcessingState.Setup, GemProcessingState.Ready) => true,
                (GemProcessingState.Setup, GemProcessingState.Idle) => true,

                // Ready可以开始执行
                (GemProcessingState.Ready, GemProcessingState.Executing) => true,
                (GemProcessingState.Ready, GemProcessingState.Idle) => true,

                // Executing可以暂停或完成
                (GemProcessingState.Executing, GemProcessingState.Paused) => true,
                (GemProcessingState.Executing, GemProcessingState.Ready) => true,
                (GemProcessingState.Executing, GemProcessingState.Idle) => true,

                // Paused可以继续或停止
                (GemProcessingState.Paused, GemProcessingState.Executing) => true,
                (GemProcessingState.Paused, GemProcessingState.Idle) => true,

                // 相同状态允许
                _ when from == to => true,

                _ => false
            };
        }

        #endregion

        #region Standard Variables Registration

        /// <summary>
        /// 注册标准状态变量
        /// </summary>
        private void RegisterStandardStatusVariables()
        {
            // SVID 1: Clock
            RegisterStatusVariable(new StatusVariable
            {
                VariableId = 1,
                Name = "Clock",
                Units = "",
                Format = Core.Enums.SecsFormat.ASCII,
                ValueGetter = () => DateTime.Now.ToString("yyyyMMddHHmmss")
            });

            // SVID 2: ControlState
            RegisterStatusVariable(new StatusVariable
            {
                VariableId = 2,
                Name = "ControlState",
                Units = "",
                Format = Core.Enums.SecsFormat.U1,
                ValueGetter = () => (byte)ControlState
            });

            // SVID 3-9: 其他标准变量可按需添加
        }

        #endregion
    }
}
