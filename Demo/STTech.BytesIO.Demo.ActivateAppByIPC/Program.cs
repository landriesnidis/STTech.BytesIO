using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using STTech.BytesIO.Ipc;

namespace STTech.BytesIO.Demo.ActivateAppByIPC
{
    static class Program
    {
        private static Mutex? mutex = null;
        private const string MutexName = "Global\\STTech.BytesIO.Demo.ActivateAppByIPC.Mutex";
        private const string PipeName = "STTech.BytesIO.Demo.ActivateAppByIPC.Pipe";

        [STAThread]
        static void Main(string[] args)
        {
            // 使用系统全局 Mutex 检测是否已有实例运行
            bool createdNew;
            mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // 已有实例运行：作为客户端，通过 IPC 连接到主实例，将当前参数发送过去，然后退出
                SendArgsToRunningInstance(args);
                return;
            }

            // 无其他实例运行：启动主程序
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new MainForm(args);

            // 实例化并启动 IPC 命名管道服务端
            var server = new IpcServer { PipeName = PipeName };
            
            // 订阅客户端连入事件
            server.ClientConnected += (s, e) =>
            {
                var client = e.Client;
                // 监听此客户端发送的数据
                client.OnDataReceived += (sender, dataArgs) =>
                {
                    // 接收参数字符串
                    string receivedArgs = dataArgs.Data.EncodeToString(Encoding.UTF8);

                    // 安全地将唤醒操作交回窗体 UI 线程
                    mainForm.BeginInvoke(new Action(() =>
                    {
                        mainForm.WakeUpAndShowArgs(receivedArgs);
                    }));
                };
            };

            // 异步开启监听
            server.StartAsync();

            // 自动在 exe 相同目录下创建快捷方式
            ShortcutHelper.CreateShortcuts();

            try
            {
                Application.Run(mainForm);
            }
            finally
            {
                // 关闭并释放 IPC 服务端，释放 Mutex 信号量
                server.CloseAsync().Wait();
                server.Dispose();
                mutex.ReleaseMutex();
            }
        }

        private static void SendArgsToRunningInstance(string[] args)
        {
            try
            {
                var client = new IpcClient { PipeName = PipeName };
                // 尝试连接主实例的 IPC 服务端
                var result = client.Connect(2000);
                if (result.IsSuccess)
                {
                    // 拼接参数并使用 UTF8 发送给主实例
                    string payload = args.Length > 0 ? string.Join(" ", args) : "(无启动参数)";
                    client.Send(Encoding.UTF8.GetBytes(payload));
                    client.Disconnect();
                }
                else
                {
                    MessageBox.Show($"无法连接到主实例: {result.ErrorCode}", "启动提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送激活指令时发生异常: {ex.Message}", "启动错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
