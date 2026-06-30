namespace STTech.BytesIO.Demo.Modbus
{
    partial class ClientForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panelTop = new System.Windows.Forms.Panel();
            this.cmbProtocol = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.nudSlaveId = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.tbHost = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.gbMonitors = new System.Windows.Forms.GroupBox();
            this.nudHoldingInterval = new System.Windows.Forms.NumericUpDown();
            this.labelHoldingInterval = new System.Windows.Forms.Label();
            this.nudHoldingQty = new System.Windows.Forms.NumericUpDown();
            this.labelHoldingQty = new System.Windows.Forms.Label();
            this.nudHoldingAddr = new System.Windows.Forms.NumericUpDown();
            this.labelHoldingAddr = new System.Windows.Forms.Label();
            this.cbMonitorHolding = new System.Windows.Forms.CheckBox();
            this.nudCoilInterval = new System.Windows.Forms.NumericUpDown();
            this.labelCoilInterval = new System.Windows.Forms.Label();
            this.nudCoilQty = new System.Windows.Forms.NumericUpDown();
            this.labelCoilQty = new System.Windows.Forms.Label();
            this.nudCoilAddr = new System.Windows.Forms.NumericUpDown();
            this.labelCoilAddr = new System.Windows.Forms.Label();
            this.cbMonitorCoils = new System.Windows.Forms.CheckBox();
            this.btnExecute = new System.Windows.Forms.Button();
            this.tbData = new System.Windows.Forms.TextBox();
            this.lblData = new System.Windows.Forms.Label();
            this.nudQuantity = new System.Windows.Forms.NumericUpDown();
            this.lblQuantity = new System.Windows.Forms.Label();
            this.nudAddress = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbFunctionCode = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSlaveId)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.panelMain.SuspendLayout();
            this.gbMonitors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudHoldingInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHoldingQty)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHoldingAddr)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoilInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoilQty)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoilAddr)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudQuantity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAddress)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panelTop, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panelMain, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.rtbLog, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(587, 433);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.cmbProtocol);
            this.panelTop.Controls.Add(this.label7);
            this.panelTop.Controls.Add(this.btnDisconnect);
            this.panelTop.Controls.Add(this.btnConnect);
            this.panelTop.Controls.Add(this.nudSlaveId);
            this.panelTop.Controls.Add(this.label6);
            this.panelTop.Controls.Add(this.nudPort);
            this.panelTop.Controls.Add(this.label2);
            this.panelTop.Controls.Add(this.tbHost);
            this.panelTop.Controls.Add(this.label1);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTop.Location = new System.Drawing.Point(2, 2);
            this.panelTop.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(583, 36);
            this.panelTop.TabIndex = 0;
            // 
            // cmbProtocol
            // 
            this.cmbProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProtocol.FormattingEnabled = true;
            this.cmbProtocol.Items.AddRange(new object[] {
            "RTU",
            "ASCII"});
            this.cmbProtocol.Location = new System.Drawing.Point(293, 9);
            this.cmbProtocol.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cmbProtocol.Name = "cmbProtocol";
            this.cmbProtocol.Size = new System.Drawing.Size(55, 20);
            this.cmbProtocol.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(253, 12);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(41, 12);
            this.label7.TabIndex = 10;
            this.label7.Text = "协议：";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(507, 7);
            this.btnDisconnect.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(60, 23);
            this.btnDisconnect.TabIndex = 5;
            this.btnDisconnect.Text = "断开";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(440, 7);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(60, 23);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "连接";
            this.btnConnect.UseVisualStyleBackColor = true;
            // 
            // nudSlaveId
            // 
            this.nudSlaveId.Location = new System.Drawing.Point(393, 9);
            this.nudSlaveId.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudSlaveId.Maximum = new decimal(new int[] {
            247,
            0,
            0,
            0});
            this.nudSlaveId.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSlaveId.Name = "nudSlaveId";
            this.nudSlaveId.Size = new System.Drawing.Size(40, 21);
            this.nudSlaveId.TabIndex = 10;
            this.nudSlaveId.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(350, 12);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 12);
            this.label6.TabIndex = 9;
            this.label6.Text = "站号：";
            // 
            // nudPort
            // 
            this.nudPort.Location = new System.Drawing.Point(190, 9);
            this.nudPort.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudPort.Name = "nudPort";
            this.nudPort.Size = new System.Drawing.Size(53, 21);
            this.nudPort.TabIndex = 3;
            this.nudPort.Value = new decimal(new int[] {
            502,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(150, 12);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "端口：";
            // 
            // tbHost
            // 
            this.tbHost.Location = new System.Drawing.Point(47, 9);
            this.tbHost.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tbHost.Name = "tbHost";
            this.tbHost.Size = new System.Drawing.Size(95, 21);
            this.tbHost.TabIndex = 0;
            this.tbHost.Text = "127.0.0.1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 12);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "主机：";
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.gbMonitors);
            this.panelMain.Controls.Add(this.btnExecute);
            this.panelMain.Controls.Add(this.tbData);
            this.panelMain.Controls.Add(this.lblData);
            this.panelMain.Controls.Add(this.nudQuantity);
            this.panelMain.Controls.Add(this.lblQuantity);
            this.panelMain.Controls.Add(this.nudAddress);
            this.panelMain.Controls.Add(this.label4);
            this.panelMain.Controls.Add(this.cmbFunctionCode);
            this.panelMain.Controls.Add(this.label3);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Enabled = false;
            this.panelMain.Location = new System.Drawing.Point(2, 42);
            this.panelMain.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(583, 156);
            this.panelMain.TabIndex = 1;
            // 
            // gbMonitors
            // 
            this.gbMonitors.Controls.Add(this.nudHoldingInterval);
            this.gbMonitors.Controls.Add(this.labelHoldingInterval);
            this.gbMonitors.Controls.Add(this.nudHoldingQty);
            this.gbMonitors.Controls.Add(this.labelHoldingQty);
            this.gbMonitors.Controls.Add(this.nudHoldingAddr);
            this.gbMonitors.Controls.Add(this.labelHoldingAddr);
            this.gbMonitors.Controls.Add(this.cbMonitorHolding);
            this.gbMonitors.Controls.Add(this.nudCoilInterval);
            this.gbMonitors.Controls.Add(this.labelCoilInterval);
            this.gbMonitors.Controls.Add(this.nudCoilQty);
            this.gbMonitors.Controls.Add(this.labelCoilQty);
            this.gbMonitors.Controls.Add(this.nudCoilAddr);
            this.gbMonitors.Controls.Add(this.labelCoilAddr);
            this.gbMonitors.Controls.Add(this.cbMonitorCoils);
            this.gbMonitors.Location = new System.Drawing.Point(9, 39);
            this.gbMonitors.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gbMonitors.Name = "gbMonitors";
            this.gbMonitors.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gbMonitors.Size = new System.Drawing.Size(565, 73);
            this.gbMonitors.TabIndex = 12;
            this.gbMonitors.TabStop = false;
            this.gbMonitors.Text = "监视器 (Monitors)";
            // 
            // nudHoldingInterval
            // 
            this.nudHoldingInterval.Location = new System.Drawing.Point(360, 45);
            this.nudHoldingInterval.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudHoldingInterval.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.nudHoldingInterval.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudHoldingInterval.Name = "nudHoldingInterval";
            this.nudHoldingInterval.Size = new System.Drawing.Size(67, 21);
            this.nudHoldingInterval.TabIndex = 13;
            this.nudHoldingInterval.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // labelHoldingInterval
            // 
            this.labelHoldingInterval.AutoSize = true;
            this.labelHoldingInterval.Location = new System.Drawing.Point(293, 48);
            this.labelHoldingInterval.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelHoldingInterval.Name = "labelHoldingInterval";
            this.labelHoldingInterval.Size = new System.Drawing.Size(59, 12);
            this.labelHoldingInterval.TabIndex = 12;
            this.labelHoldingInterval.Text = "间隔(ms):";
            // 
            // nudHoldingQty
            // 
            this.nudHoldingQty.Location = new System.Drawing.Point(227, 45);
            this.nudHoldingQty.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudHoldingQty.Name = "nudHoldingQty";
            this.nudHoldingQty.Size = new System.Drawing.Size(53, 21);
            this.nudHoldingQty.TabIndex = 11;
            this.nudHoldingQty.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // labelHoldingQty
            // 
            this.labelHoldingQty.AutoSize = true;
            this.labelHoldingQty.Location = new System.Drawing.Point(187, 48);
            this.labelHoldingQty.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelHoldingQty.Name = "labelHoldingQty";
            this.labelHoldingQty.Size = new System.Drawing.Size(35, 12);
            this.labelHoldingQty.TabIndex = 10;
            this.labelHoldingQty.Text = "数量:";
            // 
            // nudHoldingAddr
            // 
            this.nudHoldingAddr.Location = new System.Drawing.Point(120, 45);
            this.nudHoldingAddr.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudHoldingAddr.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudHoldingAddr.Name = "nudHoldingAddr";
            this.nudHoldingAddr.Size = new System.Drawing.Size(53, 21);
            this.nudHoldingAddr.TabIndex = 9;
            // 
            // labelHoldingAddr
            // 
            this.labelHoldingAddr.AutoSize = true;
            this.labelHoldingAddr.Location = new System.Drawing.Point(80, 48);
            this.labelHoldingAddr.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelHoldingAddr.Name = "labelHoldingAddr";
            this.labelHoldingAddr.Size = new System.Drawing.Size(35, 12);
            this.labelHoldingAddr.TabIndex = 8;
            this.labelHoldingAddr.Text = "地址:";
            // 
            // cbMonitorHolding
            // 
            this.cbMonitorHolding.AutoSize = true;
            this.cbMonitorHolding.Location = new System.Drawing.Point(7, 47);
            this.cbMonitorHolding.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbMonitorHolding.Name = "cbMonitorHolding";
            this.cbMonitorHolding.Size = new System.Drawing.Size(72, 16);
            this.cbMonitorHolding.TabIndex = 7;
            this.cbMonitorHolding.Text = "保持(03)";
            this.cbMonitorHolding.UseVisualStyleBackColor = true;
            // 
            // nudCoilInterval
            // 
            this.nudCoilInterval.Location = new System.Drawing.Point(360, 19);
            this.nudCoilInterval.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudCoilInterval.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.nudCoilInterval.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudCoilInterval.Name = "nudCoilInterval";
            this.nudCoilInterval.Size = new System.Drawing.Size(67, 21);
            this.nudCoilInterval.TabIndex = 6;
            this.nudCoilInterval.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // labelCoilInterval
            // 
            this.labelCoilInterval.AutoSize = true;
            this.labelCoilInterval.Location = new System.Drawing.Point(293, 21);
            this.labelCoilInterval.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelCoilInterval.Name = "labelCoilInterval";
            this.labelCoilInterval.Size = new System.Drawing.Size(59, 12);
            this.labelCoilInterval.TabIndex = 5;
            this.labelCoilInterval.Text = "间隔(ms):";
            // 
            // nudCoilQty
            // 
            this.nudCoilQty.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudCoilQty.Location = new System.Drawing.Point(227, 19);
            this.nudCoilQty.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudCoilQty.Maximum = new decimal(new int[] {
            800,
            0,
            0,
            0});
            this.nudCoilQty.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudCoilQty.Name = "nudCoilQty";
            this.nudCoilQty.Size = new System.Drawing.Size(53, 21);
            this.nudCoilQty.TabIndex = 4;
            this.nudCoilQty.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // labelCoilQty
            // 
            this.labelCoilQty.AutoSize = true;
            this.labelCoilQty.Location = new System.Drawing.Point(187, 21);
            this.labelCoilQty.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelCoilQty.Name = "labelCoilQty";
            this.labelCoilQty.Size = new System.Drawing.Size(35, 12);
            this.labelCoilQty.TabIndex = 3;
            this.labelCoilQty.Text = "数量:";
            // 
            // nudCoilAddr
            // 
            this.nudCoilAddr.Location = new System.Drawing.Point(120, 19);
            this.nudCoilAddr.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudCoilAddr.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudCoilAddr.Name = "nudCoilAddr";
            this.nudCoilAddr.Size = new System.Drawing.Size(53, 21);
            this.nudCoilAddr.TabIndex = 2;
            // 
            // labelCoilAddr
            // 
            this.labelCoilAddr.AutoSize = true;
            this.labelCoilAddr.Location = new System.Drawing.Point(80, 21);
            this.labelCoilAddr.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelCoilAddr.Name = "labelCoilAddr";
            this.labelCoilAddr.Size = new System.Drawing.Size(35, 12);
            this.labelCoilAddr.TabIndex = 1;
            this.labelCoilAddr.Text = "地址:";
            // 
            // cbMonitorCoils
            // 
            this.cbMonitorCoils.AutoSize = true;
            this.cbMonitorCoils.Location = new System.Drawing.Point(7, 21);
            this.cbMonitorCoils.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbMonitorCoils.Name = "cbMonitorCoils";
            this.cbMonitorCoils.Size = new System.Drawing.Size(72, 16);
            this.cbMonitorCoils.TabIndex = 0;
            this.cbMonitorCoils.Text = "线圈(01)";
            this.cbMonitorCoils.UseVisualStyleBackColor = true;
            // 
            // btnExecute
            // 
            this.btnExecute.Location = new System.Drawing.Point(494, 123);
            this.btnExecute.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(80, 23);
            this.btnExecute.TabIndex = 8;
            this.btnExecute.Text = "发送请求";
            this.btnExecute.UseVisualStyleBackColor = true;
            // 
            // tbData
            // 
            this.tbData.Location = new System.Drawing.Point(106, 127);
            this.tbData.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tbData.Name = "tbData";
            this.tbData.Size = new System.Drawing.Size(378, 21);
            this.tbData.TabIndex = 7;
            // 
            // lblData
            // 
            this.lblData.AutoSize = true;
            this.lblData.Location = new System.Drawing.Point(7, 131);
            this.lblData.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblData.Name = "lblData";
            this.lblData.Size = new System.Drawing.Size(95, 12);
            this.lblData.TabIndex = 6;
            this.lblData.Text = "数据(逗号间隔):";
            // 
            // nudQuantity
            // 
            this.nudQuantity.Location = new System.Drawing.Point(387, 14);
            this.nudQuantity.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudQuantity.Maximum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            this.nudQuantity.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudQuantity.Name = "nudQuantity";
            this.nudQuantity.Size = new System.Drawing.Size(67, 21);
            this.nudQuantity.TabIndex = 5;
            this.nudQuantity.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblQuantity
            // 
            this.lblQuantity.AutoSize = true;
            this.lblQuantity.Location = new System.Drawing.Point(347, 17);
            this.lblQuantity.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblQuantity.Name = "lblQuantity";
            this.lblQuantity.Size = new System.Drawing.Size(41, 12);
            this.lblQuantity.TabIndex = 4;
            this.lblQuantity.Text = "数量：";
            // 
            // nudAddress
            // 
            this.nudAddress.Location = new System.Drawing.Point(267, 14);
            this.nudAddress.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.nudAddress.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudAddress.Name = "nudAddress";
            this.nudAddress.Size = new System.Drawing.Size(67, 21);
            this.nudAddress.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(227, 17);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 2;
            this.label4.Text = "地址：";
            // 
            // cmbFunctionCode
            // 
            this.cmbFunctionCode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFunctionCode.FormattingEnabled = true;
            this.cmbFunctionCode.Items.AddRange(new object[] {
            "01 Read Coils",
            "02 Read Discrete Inputs",
            "03 Read Holding Registers",
            "04 Read Input Registers",
            "05 Write Single Coil",
            "06 Write Single Register",
            "15 Write Multiple Coils",
            "16 Write Multiple Registers"});
            this.cmbFunctionCode.Location = new System.Drawing.Point(67, 15);
            this.cmbFunctionCode.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cmbFunctionCode.Name = "cmbFunctionCode";
            this.cmbFunctionCode.Size = new System.Drawing.Size(148, 20);
            this.cmbFunctionCode.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 17);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 0;
            this.label3.Text = "功能码：";
            // 
            // rtbLog
            // 
            this.rtbLog.BackColor = System.Drawing.Color.Black;
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.ForeColor = System.Drawing.Color.Lime;
            this.rtbLog.Location = new System.Drawing.Point(2, 202);
            this.rtbLog.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(583, 229);
            this.rtbLog.TabIndex = 8;
            this.rtbLog.Text = "";
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(587, 433);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "ClientForm";
            this.Text = "STTech Modbus Client Demo";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSlaveId)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            this.gbMonitors.ResumeLayout(false);
            this.gbMonitors.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudHoldingInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHoldingQty)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHoldingAddr)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoilInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoilQty)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoilAddr)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudQuantity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAddress)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.TextBox tbHost;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbProtocol;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown nudSlaveId;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbFunctionCode;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nudAddress;
        private System.Windows.Forms.Label lblQuantity;
        private System.Windows.Forms.NumericUpDown nudQuantity;
        private System.Windows.Forms.Label lblData;
        private System.Windows.Forms.TextBox tbData;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.GroupBox gbMonitors;
        private System.Windows.Forms.CheckBox cbMonitorCoils;
        private System.Windows.Forms.Label labelCoilAddr;
        private System.Windows.Forms.NumericUpDown nudCoilAddr;
        private System.Windows.Forms.Label labelCoilQty;
        private System.Windows.Forms.NumericUpDown nudCoilQty;
        private System.Windows.Forms.Label labelCoilInterval;
        private System.Windows.Forms.NumericUpDown nudCoilInterval;
        private System.Windows.Forms.CheckBox cbMonitorHolding;
        private System.Windows.Forms.Label labelHoldingAddr;
        private System.Windows.Forms.NumericUpDown nudHoldingAddr;
        private System.Windows.Forms.Label labelHoldingQty;
        private System.Windows.Forms.NumericUpDown nudHoldingQty;
        private System.Windows.Forms.Label labelHoldingInterval;
        private System.Windows.Forms.NumericUpDown nudHoldingInterval;
        private System.Windows.Forms.RichTextBox rtbLog;

        #endregion
    }
}