using SECS2GEM.Core.Enums;
using SECS2GEM.Domain.Models;

namespace SECS2GEM.Domain.Interfaces
{
    /// <summary>
    /// GEM状态接口
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 1. 封装GEM协议定义的所有状态信息
    /// 2. 提供状态变量（SV）和设备常量（EC）的访问
    /// 3. 管理报警和事件报告配置
    /// 
    /// 状态模型包括：
    /// - 通信状态（Communication State）
    /// - 控制状态（Control State）
    /// - 处理状态（Processing State）
    /// </remarks>
    public interface IGemState
    {
        /// <summary>
        /// 设备型号 (MDLN)
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// 软件版本 (SOFTREV)
        /// </summary>
        string SoftwareRevision { get; }

        /// <summary>
        /// 通信状态
        /// </summary>
        GemCommunicationState CommunicationState { get; }

        /// <summary>
        /// 控制状态
        /// </summary>
        GemControlState ControlState { get; }

        /// <summary>
        /// 处理状态
        /// </summary>
        GemProcessingState ProcessingState { get; }

        /// <summary>
        /// 是否在线
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// 是否远程控制模式
        /// </summary>
        bool IsRemoteControl { get; }

        /// <summary>
        /// 通信状态变化事件
        /// </summary>
        event EventHandler<GemCommunicationState>? CommunicationStateChanged;

        /// <summary>
        /// 控制状态变化事件
        /// </summary>
        event EventHandler<GemControlState>? ControlStateChanged;

        #region 状态变量 (Status Variables)

        /// <summary>
        /// 获取状态变量值
        /// </summary>
        /// <param name="svid">状态变量ID</param>
        /// <returns>变量值，如果不存在返回null</returns>
        object? GetStatusVariable(uint svid);

        /// <summary>
        /// 设置状态变量值
        /// </summary>
        /// <param name="svid">状态变量ID</param>
        /// <param name="value">变量值</param>
        void SetStatusVariable(uint svid, object value);

        /// <summary>
        /// 获取所有状态变量定义
        /// </summary>
        IReadOnlyCollection<StatusVariable> GetAllStatusVariables();

        /// <summary>
        /// 注册状态变量
        /// </summary>
        /// <param name="variable">状态变量定义</param>
        void RegisterStatusVariable(StatusVariable variable);

        #endregion

        #region 设备常量 (Equipment Constants)

        /// <summary>
        /// 获取设备常量值
        /// </summary>
        /// <param name="ecid">设备常量ID</param>
        /// <returns>常量值，如果不存在返回null</returns>
        object? GetEquipmentConstant(uint ecid);

        /// <summary>
        /// 设置设备常量值
        /// </summary>
        /// <param name="ecid">设备常量ID</param>
        /// <param name="value">常量值</param>
        /// <returns>是否设置成功</returns>
        bool TrySetEquipmentConstant(uint ecid, object value);

        /// <summary>
        /// 获取所有设备常量定义
        /// </summary>
        IReadOnlyCollection<EquipmentConstant> GetAllEquipmentConstants();

        /// <summary>
        /// 注册设备常量
        /// </summary>
        /// <param name="constant">设备常量定义</param>
        void RegisterEquipmentConstant(EquipmentConstant constant);

        #endregion

        #region 状态转换

        /// <summary>
        /// 设置通信状态
        /// </summary>
        bool SetCommunicationState(GemCommunicationState state);

        /// <summary>
        /// 设置控制状态
        /// </summary>
        bool SetControlState(GemControlState state);

        /// <summary>
        /// 设置处理状态
        /// </summary>
        bool SetProcessingState(GemProcessingState state);

        /// <summary>
        /// 请求上线
        /// </summary>
        bool RequestOnline();

        /// <summary>
        /// 请求离线
        /// </summary>
        bool RequestOffline();

        /// <summary>
        /// 切换到本地控制
        /// </summary>
        bool SwitchToLocal();

        /// <summary>
        /// 切换到远程控制
        /// </summary>
        bool SwitchToRemote();

        #endregion
    }
}
