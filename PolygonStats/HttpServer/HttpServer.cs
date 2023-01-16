// MIT License - Copyright (c) 2016 Can Güney Aksakalli
// https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html

using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;

namespace PolygonStats.HttpServer
{
    class HttpServer
    {
        private Thread _serverThread;
        private HttpListener _listener;
        private int _port;

        public int Port
        {
            get { return _port; }
            private set { }
        }

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public HttpServer(int port)
        {
            this.Initialize(port);
        }

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public HttpServer()
        {
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Initialize(port);
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://*:{_port.ToString()}/");
            _listener.Start();
            while (_listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception) { }
            }
        }

        private void Process(HttpListenerContext context)
        {
            try
            {

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((context.Request.HttpMethod == "POST") && context.Request.Url.AbsolutePath.StartsWith("/remove"))
                {
                    string accName = context.Request.Url.AbsolutePath.Replace("/remove-", "");
                    StatManager.sharedInstance.removeEntry(accName);
                    context.Response.Redirect("/");
                }

                byte[] data = Encoding.UTF8.GetBytes(PageData.getData(context.Request.Url.AbsolutePath.StartsWith("/admin")));

                context.Response.ContentType = "text/html";
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = data.LongLength;

                context.Response.OutputStream.Write(data, 0, data.Length);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Flush();
            }
            catch (Exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            context.Response.OutputStream.Close();
        }

        private void Initialize(int port)
        {
            this._port = port;
            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }
    }
}
