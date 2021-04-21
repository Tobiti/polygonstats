using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolygonStats.Configuration
{
    class Config
    {
        public class BackendSocketSettings
        {
            public int port { get; set; }
        }
        public BackendSocketSettings backendSettings { get; set; }

        public class HttpSettings
        {
            public bool enabled { get; set; }
            public int port { get; set; }
        }
        public HttpSettings httpSettings { get; set; }
        public class MysqlSettings
        {
            public bool enabled { get; set; }
            public string ipAddress { get; set; }
            public int port { get; set; }
            public string database { get; set; }
            public string username { get; set; }
            public string password { get; set; }
        }
        public MysqlSettings mysqlSettings { get; set; }

        public Config()
        {
            backendSettings = new BackendSocketSettings()
            {
                port = 9838
            };

            httpSettings = new HttpSettings()
            {
                enabled = true,
                port = 8888
            };

            mysqlSettings = new MysqlSettings()
            {
                enabled = false,
                ipAddress = "localhost",
                port = 8888,
                database = "",
                username = "",
                password = ""
            };
        }
    }
}
