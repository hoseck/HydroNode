using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HydroNode.Socket
{
    internal class ClientManager
    {
        private readonly List<TcpClient> _clients = new();

        public void AddClient(TcpClient client)
        {
            _clients.Add(client);
        }

        public void RemoveClient(TcpClient client)
        {
            _clients.Remove(client);
        }

        public int GetClientCount() => _clients.Count;
    }
}
