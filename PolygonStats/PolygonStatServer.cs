using System;
using System.Net;
using System.Net.Sockets;
using NetCoreServer;
using Serilog;

namespace PolygonStats
{
    class PolygonStatServer : TcpServer
    {
        public PolygonStatServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new ClientSession(this); }

        protected override void OnError(SocketError error)
        {
            Log.Error($"Chat TCP server caught an error with code {error}");
        }
    }
}
