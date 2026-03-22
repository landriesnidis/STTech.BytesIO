using System;
using System.Threading.Tasks;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Tcp;

namespace ModbusProxyTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Modbus Transparent Proxy Test ===");

            var downstreamServer = new ModbusTcpServer(ModbusProtocolFormat.RTU);
            downstreamServer.SlaveId = 2;
            downstreamServer.Port = 5021;
            downstreamServer.Start();
            Console.WriteLine($"[System] Downstream Server (SlaveId = 2) started on port 5021.");

            var upstreamClientConnectToDownstream = new ModbusTcpClient(ModbusProtocolFormat.RTU);
            upstreamClientConnectToDownstream.Host = "127.0.0.1";
            upstreamClientConnectToDownstream.Port = 5021;

            var proxyServer = new ModbusTcpServer(ModbusProtocolFormat.RTU);
            proxyServer.SlaveId = 1;
            proxyServer.Port = 5020;
            proxyServer.DownstreamClient = upstreamClientConnectToDownstream;
            
            proxyServer.Start();
            proxyServer.InnerServer.ClientConnected += (s, e) => {
                Console.WriteLine("[Proxy Server] Client Connected");
                e.Client.OnDataReceived += (s2, e2) => Console.WriteLine($"[Proxy Server] Raw Recv: {e2.Data.Length} bytes");
                e.Client.OnExceptionOccurs += (s2, e2) => Console.WriteLine($"[Proxy Server] EXCEPTION in Client: {e2.Exception}");
            };
            upstreamClientConnectToDownstream.OnDataSent += (s, e) => Console.WriteLine($"[Proxy-Downstream] Sent: {BitConverter.ToString(e.Data)}");
            upstreamClientConnectToDownstream.OnDataReceived += (s, e) => Console.WriteLine($"[Proxy-Downstream] Recv: {BitConverter.ToString(e.Data.ToArray())}");
            upstreamClientConnectToDownstream.Connect();

            Console.WriteLine($"[System] Proxy Server (SlaveId = 1) started on port 5020 and connected to downstream.");

            var masterClient = new ModbusTcpClient(ModbusProtocolFormat.RTU);
            masterClient.Host = "127.0.0.1";
            masterClient.Port = 5020;
            masterClient.OnDataSent += (s, e) => Console.WriteLine($"[Master] Sent: {BitConverter.ToString(e.Data)}");
            masterClient.Connect();
            Console.WriteLine("[System] Master Client connected to Proxy Server.");

            await Task.Delay(500);

            Console.WriteLine("\n--- Testing Local Slave (ID=1) ---");
            var reply1 = masterClient.ReadHoldingRegister(1, 200, 1);
            Console.WriteLine($"[Master] Local Slave Response Status: {reply1.Status}");

            await Task.Delay(500);

            Console.WriteLine("\n--- Testing Downstream Slave (ID=2) ---");
            var reply2 = masterClient.ReadHoldingRegister(2, 100, 1);
            Console.WriteLine($"[Master] Downstream Slave Response Status: {reply2.Status}");

            await Task.Delay(500);
            Console.WriteLine("Test Finished.");
        }
    }
}
