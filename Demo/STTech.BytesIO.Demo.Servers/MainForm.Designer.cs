namespace STTech.BytesIO.Demo.Servers
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
            menuStrip1 = new MenuStrip();
            新建ToolStripMenuItem = new ToolStripMenuItem();
            tsmiCreateTcpServer = new ToolStripMenuItem();
            tsmiCreateIpcServer = new ToolStripMenuItem();
            tsmiCreateQuicServer = new ToolStripMenuItem();
            tsmiCreateP2PServer = new ToolStripMenuItem();
            tab = new ApeFree.ApeForms.Core.Controls.SlideTabControl();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { 新建ToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(7, 3, 0, 3);
            menuStrip1.Size = new Size(1467, 34);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // 新建ToolStripMenuItem
            // 
            新建ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { tsmiCreateTcpServer, tsmiCreateIpcServer, tsmiCreateQuicServer, tsmiCreateP2PServer });
            新建ToolStripMenuItem.Name = "新建ToolStripMenuItem";
            新建ToolStripMenuItem.Size = new Size(116, 28);
            新建ToolStripMenuItem.Text = "新建服务端";
            // 
            // tsmiCreateTcpServer
            // 
            tsmiCreateTcpServer.Name = "tsmiCreateTcpServer";
            tsmiCreateTcpServer.Size = new Size(270, 34);
            tsmiCreateTcpServer.Text = "TCP服务端";
            tsmiCreateTcpServer.Click += tsmiCreateTcpServer_Click;
            // 
            // tsmiCreateIpcServer
            // 
            tsmiCreateIpcServer.Name = "tsmiCreateIpcServer";
            tsmiCreateIpcServer.Size = new Size(270, 34);
            tsmiCreateIpcServer.Text = "IPC服务器";
            tsmiCreateIpcServer.Click += tsmiCreateIpcServer_Click;
            // 
            // tsmiCreateQuicServer
            // 
            tsmiCreateQuicServer.Name = "tsmiCreateQuicServer";
            tsmiCreateQuicServer.Size = new Size(270, 34);
            tsmiCreateQuicServer.Text = "QUIC服务端";
            tsmiCreateQuicServer.Click += tsmiCreateQuicServer_Click;
            // 
            // tsmiCreateP2PServer
            // 
            tsmiCreateP2PServer.Name = "tsmiCreateP2PServer";
            tsmiCreateP2PServer.Size = new Size(270, 34);
            tsmiCreateP2PServer.Text = "P2P种子节点";
            tsmiCreateP2PServer.Click += tsmiCreateP2PServer_Click;
            // 
            // tab
            // 
            tab.CloseAllPagesOptionText = null;
            tab.ClosePageOptionText = null;
            tab.Dock = DockStyle.Fill;
            tab.Location = new Point(0, 34);
            tab.Margin = new Padding(7, 8, 7, 8);
            tab.Name = "tab";
            tab.PageItemContextMenu = null;
            tab.Rate = 1;
            tab.ShowPageCloseButton = true;
            tab.Size = new Size(1467, 866);
            stateColorSet1.GotFocusBackColor = Color.FromArgb(0, 122, 204);
            stateColorSet1.GotFocusForeColor = Color.White;
            stateColorSet1.LostFocusBackColor = Color.FromArgb(251, 251, 251);
            stateColorSet1.LostFocusForeColor = Color.FromArgb(30, 30, 30);
            stateColorSet1.MouseDownBackColor = Color.FromArgb(14, 97, 152);
            stateColorSet1.MouseDownForeColor = Color.White;
            stateColorSet1.MouseLeaveBackColor = Color.FromArgb(0, 122, 204);
            stateColorSet1.MouseLeaveForeColor = Color.White;
            stateColorSet1.MouseMoveBackColor = Color.FromArgb(82, 176, 239);
            stateColorSet1.MouseMoveForeColor = Color.White;
            tab.StateColorSet = stateColorSet1;
            tab.TabIndex = 1;
            tab.TitleDock = DockStyle.Top;
            tab.TitleLayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1467, 900);
            Controls.Add(tab);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "STTech.BytesIO 通信服务端调试工具";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 新建ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateTcpServer;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateIpcServer;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateQuicServer;
        private ToolStripMenuItem tsmiCreateP2PServer;
        private ApeFree.ApeForms.Core.Controls.SlideTabControl tab;
    }
}
