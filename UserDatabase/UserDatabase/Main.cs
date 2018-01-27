using System;
using System.Net.Sockets;
using UniversalServer.Interfaces;

namespace vortexstudio.universalserver.userdatabase
{
    public class UserDatabase : IServerPlugin
    {
        public string Name { get { return "userdatabase"; } }

        #region OnLoad

        //Activates when the server loads the plugin
        public void onLoad(string ServerDirectory)
        {
            
        }

        #endregion

        #region OnInvoke

        //Activates when the plugin has been invoked
        public void Invoke(TcpListener _serverSocket, TcpClient _clientSocket, int port, string UserRequest, string ServerDirectory)
        {
            Console.WriteLine(UserRequest);
            Console.WriteLine(ServerDirectory);
        }

        #endregion
    }
}
