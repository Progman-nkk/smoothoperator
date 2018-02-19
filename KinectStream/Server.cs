using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SmoothStream
{

    class Server
    {
        System.Net.IPEndPoint endPoint;
        TcpListener listener;
        public Server(int portNumber, long address)
        {
            endPoint = new System.Net.IPEndPoint(address, portNumber);
            listener = new TcpListener(endPoint);
            listener.Start();
        }

    }
}
