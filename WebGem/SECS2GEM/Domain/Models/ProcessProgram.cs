namespace SECS2GEM.Domain.Models
{
    /// <summary>
    /// 配方信息 (Process Program)
    /// </summary>
    /// <remarks>
    /// 配方用于S7消息的配方管理。
    /// </remarks>
    public sealed class ProcessProgram
    {
        /// <summary>
        /// 配方ID (PPID)
        /// </summary>
        public string ProgramId { get; set; } = string.Empty;

        /// <summary>
        /// 配方内容 (PPBODY)
        /// </summary>
        public byte[] Body { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 配方类型
        /// </summary>
        public ProcessProgramType Type { get; set; } = ProcessProgramType.Equipment;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 配方大小（字节）
        /// </summary>
        public int Size => Body.Length;
    }

    /// <summary>
    /// 配方类型
    /// </summary>
    public enum ProcessProgramType
    {
        /// <summary>设备配方</summary>
        Equipment,

        /// <summary>格式化配方</summary>
        Formatted,

        /// <summary>非格式化配方</summary>
        Unformatted
    }

    /// <summary>
    /// 配方管理配置
    /// </summary>
    public sealed class ProcessProgramConfiguration
    {
        /// <summary>
        /// 最大配方数量
        /// </summary>
        public int MaxProgramCount { get; set; } = 100;

        /// <summary>
        /// 最大配方大小（字节）
        /// </summary>
        public int MaxProgramSize { get; set; } = 1024 * 1024; // 1MB

        /// <summary>
        /// 已存储的配方
        /// </summary>
        public Dictionary<string, ProcessProgram> Programs { get; } = new();

        /// <summary>
        /// 当前选中的配方ID
        /// </summary>
        public string? SelectedProgramId { get; set; }

        /// <summary>
        /// 添加配方
        /// </summary>
        public ProcessProgramAck AddProgram(ProcessProgram program)
        {
            if (program.Body.Length > MaxProgramSize)
            {
                return ProcessProgramAck.LengthError;
            }

            if (Programs.Count >= MaxProgramCount && !Programs.ContainsKey(program.ProgramId))
            {
                return ProcessProgramAck.MatrixOverflow;
            }

            Programs[program.ProgramId] = program;
            return ProcessProgramAck.Accepted;
        }

        /// <summary>
        /// 删除配方
        /// </summary>
        public ProcessProgramAck DeleteProgram(string programId)
        {
            if (!Programs.ContainsKey(programId))
            {
                return ProcessProgramAck.PpidNotFound;
            }

            if (SelectedProgramId == programId)
            {
                return ProcessProgramAck.CommandError;
            }

            Programs.Remove(programId);
            return ProcessProgramAck.Accepted;
        }

        /// <summary>
        /// 获取配方
        /// </summary>
        public ProcessProgram? GetProgram(string programId)
        {
            return Programs.TryGetValue(programId, out var program) ? program : null;
        }

        /// <summary>
        /// 获取所有配方ID
        /// </summary>
        public IEnumerable<string> GetAllProgramIds()
        {
            return Programs.Keys;
        }
    }

    /// <summary>
    /// 配方操作确认码 (ACKC7)
    /// </summary>
    public enum ProcessProgramAck : byte
    {
        /// <summary>接受</summary>
        Accepted = 0,

        /// <summary>权限错误</summary>
        PermissionError = 1,

        /// <summary>长度错误</summary>
        LengthError = 2,

        /// <summary>矩阵溢出</summary>
        MatrixOverflow = 3,

        /// <summary>PPID不存在</summary>
        PpidNotFound = 4,

        /// <summary>模式错误</summary>
        ModeError = 5,

        /// <summary>命令执行中</summary>
        CommandError = 6
    }
}
