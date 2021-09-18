using PolygonStats.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Serilog;
using System.Collections.Generic;

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
        private ConcurrentDictionary<String, BlockingCollection<RawData>> blockingRawDataDictionary = new ConcurrentDictionary<String, BlockingCollection<RawData>>();
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
            BlockingCollection<RawData> collection = blockingRawDataDictionary.GetOrAdd(message.origin, new BlockingCollection<RawData>());
            collection.Add(message.rawData);
        }

        private void RawDataConsumer()
        {
            while (true)
            {
                foreach(String key in blockingRawDataDictionary.Keys)
                {
                    BlockingCollection<RawData> collection;
                    if (blockingRawDataDictionary.TryGetValue(key, out collection))
                    {
                        List<RawData> rawDataList = new List<RawData>();
                        RawData rawData;
                        while (collection.TryTake(out rawData))
                        {
                            rawDataList.Add(rawData);
                        }
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ConfigurationManager.shared.config.rawDataSettings.webhookUrl);
                        request.Headers.Add("origin", key);
                        request.Content = new StringContent(JsonSerializer.Serialize(rawDataList.ToArray()), Encoding.UTF8, "application/json"); ;
                        Log.Debug($"Send Request:\n{JsonSerializer.Serialize(request)}");
                        try
                        {
                            HttpResponseMessage response = _client.Send(request);
                            Log.Debug($"Response:{JsonSerializer.Serialize(response)}");
                        } catch (Exception e)
                        {
                            Log.Information($"Request error: {e.Message}");
                            collection.AddRange(rawDataList);
                            break;
                        }

                    }
                }
                Thread.Sleep(Math.Min(1000, ConfigurationManager.shared.config.rawDataSettings.delayMs));
            }
        }
    }
}
