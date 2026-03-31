namespace SECS2GEM.Simulator
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            
            // 主布局面板
            this.mainTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.topPanel = new System.Windows.Forms.Panel();
            this.bottomPanel = new System.Windows.Forms.Panel();
            
            // 连接设置组
            this.grpConnection = new System.Windows.Forms.GroupBox();
            this.lblIpAddress = new System.Windows.Forms.Label();
            this.txtIpAddress = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.lblDeviceId = new System.Windows.Forms.Label();
            this.txtDeviceId = new System.Windows.Forms.TextBox();
            this.lblConnectionMode = new System.Windows.Forms.Label();
            this.cmbConnectionMode = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatusValue = new System.Windows.Forms.Label();
            this.lblT3Timeout = new System.Windows.Forms.Label();
            this.txtT3Timeout = new System.Windows.Forms.TextBox();
            this.lblT5Timeout = new System.Windows.Forms.Label();
            this.txtT5Timeout = new System.Windows.Forms.TextBox();
            this.lblT6Timeout = new System.Windows.Forms.Label();
            this.txtT6Timeout = new System.Windows.Forms.TextBox();
            this.lblT7Timeout = new System.Windows.Forms.Label();
            this.txtT7Timeout = new System.Windows.Forms.TextBox();
            this.lblT8Timeout = new System.Windows.Forms.Label();
            this.txtT8Timeout = new System.Windows.Forms.TextBox();
            this.lblLinktestInterval = new System.Windows.Forms.Label();
            this.txtLinktestInterval = new System.Windows.Forms.TextBox();

            // Stream 1 消息组
            this.grpStream1 = new System.Windows.Forms.GroupBox();
            this.btnS1F1 = new System.Windows.Forms.Button();
            this.btnS1F3 = new System.Windows.Forms.Button();
            this.btnS1F11 = new System.Windows.Forms.Button();
            this.btnS1F13 = new System.Windows.Forms.Button();
            this.btnS1F15 = new System.Windows.Forms.Button();
            this.btnS1F17 = new System.Windows.Forms.Button();

            // Stream 2 消息组
            this.grpStream2 = new System.Windows.Forms.GroupBox();
            this.btnS2F13 = new System.Windows.Forms.Button();
            this.btnS2F15 = new System.Windows.Forms.Button();
            this.btnS2F17 = new System.Windows.Forms.Button();
            this.btnS2F29 = new System.Windows.Forms.Button();
            this.btnS2F31 = new System.Windows.Forms.Button();
            this.btnS2F33 = new System.Windows.Forms.Button();
            this.btnS2F35 = new System.Windows.Forms.Button();
            this.btnS2F37 = new System.Windows.Forms.Button();
            this.btnS2F41 = new System.Windows.Forms.Button();

            // 其他Stream消息组
            this.grpOtherStreams = new System.Windows.Forms.GroupBox();
            this.btnS5F1 = new System.Windows.Forms.Button();
            this.btnS5F3 = new System.Windows.Forms.Button();
            this.btnS5F5 = new System.Windows.Forms.Button();
            this.btnS6F11 = new System.Windows.Forms.Button();
            this.btnS6F15 = new System.Windows.Forms.Button();
            this.btnS7F1 = new System.Windows.Forms.Button();
            this.btnS7F3 = new System.Windows.Forms.Button();
            this.btnS7F5 = new System.Windows.Forms.Button();
            this.btnS7F17 = new System.Windows.Forms.Button();
            this.btnS7F19 = new System.Windows.Forms.Button();
            this.btnS10F1 = new System.Windows.Forms.Button();
            this.btnS10F3 = new System.Windows.Forms.Button();

            // 日志组
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.btnClearLog = new System.Windows.Forms.Button();

            // 状态栏
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();

            this.SuspendLayout();
            this.mainTableLayout.SuspendLayout();
            this.topPanel.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.grpConnection.SuspendLayout();
            this.grpStream1.SuspendLayout();
            this.grpStream2.SuspendLayout();
            this.grpOtherStreams.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.statusStrip.SuspendLayout();

            // 
            // mainTableLayout
            // 
            this.mainTableLayout.ColumnCount = 1;
            this.mainTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayout.RowCount = 2;
            this.mainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.mainTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayout.Controls.Add(this.topPanel, 0, 0);
            this.mainTableLayout.Controls.Add(this.bottomPanel, 0, 1);
            this.mainTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayout.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayout.Name = "mainTableLayout";
            this.mainTableLayout.Size = new System.Drawing.Size(1200, 700);
            this.mainTableLayout.TabIndex = 0;

            // 
            // topPanel
            // 
            this.topPanel.Controls.Add(this.grpConnection);
            this.topPanel.Controls.Add(this.grpStream1);
            this.topPanel.Controls.Add(this.grpStream2);
            this.topPanel.Controls.Add(this.grpOtherStreams);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanel.Location = new System.Drawing.Point(3, 3);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(1194, 224);
            this.topPanel.TabIndex = 0;

            // 
            // bottomPanel
            // 
            this.bottomPanel.Controls.Add(this.grpLog);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomPanel.Location = new System.Drawing.Point(3, 233);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(1194, 464);
            this.bottomPanel.TabIndex = 1;

            // 
            // grpConnection - 连接设置
            // 
            this.grpConnection.Controls.Add(this.lblIpAddress);
            this.grpConnection.Controls.Add(this.txtIpAddress);
            this.grpConnection.Controls.Add(this.lblPort);
            this.grpConnection.Controls.Add(this.txtPort);
            this.grpConnection.Controls.Add(this.lblDeviceId);
            this.grpConnection.Controls.Add(this.txtDeviceId);
            this.grpConnection.Controls.Add(this.lblConnectionMode);
            this.grpConnection.Controls.Add(this.cmbConnectionMode);
            this.grpConnection.Controls.Add(this.lblT3Timeout);
            this.grpConnection.Controls.Add(this.txtT3Timeout);
            this.grpConnection.Controls.Add(this.lblT5Timeout);
            this.grpConnection.Controls.Add(this.txtT5Timeout);
            this.grpConnection.Controls.Add(this.lblT6Timeout);
            this.grpConnection.Controls.Add(this.txtT6Timeout);
            this.grpConnection.Controls.Add(this.lblT7Timeout);
            this.grpConnection.Controls.Add(this.txtT7Timeout);
            this.grpConnection.Controls.Add(this.lblT8Timeout);
            this.grpConnection.Controls.Add(this.txtT8Timeout);
            this.grpConnection.Controls.Add(this.lblLinktestInterval);
            this.grpConnection.Controls.Add(this.txtLinktestInterval);
            this.grpConnection.Controls.Add(this.btnConnect);
            this.grpConnection.Controls.Add(this.btnDisconnect);
            this.grpConnection.Controls.Add(this.lblStatus);
            this.grpConnection.Controls.Add(this.lblStatusValue);
            this.grpConnection.Location = new System.Drawing.Point(10, 5);
            this.grpConnection.Name = "grpConnection";
            this.grpConnection.Size = new System.Drawing.Size(320, 215);
            this.grpConnection.TabIndex = 0;
            this.grpConnection.TabStop = false;
            this.grpConnection.Text = "连接设置";

            // lblIpAddress
            this.lblIpAddress.AutoSize = true;
            this.lblIpAddress.Location = new System.Drawing.Point(10, 25);
            this.lblIpAddress.Name = "lblIpAddress";
            this.lblIpAddress.Size = new System.Drawing.Size(55, 17);
            this.lblIpAddress.Text = "IP地址:";

            // txtIpAddress
            this.txtIpAddress.Location = new System.Drawing.Point(80, 22);
            this.txtIpAddress.Name = "txtIpAddress";
            this.txtIpAddress.Size = new System.Drawing.Size(110, 23);
            this.txtIpAddress.Text = "127.0.0.1";

            // lblPort
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(200, 25);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(40, 17);
            this.lblPort.Text = "端口:";

            // txtPort
            this.txtPort.Location = new System.Drawing.Point(245, 22);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(60, 23);
            this.txtPort.Text = "5000";

            // lblDeviceId
            this.lblDeviceId.AutoSize = true;
            this.lblDeviceId.Location = new System.Drawing.Point(10, 55);
            this.lblDeviceId.Name = "lblDeviceId";
            this.lblDeviceId.Size = new System.Drawing.Size(65, 17);
            this.lblDeviceId.Text = "DeviceID:";

            // txtDeviceId
            this.txtDeviceId.Location = new System.Drawing.Point(80, 52);
            this.txtDeviceId.Name = "txtDeviceId";
            this.txtDeviceId.Size = new System.Drawing.Size(60, 23);
            this.txtDeviceId.Text = "0";

            // lblConnectionMode
            this.lblConnectionMode.AutoSize = true;
            this.lblConnectionMode.Location = new System.Drawing.Point(150, 55);
            this.lblConnectionMode.Name = "lblConnectionMode";
            this.lblConnectionMode.Size = new System.Drawing.Size(40, 17);
            this.lblConnectionMode.Text = "模式:";

            // cmbConnectionMode
            this.cmbConnectionMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbConnectionMode.Items.AddRange(new object[] { "Passive (被动)", "Active (主动)" });
            this.cmbConnectionMode.Location = new System.Drawing.Point(195, 52);
            this.cmbConnectionMode.Name = "cmbConnectionMode";
            this.cmbConnectionMode.Size = new System.Drawing.Size(110, 25);
            this.cmbConnectionMode.SelectedIndex = 0;

            // T3 Timeout
            this.lblT3Timeout.AutoSize = true;
            this.lblT3Timeout.Location = new System.Drawing.Point(10, 85);
            this.lblT3Timeout.Name = "lblT3Timeout";
            this.lblT3Timeout.Size = new System.Drawing.Size(25, 17);
            this.lblT3Timeout.Text = "T3:";

            this.txtT3Timeout.Location = new System.Drawing.Point(35, 82);
            this.txtT3Timeout.Name = "txtT3Timeout";
            this.txtT3Timeout.Size = new System.Drawing.Size(40, 23);
            this.txtT3Timeout.Text = "45";

            // T5 Timeout
            this.lblT5Timeout.AutoSize = true;
            this.lblT5Timeout.Location = new System.Drawing.Point(80, 85);
            this.lblT5Timeout.Name = "lblT5Timeout";
            this.lblT5Timeout.Size = new System.Drawing.Size(25, 17);
            this.lblT5Timeout.Text = "T5:";

            this.txtT5Timeout.Location = new System.Drawing.Point(105, 82);
            this.txtT5Timeout.Name = "txtT5Timeout";
            this.txtT5Timeout.Size = new System.Drawing.Size(40, 23);
            this.txtT5Timeout.Text = "10";

            // T6 Timeout
            this.lblT6Timeout.AutoSize = true;
            this.lblT6Timeout.Location = new System.Drawing.Point(150, 85);
            this.lblT6Timeout.Name = "lblT6Timeout";
            this.lblT6Timeout.Size = new System.Drawing.Size(25, 17);
            this.lblT6Timeout.Text = "T6:";

            this.txtT6Timeout.Location = new System.Drawing.Point(175, 82);
            this.txtT6Timeout.Name = "txtT6Timeout";
            this.txtT6Timeout.Size = new System.Drawing.Size(40, 23);
            this.txtT6Timeout.Text = "5";

            // T7 Timeout
            this.lblT7Timeout.AutoSize = true;
            this.lblT7Timeout.Location = new System.Drawing.Point(220, 85);
            this.lblT7Timeout.Name = "lblT7Timeout";
            this.lblT7Timeout.Size = new System.Drawing.Size(25, 17);
            this.lblT7Timeout.Text = "T7:";

            this.txtT7Timeout.Location = new System.Drawing.Point(245, 82);
            this.txtT7Timeout.Name = "txtT7Timeout";
            this.txtT7Timeout.Size = new System.Drawing.Size(40, 23);
            this.txtT7Timeout.Text = "10";

            // T8 Timeout
            this.lblT8Timeout.AutoSize = true;
            this.lblT8Timeout.Location = new System.Drawing.Point(10, 115);
            this.lblT8Timeout.Name = "lblT8Timeout";
            this.lblT8Timeout.Size = new System.Drawing.Size(25, 17);
            this.lblT8Timeout.Text = "T8:";

            this.txtT8Timeout.Location = new System.Drawing.Point(35, 112);
            this.txtT8Timeout.Name = "txtT8Timeout";
            this.txtT8Timeout.Size = new System.Drawing.Size(40, 23);
            this.txtT8Timeout.Text = "5";

            // Linktest Interval
            this.lblLinktestInterval.AutoSize = true;
            this.lblLinktestInterval.Location = new System.Drawing.Point(80, 115);
            this.lblLinktestInterval.Name = "lblLinktestInterval";
            this.lblLinktestInterval.Size = new System.Drawing.Size(55, 17);
            this.lblLinktestInterval.Text = "心跳(秒):";

            this.txtLinktestInterval.Location = new System.Drawing.Point(145, 112);
            this.txtLinktestInterval.Name = "txtLinktestInterval";
            this.txtLinktestInterval.Size = new System.Drawing.Size(40, 23);
            this.txtLinktestInterval.Text = "0";

            // btnConnect
            this.btnConnect.Location = new System.Drawing.Point(10, 145);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(90, 30);
            this.btnConnect.TabIndex = 10;
            this.btnConnect.Text = "连接";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);

            // btnDisconnect
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(110, 145);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(90, 30);
            this.btnDisconnect.TabIndex = 11;
            this.btnDisconnect.Text = "断开";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(10, 185);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(40, 17);
            this.lblStatus.Text = "状态:";

            // lblStatusValue
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.ForeColor = System.Drawing.Color.Red;
            this.lblStatusValue.Location = new System.Drawing.Point(55, 185);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new System.Drawing.Size(50, 17);
            this.lblStatusValue.Text = "未连接";

            // 
            // grpStream1 - Stream 1 消息
            // 
            this.grpStream1.Controls.Add(this.btnS1F1);
            this.grpStream1.Controls.Add(this.btnS1F3);
            this.grpStream1.Controls.Add(this.btnS1F11);
            this.grpStream1.Controls.Add(this.btnS1F13);
            this.grpStream1.Controls.Add(this.btnS1F15);
            this.grpStream1.Controls.Add(this.btnS1F17);
            this.grpStream1.Location = new System.Drawing.Point(340, 5);
            this.grpStream1.Name = "grpStream1";
            this.grpStream1.Size = new System.Drawing.Size(260, 105);
            this.grpStream1.TabIndex = 1;
            this.grpStream1.TabStop = false;
            this.grpStream1.Text = "Stream 1 - 设备状态";

            // btnS1F1
            this.btnS1F1.Location = new System.Drawing.Point(10, 25);
            this.btnS1F1.Name = "btnS1F1";
            this.btnS1F1.Size = new System.Drawing.Size(75, 30);
            this.btnS1F1.Text = "S1F1";
            this.btnS1F1.Click += new System.EventHandler(this.btnS1F1_Click);

            // btnS1F3
            this.btnS1F3.Location = new System.Drawing.Point(90, 25);
            this.btnS1F3.Name = "btnS1F3";
            this.btnS1F3.Size = new System.Drawing.Size(75, 30);
            this.btnS1F3.Text = "S1F3";
            this.btnS1F3.Click += new System.EventHandler(this.btnS1F3_Click);

            // btnS1F11
            this.btnS1F11.Location = new System.Drawing.Point(170, 25);
            this.btnS1F11.Name = "btnS1F11";
            this.btnS1F11.Size = new System.Drawing.Size(75, 30);
            this.btnS1F11.Text = "S1F11";
            this.btnS1F11.Click += new System.EventHandler(this.btnS1F11_Click);

            // btnS1F13
            this.btnS1F13.Location = new System.Drawing.Point(10, 60);
            this.btnS1F13.Name = "btnS1F13";
            this.btnS1F13.Size = new System.Drawing.Size(75, 30);
            this.btnS1F13.Text = "S1F13";
            this.btnS1F13.Click += new System.EventHandler(this.btnS1F13_Click);

            // btnS1F15
            this.btnS1F15.Location = new System.Drawing.Point(90, 60);
            this.btnS1F15.Name = "btnS1F15";
            this.btnS1F15.Size = new System.Drawing.Size(75, 30);
            this.btnS1F15.Text = "S1F15";
            this.btnS1F15.Click += new System.EventHandler(this.btnS1F15_Click);

            // btnS1F17
            this.btnS1F17.Location = new System.Drawing.Point(170, 60);
            this.btnS1F17.Name = "btnS1F17";
            this.btnS1F17.Size = new System.Drawing.Size(75, 30);
            this.btnS1F17.Text = "S1F17";
            this.btnS1F17.Click += new System.EventHandler(this.btnS1F17_Click);

            // 
            // grpStream2 - Stream 2 消息
            // 
            this.grpStream2.Controls.Add(this.btnS2F13);
            this.grpStream2.Controls.Add(this.btnS2F15);
            this.grpStream2.Controls.Add(this.btnS2F17);
            this.grpStream2.Controls.Add(this.btnS2F29);
            this.grpStream2.Controls.Add(this.btnS2F31);
            this.grpStream2.Controls.Add(this.btnS2F33);
            this.grpStream2.Controls.Add(this.btnS2F35);
            this.grpStream2.Controls.Add(this.btnS2F37);
            this.grpStream2.Controls.Add(this.btnS2F41);
            this.grpStream2.Location = new System.Drawing.Point(610, 5);
            this.grpStream2.Name = "grpStream2";
            this.grpStream2.Size = new System.Drawing.Size(340, 105);
            this.grpStream2.TabIndex = 2;
            this.grpStream2.TabStop = false;
            this.grpStream2.Text = "Stream 2 - 设备控制";

            // btnS2F13
            this.btnS2F13.Location = new System.Drawing.Point(10, 25);
            this.btnS2F13.Name = "btnS2F13";
            this.btnS2F13.Size = new System.Drawing.Size(70, 30);
            this.btnS2F13.Text = "S2F13";
            this.btnS2F13.Click += new System.EventHandler(this.btnS2F13_Click);

            // btnS2F15
            this.btnS2F15.Location = new System.Drawing.Point(85, 25);
            this.btnS2F15.Name = "btnS2F15";
            this.btnS2F15.Size = new System.Drawing.Size(70, 30);
            this.btnS2F15.Text = "S2F15";
            this.btnS2F15.Click += new System.EventHandler(this.btnS2F15_Click);

            // btnS2F17
            this.btnS2F17.Location = new System.Drawing.Point(160, 25);
            this.btnS2F17.Name = "btnS2F17";
            this.btnS2F17.Size = new System.Drawing.Size(70, 30);
            this.btnS2F17.Text = "S2F17";
            this.btnS2F17.Click += new System.EventHandler(this.btnS2F17_Click);

            // btnS2F29
            this.btnS2F29.Location = new System.Drawing.Point(235, 25);
            this.btnS2F29.Name = "btnS2F29";
            this.btnS2F29.Size = new System.Drawing.Size(70, 30);
            this.btnS2F29.Text = "S2F29";
            this.btnS2F29.Click += new System.EventHandler(this.btnS2F29_Click);

            // btnS2F31
            this.btnS2F31.Location = new System.Drawing.Point(10, 60);
            this.btnS2F31.Name = "btnS2F31";
            this.btnS2F31.Size = new System.Drawing.Size(70, 30);
            this.btnS2F31.Text = "S2F31";
            this.btnS2F31.Click += new System.EventHandler(this.btnS2F31_Click);

            // btnS2F33
            this.btnS2F33.Location = new System.Drawing.Point(85, 60);
            this.btnS2F33.Name = "btnS2F33";
            this.btnS2F33.Size = new System.Drawing.Size(70, 30);
            this.btnS2F33.Text = "S2F33";
            this.btnS2F33.Click += new System.EventHandler(this.btnS2F33_Click);

            // btnS2F35
            this.btnS2F35.Location = new System.Drawing.Point(160, 60);
            this.btnS2F35.Name = "btnS2F35";
            this.btnS2F35.Size = new System.Drawing.Size(70, 30);
            this.btnS2F35.Text = "S2F35";
            this.btnS2F35.Click += new System.EventHandler(this.btnS2F35_Click);

            // btnS2F37
            this.btnS2F37.Location = new System.Drawing.Point(235, 60);
            this.btnS2F37.Name = "btnS2F37";
            this.btnS2F37.Size = new System.Drawing.Size(70, 30);
            this.btnS2F37.Text = "S2F37";
            this.btnS2F37.Click += new System.EventHandler(this.btnS2F37_Click);

            // btnS2F41
            this.btnS2F41.Location = new System.Drawing.Point(310, 25);
            this.btnS2F41.Name = "btnS2F41";
            this.btnS2F41.Size = new System.Drawing.Size(20, 65);
            this.btnS2F41.Text = "41";
            this.btnS2F41.Click += new System.EventHandler(this.btnS2F41_Click);

            // 
            // grpOtherStreams - 其他Stream消息
            // 
            this.grpOtherStreams.Controls.Add(this.btnS5F1);
            this.grpOtherStreams.Controls.Add(this.btnS5F3);
            this.grpOtherStreams.Controls.Add(this.btnS5F5);
            this.grpOtherStreams.Controls.Add(this.btnS6F11);
            this.grpOtherStreams.Controls.Add(this.btnS6F15);
            this.grpOtherStreams.Controls.Add(this.btnS7F1);
            this.grpOtherStreams.Controls.Add(this.btnS7F3);
            this.grpOtherStreams.Controls.Add(this.btnS7F5);
            this.grpOtherStreams.Controls.Add(this.btnS7F17);
            this.grpOtherStreams.Controls.Add(this.btnS7F19);
            this.grpOtherStreams.Controls.Add(this.btnS10F1);
            this.grpOtherStreams.Controls.Add(this.btnS10F3);
            this.grpOtherStreams.Location = new System.Drawing.Point(340, 115);
            this.grpOtherStreams.Name = "grpOtherStreams";
            this.grpOtherStreams.Size = new System.Drawing.Size(610, 105);
            this.grpOtherStreams.TabIndex = 3;
            this.grpOtherStreams.TabStop = false;
            this.grpOtherStreams.Text = "其他Stream - 告警/事件/程序/终端";

            // btnS5F1
            this.btnS5F1.Location = new System.Drawing.Point(10, 25);
            this.btnS5F1.Name = "btnS5F1";
            this.btnS5F1.Size = new System.Drawing.Size(70, 30);
            this.btnS5F1.Text = "S5F1";
            this.btnS5F1.Click += new System.EventHandler(this.btnS5F1_Click);

            // btnS5F3
            this.btnS5F3.Location = new System.Drawing.Point(85, 25);
            this.btnS5F3.Name = "btnS5F3";
            this.btnS5F3.Size = new System.Drawing.Size(70, 30);
            this.btnS5F3.Text = "S5F3";
            this.btnS5F3.Click += new System.EventHandler(this.btnS5F3_Click);

            // btnS5F5
            this.btnS5F5.Location = new System.Drawing.Point(160, 25);
            this.btnS5F5.Name = "btnS5F5";
            this.btnS5F5.Size = new System.Drawing.Size(70, 30);
            this.btnS5F5.Text = "S5F5";
            this.btnS5F5.Click += new System.EventHandler(this.btnS5F5_Click);

            // btnS6F11
            this.btnS6F11.Location = new System.Drawing.Point(235, 25);
            this.btnS6F11.Name = "btnS6F11";
            this.btnS6F11.Size = new System.Drawing.Size(70, 30);
            this.btnS6F11.Text = "S6F11";
            this.btnS6F11.Click += new System.EventHandler(this.btnS6F11_Click);

            // btnS6F15
            this.btnS6F15.Location = new System.Drawing.Point(310, 25);
            this.btnS6F15.Name = "btnS6F15";
            this.btnS6F15.Size = new System.Drawing.Size(70, 30);
            this.btnS6F15.Text = "S6F15";
            this.btnS6F15.Click += new System.EventHandler(this.btnS6F15_Click);

            // btnS7F1
            this.btnS7F1.Location = new System.Drawing.Point(385, 25);
            this.btnS7F1.Name = "btnS7F1";
            this.btnS7F1.Size = new System.Drawing.Size(70, 30);
            this.btnS7F1.Text = "S7F1";
            this.btnS7F1.Click += new System.EventHandler(this.btnS7F1_Click);

            // btnS7F3
            this.btnS7F3.Location = new System.Drawing.Point(460, 25);
            this.btnS7F3.Name = "btnS7F3";
            this.btnS7F3.Size = new System.Drawing.Size(70, 30);
            this.btnS7F3.Text = "S7F3";
            this.btnS7F3.Click += new System.EventHandler(this.btnS7F3_Click);

            // btnS7F5
            this.btnS7F5.Location = new System.Drawing.Point(535, 25);
            this.btnS7F5.Name = "btnS7F5";
            this.btnS7F5.Size = new System.Drawing.Size(70, 30);
            this.btnS7F5.Text = "S7F5";
            this.btnS7F5.Click += new System.EventHandler(this.btnS7F5_Click);

            // btnS7F17
            this.btnS7F17.Location = new System.Drawing.Point(10, 60);
            this.btnS7F17.Name = "btnS7F17";
            this.btnS7F17.Size = new System.Drawing.Size(70, 30);
            this.btnS7F17.Text = "S7F17";
            this.btnS7F17.Click += new System.EventHandler(this.btnS7F17_Click);

            // btnS7F19
            this.btnS7F19.Location = new System.Drawing.Point(85, 60);
            this.btnS7F19.Name = "btnS7F19";
            this.btnS7F19.Size = new System.Drawing.Size(70, 30);
            this.btnS7F19.Text = "S7F19";
            this.btnS7F19.Click += new System.EventHandler(this.btnS7F19_Click);

            // btnS10F1
            this.btnS10F1.Location = new System.Drawing.Point(160, 60);
            this.btnS10F1.Name = "btnS10F1";
            this.btnS10F1.Size = new System.Drawing.Size(70, 30);
            this.btnS10F1.Text = "S10F1";
            this.btnS10F1.Click += new System.EventHandler(this.btnS10F1_Click);

            // btnS10F3
            this.btnS10F3.Location = new System.Drawing.Point(235, 60);
            this.btnS10F3.Name = "btnS10F3";
            this.btnS10F3.Size = new System.Drawing.Size(70, 30);
            this.btnS10F3.Text = "S10F3";
            this.btnS10F3.Click += new System.EventHandler(this.btnS10F3_Click);

            // 
            // grpLog - 日志
            // 
            this.grpLog.Controls.Add(this.rtbLog);
            this.grpLog.Controls.Add(this.btnClearLog);
            this.grpLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLog.Location = new System.Drawing.Point(0, 0);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(1194, 464);
            this.grpLog.TabIndex = 4;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "通信日志";

            // rtbLog
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.BackColor = System.Drawing.Color.Black;
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 10F);
            this.rtbLog.ForeColor = System.Drawing.Color.LimeGreen;
            this.rtbLog.Location = new System.Drawing.Point(10, 25);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(1080, 430);
            this.rtbLog.TabIndex = 0;
            this.rtbLog.Text = "";

            // btnClearLog
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLog.Location = new System.Drawing.Point(1100, 25);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(85, 30);
            this.btnClearLog.Text = "清除日志";
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);

            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.toolStripStatusLabel });
            this.statusStrip.Location = new System.Drawing.Point(0, 678);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip.TabIndex = 5;

            // toolStripStatusLabel
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(200, 17);
            this.toolStripStatusLabel.Text = "SECS/GEM Simulator - 就绪";

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.mainTableLayout);
            this.Controls.Add(this.statusStrip);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SECS/GEM Simulator - HSMS通信测试工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);

            this.mainTableLayout.ResumeLayout(false);
            this.topPanel.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            this.grpConnection.ResumeLayout(false);
            this.grpConnection.PerformLayout();
            this.grpStream1.ResumeLayout(false);
            this.grpStream2.ResumeLayout(false);
            this.grpOtherStreams.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // 布局
        private System.Windows.Forms.TableLayoutPanel mainTableLayout;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Panel bottomPanel;

        // 连接设置组
        private System.Windows.Forms.GroupBox grpConnection;
        private System.Windows.Forms.Label lblIpAddress;
        private System.Windows.Forms.TextBox txtIpAddress;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label lblDeviceId;
        private System.Windows.Forms.TextBox txtDeviceId;
        private System.Windows.Forms.Label lblConnectionMode;
        private System.Windows.Forms.ComboBox cmbConnectionMode;
        private System.Windows.Forms.Label lblT3Timeout;
        private System.Windows.Forms.TextBox txtT3Timeout;
        private System.Windows.Forms.Label lblT5Timeout;
        private System.Windows.Forms.TextBox txtT5Timeout;
        private System.Windows.Forms.Label lblT6Timeout;
        private System.Windows.Forms.TextBox txtT6Timeout;
        private System.Windows.Forms.Label lblT7Timeout;
        private System.Windows.Forms.TextBox txtT7Timeout;
        private System.Windows.Forms.Label lblT8Timeout;
        private System.Windows.Forms.TextBox txtT8Timeout;
        private System.Windows.Forms.Label lblLinktestInterval;
        private System.Windows.Forms.TextBox txtLinktestInterval;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblStatusValue;

        // Stream 1 消息组
        private System.Windows.Forms.GroupBox grpStream1;
        private System.Windows.Forms.Button btnS1F1;
        private System.Windows.Forms.Button btnS1F3;
        private System.Windows.Forms.Button btnS1F11;
        private System.Windows.Forms.Button btnS1F13;
        private System.Windows.Forms.Button btnS1F15;
        private System.Windows.Forms.Button btnS1F17;

        // Stream 2 消息组
        private System.Windows.Forms.GroupBox grpStream2;
        private System.Windows.Forms.Button btnS2F13;
        private System.Windows.Forms.Button btnS2F15;
        private System.Windows.Forms.Button btnS2F17;
        private System.Windows.Forms.Button btnS2F29;
        private System.Windows.Forms.Button btnS2F31;
        private System.Windows.Forms.Button btnS2F33;
        private System.Windows.Forms.Button btnS2F35;
        private System.Windows.Forms.Button btnS2F37;
        private System.Windows.Forms.Button btnS2F41;

        // 其他Stream消息组
        private System.Windows.Forms.GroupBox grpOtherStreams;
        private System.Windows.Forms.Button btnS5F1;
        private System.Windows.Forms.Button btnS5F3;
        private System.Windows.Forms.Button btnS5F5;
        private System.Windows.Forms.Button btnS6F11;
        private System.Windows.Forms.Button btnS6F15;
        private System.Windows.Forms.Button btnS7F1;
        private System.Windows.Forms.Button btnS7F3;
        private System.Windows.Forms.Button btnS7F5;
        private System.Windows.Forms.Button btnS7F17;
        private System.Windows.Forms.Button btnS7F19;
        private System.Windows.Forms.Button btnS10F1;
        private System.Windows.Forms.Button btnS10F3;

        // 日志组
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Button btnClearLog;

        // 状态栏
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
    }
}
