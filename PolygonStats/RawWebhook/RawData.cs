using System;

namespace PolygonStats.RawWebhook
{
    class RawData
    {
        public int type { get; set; }
        public long timestamp { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public bool raw { get; set; }
        public string payload { get; set; }
    }
    class RawDataMessage
    {
        public String origin { get; set; }
        public RawData rawData { get; set; }
    }
}
