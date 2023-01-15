using System;
using System.Text.RegularExpressions;
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
            if (!ConfigurationManager.Shared.Config.Encounter.Enabled) {
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
            if (ConfigurationManager.Shared.Config.MySql.Enabled 
                && ConfigurationManager.Shared.Config.Encounter.SaveToDatabase) {
                using(var context = connectionManager.GetContext()) {
                    context.Database.ExecuteSqlRaw("DELETE FROM `Encounter` WHERE `timestamp` < DATE_SUB( CURRENT_TIME(), INTERVAL 20 MINUTE)");
                }
            }
        }

        public void AddEncounter(EncounterOutProto encounter) {
            if (!ConfigurationManager.Shared.Config.Encounter.Enabled) {
                return;
            }
            blockingEncounterQueue.Add(encounter);
        }

        private void EncounterConsumer() {
            while(true) {
                List<EncounterOutProto> encounterList = new List<EncounterOutProto>();

                if (ConfigurationManager.Shared.Config.MySql.Enabled && ConfigurationManager.Shared.Config.Encounter.SaveToDatabase)
                {
                    using (var context = new MySQLContext())
                    {
                        while (blockingEncounterQueue.Count > 0)
                        {
                            EncounterOutProto encounter = blockingEncounterQueue.Take();
                            if (alreadySendEncounters.ContainsKey(encounter.Pokemon.EncounterId))
                            {
                                continue;
                            }
                            lock (lockObj)
                            {
                                alreadySendEncounters.Add(encounter.Pokemon.EncounterId, DateTime.Now);
                            }
                            encounterList.Add(encounter);
                            connectionManager.AddEncounterToDatabase(encounter, context);
                        }
                        context.SaveChanges();
                    }
                } else 
                {
                    while (blockingEncounterQueue.Count > 0)
                    {
                        EncounterOutProto encounter = blockingEncounterQueue.Take();
                        if (alreadySendEncounters.ContainsKey(encounter.Pokemon.EncounterId))
                        {
                            continue;
                        }
                        lock (lockObj)
                        {
                            alreadySendEncounters.Add(encounter.Pokemon.EncounterId, DateTime.Now);
                        }
                        encounterList.Add(encounter);
                    }
                }
                if(encounterList.Count > 0) {
                    ConfigurationManager.Shared.Config.Encounter.DiscordWebhooks.ForEach(hook => SendDiscordWebhooks(hook, encounterList));
                    Thread.Sleep(3000);
                }
                Thread.Sleep(1000);
            }
        }

        private void SendDiscordWebhooks(Configuration.Config.EncounterSettings.WebhookSettings webhook, List<EncounterOutProto> encounterList) {
            List<Discord.Embed> embeds = new List<Discord.Embed>();
            foreach(EncounterOutProto encounter in encounterList) {
                PokemonProto pokemon = encounter.Pokemon.Pokemon;
                if(webhook.FilterByIV) {
                    if (webhook.OnlyEqual)
                    {
                        if (pokemon.IndividualAttack != webhook.MinAttackIV)
                        {
                            continue;
                        }
                        if (pokemon.IndividualDefense != webhook.MinDefenseIV)
                        {
                            continue;
                        }
                        if (pokemon.IndividualStamina != webhook.MinStaminaIV)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (pokemon.IndividualAttack < webhook.MinAttackIV)
                        {
                            continue;
                        }
                        if (pokemon.IndividualDefense < webhook.MinDefenseIV)
                        {
                            continue;
                        }
                        if (pokemon.IndividualStamina < webhook.MinStaminaIV)
                        {
                            continue;
                        }
                    }
                }
                if(webhook.FilterByLocation) {
                    if(DistanceTo(webhook.Latitude, webhook.Longitude, encounter.Pokemon.Latitude, encounter.Pokemon.Longitude) > webhook.DistanceInKm) {
                        continue;
                    }
                }
                if (webhook.FilterByShiny)
                {
                    if (pokemon.PokemonDisplay.Shiny != webhook.IsShiny) { 
                        continue; 
                    }
                }

                String customLink = "";
                if (webhook.CustomLink != null)
                {
                    customLink = $"[{webhook.CustomLink.Title}]({getReplacedCustomLink(webhook.CustomLink.Link, encounter)})";
                }
                EmbedBuilder eb = new EmbedBuilder(){
                    Title = $"Level {getPokemonLevel(pokemon.CpMultiplier)} {pokemon.PokemonId.ToString("g")} (#{(int) pokemon.PokemonId})",
                    Author = new EmbedAuthorBuilder(){

                        Name = $"{Math.Round(encounter.Pokemon.Latitude,5)}, {Math.Round(encounter.Pokemon.Longitude,5)}",
                    },
                    ThumbnailUrl = getPokemonImageUrl(pokemon.PokemonId),
                    Fields = new List<EmbedFieldBuilder>(){
                        new EmbedFieldBuilder(){
                            Name = "Stats",
                            Value = $"CP: {pokemon.Cp}\nIVs:{pokemon.IndividualAttack}/{pokemon.IndividualDefense}/{pokemon.IndividualStamina} | {getIV(pokemon.IndividualAttack,pokemon.IndividualDefense,pokemon.IndividualStamina)}%"
                        },
                        new EmbedFieldBuilder(){
                            Name = "Moves",
                            Value = $"Fast: {formatMove(pokemon.Move1.ToString())}\nCharge: {formatMove(pokemon.Move2.ToString())}"
                        },
                        new EmbedFieldBuilder() {
                            Name = "Links",
                            Value = $"[Google Maps](https://maps.google.com/maps?q={Math.Round(encounter.Pokemon.Latitude,5)},{Math.Round(encounter.Pokemon.Longitude,5)}) [Apple Maps](http://maps.apple.com/?daddr={Math.Round(encounter.Pokemon.Latitude,5)},{Math.Round(encounter.Pokemon.Longitude,5)}) {customLink}"
                        }

                    },
                    Color = Color.Blue
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
                    using(DiscordWebhookClient client = new DiscordWebhookClient(webhook.WebhookUrl)) {
                        client.SendMessageAsync(null, false, embeds);
                        wasSended = true;
                    }
                } catch (Exception) {
                    errors++;
                }
            }
        }
        private string getReplacedCustomLink(String customlink, EncounterOutProto encounter)
        {
            String link = customlink.Replace("{latitude}", Math.Round(encounter.Pokemon.Latitude, 5).ToString());
            link = link.Replace("{longitude}", Math.Round(encounter.Pokemon.Longitude, 5).ToString());
            link = link.Replace("{encounterId}", encounter.Pokemon.EncounterId.ToString());
            return link;
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
        
        private double getPokemonLevel(float cpMultiplier)
        {
            double pokemonLevel;
            if (cpMultiplier < 0.734) {
                pokemonLevel = 58.35178527 * cpMultiplier * cpMultiplier - 2.838007664 * cpMultiplier + 0.8539209906;
            } else {
                pokemonLevel = 171.0112688 * cpMultiplier - 95.20425243;
            }
            pokemonLevel = (Math.Round(pokemonLevel) * 2) / 2;
            return pokemonLevel;
        }

        private double getIV (int atk, int def, int sta)
        {
            double iv = ((atk+def+sta)/45f)*100f;
            return Math.Round(iv,1);
        }

        private string splitUppercase(string input) {
            var regex = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])");
            return regex.Replace(input, " ");
        }
        private string formatMove(string move)
        {
            move =  move.Replace("Fast","");
            return splitUppercase(move);
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
