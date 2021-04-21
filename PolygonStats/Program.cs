using System;
using System.Net;
using PolygonStats.Configuration;
using PolygonStats.HttpServer;

namespace PolygonStats
{
    class Program
    {
        static void Main(string[] args)
        {
            PolygonStats.HttpServer.HttpServer httpServer = null;
            if (ConfigurationManager.shared.config.httpSettings.enabled)
            {
                // Start http server
                httpServer = new PolygonStats.HttpServer.HttpServer(ConfigurationManager.shared.config.httpSettings.port);
            }


            Console.WriteLine($"TCP server port: {ConfigurationManager.shared.config.backendSettings.port}");

            // Create a new TCP chat server
            var server = new PolygonStatServer(IPAddress.Any, ConfigurationManager.shared.config.backendSettings.port);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server!");

            // Perform text input
            for (; ; )
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            if (httpServer != null)
            {
                httpServer.Stop();
            }
            Console.WriteLine("Done!");
        }
    }
}
