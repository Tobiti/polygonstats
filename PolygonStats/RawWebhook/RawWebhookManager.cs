using PolygonStats.Configuration;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace PolygonStats.RawWebhook
{
    class RawWebhookManager : IDisposable
    {

        private static RawWebhookManager _shared;
        public static RawWebhookManager shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = new RawWebhookManager();
                }
                return _shared;
            }
        }

        private HttpClient _client;

        public void Dispose()
        {
        }

        private Thread consumerThread;
        private BlockingCollection<RawDataMessage> blockingRawDataQueue = new BlockingCollection<RawDataMessage>();
        private readonly Object lockObj = new Object();

        public RawWebhookManager()
        {
            if (!ConfigurationManager.shared.config.rawDataSettings.enabled)
            {
                return;
            }
            _client = new HttpClient();
            consumerThread = new Thread(RawDataConsumer);
            consumerThread.Start();
        }

        ~RawWebhookManager()
        {
            consumerThread.Interrupt();
            consumerThread.Join();
        }

        public void AddRawData(RawDataMessage message)
        {
            if (!ConfigurationManager.shared.config.rawDataSettings.enabled)
            {
                return;
            }
            blockingRawDataQueue.Add(message);
        }

        private void RawDataConsumer()
        {
            while (true)
            {
                RawDataMessage rawDataMessage;
                while (blockingRawDataQueue.TryTake(out rawDataMessage))
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ConfigurationManager.shared.config.rawDataSettings.webhookUrl);
                    request.Headers.Add("origin", rawDataMessage.origin);
                    Log.Information($"Raw Data:\n{JsonSerializer.Serialize(rawDataMessage.rawData)}");
                    request.Content = new StringContent(JsonSerializer.Serialize(rawDataMessage.rawData), Encoding.UTF8);
                    Log.Information($"Send Request:\n{JsonSerializer.Serialize(request)}");
                    HttpResponseMessage response = _client.Send(request);
                    Log.Information($"Response:{JsonSerializer.Serialize(response)}");
                }
            }
        }
    }
}
