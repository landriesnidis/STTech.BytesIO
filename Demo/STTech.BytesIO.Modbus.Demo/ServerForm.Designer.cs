namespace STTech.BytesIO.Modbus.Demo
{
    partial class ServerForm
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnCreateClient = new System.Windows.Forms.Button();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.nudSlaveId = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbProtocol = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpCoils = new System.Windows.Forms.TabPage();
            this.dgvCoils = new System.Windows.Forms.DataGridView();
            this.tpDiscreteInputs = new System.Windows.Forms.TabPage();
            this.dgvDiscreteInputs = new System.Windows.Forms.DataGridView();
            this.tpInputRegisters = new System.Windows.Forms.TabPage();
            this.dgvInputRegisters = new System.Windows.Forms.DataGridView();
            this.tpHoldingRegisters = new System.Windows.Forms.TabPage();
            this.dgvHoldingRegisters = new System.Windows.Forms.DataGridView();
            this.tbLog = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSlaveId)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tpCoils.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCoils)).BeginInit();
            this.tpDiscreteInputs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDiscreteInputs)).BeginInit();
            this.tpInputRegisters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvInputRegisters)).BeginInit();
            this.tpHoldingRegisters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHoldingRegisters)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 131F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 133F));
            this.tableLayoutPanel1.Controls.Add(this.btnStartStop, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnCreateClient, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tabControl1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tbLog, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(978, 700);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.nudSlaveId);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.nudPort);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.cmbProtocol);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(626, 54);
            this.panel1.TabIndex = 0;
            // 
            // btnCreateClient
            // 
            this.btnCreateClient.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCreateClient.Location = new System.Drawing.Point(848, 3);
            this.btnCreateClient.Name = "btnCreateClient";
            this.btnCreateClient.Size = new System.Drawing.Size(127, 54);
            this.btnCreateClient.TabIndex = 7;
            this.btnCreateClient.Text = "创建客户端";
            this.btnCreateClient.UseVisualStyleBackColor = true;
            // 
            // btnStartStop
            // 
            this.btnStartStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartStop.Location = new System.Drawing.Point(717, 3);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(125, 54);
            this.btnStartStop.TabIndex = 6;
            this.btnStartStop.Text = "启动服务";
            this.btnStartStop.UseVisualStyleBackColor = true;
            // 
            // nudSlaveId
            // 
            this.nudSlaveId.Location = new System.Drawing.Point(395, 13);
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
            this.nudSlaveId.Size = new System.Drawing.Size(60, 28);
            this.nudSlaveId.TabIndex = 5;
            this.nudSlaveId.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(325, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(62, 18);
            this.label3.TabIndex = 4;
            this.label3.Text = "站号：";
            // 
            // nudPort
            // 
            this.nudPort.Location = new System.Drawing.Point(235, 13);
            this.nudPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudPort.Name = "nudPort";
            this.nudPort.Size = new System.Drawing.Size(80, 28);
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
            this.label2.Location = new System.Drawing.Point(175, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "端口：";
            // 
            // cmbProtocol
            // 
            this.cmbProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProtocol.FormattingEnabled = true;
            this.cmbProtocol.Items.AddRange(new object[] {
            "RTU",
            "ASCII"});
            this.cmbProtocol.Location = new System.Drawing.Point(70, 14);
            this.cmbProtocol.Name = "cmbProtocol";
            this.cmbProtocol.Size = new System.Drawing.Size(90, 26);
            this.cmbProtocol.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "协议：";
            // 
            // tabControl1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.tabControl1, 3);
            this.tabControl1.Controls.Add(this.tpCoils);
            this.tabControl1.Controls.Add(this.tpDiscreteInputs);
            this.tabControl1.Controls.Add(this.tpInputRegisters);
            this.tabControl1.Controls.Add(this.tpHoldingRegisters);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(3, 63);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(972, 378);
            this.tabControl1.TabIndex = 1;
            // 
            // tpCoils
            // 
            this.tpCoils.Controls.Add(this.dgvCoils);
            this.tpCoils.Location = new System.Drawing.Point(4, 28);
            this.tpCoils.Name = "tpCoils";
            this.tpCoils.Padding = new System.Windows.Forms.Padding(3);
            this.tpCoils.Size = new System.Drawing.Size(964, 346);
            this.tpCoils.TabIndex = 0;
            this.tpCoils.Text = "线圈 (Coils)";
            this.tpCoils.UseVisualStyleBackColor = true;
            // 
            // dgvCoils
            // 
            this.dgvCoils.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCoils.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCoils.Location = new System.Drawing.Point(3, 3);
            this.dgvCoils.Name = "dgvCoils";
            this.dgvCoils.RowHeadersWidth = 62;
            this.dgvCoils.RowTemplate.Height = 30;
            this.dgvCoils.Size = new System.Drawing.Size(958, 340);
            this.dgvCoils.TabIndex = 0;
            // 
            // tpDiscreteInputs
            // 
            this.tpDiscreteInputs.Controls.Add(this.dgvDiscreteInputs);
            this.tpDiscreteInputs.Location = new System.Drawing.Point(4, 28);
            this.tpDiscreteInputs.Name = "tpDiscreteInputs";
            this.tpDiscreteInputs.Padding = new System.Windows.Forms.Padding(3);
            this.tpDiscreteInputs.Size = new System.Drawing.Size(886, 346);
            this.tpDiscreteInputs.TabIndex = 1;
            this.tpDiscreteInputs.Text = "离散输入 (Discrete Inputs)";
            this.tpDiscreteInputs.UseVisualStyleBackColor = true;
            // 
            // dgvDiscreteInputs
            // 
            this.dgvDiscreteInputs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDiscreteInputs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDiscreteInputs.Location = new System.Drawing.Point(3, 3);
            this.dgvDiscreteInputs.Name = "dgvDiscreteInputs";
            this.dgvDiscreteInputs.RowHeadersWidth = 62;
            this.dgvDiscreteInputs.RowTemplate.Height = 30;
            this.dgvDiscreteInputs.Size = new System.Drawing.Size(880, 340);
            this.dgvDiscreteInputs.TabIndex = 0;
            // 
            // tpInputRegisters
            // 
            this.tpInputRegisters.Controls.Add(this.dgvInputRegisters);
            this.tpInputRegisters.Location = new System.Drawing.Point(4, 28);
            this.tpInputRegisters.Name = "tpInputRegisters";
            this.tpInputRegisters.Size = new System.Drawing.Size(886, 346);
            this.tpInputRegisters.TabIndex = 2;
            this.tpInputRegisters.Text = "输入寄存器 (Input Registers)";
            this.tpInputRegisters.UseVisualStyleBackColor = true;
            // 
            // dgvInputRegisters
            // 
            this.dgvInputRegisters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvInputRegisters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvInputRegisters.Location = new System.Drawing.Point(0, 0);
            this.dgvInputRegisters.Name = "dgvInputRegisters";
            this.dgvInputRegisters.RowHeadersWidth = 62;
            this.dgvInputRegisters.RowTemplate.Height = 30;
            this.dgvInputRegisters.Size = new System.Drawing.Size(886, 346);
            this.dgvInputRegisters.TabIndex = 0;
            // 
            // tpHoldingRegisters
            // 
            this.tpHoldingRegisters.Controls.Add(this.dgvHoldingRegisters);
            this.tpHoldingRegisters.Location = new System.Drawing.Point(4, 28);
            this.tpHoldingRegisters.Name = "tpHoldingRegisters";
            this.tpHoldingRegisters.Size = new System.Drawing.Size(886, 346);
            this.tpHoldingRegisters.TabIndex = 3;
            this.tpHoldingRegisters.Text = "保持寄存器 (Holding Registers)";
            this.tpHoldingRegisters.UseVisualStyleBackColor = true;
            // 
            // dgvHoldingRegisters
            // 
            this.dgvHoldingRegisters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHoldingRegisters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHoldingRegisters.Location = new System.Drawing.Point(0, 0);
            this.dgvHoldingRegisters.Name = "dgvHoldingRegisters";
            this.dgvHoldingRegisters.RowHeadersWidth = 62;
            this.dgvHoldingRegisters.RowTemplate.Height = 30;
            this.dgvHoldingRegisters.Size = new System.Drawing.Size(886, 346);
            this.dgvHoldingRegisters.TabIndex = 0;
            // 
            // tbLog
            // 
            this.tbLog.BackColor = System.Drawing.Color.Black;
            this.tableLayoutPanel1.SetColumnSpan(this.tbLog, 3);
            this.tbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbLog.ForeColor = System.Drawing.Color.Lime;
            this.tbLog.Location = new System.Drawing.Point(3, 447);
            this.tbLog.Name = "tbLog";
            this.tbLog.Size = new System.Drawing.Size(972, 250);
            this.tbLog.TabIndex = 2;
            this.tbLog.Text = "";
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(978, 700);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ServerForm";
            this.Text = "STTech Modbus Server Demo";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSlaveId)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tpCoils.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCoils)).EndInit();
            this.tpDiscreteInputs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDiscreteInputs)).EndInit();
            this.tpInputRegisters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvInputRegisters)).EndInit();
            this.tpHoldingRegisters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHoldingRegisters)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbProtocol;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudSlaveId;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Button btnCreateClient;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tpCoils;
        private System.Windows.Forms.TabPage tpDiscreteInputs;
        private System.Windows.Forms.TabPage tpInputRegisters;
        private System.Windows.Forms.TabPage tpHoldingRegisters;
        private System.Windows.Forms.DataGridView dgvCoils;
        private System.Windows.Forms.DataGridView dgvDiscreteInputs;
        private System.Windows.Forms.DataGridView dgvInputRegisters;
        private System.Windows.Forms.DataGridView dgvHoldingRegisters;
        private System.Windows.Forms.RichTextBox tbLog;
    }
}

