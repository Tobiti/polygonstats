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

        public Config()
        {
            debugSettings = new DebugSettings()
            {
                debug = false,
                toFiles = false
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
        }
    }
}
