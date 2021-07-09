using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonStats.Configuration
{
    class Config
    {
        public class DebugSettings
        {
            public bool debug { get; set; }
            public bool toFiles { get; set; }
            public bool debugMessages { get; set; }
        }
        public DebugSettings debugSettings { get; set; }
        public class BackendSocketSettings
        {
            public int port { get; set; }
        }
        public BackendSocketSettings backendSettings { get; set; }

        public class HttpSettings
        {
            public bool enabled { get; set; }
            public int port { get; set; }
            public bool showAccountNames { get; set; }
        }
        public HttpSettings httpSettings { get; set; }
        public class MysqlSettings
        {
            public bool enabled { get; set; }
            public string dbConnectionString { get; set; }
        }
        public MysqlSettings mysqlSettings { get; set; }
        public class RocketMapSettings
        {
            public bool enabled { get; set; }
            public string dbConnectionString { get; set; }
        }
        public RocketMapSettings rocketMapSettings { get; set; }

        public class EncounterSettings {
            public class WebhookSettings {
                public string webhookUrl { get; set; }
                public bool filterByIV { get; set; }
                public int minAttackIV { get; set; }
                public int minDefenseIV { get; set; }
                public int minStaminaIV { get; set; }

                public bool filterByLocation { get; set; }
                public double latitude { get; set; }
                public double longitude { get; set; }
                public double distanceInKm { get; set; }
            }

            public bool enabled { get; set; }
            public bool saveToDatabase { get; set; }
            public List<WebhookSettings> discordWebhooks { get; set; }
        }

        public EncounterSettings encounterSettings { get; set; }

        public Config()
        {
            debugSettings = new DebugSettings()
            {
                debug = false,
                toFiles = false,
                debugMessages = false
            };

            backendSettings = new BackendSocketSettings()
            {
                port = 9838
            };

            httpSettings = new HttpSettings()
            {
                enabled = true,
                port = 8888,
                showAccountNames = false
            };

            mysqlSettings = new MysqlSettings()
            {
                enabled = false,
                dbConnectionString = "server=localhost; port=3306; database=mysqldotnet; user=mysqldotnetuser; password=Pa55w0rd!; Persist Security Info=false; Connect Timeout=300"
            };

            rocketMapSettings = new RocketMapSettings()
            {
                enabled = false,
                dbConnectionString = "server=localhost; port=3306; database=mysqldotnet; user=mysqldotnetuser; password=Pa55w0rd!; Persist Security Info=false; Connect Timeout=300"
            };

            encounterSettings = new EncounterSettings() 
            {
                enabled = false,
                saveToDatabase = false,
                discordWebhooks = new List<EncounterSettings.WebhookSettings>(){
                    new EncounterSettings.WebhookSettings() {
                        webhookUrl = "discord webhook url",
                        filterByIV = false,
                        minAttackIV = 0,
                        minDefenseIV = 0,
                        minStaminaIV = 0,
                        filterByLocation = false,
                        latitude = 0.1,
                        longitude = 0.1,
                        distanceInKm = 20
                    }
                }
            };
        }
    }
}
