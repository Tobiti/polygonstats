using System;
using System.Text;
using System.Net.Sockets;
using NetCoreServer;
using System.Text.Json;
using POGOProtos.Rpc;
using Google.Protobuf.Collections;
using static System.Linq.Queryable;
using static System.Linq.Enumerable;
using PolygonStats.Models;
using System.Collections.Generic;
using Serilog;
using Microsoft.EntityFrameworkCore;
using PolygonStats.RawWebhook;
using System.Globalization;
using System.Threading;
using PolyConfig = PolygonStats.Configuration.ConfigurationManager;

namespace PolygonStats
{
    class ClientSession : TcpSession
    {
        private StringBuilder messageBuffer = new StringBuilder();
        private string accountName = null;
        private MySQLConnectionManager dbManager = new();
        private int dbSessionId = -1;
        private int accountId;

        private int messageCount = 0;
        private ILogger logger;

        private DateTime lastMessageDateTime = DateTime.UtcNow;
        private WildPokemonProto lastEncounterPokemon = null;
        private Dictionary<ulong, DateTime> holoPokemon = new();

        public ClientSession(TcpServer server) : base(server)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            if (PolyConfig.Shared.Config.Debug.ToFiles)
            {
                LoggerConfiguration configuration = new LoggerConfiguration()
                    .WriteTo.File($"logs/sessions/{Id}.log", rollingInterval: RollingInterval.Day);
                configuration = PolyConfig.Shared.Config.Debug.Debug
                    ? configuration.MinimumLevel.Debug()
                    : configuration.MinimumLevel.Information();
                logger = configuration.CreateLogger();
            } else
            {
                logger = Log.Logger;
            }
        }

        public bool isConnected() => (DateTime.UtcNow - lastMessageDateTime).TotalMinutes <= 20;

        protected override void OnConnected()
        {
            this.Socket.ReceiveBufferSize = 8192 * 4;
            this.Socket.ReceiveTimeout = 10000;
        }

        protected override void OnDisconnected()
        {
            Log.Information($"User {this.accountName} with sessionId {Id} has disconnected.");

            // Add ent time to session
            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                if(dbSessionId != -1)
                {
                    using var context = dbManager.GetContext(); Session dbSession = dbManager.GetSession(context, dbSessionId);
                    dbSession.EndTime = lastMessageDateTime;
                    context.SaveChanges();
                }
            }

        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            lastMessageDateTime = DateTime.UtcNow;
            string currentMessage = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            if (PolyConfig.Shared.Config.Debug.DebugMessages)
            {
                logger.Debug($"Message #{++messageCount} was received!");
            }

            messageBuffer.Append(currentMessage);
            var jsonStrings = messageBuffer.ToString().Split("\n", StringSplitOptions.RemoveEmptyEntries);
            messageBuffer.Clear();
            if (PolyConfig.Shared.Config.Debug.DebugMessages)
            {
                logger.Debug($"Message was split into {jsonStrings.Length} jsonObjects.");
            }
            for(int index = 0; index < jsonStrings.Length; index++)
            {
                string jsonString = jsonStrings[index];
                string trimedJsonString = jsonString.Trim('\r', '\n');
                if(!trimedJsonString.StartsWith("{"))
                {
                    if (PolyConfig.Shared.Config.Debug.DebugMessages)
                    {
                        logger.Debug("Json string didnt start with a {.");
                    }
                    continue;
                }
                if(!trimedJsonString.EndsWith("}"))
                {
                    if (PolyConfig.Shared.Config.Debug.DebugMessages)
                    {
                        logger.Debug("Json string didnt end with a }.");
                    }
                    if(index == jsonStrings.Length - 1){
                        messageBuffer.Append(jsonString);
                    }
                    continue;
                }
                try
                {
                    MessageObject message = JsonSerializer.Deserialize<MessageObject>(trimedJsonString);

                    if (PolyConfig.Shared.Config.Debug.DebugMessages)
                    {
                        logger.Debug($"Handle JsonObject #{index} with {message.payloads.Count} payloads.");
                    }
                    foreach (Payload payload in message.payloads)
                    {
                        if(payload.account_name == null || payload.account_name.Equals("null"))
                        {
                            continue;
                        }
                        AddAccountAndSessionIfNeeded(payload);
                        HandlePayload(payload);
                        if (PolyConfig.Shared.Config.RawData.Enabled) {
                            RawWebhookManager.shared.AddRawData(new RawDataMessage()
                            {
                                origin = payload.account_name,
                                rawData = new RawData()
                                {
                                    type = payload.type,
                                    lat = payload.lat,
                                    lng = payload.lng,
                                    timestamp = payload.timestamp,
                                    raw = true,
                                    payload = payload.proto
                                }
                            });
                        }
                    }
                }
                catch (JsonException)
                {
                    if(index == jsonStrings.Length - 1){
                        messageBuffer.Append(jsonString);
                    }
                }
            }

            if (PolyConfig.Shared.Config.Debug.DebugMessages)
            {
                logger.Debug($"Message #{messageCount} was handled!");
            }
        }

        private void AddAccountAndSessionIfNeeded(Payload payload) {
            if (this.accountName != payload.account_name)
            {
                this.accountName = payload.account_name;
                GetStatEntry();

                if (PolyConfig.Shared.Config.MySql.Enabled)
                {
                    using var context = dbManager.GetContext(); Account account = context.Accounts.Where(a => a.Name == this.accountName).FirstOrDefault<Account>();
                    if (account == null)
                    {
                        account = new Account
                        {
                            Name = this.accountName,
                            HashedName = ""
                        };
                        context.Accounts.Add(account);
                    }
                    Log.Information($"User {this.accountName} with sessionId {Id} has connected.");
                    Session dbSession = new Session { StartTime = DateTime.UtcNow, LogEntrys = new List<LogEntry>() };
                    account.Sessions.Add(dbSession);
                    context.SaveChanges();

                    dbSessionId = dbSession.Id;
                    accountId = account.Id;
                }
            }
        }

        private Stats GetStatEntry() => PolyConfig.Shared.Config.Http.Enabled ? StatManager.sharedInstance.getEntry(accountName) : null;

        private void HandlePayload(Payload payload)
        {
            logger.Debug($"Payload with type {payload.getMethodType():g}");
            switch (payload.getMethodType())
            {
                case Method.CheckAwardedBadges:
                    var badge = CheckAwardedBadgesOutProto.Parser.ParseFrom(payload.getDate());
                    logger.Debug($"Proto: {JsonSerializer.Serialize(badge)}");
                    break;
                case Method.Encounter:
                    var encounter = EncounterOutProto.Parser.ParseFrom(payload.getDate());
                    logger.Debug($"Proto: {JsonSerializer.Serialize(encounter)}");
                    ProcessEncounter(payload.account_name, encounter, payload);
                    break;
                case Method.CatchPokemon:
                    var caught = CatchPokemonOutProto.Parser.ParseFrom(payload.getDate());
                    logger.Debug($"Proto: {JsonSerializer.Serialize(caught)}");
                    ProcessCaughtPokemon(caught);
                    break;
                case Method.GymFeedPokemon:
                    var fedPokemon = GymFeedPokemonOutProto.Parser.ParseFrom(payload.getDate());
                    if (fedPokemon.Result == GymFeedPokemonOutProto.Types.Result.Success)
                    {
                        ProcessFeedBerry(payload.account_name, fedPokemon);
                    }
                    break;
                case Method.CompleteQuest:
                    var quest = CompleteQuestOutProto.Parser.ParseFrom(payload.getDate());
                    if (quest.Status == CompleteQuestOutProto.Types.Status.Success)
                    {
                        ProcessQuestRewards(payload.account_name, quest.Quest.Quest.QuestRewards);
                    }
                    break;
                case Method.CompleteQuestStampCard:
                    var questCard = CompleteQuestStampCardOutProto.Parser.ParseFrom(payload.getDate());
                    if (questCard.Status == CompleteQuestStampCardOutProto.Types.Status.Success)
                    {
                        ProcessQuestRewards(payload.account_name, questCard.Reward);
                    }
                    break;
                case Method.GetHatchedEggs:
                    var hatchedEggs = GetHatchedEggsOutProto.Parser.ParseFrom(payload.getDate());
                    if (hatchedEggs.Success)
                    {
                        ProcessHatchedEggReward(payload.account_name, hatchedEggs);
                    }
                    break;
                case Method.GetMapObjects:
                    var map = GetMapObjectsOutProto.Parser.ParseFrom(payload.getDate());
                    if (map.Status == GetMapObjectsOutProto.Types.Status.Success)
                    {
                        if (PolyConfig.Shared.Config.RocketMap.Enabled)
                        {
                            RocketMap.RocketMapManager.shared.AddCells(map.MapCell.ToList());
                            RocketMap.RocketMapManager.shared.AddWeather(map.ClientWeather.ToList(), (int) map.TimeOfDay);
                            RocketMap.RocketMapManager.shared.AddSpawnpoints(map);
                            foreach (var mapCell in map.MapCell)
                            {
                                RocketMap.RocketMapManager.shared.AddForts(mapCell.Fort.ToList());
                            }
                        }
                    }
                    break;
                case Method.FortDetails:
                    var fort = FortDetailsOutProto.Parser.ParseFrom(payload.getDate());
                    if (PolyConfig.Shared.Config.RocketMap.Enabled)
                    {
                        RocketMap.RocketMapManager.shared.UpdateFortInformations(fort);
                    }
                    break;
                case Method.GymGetInfo:
                    var gym = GymGetInfoOutProto.Parser.ParseFrom(payload.getDate());
                    if (gym.Result == GymGetInfoOutProto.Types.Result.Success)
                    {
                        if (PolyConfig.Shared.Config.RocketMap.Enabled)
                        {
                            RocketMap.RocketMapManager.shared.UpdateGymDetails(gym);
                        }
                    }
                    break;
                case Method.FortSearch:
                    var fortSearch = FortSearchOutProto.Parser.ParseFrom(payload.getDate());
                    if (fortSearch.Result == FortSearchOutProto.Types.Result.Success)
                    {
                        ProcessSpinnedFort(payload.account_name, fortSearch);
                        if (PolyConfig.Shared.Config.RocketMap.Enabled)
                        {
                            RocketMap.RocketMapManager.shared.AddQuest(fortSearch);
                        }
                    }
                    break;
                case Method.EvolvePokemon:
                    var evolvePokemon = EvolvePokemonOutProto.Parser.ParseFrom(payload.getDate());
                    if(evolvePokemon.Result == EvolvePokemonOutProto.Types.Result.Success)
                    {
                        ProcessEvolvedPokemon(payload.account_name, evolvePokemon);
                    }
                    break;
                case Method.GetHoloholoInventory:
                    var holoInventory = GetHoloholoInventoryOutProto.Parser.ParseFrom(payload.getDate());
                    logger.Debug($"Proto: {JsonSerializer.Serialize(holoInventory)}");
                    ProcessHoloHoloInventory(payload.account_name, holoInventory);
                    break;
                case Method.InvasionBattleUpdate:
                    var updateBattle = UpdateInvasionBattleOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessUpdateInvasionBattle(payload.account_name, updateBattle);
                    break;
                case Method.InvasionEncounter:
                    var invasionEncounter = InvasionEncounterOutProto.Parser.ParseFrom(payload.getDate());
                    if (invasionEncounter.EncounterPokemon != null) {
                        this.lastEncounterPokemon = new WildPokemonProto()
                        {
                            Pokemon = invasionEncounter.EncounterPokemon
                        };
                    }
                    break;
                case Method.AttackRaid:
                    var attackRaidBattle = AttackRaidBattleOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessAttackRaidBattle(payload.account_name, attackRaidBattle);
                    break;
                case Method.GetPlayer:
                    var player = GetPlayerOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessPlayer(payload.account_name, player, int.Parse(payload.level));
                    break;
                default:
                    break;
            }
        }

        private void ProcessEncounter(string account_name, EncounterOutProto encounterProto, Payload payload)
        {
            if (encounterProto.Pokemon == null
                || encounterProto.Pokemon.Pokemon == null)
            {
                return;
            }

            if (PolyConfig.Shared.Config.RocketMap.Enabled)
            {
                RocketMap.RocketMapManager.shared.AddEncounter(encounterProto, payload);
            }

            if (!PolyConfig.Shared.Config.Encounter.Enabled) {
                return;
            }
            lastEncounterPokemon = encounterProto.Pokemon;
            EncounterManager.shared.AddEncounter(encounterProto);
        }

        private void ProcessAttackRaidBattle(string account_name, AttackRaidBattleOutProto raid)
        {
            if (raid.Result != AttackRaidBattleOutProto.Types.Result.Success
                || raid.BattleUpdate == null
                || raid.BattleUpdate.BattleLog == null
                || raid.BattleUpdate.BattleLog.BattleActions == null
                || raid.BattleUpdate.BattleLog.BattleActions.Count == 0)
            {
                return;
            }

            BattleActionProto lastEntry = raid.BattleUpdate.BattleLog.BattleActions[^1];
            
            if (lastEntry.BattleResults == null)
            {
                return;
            }

            // Get user
            BattleParticipantProto user = lastEntry.BattleResults.Attackers.FirstOrDefault(x => x.TrainerPublicProfile.Name == account_name);
            
            if (user == null)
            {
                return;
            }

            int index = lastEntry.BattleResults.Attackers.IndexOf(user);

            if (lastEntry.BattleResults.PostRaidEncounter != null && lastEntry.BattleResults.PostRaidEncounter.Count > 0)
            {
                lastEncounterPokemon = new WildPokemonProto()
                {
                    Pokemon = lastEntry.BattleResults.PostRaidEncounter.First().Pokemon
                };
            }

            if (PolyConfig.Shared.Config.Http.Enabled)
            {
                Stats entry = GetStatEntry();
                entry.AddXp(lastEntry.BattleResults.PlayerXpAwarded[index]);
                int stardust = 0;
                stardust += lastEntry.BattleResults.RaidItemRewards[index].LootItem.Sum(loot => loot.Stardust ? loot.Count : 0);
                stardust += lastEntry.BattleResults.DefaultRaidItemRewards[index].LootItem.Sum(loot => loot.Stardust ? loot.Count : 0);
                entry.AddStardust(stardust);
            }

            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                int stardust = 0;
                if (lastEntry.BattleResults.RaidItemRewards.Count > index)
                {
                    stardust += lastEntry.BattleResults.RaidItemRewards[index].LootItem.Sum(loot => loot.Stardust ? loot.Count : 0);
                }

                if (lastEntry.BattleResults.DefaultRaidItemRewards.Count > index)
                {
                    stardust += lastEntry.BattleResults.DefaultRaidItemRewards[index].LootItem.Sum(loot => loot.Stardust ? loot.Count : 0);
                }

                int xp = 0;
                if (lastEntry.BattleResults.PlayerXpAwarded.Count > index)
                {
                    xp = lastEntry.BattleResults.PlayerXpAwarded[index];
                }

                dbManager.AddRaidToDatabase(dbSessionId, xp, stardust);
            }
        }

        private void ProcessUpdateInvasionBattle(string account_name, UpdateInvasionBattleOutProto updateBattle)
        {
            if (updateBattle.Status != InvasionStatus.Types.Status.Success
                || updateBattle.Rewards == null)
            {
                return;
            }

            if (PolyConfig.Shared.Config.Http.Enabled)
            {
                Stats entry = GetStatEntry();
                foreach (LootItemProto loot in updateBattle.Rewards.LootItem)
                {
                    switch (loot.TypeCase)
                    {
                        case LootItemProto.TypeOneofCase.Experience:
                            entry.AddXp(loot.Count);
                            break;
                        case LootItemProto.TypeOneofCase.Stardust:
                            entry.AddStardust(loot.Count);
                            break;
                        default:
                            break;
                    }
                }
            }

            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                dbManager.AddRocketToDatabase(dbSessionId, updateBattle);
            }
        }

        private void ProcessHoloHoloInventory(string account_name, GetHoloholoInventoryOutProto holoInventory)
        {
            if (!PolyConfig.Shared.Config.MySql.Enabled
                || holoInventory.InventoryDelta == null
                || holoInventory.InventoryDelta.InventoryItem == null)
            {
                return;
            }

            foreach (var item in holoInventory.InventoryDelta.InventoryItem
              .Where(item => item.InventoryItemData != null))
            {
                if (item.InventoryItemData.Pokemon != null)
                {
                    PokemonProto pokemon = item.InventoryItemData.Pokemon;

                    using var context = dbManager.GetContext();
                    int effected = context.Database.ExecuteSqlRaw($"UPDATE `SessionLogEntry` SET PokemonName=\"{pokemon.PokemonId:G}\", Attack={pokemon.IndividualAttack}, Defense={pokemon.IndividualDefense}, Stamina={pokemon.IndividualStamina} WHERE PokemonUniqueId={pokemon.Id} AND `timestamp` BETWEEN (DATE_SUB(UTC_TIMESTAMP(),INTERVAL 3 MINUTE)) AND (DATE_ADD(UTC_TIMESTAMP(),INTERVAL 2 MINUTE)) ORDER BY Id");
                    if (effected > 0
                        && pokemon.IndividualAttack == 15
                        && pokemon.IndividualDefense == 15
                        && pokemon.IndividualStamina == 15)
                    {
                        if (!holoPokemon.ContainsKey(pokemon.Id))
                        {
                            holoPokemon.Add(pokemon.Id, DateTime.Now);
                            context.Database.ExecuteSqlRaw($"UPDATE `Session` SET MaxIV=MaxIV+1, LastUpdate=UTC_TIMESTAMP() WHERE Id={dbSessionId} ORDER BY Id");
                        }
                        else
                        {
                            foreach (var id in holoPokemon.Keys.ToList()
                              .Where(id => (DateTime.Now - holoPokemon[id]).TotalMinutes > 10))
                            {
                                holoPokemon.Remove(id);
                            }
                        }
                    }
                }

                if (item.InventoryItemData.PlayerStats != null)
                {
                    dbManager.UpdateLevelAndExp(accountId, item.InventoryItemData.PlayerStats);
                }
            }
        }

        private void ProcessEvolvedPokemon(string account_name, EvolvePokemonOutProto evolvePokemon)
        {
            if (PolyConfig.Shared.Config.Http.Enabled)
            {
                Stats entry = GetStatEntry();
                entry.AddXp(evolvePokemon.ExpAwarded);
            }

            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                dbManager.AddEvolvePokemonToDatabase(dbSessionId, evolvePokemon);
            }
        }

        private void ProcessFeedBerry(string account_name, GymFeedPokemonOutProto feedPokemonProto)
        {
            if (PolyConfig.Shared.Config.Http.Enabled)
            {
                Stats entry = GetStatEntry();
                entry.AddXp(feedPokemonProto.XpAwarded);
                entry.AddStardust(feedPokemonProto.StardustAwarded);
            }

            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                dbManager.AddFeedBerryToDatabase(dbSessionId, feedPokemonProto);
            }
        }

        private void ProcessSpinnedFort(string account_name, FortSearchOutProto fortSearchProto)
        {
            if (PolyConfig.Shared.Config.Http.Enabled)
            {
                Stats entry = GetStatEntry();
                entry.AddSpinnedPokestop();
                entry.AddXp(fortSearchProto.XpAwarded);
            }

            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                dbManager.AddSpinnedFortToDatabase(dbSessionId, fortSearchProto);
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP session caught an error with code {error}");
        }

        private void ProcessQuestRewards(string acc, RepeatedField<QuestRewardProto> rewards)
        {
            if (PolyConfig.Shared.Config.Http.Enabled)
            {
                Stats entry = GetStatEntry();
                foreach (QuestRewardProto reward in rewards)
                {
                    if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Exp)
                    {
                        entry.AddXp(reward.Exp);
                    }

                    if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Stardust)
                    {
                        entry.AddStardust(reward.Stardust);
                    }
                }
            }

            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                dbManager.AddQuestToDatabase(dbSessionId, rewards);
            }
        }
        private void ProcessHatchedEggReward(string acc, GetHatchedEggsOutProto hatchedEggs)
        {
            if (hatchedEggs.HatchedPokemon.Count <= 0)
            {
                return;
            }

            if (PolyConfig.Shared.Config.Http.Enabled)
            {
                Stats entry = GetStatEntry();

                entry.AddXp(hatchedEggs.ExpAwarded.Sum());
                entry.AddStardust(hatchedEggs.StardustAwarded.Sum());

                foreach (var pokemon in hatchedEggs.HatchedPokemon)
                {
                    if (pokemon.PokemonDisplay != null && pokemon.PokemonDisplay.Shiny)
                    {
                        entry.ShinyPokemon++;
                    }
                }
            }

            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                dbManager.AddHatchedEggToDatabase(dbSessionId, hatchedEggs);
            }
        }
        private void ProcessPlayer(string acc, GetPlayerOutProto player, int level)
        {
            if (!player.Success)
            {
                return;
            }

            if (PolyConfig.Shared.Config.MySql.Enabled)
            {
                dbManager.AddPlayerInfoToDatabase(accountId, player, level);
            }
        }

        public void ProcessCaughtPokemon(CatchPokemonOutProto caughtPokemon)
        {
            Stats entry = GetStatEntry();
            switch (caughtPokemon.Status)
            {
                case CatchPokemonOutProto.Types.Status.CatchSuccess:
                    if (entry != null)
                    {
                        entry.CaughtPokemon++;
                        if (caughtPokemon.PokemonDisplay != null && caughtPokemon.PokemonDisplay.Shiny)
                        {
                            entry.ShinyPokemon++;
                        }

                        entry.AddXp(caughtPokemon.Scores.Exp.Sum());
                        entry.AddStardust(caughtPokemon.Scores.Stardust.Sum());
                    }

                    if (PolyConfig.Shared.Config.MySql.Enabled)
                    {
                        dbManager.AddPokemonToDatabase(dbSessionId, caughtPokemon, null);
                    }
                    break;
                case CatchPokemonOutProto.Types.Status.CatchFlee:
                    if (entry != null)
                    {
                        entry.AddXp(caughtPokemon.Scores.Exp.Sum());
                        entry.FleetPokemon++;
                    }

                    if (PolyConfig.Shared.Config.MySql.Enabled)
                    {
                        dbManager.AddPokemonToDatabase(dbSessionId, caughtPokemon, lastEncounterPokemon);
                    }
                    break;
            }
        }
    }
}
