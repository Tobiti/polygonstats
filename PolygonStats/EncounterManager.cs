using System;
using Discord.Webhook;
using Discord;
using System.Threading;
using static System.Linq.Queryable;
using static System.Linq.Enumerable;
using PolygonStats.Configuration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using POGOProtos.Rpc;
using Microsoft.EntityFrameworkCore;

namespace PolygonStats
{
    public class EncounterManager : IDisposable {

        private static EncounterManager _shared;
        public static EncounterManager shared
        {
            get
            {
                if(_shared == null)
                {
                    _shared = new EncounterManager();
                }
                return _shared;
            }
        }
        private Timer cleanTimer;
        private Thread consumerThread;
        private MySQLConnectionManager connectionManager = new MySQLConnectionManager();
        private BlockingCollection<EncounterOutProto> blockingEncounterQueue = new BlockingCollection<EncounterOutProto>();
        private Dictionary<ulong, DateTime> alreadySendEncounters = new Dictionary<ulong, DateTime>();
        private readonly Object lockObj = new Object();

        public EncounterManager() {
            if (!ConfigurationManager.shared.config.encounterSettings.enabled) {
                return;
            }
            cleanTimer = new Timer(DoCleanTimer, null, TimeSpan.Zero, TimeSpan.FromMinutes(20));

            consumerThread = new Thread(EncounterConsumer);
            consumerThread.Start();
        }

        ~EncounterManager() {
            cleanTimer?.Dispose();
            consumerThread.Interrupt();
            consumerThread.Join();
        }
        private void DoCleanTimer(object state) {
            lock (lockObj)
            {
                List<ulong> deleteEncounters = alreadySendEncounters.Keys.Where(key =>
                {
                    return alreadySendEncounters[key].CompareTo(DateTime.Now.Subtract(TimeSpan.FromMinutes(20))) < 0;
                }).ToList();
                deleteEncounters.ForEach(id => alreadySendEncounters.Remove(id));
            }

            // Delete all encounter older than 20 minutes from db
            if (ConfigurationManager.shared.config.mysqlSettings.enabled 
                && ConfigurationManager.shared.config.encounterSettings.saveToDatabase) {
                using(var context = connectionManager.GetContext()) {
                    context.Database.ExecuteSqlRaw("DELETE FROM `Encounter` WHERE `timestamp` < DATE_SUB( CURRENT_TIME(), INTERVAL 20 MINUTE)");
                }
            }
        }

        public void AddEncounter(EncounterOutProto encounter) {
            if (!ConfigurationManager.shared.config.encounterSettings.enabled) {
                return;
            }
            blockingEncounterQueue.Add(encounter);
        }

        private void EncounterConsumer() {
            while(true) {
                List<EncounterOutProto> encounterList = new List<EncounterOutProto>();
                while (blockingEncounterQueue.Count > 0)
                {
                    EncounterOutProto encounter = blockingEncounterQueue.Take();
                    if (alreadySendEncounters.ContainsKey(encounter.Pokemon.EncounterId)) {
                        continue;
                    }
                    lock (lockObj)
                    {
                        alreadySendEncounters.Add(encounter.Pokemon.EncounterId, DateTime.Now);
                    }
                    encounterList.Add(encounter);

                    if (!ConfigurationManager.shared.config.mysqlSettings.enabled || !ConfigurationManager.shared.config.encounterSettings.saveToDatabase)
                    {
                        continue;
                    }
                    connectionManager.AddEncounterToDatabase(encounter);
                }
                if(encounterList.Count > 0) {
                    ConfigurationManager.shared.config.encounterSettings.discordWebhooks.ForEach(hook => SendDiscordWebhooks(hook, encounterList));
                    Thread.Sleep(3000);
                }
                Thread.Sleep(1000);
            }
        }

        private void SendDiscordWebhooks(Config.EncounterSettings.WebhookSettings webhook, List<EncounterOutProto> encounterList) {
            List<Discord.Embed> embeds = new List<Discord.Embed>();
            foreach(EncounterOutProto encounter in encounterList) {
                PokemonProto pokemon = encounter.Pokemon.Pokemon;
                if(webhook.filterByIV) {
                    if(pokemon.IndividualAttack < webhook.minAttackIV) {
                        continue;
                    }
                    if(pokemon.IndividualDefense < webhook.minDefenseIV) {
                        continue;
                    }
                    if(pokemon.IndividualStamina < webhook.minStaminaIV) {
                        continue;
                    }
                }
                if(webhook.filterByLocation) {
                    if(DistanceTo(webhook.latitude, webhook.longitude, encounter.Pokemon.Latitude, encounter.Pokemon.Longitude) > webhook.distanceInKm) {
                        continue;
                    }
                }

                EmbedBuilder eb = new EmbedBuilder(){
                    Author = new EmbedAuthorBuilder(){
                        Name = $"{pokemon.PokemonId.ToString("g")} (#{(int) pokemon.PokemonId})",
                    },
                    ThumbnailUrl = getPokemonImageUrl(pokemon.PokemonId),
                    Fields = new List<EmbedFieldBuilder>(){
                        new EmbedFieldBuilder(){
                            Name = "IV",
                            Value = $"Attack: {pokemon.IndividualAttack}\nDefense: {pokemon.IndividualDefense}\nStamina: {pokemon.IndividualStamina}"
                        },
                        new EmbedFieldBuilder() {
                            Name = "Coordinates",
                            Value = $"[{encounter.Pokemon.Latitude}, {encounter.Pokemon.Longitude}](https://maps.google.com/maps?q={encounter.Pokemon.Latitude},{encounter.Pokemon.Longitude})"
                        }
                    }
                };

                embeds.Add(eb.Build());
            }

            if(embeds.Count <= 0) {
                return;
            }

            int errors = 0;
            bool wasSended = false;
            
            while(!wasSended && errors <= 5) {
                try {
                    using(DiscordWebhookClient client = new DiscordWebhookClient(webhook.webhookUrl)) {
                        client.SendMessageAsync(null, false, embeds);
                        wasSended = true;
                    }
                } catch (Exception) {
                    errors++;
                }
            }
        }

        private string getPokemonImageUrl(HoloPokemonId pokemon)
        {
            switch (pokemon)
            {
                case HoloPokemonId.MrRime:
                    return $"https://img.pokemondb.net/sprites/go/normal/mr-rime.png";
                case HoloPokemonId.MrMime:
                    return $"https://img.pokemondb.net/sprites/bank/normal/mr-mime.png";
                case HoloPokemonId.MimeJr:
                    return $"https://img.pokemondb.net/sprites/bank/normal/mime-jr.png";
                default:
                    return $"https://img.pokemondb.net/sprites/bank/normal/{pokemon.ToString("g").ToLower().Replace("female", "-f").Replace("male", "-m")}.png";
            }
        }

        public double DistanceTo(double lat1, double lon1, double lat2, double lon2, char unit = 'K')
        {
            double rlat1 = Math.PI*lat1/180;
            double rlat2 = Math.PI*lat2/180;
            double theta = lon1 - lon2;
            double rtheta = Math.PI*theta/180;
            double dist = Math.Sin(rlat1)*Math.Sin(rlat2) + Math.Cos(rlat1) * Math.Cos(rlat2)*Math.Cos(rtheta);
            dist = Math.Acos(dist);
            dist = dist*180/Math.PI;
            dist = dist*60*1.1515;

            switch (unit)
            {
                case 'K': //Kilometers -> default
                    return dist*1.609344;
                case 'N': //Nautical Miles 
                    return dist*0.8684;
                case 'M': //Miles
                    return dist;
            }

            return dist;
        }

        public void Dispose()
        {
            cleanTimer?.Dispose();
            consumerThread?.Interrupt();
            consumerThread?.Join();
        }
    }
}
