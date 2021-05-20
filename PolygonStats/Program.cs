using System;
using System.Data.Entity;
using System.Net;
using System.Threading;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PolygonStats.Configuration;

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

            // Init db
            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                MySQLConnectionManager manager = new MySQLConnectionManager();
                var migrator = manager.GetContext().Database.GetService<IMigrator>();
                migrator.Migrate();
                manager.GetContext().Database.EnsureCreated();
                manager.GetContext().SaveChanges();
            }

            Console.WriteLine($"TCP server port: {ConfigurationManager.shared.config.backendSettings.port}");

            // Create a new TCP chat server
            var server = new PolygonStatServer(IPAddress.Any, ConfigurationManager.shared.config.backendSettings.port);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Use CTRL+C to close the software!");

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();

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
