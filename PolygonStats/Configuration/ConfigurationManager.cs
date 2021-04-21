using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PolygonStats.Configuration
{
    class ConfigurationManager
    {
        private static ConfigurationManager _shared;
        public static ConfigurationManager shared
        {
            get
            {
                if(_shared == null)
                {
                    _shared = new ConfigurationManager();
                }
                return _shared;
            }
        }
        public Config config { get; set; }
        private string jsonSource;

        public ConfigurationManager()
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configurationBuilder.AddJsonFile("Config.json", true, false);
            IConfiguration buildedConfig = configurationBuilder.Build();

            jsonSource = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Config.json";

            config = new Config();
            buildedConfig.Bind(config);

            if (!File.Exists(jsonSource))
            {
                Save();
            }

            Console.WriteLine("Config was loaded!");
        }

        public void Save()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;
            // open config file
            string json = JsonSerializer.Serialize(config, options);
            //write string to file
            System.IO.File.WriteAllText(jsonSource, json);

            Console.WriteLine("Config was created!");
        }
    }
}
