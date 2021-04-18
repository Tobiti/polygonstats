using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using POGOProtos.Rpc;

namespace PolygonStats
{
    public class Payload
    {
        public int type { get; set; }
        public string proto { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public object timestamp { get; set; }
        public string token { get; set; }
        public string level { get; set; }
        public string account_name { get; set; }
        public string account_id { get; set; }

        public byte[] getDate()
        {
            return Convert.FromBase64String(this.proto);
        }
        public Method getMethodType()
        {
            return (Method) this.type;
        }
    }

    public class MessageObject
    {
        public List<Payload> payloads { get; set; }
        public string key { get; set; }
    }
}
