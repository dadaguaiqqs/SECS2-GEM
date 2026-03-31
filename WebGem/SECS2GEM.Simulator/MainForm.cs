using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SECS2GEM.Core.Entities;
using SECS2GEM.Core.Enums;
using SECS2GEM.Domain.Interfaces;
using SECS2GEM.Infrastructure.Configuration;
using SECS2GEM.Infrastructure.Connection;
using SECS2GEM.Infrastructure.Serialization;
using SECS2GEM.Infrastructure.Services;
using SECS2GEM.Application.State;

namespace SECS2GEM.Simulator
{
    /// <summary>
    /// SECS/GEM Simulator 主窗体
    /// 用于测试 HSMS 通信和 SECS-II 消息收发
    /// </summary>
    public partial class MainForm : Form
    {
        private HsmsConnection? _connection;
        private SecsSerializer? _serializer;
        private TransactionManager? _transactionManager;
        private GemStateManager? _stateManager;
        private bool _isConnected = false;

        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            SetMessageButtonsEnabled(false);
        }

        /// <summary>
        /// 初始化服务组件
        /// </summary>
        private void InitializeServices()
        {
            _serializer = new SecsSerializer();
            _transactionManager = new TransactionManager();
            _stateManager = new GemStateManager("SECS2GEM", "1.0.0");

            LogMessage("服务初始化完成", LogLevel.Info);
        }

        /// <summary>
        /// 连接按钮点击事件
        /// </summary>
        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ValidateConnectionSettings())
                {
                    return;
                }

                var config = CreateConfiguration();
                _connection = new HsmsConnection(config, _serializer!, _transactionManager!);

                // 设置GemState
                _connection.SetGemState(_stateManager!);

                // 订阅连接事件
                _connection.PrimaryMessageReceived += OnPrimaryMessageReceived;
                _connection.StateChanged += OnStateChanged;

                LogMessage($"正在连接到 {config.IpAddress}:{config.Port}...", LogLevel.Info);
                UpdateStatus("连接中...", Color.Orange);
                btnConnect.Enabled = false;

                if (config.Mode == HsmsConnectionMode.Active)
                {
                    await _connection.ConnectAsync();
                    // 主动模式：ConnectAsync 成功后状态由 OnStateChanged 更新
                }
                else
                {
                    await _connection.StartListeningAsync();
                    // 被动模式：监听已启动，允许用户点击断开（停止监听）
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    SetConnectionSettingsEnabled(false);
                    LogMessage("等待远程连接...", LogLevel.Info);
                    UpdateStatus("等待连接", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"连接失败: {ex.Message}", LogLevel.Error);
                UpdateStatus("连接失败", Color.Red);
                btnConnect.Enabled = true;
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                }
                _connection = null;
            }
        }

        /// <summary>
        /// 断开连接按钮点击事件
        /// </summary>
        private async void btnDisconnect_Click(object? sender, EventArgs e)
        {
            // 先更新UI状态，防止重复点击
            btnDisconnect.Enabled = false;
            LogMessage("正在断开连接...", LogLevel.Info);
            
            try
            {
                if (_connection != null)
                {
                    // 先取消订阅事件，避免断开过程中触发UI更新
                    _connection.PrimaryMessageReceived -= OnPrimaryMessageReceived;
                    _connection.StateChanged -= OnStateChanged;
                    
                    // 异步断开连接
                    await _connection.DisconnectAsync();
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                _isConnected = false;
                btnConnect.Enabled = true;
                SetMessageButtonsEnabled(false);
                SetConnectionSettingsEnabled(true);

                UpdateStatus("未连接", Color.Red);
                LogMessage("已断开连接", LogLevel.Info);
            }
            catch (Exception ex)
            {
                LogMessage($"断开连接时出错: {ex.Message}", LogLevel.Error);
                // 即使出错也要恢复UI状态
                _isConnected = false;
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                SetMessageButtonsEnabled(false);
                SetConnectionSettingsEnabled(true);
                UpdateStatus("未连接", Color.Red);
            }
        }

        /// <summary>
        /// 验证连接设置
        /// </summary>
        private bool ValidateConnectionSettings()
        {
            if (string.IsNullOrWhiteSpace(txtIpAddress.Text))
            {
                MessageBox.Show("请输入IP地址", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号 (1-65535)", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!ushort.TryParse(txtDeviceId.Text, out _))
            {
                MessageBox.Show("请输入有效的DeviceID (0-65535)", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建HSMS配置
        /// </summary>
        private HsmsConfiguration CreateConfiguration()
        {
            var isActive = cmbConnectionMode.SelectedIndex == 1;
            
            return new HsmsConfiguration
            {
                IpAddress = txtIpAddress.Text.Trim(),
                Port = int.Parse(txtPort.Text),
                DeviceId = ushort.Parse(txtDeviceId.Text),
                Mode = isActive ? HsmsConnectionMode.Active : HsmsConnectionMode.Passive,
                T3 = int.Parse(txtT3Timeout.Text),
                T5 = int.Parse(txtT5Timeout.Text),
                T6 = int.Parse(txtT6Timeout.Text),
                T7 = int.Parse(txtT7Timeout.Text),
                T8 = int.Parse(txtT8Timeout.Text),
                LinktestInterval = int.Parse(txtLinktestInterval.Text),
               
            };
        }

        #region Stream 1 消息处理

        /// <summary>
        /// S1F1 - Are You There Request
        /// </summary>
        private async void btnS1F1_Click(object? sender, EventArgs e)
        {
            await SendMessageAsync(1, 1, true, null, "Are You There Request");
        }

        /// <summary>
        /// S1F3 - Selected Equipment Status Request
        /// </summary>
        private async void btnS1F3_Click(object? sender, EventArgs e)
        {
            // 请求状态变量 (示例：请求SVID 1, 2, 3)
            var svids = SecsItem.L(
               
            );
            await SendMessageAsync(1, 3, true, svids, "Selected Equipment Status Request");
        }

        /// <summary>
        /// S1F11 - Status Variable Namelist Request
        /// </summary>
        private async void btnS1F11_Click(object? sender, EventArgs e)
        {
            // 请求所有状态变量名称（空列表）
            var svids = SecsItem.L();
            await SendMessageAsync(1, 11, true, svids, "Status Variable Namelist Request");
        }

        /// <summary>
        /// S1F13 - Establish Communications Request
        /// </summary>
        private async void btnS1F13_Click(object? sender, EventArgs e)
        {
            // 空列表表示请求建立通信
            var data = SecsItem.L();
            await SendMessageAsync(1, 13, true, data, "Establish Communications Request");
        }

        /// <summary>
        /// S1F15 - Request OFF-LINE
        /// </summary>
        private async void btnS1F15_Click(object? sender, EventArgs e)
        {
            await SendMessageAsync(1, 15, true, null, "Request OFF-LINE");
        }

        /// <summary>
        /// S1F17 - Request ON-LINE
        /// </summary>
        private async void btnS1F17_Click(object? sender, EventArgs e)
        {
            await SendMessageAsync(1, 17, true, null, "Request ON-LINE");
        }

        #endregion

        #region Stream 2 消息处理

        /// <summary>
        /// S2F13 - Equipment Constant Request
        /// </summary>
        private async void btnS2F13_Click(object? sender, EventArgs e)
        {
            // 请求所有设备常量（空列表）
            var ecids = SecsItem.L();
            await SendMessageAsync(2, 13, true, ecids, "Equipment Constant Request");
        }

        /// <summary>
        /// S2F15 - New Equipment Constant Send
        /// </summary>
        private async void btnS2F15_Click(object? sender, EventArgs e)
        {
            // 示例：设置设备常量
            var data = SecsItem.L(
                SecsItem.L(
                    SecsItem.U4(1),   // ECID
                    SecsItem.I4(100)  // ECV
                )
            );
            await SendMessageAsync(2, 15, true, data, "New Equipment Constant Send");
        }

        /// <summary>
        /// S2F17 - Date and Time Request
        /// </summary>
        private async void btnS2F17_Click(object? sender, EventArgs e)
        {
            await SendMessageAsync(2, 17, true, null, "Date and Time Request");
        }

        /// <summary>
        /// S2F29 - Equipment Constant Namelist Request
        /// </summary>
        private async void btnS2F29_Click(object? sender, EventArgs e)
        {
            var ecids = SecsItem.L();
            await SendMessageAsync(2, 29, true, ecids, "Equipment Constant Namelist Request");
        }

        /// <summary>
        /// S2F31 - Date and Time Set Request
        /// </summary>
        private async void btnS2F31_Click(object? sender, EventArgs e)
        {
            var time = SecsItem.A(DateTime.Now.ToString("yyyyMMddHHmmss"));
            await SendMessageAsync(2, 31, true, time, "Date and Time Set Request");
        }

        /// <summary>
        /// S2F33 - Define Report
        /// </summary>
        private async void btnS2F33_Click(object? sender, EventArgs e)
        {
            // 示例：定义报告
            var data = SecsItem.L(
                SecsItem.U4(0),  // DATAID
                SecsItem.L(
                    SecsItem.L(
                        SecsItem.U4(1),  // RPTID
                        SecsItem.L(
                            SecsItem.U4(1),  // VID
                            SecsItem.U4(2)
                        )
                    )
                )
            );
            await SendMessageAsync(2, 33, true, data, "Define Report");
        }

        /// <summary>
        /// S2F35 - Link Event Report
        /// </summary>
        private async void btnS2F35_Click(object? sender, EventArgs e)
        {
            // 示例：链接事件报告
            var data = SecsItem.L(
                SecsItem.U4(0),  // DATAID
                SecsItem.L(
                    SecsItem.L(
                        SecsItem.U4(1),  // CEID
                        SecsItem.L(
                            SecsItem.U4(1)  // RPTID
                        )
                    )
                )
            );
            await SendMessageAsync(2, 35, true, data, "Link Event Report");
        }

        /// <summary>
        /// S2F37 - Enable/Disable Event Report
        /// </summary>
        private async void btnS2F37_Click(object? sender, EventArgs e)
        {
            // 启用所有事件
            var data = SecsItem.L(
                SecsItem.Boolean(true),  // CEED
                SecsItem.L()             // 空列表表示所有事件
            );
            await SendMessageAsync(2, 37, true, data, "Enable Event Report");
        }

        /// <summary>
        /// S2F41 - Host Command Send
        /// </summary>
        private async void btnS2F41_Click(object? sender, EventArgs e)
        {
            // 示例：发送远程命令
            var data = SecsItem.L(
                SecsItem.A("START"),  // RCMD
                SecsItem.L()          // 参数列表
            );
            await SendMessageAsync(2, 41, true, data, "Host Command Send");
        }

        #endregion

        #region 其他Stream消息处理

        /// <summary>
        /// S5F1 - Alarm Report Send
        /// </summary>
        private async void btnS5F1_Click(object? sender, EventArgs e)
        {
            var data = SecsItem.L(
                SecsItem.B(0x80),            // ALCD
                SecsItem.U4(1),              // ALID
                SecsItem.A("Test Alarm")     // ALTX
            );
            await SendMessageAsync(5, 1, true, data, "Alarm Report Send");
        }

        /// <summary>
        /// S5F3 - Enable/Disable Alarm Send
        /// </summary>
        private async void btnS5F3_Click(object? sender, EventArgs e)
        {
            var data = SecsItem.L(
                SecsItem.B(0x80),  // ALED
                SecsItem.U4(1)     // ALID
            );
            await SendMessageAsync(5, 3, true, data, "Enable Alarm Send");
        }

        /// <summary>
        /// S5F5 - List Alarms Request
        /// </summary>
        private async void btnS5F5_Click(object? sender, EventArgs e)
        {
            var alids = SecsItem.L();
            await SendMessageAsync(5, 5, true, alids, "List Alarms Request");
        }

        /// <summary>
        /// S6F11 - Event Report Send
        /// </summary>
        private async void btnS6F11_Click(object? sender, EventArgs e)
        {
            var data = SecsItem.L(
                SecsItem.U4(1),  // DATAID
                SecsItem.U4(1),  // CEID
                SecsItem.L()     // RPT
            );
            await SendMessageAsync(6, 11, true, data, "Event Report Send");
        }

        /// <summary>
        /// S6F15 - Event Report Request
        /// </summary>
        private async void btnS6F15_Click(object? sender, EventArgs e)
        {
            var ceid = SecsItem.U4(1);
            await SendMessageAsync(6, 15, true, ceid, "Event Report Request");
        }

        /// <summary>
        /// S7F1 - Process Program Load Inquire
        /// </summary>
        private async void btnS7F1_Click(object? sender, EventArgs e)
        {
            var data = SecsItem.L(
                SecsItem.A("RECIPE001"),  // PPID
                SecsItem.U4(1024)         // LENGTH
            );
            await SendMessageAsync(7, 1, true, data, "Process Program Load Inquire");
        }

        /// <summary>
        /// S7F3 - Process Program Send
        /// </summary>
        private async void btnS7F3_Click(object? sender, EventArgs e)
        {
            var data = SecsItem.L(
                SecsItem.A("RECIPE001"),                      // PPID
                SecsItem.B(Encoding.ASCII.GetBytes("BODY"))   // PPBODY
            );
            await SendMessageAsync(7, 3, true, data, "Process Program Send");
        }

        /// <summary>
        /// S7F5 - Process Program Request
        /// </summary>
        private async void btnS7F5_Click(object? sender, EventArgs e)
        {
            var ppid = SecsItem.A("RECIPE001");
            await SendMessageAsync(7, 5, true, ppid, "Process Program Request");
        }

        /// <summary>
        /// S7F17 - Delete Process Program Send
        /// </summary>
        private async void btnS7F17_Click(object? sender, EventArgs e)
        {
            var ppids = SecsItem.L(
                SecsItem.A("RECIPE001")
            );
            await SendMessageAsync(7, 17, true, ppids, "Delete Process Program Send");
        }

        /// <summary>
        /// S7F19 - Current EPPD Request
        /// </summary>
        private async void btnS7F19_Click(object? sender, EventArgs e)
        {
            await SendMessageAsync(7, 19, true, null, "Current EPPD Request");
        }

        /// <summary>
        /// S10F1 - Terminal Request
        /// </summary>
        private async void btnS10F1_Click(object? sender, EventArgs e)
        {
            var data = SecsItem.L(
                SecsItem.B(0),               // TID
                SecsItem.A("Test Message")   // TEXT
            );
            await SendMessageAsync(10, 1, true, data, "Terminal Request");
        }

        /// <summary>
        /// S10F3 - Terminal Display Single
        /// </summary>
        private async void btnS10F3_Click(object? sender, EventArgs e)
        {
            var data = SecsItem.L(
                SecsItem.B(0),                 // TID
                SecsItem.A("Display Message")  // TEXT
            );
            await SendMessageAsync(10, 3, true, data, "Terminal Display Single");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 发送SECS消息
        /// </summary>
        private async Task SendMessageAsync(byte stream, byte function, bool wbit, SecsItem? data, string description)
        {
            if (_connection == null || !_isConnected)
            {
                LogMessage("未连接，无法发送消息", LogLevel.Warning);
                return;
            }

            try
            {
                var message = new SecsMessage(stream, function, wbit, data);

                LogMessage($">>> 发送 S{stream}F{function} ({description})", LogLevel.Send);
                if (data != null)
                {
                    LogMessage($"    数据: {FormatSecsItem(data)}", LogLevel.Send);
                }

                var response = await _connection.SendAsync(message);

                if (response != null)
                {
                    LogMessage($"<<< 收到 S{response.Stream}F{response.Function}", LogLevel.Receive);
                    if (response.Item != null)
                    {
                        LogMessage($"    数据: {FormatSecsItem(response.Item)}", LogLevel.Receive);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"发送消息失败: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 格式化SecsItem为可读字符串
        /// </summary>
        private string FormatSecsItem(SecsItem item, int depth = 0)
        {
            var indent = new string(' ', depth * 2);
            var sb = new StringBuilder();

            if (item.Format == SecsFormat.List)
            {
                sb.AppendLine($"{indent}<L [{item.Count}]>");
                foreach (var child in item.Items)
                {
                    sb.Append(FormatSecsItem(child, depth + 1));
                }
                sb.AppendLine($"{indent}>");
            }
            else
            {
                var valueStr = FormatItemValue(item);
                sb.AppendLine($"{indent}{valueStr}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 格式化单个SecsItem的值
        /// </summary>
        private string FormatItemValue(SecsItem item)
        {
            try
            {
                return item.Format switch
                {
                    SecsFormat.ASCII => $"<A \"{item.GetString()}\">",
                    SecsFormat.JIS8 => $"<J \"{item.GetString()}\">",
                    SecsFormat.Unicode => $"<U \"{item.GetString()}\">",
                    SecsFormat.Binary => FormatBinaryValue(item),
                    SecsFormat.Boolean => FormatBooleanValue(item),
                    SecsFormat.I1 or SecsFormat.I2 or SecsFormat.I4 or SecsFormat.I8 => FormatIntegerValue(item),
                    SecsFormat.U1 or SecsFormat.U2 or SecsFormat.U4 or SecsFormat.U8 => FormatUnsignedValue(item),
                    SecsFormat.F4 or SecsFormat.F8 => FormatFloatValue(item),
                    _ => $"<{item.Format}> ?"
                };
            }
            catch
            {
                return $"<{item.Format}> (格式化失败)";
            }
        }

        private string FormatBinaryValue(SecsItem item)
        {
            var bytes = item.GetBytes();
            if (bytes.Length == 0) return "<B>";
            if (bytes.Length <= 16)
            {
                return $"<B 0x{BitConverter.ToString(bytes).Replace("-", " 0x")}>";
            }
            return $"<B [{bytes.Length}] 0x{BitConverter.ToString(bytes.Take(8).ToArray()).Replace("-", " 0x")} ...>";
        }

        private string FormatBooleanValue(SecsItem item)
        {
            var values = item.GetBooleans();
            // 也显示原始字节值，便于调试
            byte[] rawBytes;
            try
            {
                // Boolean 内部存储为 bool[]，转换为字节
                rawBytes = values.Select(b => (byte)(b ? 1 : 0)).ToArray();
            }
            catch
            {
                rawBytes = Array.Empty<byte>();
            }
            
            var rawHex = rawBytes.Length > 0 ? $" [0x{BitConverter.ToString(rawBytes).Replace("-", " 0x")}]" : "";
            
            if (values.Length == 0) return "<BOOLEAN>";
            if (values.Length == 1) return $"<BOOLEAN {(values[0] ? "TRUE" : "FALSE")}>{rawHex}";
            return $"<BOOLEAN [{values.Length}] {string.Join(" ", values.Select(b => b ? "T" : "F"))}>{rawHex}";
        }

        private string FormatIntegerValue(SecsItem item)
        {
            var formatCode = item.Format.ToString();
            var values = item.GetInt64Array();
            if (values.Length == 0) return $"<{formatCode}>";
            if (values.Length == 1) return $"<{formatCode} {values[0]}>";
            return $"<{formatCode} [{values.Length}] {string.Join(" ", values)}>";
        }

        private string FormatUnsignedValue(SecsItem item)
        {
            var formatCode = item.Format.ToString();
            var values = item.GetUInt64Array();
            if (values.Length == 0) return $"<{formatCode}>";
            if (values.Length == 1) return $"<{formatCode} {values[0]}>";
            return $"<{formatCode} [{values.Length}] {string.Join(" ", values)}>";
        }

        private string FormatFloatValue(SecsItem item)
        {
            var formatCode = item.Format.ToString();
            var values = item.GetDoubleArray();
            if (values.Length == 0) return $"<{formatCode}>";
            if (values.Length == 1) return $"<{formatCode} {values[0]:G}>"; 
            return $"<{formatCode} [{values.Length}] {string.Join(" ", values.Select(v => v.ToString("G")))}>";
        }

        /// <summary>
        /// Primary消息接收事件处理
        /// </summary>
        private void OnPrimaryMessageReceived(object? sender, SecsMessageReceivedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnPrimaryMessageReceived(sender, e));
                return;
            }

            var message = e.Message;
            LogMessage($"<<< 收到Primary消息 S{message.Stream}F{message.Function}", LogLevel.Receive);
            if (message.Item != null)
            {
                LogMessage($"    数据: {FormatSecsItem(message.Item)}", LogLevel.Receive);
            }
        }

        /// <summary>
        /// 连接状态变化事件处理
        /// </summary>
        private void OnStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnStateChanged(sender, e));
                return;
            }

            LogMessage($"连接状态变化: {e.OldState} -> {e.NewState}", LogLevel.Info);

            switch (e.NewState)
            {
                case ConnectionState.Connected:
                case ConnectionState.Selected:
                    UpdateStatus(e.NewState == ConnectionState.Selected ? "已选择" : "已连接", Color.Green);
                    _isConnected = true;
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    SetConnectionSettingsEnabled(false);
                    SetMessageButtonsEnabled(e.NewState == ConnectionState.Selected);
                    break;
                case ConnectionState.NotConnected:
                    // 仅当不是正在手动断开时才更新 UI（避免与 btnDisconnect_Click 冲突）
                    if (_connection != null)
                    {
                        UpdateStatus("已断开", Color.Red);
                        _isConnected = false;
                        btnConnect.Enabled = true;
                        btnDisconnect.Enabled = false;
                        SetMessageButtonsEnabled(false);
                        SetConnectionSettingsEnabled(true);
                    }
                    break;
                default:
                    UpdateStatus(e.NewState.ToString(), Color.Orange);
                    break;
            }
        }

        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void UpdateStatus(string status, Color color)
        {
            lblStatusValue.Text = status;
            lblStatusValue.ForeColor = color;
            toolStripStatusLabel.Text = $"SECS/GEM Simulator - {status}";
        }

        /// <summary>
        /// 设置消息按钮启用状态
        /// </summary>
        private void SetMessageButtonsEnabled(bool enabled)
        {
            // Stream 1
            btnS1F1.Enabled = enabled;
            btnS1F3.Enabled = enabled;
            btnS1F11.Enabled = enabled;
            btnS1F13.Enabled = enabled;
            btnS1F15.Enabled = enabled;
            btnS1F17.Enabled = enabled;

            // Stream 2
            btnS2F13.Enabled = enabled;
            btnS2F15.Enabled = enabled;
            btnS2F17.Enabled = enabled;
            btnS2F29.Enabled = enabled;
            btnS2F31.Enabled = enabled;
            btnS2F33.Enabled = enabled;
            btnS2F35.Enabled = enabled;
            btnS2F37.Enabled = enabled;
            btnS2F41.Enabled = enabled;

            // 其他Streams
            btnS5F1.Enabled = enabled;
            btnS5F3.Enabled = enabled;
            btnS5F5.Enabled = enabled;
            btnS6F11.Enabled = enabled;
            btnS6F15.Enabled = enabled;
            btnS7F1.Enabled = enabled;
            btnS7F3.Enabled = enabled;
            btnS7F5.Enabled = enabled;
            btnS7F17.Enabled = enabled;
            btnS7F19.Enabled = enabled;
            btnS10F1.Enabled = enabled;
            btnS10F3.Enabled = enabled;
        }

        /// <summary>
        /// 设置连接设置控件启用状态
        /// </summary>
        private void SetConnectionSettingsEnabled(bool enabled)
        {
            txtIpAddress.Enabled = enabled;
            txtPort.Enabled = enabled;
            txtDeviceId.Enabled = enabled;
            cmbConnectionMode.Enabled = enabled;
            txtT3Timeout.Enabled = enabled;
            txtT5Timeout.Enabled = enabled;
            txtT6Timeout.Enabled = enabled;
            txtT7Timeout.Enabled = enabled;
            txtT8Timeout.Enabled = enabled;
            txtLinktestInterval.Enabled = enabled;
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        private void LogMessage(string message, LogLevel level)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogMessage(message, level));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var color = level switch
            {
                LogLevel.Error => Color.Red,
                LogLevel.Warning => Color.Yellow,
                LogLevel.Success => Color.Lime,
                LogLevel.Send => Color.Cyan,
                LogLevel.Receive => Color.LightGreen,
                _ => Color.White
            };

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = Color.Gray;
            rtbLog.AppendText($"[{timestamp}] ");
            rtbLog.SelectionColor = color;
            rtbLog.AppendText($"{message}\n");
            rtbLog.ScrollToCaret();
        }

        /// <summary>
        /// 清除日志按钮点击事件
        /// </summary>
        private void btnClearLog_Click(object? sender, EventArgs e)
        {
            rtbLog.Clear();
            LogMessage("日志已清除", LogLevel.Info);
        }

        /// <summary>
        /// 窗体关闭事件处理
        /// </summary>
        private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_connection != null && _isConnected)
            {
                try
                {
                    await _connection.DisconnectAsync();
                    await _connection.DisposeAsync();
                }
                catch
                {
                    // 忽略关闭时的错误
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success,
        Send,
        Receive
    }
}
