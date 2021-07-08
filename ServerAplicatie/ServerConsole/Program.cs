using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace ServerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // Server s = new Server("172.28.46.198", 6666, 7777);
            Server s = new Server("127.0.0.1", 6666, 7777);
        }
    }
}