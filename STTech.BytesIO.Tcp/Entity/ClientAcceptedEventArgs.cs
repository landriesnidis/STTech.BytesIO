using STTech.BytesIO.Core;
using System;

namespace STTech.BytesIO.Tcp
{
    public class ClientAcceptedEventArgs : EventArgs
    {
        public System.Net.Sockets.Socket ClientSocket { get;  }

        public ClientAcceptedEventArgs(System.Net.Sockets.Socket clientSocket)
        {
            ClientSocket = clientSocket;
        }
    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public ClientConnectedEventArgs(System.Net.Sockets.Socket clientSocket, TcpClient client)
        {
            Socket = clientSocket;
            Client = client;
        }

        public System.Net.Sockets.Socket Socket { get;  }
        public TcpClient Client { get;  }
    }

    public class ClientDisconnectedEventArgs : DisconnectedEventArgs
    {
        public ClientDisconnectedEventArgs(TcpClient client)
        {
            Client = client;
        }

        public TcpClient Client { get;  }
    }
}
