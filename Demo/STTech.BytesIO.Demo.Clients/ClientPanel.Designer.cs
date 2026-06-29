namespace STTech.BytesIO.Demo.Clients
{
    partial class ClientPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClientPanel));
            groupBox1 = new GroupBox();
            propertyGrid = new PropertyGrid();
            toolStrip1 = new ToolStrip();
            btnConnect = new ToolStripButton();
            btnDisconnect = new ToolStripButton();
            groupBox2 = new GroupBox();
            tbRecv = new RichTextBox();
            groupBox3 = new GroupBox();
            tbSend = new RichTextBox();
            btnSend = new Button();
            toolStrip2 = new ToolStrip();
            btnScreenClean = new ToolStripButton();
            btnHexMode = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            groupBox1.SuspendLayout();
            toolStrip1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            toolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(propertyGrid);
            groupBox1.Controls.Add(toolStrip1);
            groupBox1.Dock = DockStyle.Left;
            groupBox1.Location = new Point(0, 0);
            groupBox1.Margin = new Padding(4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4);
            groupBox1.Size = new Size(302, 774);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "连接信息";
            // 
            // propertyGrid
            // 
            propertyGrid.BackColor = SystemColors.Control;
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.Location = new Point(4, 45);
            propertyGrid.Margin = new Padding(4);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(294, 725);
            propertyGrid.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { btnConnect, btnDisconnect });
            toolStrip1.Location = new Point(4, 20);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(294, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnConnect
            // 
            btnConnect.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnConnect.Image = (Image)resources.GetObject("btnConnect.Image");
            btnConnect.ImageTransparentColor = Color.Magenta;
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(36, 22);
            btnConnect.Text = "连接";
            btnConnect.Click += btnConnect_Click;
            // 
            // btnDisconnect
            // 
            btnDisconnect.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDisconnect.Image = (Image)resources.GetObject("btnDisconnect.Image");
            btnDisconnect.ImageTransparentColor = Color.Magenta;
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(36, 22);
            btnDisconnect.Text = "断开";
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(tbRecv);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Location = new Point(302, 0);
            groupBox2.Margin = new Padding(4);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4);
            groupBox2.Size = new Size(672, 632);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "日志";
            // 
            // tbRecv
            // 
            tbRecv.Dock = DockStyle.Fill;
            tbRecv.Location = new Point(4, 20);
            tbRecv.Margin = new Padding(4);
            tbRecv.Name = "tbRecv";
            tbRecv.ReadOnly = true;
            tbRecv.Size = new Size(664, 608);
            tbRecv.TabIndex = 0;
            tbRecv.Text = "";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(tbSend);
            groupBox3.Controls.Add(btnSend);
            groupBox3.Controls.Add(toolStrip2);
            groupBox3.Dock = DockStyle.Bottom;
            groupBox3.Location = new Point(302, 632);
            groupBox3.Margin = new Padding(4);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(4);
            groupBox3.Size = new Size(672, 142);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "输入区";
            // 
            // tbSend
            // 
            tbSend.Dock = DockStyle.Fill;
            tbSend.Location = new Point(4, 45);
            tbSend.Margin = new Padding(4);
            tbSend.Name = "tbSend";
            tbSend.Size = new Size(599, 93);
            tbSend.TabIndex = 1;
            tbSend.Text = "";
            // 
            // btnSend
            // 
            btnSend.Dock = DockStyle.Right;
            btnSend.Location = new Point(603, 45);
            btnSend.Margin = new Padding(4);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(65, 93);
            btnSend.TabIndex = 2;
            btnSend.Text = "发送";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // toolStrip2
            // 
            toolStrip2.Items.AddRange(new ToolStripItem[] { btnScreenClean, btnHexMode, toolStripSeparator1 });
            toolStrip2.Location = new Point(4, 20);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new Size(664, 25);
            toolStrip2.TabIndex = 3;
            toolStrip2.Text = "toolStrip2";
            // 
            // btnScreenClean
            // 
            btnScreenClean.Alignment = ToolStripItemAlignment.Right;
            btnScreenClean.CheckOnClick = true;
            btnScreenClean.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnScreenClean.ForeColor = Color.Green;
            btnScreenClean.Image = (Image)resources.GetObject("btnScreenClean.Image");
            btnScreenClean.ImageTransparentColor = Color.Magenta;
            btnScreenClean.Name = "btnScreenClean";
            btnScreenClean.Size = new Size(44, 22);
            btnScreenClean.Text = "Clean";
            btnScreenClean.Click += btnScreenClean_Click;
            // 
            // btnHexMode
            // 
            btnHexMode.CheckOnClick = true;
            btnHexMode.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnHexMode.Image = (Image)resources.GetObject("btnHexMode.Image");
            btnHexMode.ImageTransparentColor = Color.Magenta;
            btnHexMode.Name = "btnHexMode";
            btnHexMode.Size = new Size(73, 22);
            btnHexMode.Text = "Hex Mode";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 25);
            // 
            // ClientPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(groupBox2);
            Controls.Add(groupBox3);
            Controls.Add(groupBox1);
            Margin = new Padding(4);
            Name = "ClientPanel";
            Size = new Size(974, 774);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnDisconnect;
        private System.Windows.Forms.ToolStripButton btnConnect;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RichTextBox tbRecv;
        private System.Windows.Forms.RichTextBox tbSend;
        private System.Windows.Forms.Button btnSend;
        private ToolStrip toolStrip2;
        private ToolStripButton btnHexMode;
        private ToolStripButton btnScreenClean;
        private ToolStripSeparator toolStripSeparator1;
    }
}

