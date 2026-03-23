namespace STTech.BytesIO.Demo.Clients
{
    partial class MainForm
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
            ApeFree.ApeForms.Core.Utils.StateColorSet stateColorSet1 = new ApeFree.ApeForms.Core.Utils.StateColorSet();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.新建ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateTcpClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateUdpClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateSerialClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateQuicClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tab = new ApeFree.ApeForms.Core.Controls.SlideTabControl();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.新建ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1200, 34);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 新建ToolStripMenuItem
            // 
            this.新建ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiCreateTcpClient,
            this.tsmiCreateUdpClient,
            this.tsmiCreateSerialClient,
            this.tsmiCreateQuicClient});
            this.新建ToolStripMenuItem.Name = "新建ToolStripMenuItem";
            this.新建ToolStripMenuItem.Size = new System.Drawing.Size(116, 28);
            this.新建ToolStripMenuItem.Text = "新建客户端";
            // 
            // tsmiCreateTcpClient
            // 
            this.tsmiCreateTcpClient.Name = "tsmiCreateTcpClient";
            this.tsmiCreateTcpClient.Size = new System.Drawing.Size(270, 34);
            this.tsmiCreateTcpClient.Text = "TCP客户端";
            this.tsmiCreateTcpClient.Click += new System.EventHandler(this.tsmiCreateTcpClient_Click);
            // 
            // tsmiCreateUdpClient
            // 
            this.tsmiCreateUdpClient.Name = "tsmiCreateUdpClient";
            this.tsmiCreateUdpClient.Size = new System.Drawing.Size(270, 34);
            this.tsmiCreateUdpClient.Text = "IPC客户端";
            this.tsmiCreateUdpClient.Click += new System.EventHandler(this.tsmiCreateUdpClient_Click);
            // 
            // tsmiCreateSerialClient
            // 
            this.tsmiCreateSerialClient.Name = "tsmiCreateSerialClient";
            this.tsmiCreateSerialClient.Size = new System.Drawing.Size(270, 34);
            this.tsmiCreateSerialClient.Text = "串口客户端";
            this.tsmiCreateSerialClient.Click += new System.EventHandler(this.tsmiCreateSerialClient_Click);
            // 
            // tsmiCreateQuicClient
            // 
            this.tsmiCreateQuicClient.Name = "tsmiCreateQuicClient";
            this.tsmiCreateQuicClient.Size = new System.Drawing.Size(270, 34);
            this.tsmiCreateQuicClient.Text = "QUIC客户端";
            this.tsmiCreateQuicClient.Click += new System.EventHandler(this.tsmiCreateQuicClient_Click);
            // 
            // tab
            // 
            this.tab.CloseAllPagesOptionText = null;
            this.tab.ClosePageOptionText = null;
            this.tab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tab.Location = new System.Drawing.Point(0, 34);
            this.tab.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.tab.Name = "tab";
            this.tab.PageItemContextMenu = null;
            this.tab.Rate = 1;
            this.tab.ShowPageCloseButton = true;
            this.tab.Size = new System.Drawing.Size(1200, 641);
            stateColorSet1.GotFocusBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            stateColorSet1.GotFocusForeColor = System.Drawing.Color.White;
            stateColorSet1.LostFocusBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(251)))), ((int)(((byte)(251)))));
            stateColorSet1.LostFocusForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            stateColorSet1.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(14)))), ((int)(((byte)(97)))), ((int)(((byte)(152)))));
            stateColorSet1.MouseDownForeColor = System.Drawing.Color.White;
            stateColorSet1.MouseLeaveBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            stateColorSet1.MouseLeaveForeColor = System.Drawing.Color.White;
            stateColorSet1.MouseMoveBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(176)))), ((int)(((byte)(239)))));
            stateColorSet1.MouseMoveForeColor = System.Drawing.Color.White;
            this.tab.StateColorSet = stateColorSet1;
            this.tab.TabIndex = 1;
            this.tab.TitleDock = System.Windows.Forms.DockStyle.Top;
            this.tab.TitleLayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 675);
            this.Controls.Add(this.tab);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "MainForm";
            this.Text = "TCP/串口通讯调试客户端";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 新建ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateTcpClient;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateSerialClient;
        private ApeFree.ApeForms.Core.Controls.SlideTabControl tab;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateUdpClient;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateQuicClient;
    }
}