namespace Demo.BytesIO.Client
{
    partial class ChatClientPanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChatClientPanel));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnConnect = new System.Windows.Forms.ToolStripButton();
            this.btnDisconnect = new System.Windows.Forms.ToolStripButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tbRecv = new System.Windows.Forms.RichTextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.tbSend = new System.Windows.Forms.RichTextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.btnSendShake = new System.Windows.Forms.ToolStripButton();
            this.btnSendFile = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.文件保存路径ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tbSavePath = new System.Windows.Forms.ToolStripTextBox();
            this.groupBox1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.propertyGrid);
            this.groupBox1.Controls.Add(this.toolStrip1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(259, 546);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "连接信息";
            // 
            // propertyGrid
            // 
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.Location = new System.Drawing.Point(3, 42);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(253, 501);
            this.propertyGrid.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnConnect,
            this.btnDisconnect});
            this.toolStrip1.Location = new System.Drawing.Point(3, 17);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(253, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnConnect
            // 
            this.btnConnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnConnect.Image = ((System.Drawing.Image)(resources.GetObject("btnConnect.Image")));
            this.btnConnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(36, 22);
            this.btnConnect.Text = "连接";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnDisconnect.Image = ((System.Drawing.Image)(resources.GetObject("btnDisconnect.Image")));
            this.btnDisconnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(36, 22);
            this.btnDisconnect.Text = "断开";
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tbRecv);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(259, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(576, 446);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "日志";
            // 
            // tbRecv
            // 
            this.tbRecv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbRecv.Location = new System.Drawing.Point(3, 17);
            this.tbRecv.Name = "tbRecv";
            this.tbRecv.ReadOnly = true;
            this.tbRecv.Size = new System.Drawing.Size(570, 426);
            this.tbRecv.TabIndex = 0;
            this.tbRecv.Text = "";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.tbSend);
            this.groupBox3.Controls.Add(this.btnSend);
            this.groupBox3.Controls.Add(this.toolStrip2);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBox3.Location = new System.Drawing.Point(259, 446);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(576, 100);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "输入区";
            // 
            // tbSend
            // 
            this.tbSend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbSend.Location = new System.Drawing.Point(3, 42);
            this.tbSend.Name = "tbSend";
            this.tbSend.Size = new System.Drawing.Size(514, 55);
            this.tbSend.TabIndex = 1;
            this.tbSend.Text = "";
            // 
            // btnSend
            // 
            this.btnSend.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSend.Location = new System.Drawing.Point(517, 42);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(56, 55);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // toolStrip2
            // 
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnSendShake,
            this.btnSendFile,
            this.toolStripDropDownButton1});
            this.toolStrip2.Location = new System.Drawing.Point(3, 17);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(570, 25);
            this.toolStrip2.TabIndex = 3;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // btnSendShake
            // 
            this.btnSendShake.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnSendShake.Image = ((System.Drawing.Image)(resources.GetObject("btnSendShake.Image")));
            this.btnSendShake.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSendShake.Name = "btnSendShake";
            this.btnSendShake.Size = new System.Drawing.Size(60, 22);
            this.btnSendShake.Text = "窗口抖动";
            this.btnSendShake.Click += new System.EventHandler(this.btnSendShake_Click);
            // 
            // btnSendFile
            // 
            this.btnSendFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnSendFile.Image = ((System.Drawing.Image)(resources.GetObject("btnSendFile.Image")));
            this.btnSendFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSendFile.Name = "btnSendFile";
            this.btnSendFile.Size = new System.Drawing.Size(60, 22);
            this.btnSendFile.Text = "发送文件";
            this.btnSendFile.Click += new System.EventHandler(this.btnSendFile_Click);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件保存路径ToolStripMenuItem,
            this.tbSavePath});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(45, 22);
            this.toolStripDropDownButton1.Text = "设置";
            // 
            // 文件保存路径ToolStripMenuItem
            // 
            this.文件保存路径ToolStripMenuItem.Enabled = false;
            this.文件保存路径ToolStripMenuItem.Name = "文件保存路径ToolStripMenuItem";
            this.文件保存路径ToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
            this.文件保存路径ToolStripMenuItem.Text = "文件保存路径";
            // 
            // tbSavePath
            // 
            this.tbSavePath.Name = "tbSavePath";
            this.tbSavePath.Size = new System.Drawing.Size(180, 23);
            this.tbSavePath.Text = ".\\RecvFile";
            // 
            // ClientPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Name = "ClientPanel";
            this.Size = new System.Drawing.Size(835, 546);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);

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
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton btnSendFile;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem 文件保存路径ToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton btnSendShake;
        private System.Windows.Forms.ToolStripTextBox tbSavePath;
    }
}

