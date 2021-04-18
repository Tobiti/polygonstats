using System;
using System.Net;
using PolygonStats.HttpServer;

namespace PolygonStats
{
    class Program
    {
        static void Main(string[] args)
        {
            // Start http server
            PolygonStats.HttpServer.HttpServer httpServer = new PolygonStats.HttpServer.HttpServer(8888);

            // TCP server port
            int port = 9838;
            if (args.Length > 0)
                port = int.Parse(args[0]);

            Console.WriteLine($"TCP server port: {port}");

            Console.WriteLine();

            // Create a new TCP chat server
            var server = new PolygonStatServer(IPAddress.Any, port);

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
            httpServer.Stop();
            Console.WriteLine("Done!");
        }
    }
}
