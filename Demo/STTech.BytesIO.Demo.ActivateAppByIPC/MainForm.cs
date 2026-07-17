using System;
using System.Drawing;
using System.Windows.Forms;

namespace STTech.BytesIO.Demo.ActivateAppByIPC
{
    public partial class MainForm : Form
    {
        private string[] _initialArgs;

        public MainForm(string[] args)
        {
            _initialArgs = args;
            InitializeComponent();
            
            // 使用标准的应用程序图标作为托盘图标和窗体图标
            notifyIcon.Icon = SystemIcons.Application;
            this.Icon = SystemIcons.Application;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            txtArgs.Text = $"[{DateTime.Now:HH:mm:ss}] 软件已启动！{Environment.NewLine}";
            if (_initialArgs != null && _initialArgs.Length > 0)
            {
                txtArgs.AppendText($"初始启动参数: {string.Join(" ", _initialArgs)}{Environment.NewLine}");
            }
            else
            {
                txtArgs.AppendText($"无初始启动参数。{Environment.NewLine}");
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(1000, "提示", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
        }

        public void WakeUpAndShowArgs(string args)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(WakeUpAndShowArgs), args);
                return;
            }

            // 恢复窗体显示
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();

            txtArgs.AppendText($"[{DateTime.Now:HH:mm:ss}] 唤醒并接收参数: {args}{Environment.NewLine}");
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                RestoreForm();
            }
        }

        private void tsmiShow_Click(object sender, EventArgs e)
        {
            RestoreForm();
        }

        private void tsmiExit_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        private void RestoreForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 彻底销毁托盘图标，防止残留
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            base.OnFormClosing(e);
        }
    }
}
