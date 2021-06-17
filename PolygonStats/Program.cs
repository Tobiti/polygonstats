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

            if (ConfigurationManager.shared.config.debugSettings.debug)
            {
                loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();
            }
            else
            {
                loggerConfiguration = loggerConfiguration.MinimumLevel.Information();
            }
            Log.Logger = loggerConfiguration.CreateLogger();

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

            Log.Information($"TCP server port: {ConfigurationManager.shared.config.backendSettings.port}");

            // Create a new TCP chat server
            var server = new PolygonStatServer(IPAddress.Any, ConfigurationManager.shared.config.backendSettings.port);

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
