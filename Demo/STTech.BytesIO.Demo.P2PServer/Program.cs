using STTech.BytesIO.Core;
using STTech.BytesIO.P2P;
using System.IO;
using System.Text;
using System.Text.Json;

namespace STTech.BytesIO.Demo.P2PServer
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try { Console.Title = "STTech.BytesIO.P2P - Bootstrap Server Demo"; } catch { }

            PrintBanner();

            // 1. 引导用户输入参数 (支持从 settings.json 加载/保存)
            var (options, settings) = PromptConfiguration();

            // 2. 创建 BootstrapServer 实例
            var server = new BootstrapServer();
            server.Options = options;

            // 3. 注册事件处理
            RegisterServerEvents(server);

            // 处理 Ctrl+C 优雅退出
            var exitTcs = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                exitTcs.TrySetResult(true);
            };

            // 4. 启动引导节点服务
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("--> 正在启动 P2P 引导节点服务，请稍候...");
            Console.ResetColor();

            try
            {
                await server.StartAsync();

                // 启动成功后，将生成的 PeerId 也持久化记录到 settings.json 中
                settings.CachedPeerId = server.LocalPeerId;
                settings.Save();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[错误] 启动服务失败: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("按任意键退出...");
                if (!Console.IsInputRedirected)
                {
                    try { Console.ReadKey(); } catch { }
                }
                return;
            }

            // 5. 打印服务运行信息
            PrintServerInfo(server);

            // 6. 交互式命令行控制台
            _ = Task.Run(async () =>
            {
                await RunCommandLoopAsync(server);
                exitTcs.TrySetResult(true);
            });

            // 等待退出信号
            await exitTcs.Task;

            // 7. 关闭服务
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n--> 正在关闭 P2P 引导节点服务...");
            Console.ResetColor();
            try
            {
                await server.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[警告] 关闭服务时发生异常: {ex.Message}");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("--> 服务已安全停止，程序已退出。");
            Console.ResetColor();
        }

        /// <summary>
        /// 打印欢迎横幅
        /// </summary>
        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================================");
            Console.WriteLine("       STTech.BytesIO.P2P - BootstrapServer 引导节点示例");
            Console.WriteLine("==================================================================");
            Console.ResetColor();
            Console.WriteLine("基于 Nethermind.Libp2p 与 STTech.BytesIO 构建的去中心化 P2P 节点\n");
        }

        /// <summary>
        /// 交互式收集节点配置参数（支持 settings.json 缓存与加载）
        /// </summary>
        private static (P2PNodeOptions options, ServerSettings settings) PromptConfiguration()
        {
            var settings = ServerSettings.Load();
            bool hasCache = File.Exists(ServerSettings.SettingsFilePath);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("【步骤 1】请配置 P2P 引导节点网络参数 (直接回车使用默认值)：");
            if (hasCache)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  [提示: 已从当前目录 settings.json 加载上次配置]");
            }
            Console.ResetColor();

            // 监听 IP 地址
            Console.Write($"  * 监听 IP 地址 [默认: {settings.ListenAddress}]: ");
            var ipInput = Console.ReadLine()?.Trim();
            string listenIp = settings.ListenAddress;
            if (!string.IsNullOrWhiteSpace(ipInput))
            {
                var cleanIp = new string(ipInput.Where(c => !char.IsControl(c) && c <= 127).ToArray()).Trim();
                if (string.IsNullOrWhiteSpace(cleanIp))
                {
                    listenIp = settings.ListenAddress;
                }
                else if (System.Net.IPAddress.TryParse(cleanIp, out _))
                {
                    listenIp = cleanIp;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"    [提示] 输入的 IP '{ipInput}' 无效，将使用默认值 {settings.ListenAddress}");
                    Console.ResetColor();
                }
            }

            // TCP 监听端口
            Console.Write($"  * TCP 监听端口号 [默认: {settings.TcpPort}]: ");
            var tcpPortInput = Console.ReadLine()?.Trim();
            int tcpPort = settings.TcpPort;
            if (!string.IsNullOrWhiteSpace(tcpPortInput))
            {
                var cleanPort = new string(tcpPortInput.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleanPort, out var parsedPort) && parsedPort > 0 && parsedPort <= 65535)
                {
                    tcpPort = parsedPort;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"    [提示] 输入的端口无效，将使用默认端口 {settings.TcpPort}");
                    Console.ResetColor();
                }
            }

            // 身份种子 / PeerId 密钥
            string currentSeedHex = settings.IdentitySeedHex;
            Console.Write($"  * 身份种子/PeerId密钥 (64位十六进制字符，输入 'r' 随机生成)\n    [默认: {currentSeedHex}]: ");
            var seedInput = Console.ReadLine()?.Trim();
            byte[]? identitySeed = null;
            string identitySeedHex = currentSeedHex;

            if (string.IsNullOrWhiteSpace(seedInput))
            {
                identitySeed = !string.IsNullOrWhiteSpace(currentSeedHex) ? Convert.FromHexString(currentSeedHex) : null;
            }
            else if (seedInput.Equals("r", StringComparison.OrdinalIgnoreCase) || seedInput.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                identitySeed = null;
                identitySeedHex = string.Empty;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("    [提示] 将在启动时随机生成 PeerId。");
                Console.ResetColor();
            }
            else
            {
                var cleanSeed = new string(seedInput.Where(c => Uri.IsHexDigit(c)).ToArray());
                if (cleanSeed.Length == 64)
                {
                    identitySeed = Convert.FromHexString(cleanSeed);
                    identitySeedHex = cleanSeed.ToUpperInvariant();
                }
                else
                {
                    // 若用户输入普通文本，则通过 SHA256 自动派生 32 字节私钥种子
                    identitySeed = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(seedInput));
                    identitySeedHex = Convert.ToHexString(identitySeed);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("    [提示] 输入非64位十六进制，已通过 SHA-256 自动派生为 32 字节私钥种子。");
                    Console.ResetColor();
                }
            }

            // 是否配置高级参数
            bool hadAdvConfig = settings.EnableQuic || settings.EnableRelay || settings.EnablePubSub;
            Console.Write($"  * 是否需要配置高级网络参数 (QUIC/Relay/PubSub)？(y/N) [默认: {(hadAdvConfig ? "y" : "N")}]: ");
            var advInput = Console.ReadLine()?.Trim().ToLowerInvariant();
            bool enableQuic = settings.EnableQuic;
            int quicPort = settings.QuicPort;
            bool enableRelay = settings.EnableRelay;
            bool enablePubSub = settings.EnablePubSub;

            if (advInput == "y" || advInput == "yes" || (string.IsNullOrWhiteSpace(advInput) && hadAdvConfig))
            {
                Console.Write($"    - 是否启用 QUIC 传输？(y/N) [默认: {(enableQuic ? "y" : "N")}]: ");
                var quicInput = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(quicInput)) enableQuic = quicInput == "y" || quicInput == "yes";
                if (enableQuic)
                {
                    Console.Write($"    - QUIC 监听端口号 [默认: {quicPort}]: ");
                    var quicPortInput = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrWhiteSpace(quicPortInput))
                    {
                        var cleanQuicPort = new string(quicPortInput.Where(char.IsDigit).ToArray());
                        if (int.TryParse(cleanQuicPort, out var parsedQuicPort) && parsedQuicPort > 0 && parsedQuicPort <= 65535)
                        {
                            quicPort = parsedQuicPort;
                        }
                    }
                }

                Console.Write($"    - 是否启用 Relay 中继穿透？(y/N) [默认: {(enableRelay ? "y" : "N")}]: ");
                var relayInput = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(relayInput)) enableRelay = relayInput == "y" || relayInput == "yes";

                Console.Write($"    - 是否启用 PubSub (发布/订阅)？(y/N) [默认: {(enablePubSub ? "y" : "N")}]: ");
                var pubsubInput = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(pubsubInput)) enablePubSub = pubsubInput == "y" || pubsubInput == "yes";
            }

            // 更新并保存配置缓存
            settings.ListenAddress = listenIp;
            settings.TcpPort = tcpPort;
            settings.IdentitySeedHex = identitySeedHex;
            settings.EnableQuic = enableQuic;
            settings.QuicPort = quicPort;
            settings.EnableRelay = enableRelay;
            settings.EnablePubSub = enablePubSub;
            settings.Save();

            var options = new P2PNodeOptions
            {
                ListenAddress = listenIp,
                TcpPort = tcpPort,
                EnableTcp = true,
                EnableQuic = enableQuic,
                QuicPort = quicPort,
                EnableRelay = enableRelay,
                EnablePubSub = enablePubSub,
                IdentitySeed = identitySeed
            };

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[配置完成] 监听地址: {options.ListenAddress}:{options.TcpPort} (TCP)" +
                              (enableQuic ? $", {options.ListenAddress}:{options.QuicPort} (QUIC)" : "") +
                              (enableRelay ? ", 已启用 Relay" : "") +
                              (enablePubSub ? ", 已启用 PubSub" : "") +
                              (identitySeed != null ? $", 已配置固定身份种子 ({Convert.ToHexString(identitySeed)[..8]}...)" : ", 随机身份"));
            Console.WriteLine($"[配置已保存至 settings.json]");
            Console.ResetColor();

            return (options, settings);
        }

        /// <summary>
        /// 注册服务端各生命周期事件
        /// </summary>
        private static void RegisterServerEvents(BootstrapServer server)
        {
            server.Started += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Server] 引导节点已成功启动监听！");
                Console.ResetColor();
            };

            server.Closed += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Server] 引导节点服务已关闭。");
                Console.ResetColor();
            };

            server.OnExceptionOccurs += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [异常] {e.Exception.Message}");
                Console.ResetColor();
            };

            server.PeerConnected += (s, e) =>
            {
                var client = e.Client;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [Peer 接入] 远端 PeerId: {client.RemotePeerId}");
                Console.WriteLine($"                          远端地址: {client.RemoteAddress}");
                Console.WriteLine($"                          当前在线节点数: {server.ClientCount}");
                Console.ResetColor();

                // 绑定数据接收事件
                client.OnDataReceived += (cs, de) =>
                {
                    byte[] data = de.Data.ToArray();
                    string text = Encoding.UTF8.GetString(data);
                    string hex = BitConverter.ToString(data).Replace("-", " ");

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [收到数据] 来源: {client.RemotePeerId}");
                    Console.WriteLine($"  * 文本: {text}");
                    Console.WriteLine($"  * 十六进制: {hex} ({data.Length} 字节)");
                    Console.ResetColor();
                };

                // 绑定断开事件
                client.OnDisconnected += (cs, de) =>
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [Peer 断开] 节点: {client.RemotePeerId}，原因: {de.ReasonCode}");
                    Console.WriteLine($"                          当前在线节点数: {server.ClientCount}");
                    Console.ResetColor();
                };
            };
        }

        /// <summary>
        /// 打印服务启动后的关键信息与 Multiaddr
        /// </summary>
        private static void PrintServerInfo(BootstrapServer server)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine($"本地节点 PeerId : {server.LocalPeerId}");
            if (server.Options.IdentitySeed != null)
            {
                Console.WriteLine($"身份私钥种子   : {Convert.ToHexString(server.Options.IdentitySeed)} (已固定)");
            }
            else
            {
                Console.WriteLine($"身份私钥种子   : (随机生成)");
            }
            Console.WriteLine("可供客户端连接的 Multiaddr 地址列表：");
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var addr in server.ListenAddresses)
            {
                Console.WriteLine($"  -> {addr}");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("------------------------------------------------------------------");
            Console.ResetColor();
            Console.WriteLine("输入 'help' 或 '?' 可查看交互指令，输入 'exit' 或按 Ctrl+C 退出。\n");
        }

        /// <summary>
        /// 运行交互式命令循环
        /// </summary>
        private static async Task RunCommandLoopAsync(BootstrapServer server)
        {
            while (server.IsRunning)
            {
                Console.Write("p2p-server> ");
                var line = Console.ReadLine();
                if (line == null) break;

                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts[0].ToLowerInvariant();
                var arg = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                switch (cmd)
                {
                    case "help":
                    case "?":
                        PrintHelp();
                        break;

                    case "list":
                        PrintClients(server);
                        break;

                    case "addr":
                        PrintServerInfo(server);
                        break;

                    case "send":
                        await HandleSendCommandAsync(server, arg);
                        break;

                    case "broadcast":
                        await HandleBroadcastCommandAsync(server, arg);
                        break;

                    case "cls":
                    case "clear":
                        Console.Clear();
                        PrintBanner();
                        PrintServerInfo(server);
                        break;

                    case "exit":
                    case "quit":
                    case "q":
                        return;

                    default:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"未知命令: '{cmd}'。输入 'help' 查看支持的命令。");
                        Console.ResetColor();
                        break;
                }
            }
        }

        private static void PrintHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n支持的命令列表：");
            Console.WriteLine("  list                     - 列出当前所有已连接的客户端");
            Console.WriteLine("  addr                     - 查看本节点的监听地址与 PeerId");
            Console.WriteLine("  send <索引> <消息>       - 向指定序号的客户端发送文本消息 (如: send 0 hello)");
            Console.WriteLine("  broadcast <消息>         - 向所有在线客户端广播消息 (如: broadcast hello all)");
            Console.WriteLine("  cls / clear              - 清屏");
            Console.WriteLine("  exit / quit / q          - 停止服务并退出\n");
            Console.ResetColor();
        }

        private static void PrintClients(BootstrapServer server)
        {
            var clients = server.Clients;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n当前在线客户端列表 (共 {clients.Length} 个)：");
            if (clients.Length == 0)
            {
                Console.WriteLine("  (暂无连接的客户端)");
            }
            else
            {
                for (int i = 0; i < clients.Length; i++)
                {
                    var c = clients[i];
                    Console.WriteLine($"  [{i}] PeerId: {c.RemotePeerId}");
                    Console.WriteLine($"      地址: {c.RemoteAddress}");
                    Console.WriteLine($"      状态: {(c.IsConnected ? "已连接" : "断开")}");
                }
            }
            Console.WriteLine();
            Console.ResetColor();
        }

        private static async Task HandleSendCommandAsync(BootstrapServer server, string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                Console.WriteLine("用法: send <客户端序号> <消息内容>");
                return;
            }

            var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !int.TryParse(parts[0], out var index))
            {
                Console.WriteLine("用法: send <客户端序号> <消息内容>");
                return;
            }

            var clients = server.Clients;
            if (index < 0 || index >= clients.Length)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[错误] 无效的客户端序号: {index}。当前共 {clients.Length} 个客户端在线。");
                Console.ResetColor();
                return;
            }

            var target = clients[index];
            var msg = parts[1];
            byte[] data = Encoding.UTF8.GetBytes(msg);

            try
            {
                await target.SendAsync(data);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[发送成功] 已向客户端 [{index}] ({target.RemotePeerId}) 发送 {data.Length} 字节。");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[发送失败] {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task HandleBroadcastCommandAsync(BootstrapServer server, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine("用法: broadcast <消息内容>");
                return;
            }

            var clients = server.Clients;
            if (clients.Length == 0)
            {
                Console.WriteLine("[提示] 当前暂无在线客户端，无法广播。");
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(message);
            int successCount = 0;

            foreach (var client in clients)
            {
                try
                {
                    await client.SendAsync(data);
                    successCount++;
                }
                catch { }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[广播完成] 成功发送给 {successCount}/{clients.Length} 个客户端。");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 引导节点启动配置持久化模型
    /// </summary>
    internal class ServerSettings
    {
        public string ListenAddress { get; set; } = "0.0.0.0";
        public int TcpPort { get; set; } = 9831;
        public string IdentitySeedHex { get; set; } = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
        public string? CachedPeerId { get; set; }
        public bool EnableQuic { get; set; } = false;
        public int QuicPort { get; set; } = 9832;
        public bool EnableRelay { get; set; } = false;
        public bool EnablePubSub { get; set; } = false;

        public static string SettingsFilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

        public static ServerSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
                    var settings = JsonSerializer.Deserialize<ServerSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (settings != null) return settings;
                }
            }
            catch { }
            return new ServerSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFilePath, json, Encoding.UTF8);
            }
            catch { }
        }
    }
}
