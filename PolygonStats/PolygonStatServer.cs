using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetCoreServer;
using Serilog;

namespace PolygonStats
{
    class PolygonStatServer : TcpServer
    {
        private Timer cleanTimer;
        private int currentCount = int.MinValue;

        public PolygonStatServer(IPAddress address, int port) : base(address, port)
        {
            cleanTimer = new Timer(DoCleanTimer, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        protected override TcpSession CreateSession() { return new ClientSession(this); }

        protected override void OnError(SocketError error)
        {
            Log.Error($"Chat TCP server caught an error with code {error}");
        }
        private void DoCleanTimer(object state)
        {
            foreach (ClientSession session in this.Sessions.Values)
            {
                if (!session.isConnected())
                {
                    session.Dispose();
                }
            }
            if (currentCount != this.Sessions.Count)
            {
                currentCount = this.Sessions.Count;
                Log.Information($"Currently Connected: {currentCount}");
            }
        }

        protected override void Dispose(bool disposingManagedResources)
        {
            base.Dispose(disposingManagedResources);
            cleanTimer?.Dispose();
        }
    }
}
