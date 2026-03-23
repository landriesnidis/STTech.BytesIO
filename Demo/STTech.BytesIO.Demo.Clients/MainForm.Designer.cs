namespace Demo.BytesIO.Client
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.新建ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateTcpClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateUdpClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateSerialClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tab = new ApeFree.ApeForms.Core.Controls.SlideTabControl();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateChatTcpClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateChatUdpClient = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateChatSerialClient = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.新建ToolStripMenuItem,
            this.toolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 25);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 新建ToolStripMenuItem
            // 
            this.新建ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiCreateTcpClient,
            this.tsmiCreateUdpClient,
            this.tsmiCreateSerialClient});
            this.新建ToolStripMenuItem.Name = "新建ToolStripMenuItem";
            this.新建ToolStripMenuItem.Size = new System.Drawing.Size(80, 21);
            this.新建ToolStripMenuItem.Text = "新建客户端";
            // 
            // tsmiCreateTcpClient
            // 
            this.tsmiCreateTcpClient.Name = "tsmiCreateTcpClient";
            this.tsmiCreateTcpClient.Size = new System.Drawing.Size(180, 22);
            this.tsmiCreateTcpClient.Text = "TCP客户端";
            this.tsmiCreateTcpClient.Click += new System.EventHandler(this.tsmiCreateTcpClient_Click);
            // 
            // tsmiCreateUdpClient
            // 
            this.tsmiCreateUdpClient.Name = "tsmiCreateUdpClient";
            this.tsmiCreateUdpClient.Size = new System.Drawing.Size(180, 22);
            this.tsmiCreateUdpClient.Text = "UDP客户端";
            this.tsmiCreateUdpClient.Click += new System.EventHandler(this.tsmiCreateUdpClient_Click);
            // 
            // tsmiCreateSerialClient
            // 
            this.tsmiCreateSerialClient.Name = "tsmiCreateSerialClient";
            this.tsmiCreateSerialClient.Size = new System.Drawing.Size(180, 22);
            this.tsmiCreateSerialClient.Text = "串口客户端";
            this.tsmiCreateSerialClient.Click += new System.EventHandler(this.tsmiCreateSerialClient_Click);
            // 
            // tab
            // 
            this.tab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tab.Location = new System.Drawing.Point(0, 25);
            this.tab.Name = "tab";
            this.tab.Rate = 1;
            this.tab.Size = new System.Drawing.Size(800, 425);
            this.tab.TabIndex = 1;
            this.tab.TitleDock = System.Windows.Forms.DockStyle.Top;
            this.tab.TitleLayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiCreateChatTcpClient,
            this.tsmiCreateChatUdpClient,
            this.tsmiCreateChatSerialClient});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(104, 21);
            this.toolStripMenuItem1.Text = "新建聊天客户端";
            // 
            // tsmiCreateChatTcpClient
            // 
            this.tsmiCreateChatTcpClient.Name = "tsmiCreateChatTcpClient";
            this.tsmiCreateChatTcpClient.Size = new System.Drawing.Size(180, 22);
            this.tsmiCreateChatTcpClient.Text = "TCP聊天客户端";
            this.tsmiCreateChatTcpClient.Click += new System.EventHandler(this.tsmiCreateChatTcpClient_Click);
            // 
            // tsmiCreateChatUdpClient
            // 
            this.tsmiCreateChatUdpClient.Name = "tsmiCreateChatUdpClient";
            this.tsmiCreateChatUdpClient.Size = new System.Drawing.Size(180, 22);
            this.tsmiCreateChatUdpClient.Text = "UDP聊天客户端";
            this.tsmiCreateChatUdpClient.Click += new System.EventHandler(this.tsmiCreateChatUdpClient_Click);
            // 
            // tsmiCreateChatSerialClient
            // 
            this.tsmiCreateChatSerialClient.Name = "tsmiCreateChatSerialClient";
            this.tsmiCreateChatSerialClient.Size = new System.Drawing.Size(180, 22);
            this.tsmiCreateChatSerialClient.Text = "串口聊天客户端";
            this.tsmiCreateChatSerialClient.Click += new System.EventHandler(this.tsmiCreateChatSerialClient_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tab);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
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
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateChatTcpClient;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateChatUdpClient;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateChatSerialClient;
    }
}