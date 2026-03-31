namespace SECS2GEM.Infrastructure.Logging
{
    /// <summary>
    /// 消息日志配置
    /// </summary>
    /// <remarks>
    /// 配置通讯消息的记录方式，支持HEX和SML两种格式。
    /// 日志文件将按照 /{BasePath}/{IP}-{Port}-{DeviceId}/ 目录结构存储。
    /// </remarks>
    public sealed class MessageLoggingConfiguration
    {
        /// <summary>
        /// 是否启用消息日志
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 日志基础路径
        /// </summary>
        /// <remarks>
        /// 实际日志将存储在 {BasePath}/{IP}-{Port}-{DeviceId}/ 目录下
        /// </remarks>
        public string BasePath { get; set; } = "logs";

        /// <summary>
        /// 是否记录HEX格式
        /// </summary>
        public bool LogHex { get; set; } = true;

        /// <summary>
        /// 是否记录SML格式
        /// </summary>
        public bool LogSml { get; set; } = true;

        /// <summary>
        /// 单个日志文件最大大小（MB）
        /// 超过后会创建新文件
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 50;

        /// <summary>
        /// 最大日志文件保留天数
        /// 0表示不自动清理
        /// </summary>
        public int RetentionDays { get; set; } = 30;

        /// <summary>
        /// 是否包含时间戳
        /// </summary>
        public bool IncludeTimestamp { get; set; } = true;

        /// <summary>
        /// 是否按日期分割文件
        /// </summary>
        public bool SplitByDate { get; set; } = true;

        /// <summary>
        /// HEX文件名格式
        /// </summary>
        public string HexFileNameFormat { get; set; } = "messages_{0:yyyyMMdd}.hex";

        /// <summary>
        /// SML文件名格式
        /// </summary>
        public string SmlFileNameFormat { get; set; } = "messages_{0:yyyyMMdd}.sml";

        /// <summary>
        /// 创建默认启用的配置
        /// </summary>
        public static MessageLoggingConfiguration CreateEnabled(string basePath)
        {
            return new MessageLoggingConfiguration
            {
                Enabled = true,
                BasePath = basePath,
                LogHex = true,
                LogSml = true
            };
        }
    }
}
