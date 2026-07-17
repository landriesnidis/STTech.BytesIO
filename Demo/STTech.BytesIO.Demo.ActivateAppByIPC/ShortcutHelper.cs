using System;
using System.IO;

namespace STTech.BytesIO.Demo.ActivateAppByIPC
{
    public static class ShortcutHelper
    {
        public static void CreateShortcuts()
        {
            try
            {
                // 获取当前正在运行的 exe 的绝对路径
                string targetPath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(targetPath)) return;

                string dir = Path.GetDirectoryName(targetPath) ?? "";
                if (string.IsNullOrEmpty(dir)) return;

                // 在 exe 同目录下创建三个带不同预设启动参数的快捷方式
                CreateShortcut(Path.Combine(dir, "唤醒参数 - Welcome.lnk"), targetPath, "WelcomeToSTTechBytesIO", "启动参数：WelcomeToSTTechBytesIO");
                CreateShortcut(Path.Combine(dir, "唤醒参数 - Hello.lnk"), targetPath, "HelloSTTechBytesIO", "启动参数：HelloSTTechBytesIO");
                CreateShortcut(Path.Combine(dir, "唤醒参数 - Test.lnk"), targetPath, "IPC-SingleInstance-Activation-Test-Parameters", "启动参数：测试传参");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建快捷方式失败: {ex.Message}");
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string arguments, string description)
        {
            // 如果快捷方式已经存在，则无需重复创建
            if (File.Exists(shortcutPath)) return;

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            try
            {
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.Arguments = arguments;
                shortcut.Description = description;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Save();
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
            }
        }
    }
}
