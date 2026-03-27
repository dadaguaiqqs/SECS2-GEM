using SECS2GEM.Core.Entities;

namespace SECS2GEM.Domain.Interfaces
{
    /// <summary>
    /// SECS消息序列化器接口
    /// </summary>
    /// <remarks>
    /// 职责：SECS/HSMS消息与字节数组的相互转换。
    /// 
    /// 序列化流程：
    /// 1. 构建HSMS Header（10字节）
    /// 2. 递归序列化SecsItem（如果有）
    /// 3. 添加消息长度前缀（4字节）
    /// 
    /// 反序列化流程：
    /// 1. 读取消息长度（4字节）
    /// 2. 读取HSMS Header（10字节）
    /// 3. 递归解析SecsItem（如果有）
    /// </remarks>
    public interface ISecsSerializer
    {
        /// <summary>
        /// 序列化HSMS消息为字节数组
        /// </summary>
        /// <param name="message">HSMS消息</param>
        /// <returns>序列化后的字节数组（包含长度前缀）</returns>
        byte[] Serialize(HsmsMessage message);

        /// <summary>
        /// 序列化SECS消息为数据项字节数组（不含Header）
        /// </summary>
        /// <param name="item">数据项</param>
        /// <returns>序列化后的字节数组</returns>
        byte[] SerializeItem(SecsItem item);

        /// <summary>
        /// 从字节数组反序列化HSMS消息
        /// </summary>
        /// <param name="data">字节数据（不含长度前缀）</param>
        /// <returns>HSMS消息</returns>
        HsmsMessage Deserialize(ReadOnlySpan<byte> data);

        /// <summary>
        /// 反序列化数据项
        /// </summary>
        /// <param name="data">数据项字节数组</param>
        /// <returns>数据项</returns>
        SecsItem DeserializeItem(ReadOnlySpan<byte> data);

        /// <summary>
        /// 尝试从缓冲区读取完整消息
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="message">解析出的消息</param>
        /// <param name="bytesConsumed">消耗的字节数</param>
        /// <returns>是否成功读取完整消息</returns>
        bool TryReadMessage(ReadOnlySpan<byte> buffer, out HsmsMessage? message, out int bytesConsumed);
    }
}
