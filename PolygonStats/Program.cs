using System;
using System.Net;
using PolygonStats.HttpServer;

namespace PolygonStats
{
    class Program
    {
        static void Main(string[] args)
        {
            // TCP socket server port
            int socketPort = 9838;
            if (args.Length > 0)
                socketPort = int.Parse(args[0]);

            // HTTP socket server port
            int httpPort = 8888;
            if (args.Length > 1)
                httpPort = int.Parse(args[1]);

            // Start http server
            PolygonStats.HttpServer.HttpServer httpServer = new PolygonStats.HttpServer.HttpServer(httpPort);


            Console.WriteLine($"TCP server port: {socketPort}");

            Console.WriteLine();

            // Create a new TCP chat server
            var server = new PolygonStatServer(IPAddress.Any, socketPort);

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
