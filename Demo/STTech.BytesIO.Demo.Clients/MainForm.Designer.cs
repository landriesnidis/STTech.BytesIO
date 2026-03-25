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
            ApeFree.ApeForms.Core.Utils.StateColorSet stateColorSet2 = new ApeFree.ApeForms.Core.Utils.StateColorSet();
            menuStrip1 = new MenuStrip();
            新建ToolStripMenuItem = new ToolStripMenuItem();
            tsmiCreateTcpClient = new ToolStripMenuItem();
            tsmiCreateUdpClient = new ToolStripMenuItem();
            tsmiCreateSerialClient = new ToolStripMenuItem();
            tsmiCreateQuicClient = new ToolStripMenuItem();
            tab = new ApeFree.ApeForms.Core.Controls.SlideTabControl();
            tsmiCreateRJCPSerialClient = new ToolStripMenuItem();
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
            新建ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { tsmiCreateTcpClient, tsmiCreateUdpClient, tsmiCreateSerialClient, tsmiCreateRJCPSerialClient, tsmiCreateQuicClient });
            新建ToolStripMenuItem.Name = "新建ToolStripMenuItem";
            新建ToolStripMenuItem.Size = new Size(116, 28);
            新建ToolStripMenuItem.Text = "新建客户端";
            // 
            // tsmiCreateTcpClient
            // 
            tsmiCreateTcpClient.Name = "tsmiCreateTcpClient";
            tsmiCreateTcpClient.Size = new Size(209, 34);
            tsmiCreateTcpClient.Text = "TCP客户端";
            tsmiCreateTcpClient.Click += tsmiCreateTcpClient_Click;
            // 
            // tsmiCreateUdpClient
            // 
            tsmiCreateUdpClient.Name = "tsmiCreateUdpClient";
            tsmiCreateUdpClient.Size = new Size(209, 34);
            tsmiCreateUdpClient.Text = "IPC客户端";
            tsmiCreateUdpClient.Click += tsmiCreateUdpClient_Click;
            // 
            // tsmiCreateSerialClient
            // 
            tsmiCreateSerialClient.Name = "tsmiCreateSerialClient";
            tsmiCreateSerialClient.Size = new Size(209, 34);
            tsmiCreateSerialClient.Text = "串口客户端";
            tsmiCreateSerialClient.Click += tsmiCreateSerialClient_Click;
            // 
            // tsmiCreateQuicClient
            // 
            tsmiCreateQuicClient.Name = "tsmiCreateQuicClient";
            tsmiCreateQuicClient.Size = new Size(209, 34);
            tsmiCreateQuicClient.Text = "QUIC客户端";
            tsmiCreateQuicClient.Click += tsmiCreateQuicClient_Click;
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
            stateColorSet2.GotFocusBackColor = Color.FromArgb(0, 122, 204);
            stateColorSet2.GotFocusForeColor = Color.White;
            stateColorSet2.LostFocusBackColor = Color.FromArgb(251, 251, 251);
            stateColorSet2.LostFocusForeColor = Color.FromArgb(30, 30, 30);
            stateColorSet2.MouseDownBackColor = Color.FromArgb(14, 97, 152);
            stateColorSet2.MouseDownForeColor = Color.White;
            stateColorSet2.MouseLeaveBackColor = Color.FromArgb(0, 122, 204);
            stateColorSet2.MouseLeaveForeColor = Color.White;
            stateColorSet2.MouseMoveBackColor = Color.FromArgb(82, 176, 239);
            stateColorSet2.MouseMoveForeColor = Color.White;
            tab.StateColorSet = stateColorSet2;
            tab.TabIndex = 1;
            tab.TitleDock = DockStyle.Top;
            tab.TitleLayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            // 
            // tsmiCreateRJCPSerialClient
            // 
            tsmiCreateRJCPSerialClient.Name = "tsmiCreateRJCPSerialClient";
            tsmiCreateRJCPSerialClient.Size = new Size(270, 34);
            tsmiCreateRJCPSerialClient.Text = "串口客户端(RJCP)";
            tsmiCreateRJCPSerialClient.Click += tsmiCreateRJCPSerialClient_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1467, 900);
            Controls.Add(tab);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(5, 5, 5, 5);
            Name = "MainForm";
            Text = "TCP/串口通讯调试客户端";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 新建ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateTcpClient;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateSerialClient;
        private ApeFree.ApeForms.Core.Controls.SlideTabControl tab;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateUdpClient;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateQuicClient;
        private ToolStripMenuItem tsmiCreateRJCPSerialClient;
    }
}