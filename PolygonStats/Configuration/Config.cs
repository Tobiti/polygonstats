using System.Collections.Generic;

namespace PolygonStats.Configuration
{
    public class Config
    {
        public DebugSettings Debug { get; set; }
        public BackendSocketSettings Backend { get; set; }
        public HttpSettings Http { get; set; }
        public RawDataSettings RawData { get; set; }
        public MysqlSettings MySql { get; set; }
        public MadExportSettings MadExport { get; set; }
        public EncounterSettings Encounter { get; set; }


        public class DebugSettings
        {
            public bool Debug { get; set; }
            public bool ToFiles { get; set; }
            public bool DebugMessages { get; set; }
        }

        public class BackendSocketSettings
        {
            public int Port { get; set; }
        }

        public class HttpSettings
        {
            public bool Enabled { get; set; }
            public int Port { get; set; }
            public bool ShowAccountNames { get; set; }
        }

        public class RawDataSettings
        {
            public bool Enabled { get; set; }
            public string WebhookUrl { get; set; }
            public int DelayMs { get; set; }
        }

        public class MysqlSettings
        {
            public bool Enabled { get; set; }
            public string ConnectionString { get; set; }
        }

        public class MadExportSettings
        {
            public bool Enabled { get; set; }
            public string ConnectionString { get; set; }
        }

        public class EncounterSettings
        {
            public class WebhookSettings
            {
                public string WebhookUrl { get; set; }
                public bool FilterByIV { get; set; }
                public bool OnlyEqual { get; set; }
                public int MinAttackIV { get; set; }
                public int MinDefenseIV { get; set; }
                public int MinStaminaIV { get; set; }

                public bool FilterByLocation { get; set; }
                public double Latitude { get; set; }
                public double Longitude { get; set; }
                public double DistanceInKm { get; set; }
                public CustomLink CustomLink { get; set; }
            }

            public class CustomLink
            {
                public string Title { get; set; }
                public string Link { get; set; }
            }

            public bool Enabled { get; set; }
            public bool SaveToDatabase { get; set; }
            public List<WebhookSettings> DiscordWebhooks { get; set; }
        }


        public Config()
        {
            Debug = new DebugSettings()
            {
                Debug = false,
                ToFiles = false,
                DebugMessages = false
            };

            Backend = new BackendSocketSettings()
            {
                Port = 9838
            };

            Http = new HttpSettings()
            {
                Enabled = true,
                Port = 8888,
                ShowAccountNames = false
            };

            RawData = new RawDataSettings()
            {
                Enabled = false,
                WebhookUrl = "",
                DelayMs = 5000
            };

            MySql = new MysqlSettings()
            {
                Enabled = false,
                ConnectionString = "server=localhost; port=3306; database=mysqldotnet; user=mysqldotnetuser; password=Pa55w0rd!; Persist Security Info=false; Connect Timeout=300"
            };

            MadExport = new MadExportSettings()
            {
                Enabled = false,
                ConnectionString = "server=localhost; port=3306; database=mysqldotnet; user=mysqldotnetuser; password=Pa55w0rd!; Persist Security Info=false; Connect Timeout=300"
            };

            Encounter = new EncounterSettings()
            {
                Enabled = false,
                SaveToDatabase = false,
                DiscordWebhooks = new List<EncounterSettings.WebhookSettings>(){
                    new EncounterSettings.WebhookSettings() {
                        WebhookUrl = "discord webhook url",
                        FilterByIV = false,
                        OnlyEqual = false,
                        MinAttackIV = 0,
                        MinDefenseIV = 0,
                        MinStaminaIV = 0,
                        FilterByLocation = false,
                        Latitude = 0.1,
                        Longitude = 0.1,
                        DistanceInKm = 20,
                        CustomLink = new EncounterSettings.CustomLink()
                        {
                            Title = "Custom Link",
                            Link = ""
                        }
                    }
                }
            };
        }
    }
}
