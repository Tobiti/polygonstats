using System;
using Serilog;
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
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/main.log", rollingInterval: RollingInterval.Day);

            if (ConfigurationManager.Shared.Config.Debug.Debug)
            {
                loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();
            }
            else
            {
                loggerConfiguration = loggerConfiguration.MinimumLevel.Information();
            }
            Log.Logger = loggerConfiguration.CreateLogger();

            PolygonStats.HttpServer.HttpServer httpServer = null;
            if (ConfigurationManager.Shared.Config.Http.Enabled)
            {
                // Start http server
                httpServer = new PolygonStats.HttpServer.HttpServer(ConfigurationManager.Shared.Config.Http.Port);
            }

            // Init db
            if (ConfigurationManager.Shared.Config.MySql.Enabled)
            {
                MySQLConnectionManager manager = new MySQLConnectionManager();
                var migrator = manager.GetContext().Database.GetService<IMigrator>();
                migrator.Migrate();
                manager.GetContext().Database.EnsureCreated();
                manager.GetContext().SaveChanges();
            }

            Log.Information($"TCP server port: {ConfigurationManager.Shared.Config.Backend.Port}");

            // Create a new TCP chat server
            var server = new PolygonStatServer(IPAddress.Any, ConfigurationManager.Shared.Config.Backend.Port);

            // Start the server
            Log.Information("Server starting...");
            server.Start();
            Log.Information("Done!");

            Log.Information("Use CTRL+C to close the software!");

            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();

            // Stop the server
            Log.Information("Server stopping...");
            server.Stop();
            EncounterManager.shared.Dispose();
            if (httpServer != null)
            {
                httpServer.Stop();
            }
            Log.Information("Done!");
        }
    }
}
