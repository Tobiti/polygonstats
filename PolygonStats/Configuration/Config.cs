using System.Collections.Generic;

namespace PolygonStats.Configuration
{
    public class Config
    {
        public class DebugSettings
        {
            public bool Debug { get; set; }
            public bool ToFiles { get; set; }
            public bool DebugMessages { get; set; }
        }
        public DebugSettings Debug { get; set; }
        public class BackendSocketSettings
        {
            public int Port { get; set; }
        }
        public BackendSocketSettings Backend { get; set; }

        public class HttpSettings
        {
            public bool Enabled { get; set; }
            public int Port { get; set; }
            public bool ShowAccountNames { get; set; }
        }
        public HttpSettings Http { get; set; }

        public class RawDataSettings
        {
            public bool Enabled { get; set; }
            public string WebhookUrl { get; set; }
            public int DelayMs { get; set; }
        }
        public RawDataSettings RawData { get; set; }

        public class MysqlSettings
        {
            public bool Enabled { get; set; }
            public string dbConnectionString { get; set; }
        }
        public MysqlSettings MySql { get; set; }
        public class RocketMapSettings
        {
            public bool enabled { get; set; }
            public string dbConnectionString { get; set; }
        }
        public RocketMapSettings RocketMap { get; set; }

        public class EncounterSettings
        {
            public class WebhookSettings
            {
                public string webhookUrl { get; set; }
                public bool filterByIV { get; set; }
                public bool onlyEqual { get; set; }
                public int minAttackIV { get; set; }
                public int minDefenseIV { get; set; }
                public int minStaminaIV { get; set; }

                public bool filterByLocation { get; set; }
                public double latitude { get; set; }
                public double longitude { get; set; }
                public double distanceInKm { get; set; }
                public CustomLink customLink { get; set; }
            }

            public class CustomLink
            {
                public string title { get; set; }
                public string link { get; set; }
            }

            public bool Enabled { get; set; }
            public bool SaveToDatabase { get; set; }
            public List<WebhookSettings> DiscordWebhooks { get; set; }
        }

        public EncounterSettings Encounter { get; set; }

        public Config()
        {
            this.Debug = new DebugSettings()
            {
                Debug = false,
                ToFiles = false,
                DebugMessages = false
            };

            this.Backend = new BackendSocketSettings()
            {
                Port = 9838
            };

            this.Http = new HttpSettings()
            {
                Enabled = true,
                Port = 8888,
                ShowAccountNames = false
            };

            this.RawData = new RawDataSettings()
            {
                Enabled = false,
                WebhookUrl = "",
                DelayMs = 5000
            };

            this.MySql = new MysqlSettings()
            {
                Enabled = false,
                dbConnectionString = "server=localhost; port=3306; database=mysqldotnet; user=mysqldotnetuser; password=Pa55w0rd!; Persist Security Info=false; Connect Timeout=300"
            };

            this.RocketMap = new RocketMapSettings()
            {
                enabled = false,
                dbConnectionString = "server=localhost; port=3306; database=mysqldotnet; user=mysqldotnetuser; password=Pa55w0rd!; Persist Security Info=false; Connect Timeout=300"
            };

            this.Encounter = new EncounterSettings()
            {
                Enabled = false,
                SaveToDatabase = false,
                DiscordWebhooks = new List<EncounterSettings.WebhookSettings>(){
                    new EncounterSettings.WebhookSettings() {
                        webhookUrl = "discord webhook url",
                        filterByIV = false,
                        onlyEqual = false,
                        minAttackIV = 0,
                        minDefenseIV = 0,
                        minStaminaIV = 0,
                        filterByLocation = false,
                        latitude = 0.1,
                        longitude = 0.1,
                        distanceInKm = 20,
                        customLink = new EncounterSettings.CustomLink()
                        {
                            title = "Custom Link",
                            link = ""
                        }
                    }
                }
            };
        }
    }
}
