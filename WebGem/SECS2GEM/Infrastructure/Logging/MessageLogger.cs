using System.Collections.Concurrent;
using System.Text;
using SECS2GEM.Core.Entities;

namespace SECS2GEM.Infrastructure.Logging
{
    /// <summary>
    /// 消息记录器实现
    /// </summary>
    /// <remarks>
    /// 设计思路：
    /// 1. 使用生产者-消费者模式异步写入日志，避免阻塞通讯线程
    /// 2. 使用ConcurrentQueue作为消息缓冲区
    /// 3. 支持按日期分割文件和文件大小限制
    /// 4. 自动创建目录结构：/{BasePath}/{IP}-{Port}-{DeviceId}/
    /// 
    /// 执行流程：
    /// 1. InitializeAsync() - 创建日志目录
    /// 2. LogMessageAsync() - 将消息加入队列
    /// 3. 后台任务消费队列并写入文件
    /// 4. DisposeAsync() - 刷新缓冲区并关闭文件
    /// </remarks>
    public sealed class MessageLogger : IMessageLogger
    {
        private readonly MessageLoggingConfiguration _config;
        private readonly ConcurrentQueue<LogEntry> _logQueue;
        private readonly SemaphoreSlim _writeSemaphore;

        private string? _logDirectory;
        private StreamWriter? _hexWriter;
        private StreamWriter? _smlWriter;
        private DateTime _currentLogDate;
        private bool _initialized;
        private bool _disposed;

        private CancellationTokenSource? _cts;
        private Task? _writeTask;

        /// <summary>
        /// 日志条目
        /// </summary>
        private record LogEntry(
            string HexContent,
            string SmlContent,
            DateTime Timestamp);

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled => _config.Enabled && _initialized;

        /// <summary>
        /// 创建消息记录器
        /// </summary>
        public MessageLogger(MessageLoggingConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logQueue = new ConcurrentQueue<LogEntry>();
            _writeSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// 初始化日志器
        /// </summary>
        public async Task InitializeAsync(string ipAddress, int port, ushort deviceId)
        {
            if (!_config.Enabled) return;
            if (_initialized) return;

            // 构建目录路径：{BasePath}/{IP}-{Port}-{DeviceId}/
            // 替换IP中的特殊字符
            var safeIp = ipAddress.Replace(":", "_").Replace(".", "_");
            var directoryName = $"{safeIp}-{port}-{deviceId}";
            _logDirectory = Path.Combine(_config.BasePath, directoryName);

            // 创建目录
            Directory.CreateDirectory(_logDirectory);

            // 打开日志文件
            _currentLogDate = DateTime.Today;
            await OpenLogFilesAsync();

            // 启动后台写入任务
            _cts = new CancellationTokenSource();
            _writeTask = WriteLoopAsync(_cts.Token);

            _initialized = true;

            // 清理旧日志文件
            if (_config.RetentionDays > 0)
            {
                _ = CleanupOldLogsAsync();
            }
        }

        /// <summary>
        /// 记录HSMS消息
        /// </summary>
        public Task LogMessageAsync(HsmsMessage message, byte[] rawBytes, MessageDirection direction)
        {
            if (!IsEnabled) return Task.CompletedTask;

            var hexContent = _config.LogHex 
                ? SmlFormatter.FormatHex(rawBytes, direction, _config.IncludeTimestamp) 
                : string.Empty;

            var smlContent = _config.LogSml 
                ? SmlFormatter.Format(message, direction, _config.IncludeTimestamp) 
                : string.Empty;

            _logQueue.Enqueue(new LogEntry(hexContent, smlContent, DateTime.Now));
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 记录原始字节
        /// </summary>
        public Task LogRawBytesAsync(byte[] rawBytes, MessageDirection direction, string? description = null)
        {
            if (!IsEnabled) return Task.CompletedTask;

            var hexContent = _config.LogHex
                ? SmlFormatter.FormatHex(rawBytes, direction, _config.IncludeTimestamp)
                : string.Empty;

            var smlContent = string.Empty;
            if (_config.LogSml && !string.IsNullOrEmpty(description))
            {
                var sb = new StringBuilder();
                if (_config.IncludeTimestamp)
                {
                    sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");
                }
                sb.AppendLine(direction == MessageDirection.Send ? ">> SEND" : "<< RECV");
                sb.AppendLine($"  {description}");
                sb.AppendLine(".");
                sb.AppendLine();
                smlContent = sb.ToString();
            }

            _logQueue.Enqueue(new LogEntry(hexContent, smlContent, DateTime.Now));

            return Task.CompletedTask;
        }

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        public async Task FlushAsync()
        {
            if (!IsEnabled) return;

            await _writeSemaphore.WaitAsync();
            try
            {
                // 处理队列中剩余的消息
                while (_logQueue.TryDequeue(out var entry))
                {
                    await WriteEntryAsync(entry);
                }

                // 刷新文件
                if (_hexWriter != null) await _hexWriter.FlushAsync();
                if (_smlWriter != null) await _smlWriter.FlushAsync();
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        /// <summary>
        /// 后台写入循环
        /// </summary>
        private async Task WriteLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 等待一小段时间再批量写入，减少IO操作
                    await Task.Delay(100, cancellationToken);

                    if (_logQueue.IsEmpty) continue;

                    await _writeSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        // 检查是否需要切换日志文件（按日期分割）
                        if (_config.SplitByDate && DateTime.Today != _currentLogDate)
                        {
                            await RotateLogFilesAsync();
                        }

                        // 批量写入
                        while (_logQueue.TryDequeue(out var entry))
                        {
                            await WriteEntryAsync(entry);
                        }

                        // 刷新
                        if (_hexWriter != null) await _hexWriter.FlushAsync();
                        if (_smlWriter != null) await _smlWriter.FlushAsync();

                        // 检查文件大小
                        await CheckFileSizeAsync();
                    }
                    finally
                    {
                        _writeSemaphore.Release();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // 忽略写入错误，继续运行
                }
            }
        }

        /// <summary>
        /// 写入单条日志
        /// </summary>
        private async Task WriteEntryAsync(LogEntry entry)
        {
            if (_config.LogHex && _hexWriter != null && !string.IsNullOrEmpty(entry.HexContent))
            {
                await _hexWriter.WriteAsync(entry.HexContent);
            }

            if (_config.LogSml && _smlWriter != null && !string.IsNullOrEmpty(entry.SmlContent))
            {
                await _smlWriter.WriteAsync(entry.SmlContent);
            }
        }

        /// <summary>
        /// 打开日志文件
        /// </summary>
        private async Task OpenLogFilesAsync()
        {
            if (string.IsNullOrEmpty(_logDirectory)) return;

            var now = DateTime.Now;

            if (_config.LogHex)
            {
                var hexFileName = string.Format(_config.HexFileNameFormat, now);
                var hexFilePath = Path.Combine(_logDirectory, hexFileName);
                _hexWriter = new StreamWriter(hexFilePath, append: true, Encoding.UTF8)
                {
                    AutoFlush = false
                };

                // 写入文件头
                await _hexWriter.WriteLineAsync($"=== SECS/GEM Message Log (HEX) ===");
                await _hexWriter.WriteLineAsync($"=== Started at {now:yyyy-MM-dd HH:mm:ss} ===");
                await _hexWriter.WriteLineAsync();
            }

            if (_config.LogSml)
            {
                var smlFileName = string.Format(_config.SmlFileNameFormat, now);
                var smlFilePath = Path.Combine(_logDirectory, smlFileName);
                _smlWriter = new StreamWriter(smlFilePath, append: true, Encoding.UTF8)
                {
                    AutoFlush = false
                };

                // 写入文件头
                await _smlWriter.WriteLineAsync($"=== SECS/GEM Message Log (SML) ===");
                await _smlWriter.WriteLineAsync($"=== Started at {now:yyyy-MM-dd HH:mm:ss} ===");
                await _smlWriter.WriteLineAsync();
            }
        }

        /// <summary>
        /// 轮换日志文件
        /// </summary>
        private async Task RotateLogFilesAsync()
        {
            // 关闭当前文件
            if (_hexWriter != null)
            {
                await _hexWriter.DisposeAsync();
                _hexWriter = null;
            }

            if (_smlWriter != null)
            {
                await _smlWriter.DisposeAsync();
                _smlWriter = null;
            }

            // 更新日期
            _currentLogDate = DateTime.Today;

            // 打开新文件
            await OpenLogFilesAsync();
        }

        /// <summary>
        /// 检查文件大小，超过限制则轮换
        /// </summary>
        private async Task CheckFileSizeAsync()
        {
            var maxSize = _config.MaxFileSizeMB * 1024 * 1024L;
            var needRotate = false;

            if (_hexWriter?.BaseStream.Length > maxSize)
            {
                needRotate = true;
            }

            if (_smlWriter?.BaseStream.Length > maxSize)
            {
                needRotate = true;
            }

            if (needRotate)
            {
                // 关闭当前文件
                if (_hexWriter != null)
                {
                    await _hexWriter.DisposeAsync();
                    _hexWriter = null;
                }

                if (_smlWriter != null)
                {
                    await _smlWriter.DisposeAsync();
                    _smlWriter = null;
                }

                // 重命名旧文件（添加时间后缀）
                var suffix = DateTime.Now.ToString("HHmmss");
                if (string.IsNullOrEmpty(_logDirectory)) return;

                var hexFileName = string.Format(_config.HexFileNameFormat, DateTime.Now);
                var smlFileName = string.Format(_config.SmlFileNameFormat, DateTime.Now);

                var hexFilePath = Path.Combine(_logDirectory, hexFileName);
                var smlFilePath = Path.Combine(_logDirectory, smlFileName);

                if (File.Exists(hexFilePath))
                {
                    var newName = Path.Combine(_logDirectory, 
                        Path.GetFileNameWithoutExtension(hexFileName) + $"_{suffix}.hex");
                    File.Move(hexFilePath, newName);
                }

                if (File.Exists(smlFilePath))
                {
                    var newName = Path.Combine(_logDirectory,
                        Path.GetFileNameWithoutExtension(smlFileName) + $"_{suffix}.sml");
                    File.Move(smlFilePath, newName);
                }

                // 打开新文件
                await OpenLogFilesAsync();
            }
        }

        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        private Task CleanupOldLogsAsync()
        {
            if (string.IsNullOrEmpty(_logDirectory)) return Task.CompletedTask;

            var cutoffDate = DateTime.Now.AddDays(-_config.RetentionDays);

            foreach (var file in Directory.GetFiles(_logDirectory, "*.hex")
                .Concat(Directory.GetFiles(_logDirectory, "*.sml")))
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        fileInfo.Delete();
                    }
                }
                catch
                {
                    // 忽略删除失败
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            // 停止后台任务
            _cts?.Cancel();
            
            if (_writeTask != null)
            {
                try
                {
                    await _writeTask;
                }
                catch
                {
                    // 忽略
                }
            }

            // 刷新并关闭文件
            await FlushAsync();

            if (_hexWriter != null)
            {
                await _hexWriter.DisposeAsync();
            }

            if (_smlWriter != null)
            {
                await _smlWriter.DisposeAsync();
            }

            _writeSemaphore.Dispose();
            _cts?.Dispose();
        }
    }
}
