using System;
using System.Collections.Generic;
using static POGOProtos.Rpc.AllTypesAndMessagesResponsesProto.Types;

namespace PolygonStats
{
    public class Payload
    {
        public int type { get; set; }
        public string proto { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public long timestamp { get; set; }
        public string token { get; set; }
        public string level { get; set; }
        public string account_name { get; set; }
        public string account_id { get; set; }

        public byte[] getBytes()
        {
            return Convert.FromBase64String(this.proto);
        }
        public AllResquestTypesProto getMethodType()
        {
            return (AllResquestTypesProto)this.type;
        }
    }

    public class MessageObject
    {
        public List<Payload> payloads { get; set; }
        public string key { get; set; }
    }
}
