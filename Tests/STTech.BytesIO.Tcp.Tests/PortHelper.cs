using System.Net;
using System.Net.Sockets;

namespace STTech.BytesIO.Tcp.Tests
{
    internal static class PortHelper
    {
        public static int GetAvailablePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
