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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.新建ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateTcpServer = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCreateIpcServer = new System.Windows.Forms.ToolStripMenuItem();
            this.tab = new ApeFree.ApeForms.Core.Controls.SlideTabControl();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.新建ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1200, 32);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 新建ToolStripMenuItem
            // 
            this.新建ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiCreateTcpServer,
            this.tsmiCreateIpcServer});
            this.新建ToolStripMenuItem.Name = "新建ToolStripMenuItem";
            this.新建ToolStripMenuItem.Size = new System.Drawing.Size(116, 28);
            this.新建ToolStripMenuItem.Text = "新建服务端";
            // 
            // tsmiCreateTcpServer
            // 
            this.tsmiCreateTcpServer.Name = "tsmiCreateTcpServer";
            this.tsmiCreateTcpServer.Size = new System.Drawing.Size(270, 34);
            this.tsmiCreateTcpServer.Text = "TCP服务端";
            this.tsmiCreateTcpServer.Click += new System.EventHandler(this.tsmiCreateTcpServer_Click);
            // 
            // tsmiCreateIpcServer
            // 
            this.tsmiCreateIpcServer.Name = "tsmiCreateIpcServer";
            this.tsmiCreateIpcServer.Size = new System.Drawing.Size(270, 34);
            this.tsmiCreateIpcServer.Text = "IPC服务端";
            this.tsmiCreateIpcServer.Click += new System.EventHandler(this.tsmiCreateIpcServer_Click);
            // 
            // tab
            // 
            this.tab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tab.Location = new System.Drawing.Point(0, 32);
            this.tab.Name = "tab";
            this.tab.ShowPageCloseButton = true;
            this.tab.Size = new System.Drawing.Size(1200, 643);
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
            this.Name = "MainForm";
            this.Text = "STTech.BytesIO 通信服务端调试工具";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 新建ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateTcpServer;
        private System.Windows.Forms.ToolStripMenuItem tsmiCreateIpcServer;
        private ApeFree.ApeForms.Core.Controls.SlideTabControl tab;
    }
}
