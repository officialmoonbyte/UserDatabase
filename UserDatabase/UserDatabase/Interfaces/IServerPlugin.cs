using System.Net.Sockets;

namespace UniversalServer.Interfaces
{
    public interface IServerPlugin
    {
        string Name { get; }
        void Invoke(TcpListener _serverSocket, TcpClient _clientSocket, int port, string UserRequest, string ServerDirectory);
        void onLoad(string ServerDirectory);
    }
}
