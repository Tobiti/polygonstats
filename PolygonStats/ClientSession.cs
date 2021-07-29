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
using PolygonStats.Configuration;
using System.Collections.Generic;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace PolygonStats
{
    class ClientSession : TcpSession
    {
        private StringBuilder messageBuffer = new StringBuilder();
        private string accountName = null;
        private MySQLConnectionManager connectionManager = new MySQLConnectionManager();
        private int dbSessionId = -1;
        private int accountId;

        private int messageCount = 0;
        private ILogger logger;

        private DateTime lastMessageDateTime = DateTime.UtcNow;
        private WildPokemonProto lastEncounterPokemon = null;

        public ClientSession(TcpServer server) : base(server) {
            if (ConfigurationManager.shared.config.debugSettings.toFiles)
            {
                LoggerConfiguration configuration = new LoggerConfiguration()
                    .WriteTo.File($"logs/sessions/{Id}.log", rollingInterval: RollingInterval.Day);
                if (ConfigurationManager.shared.config.debugSettings.debug)
                {
                    configuration = configuration.MinimumLevel.Debug();
                } else
                {
                    configuration = configuration.MinimumLevel.Information();
                }
                logger = configuration.CreateLogger();
            } else
            {
                logger = Log.Logger;
            }
        }

        public bool isConnected()
        {
            if ((DateTime.UtcNow - lastMessageDateTime).TotalMinutes <= 20)
            {
                return true;
            }
            return false;
        }

        protected override void OnConnected()
        {
            this.Socket.ReceiveBufferSize = 8192 * 4;
            this.Socket.ReceiveTimeout = 10000;
        }

        protected override void OnDisconnected()
        {
            Log.Information($"User {this.accountName} with sessionId {Id} has disconnected.");

            // Add ent time to session
            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                if(dbSessionId != -1)
                {
                    using (var context = connectionManager.GetContext()) {
                        Session dbSession = connectionManager.GetSession(context, dbSessionId);
                        dbSession.EndTime = lastMessageDateTime;
                        context.SaveChanges();
                    }
                }
            }

        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            lastMessageDateTime = DateTime.UtcNow;
            string currentMessage = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            if (ConfigurationManager.shared.config.debugSettings.debugMessages)
            {
                logger.Debug($"Message #{++messageCount} was received!");
            }

            messageBuffer.Append(currentMessage);
            var jsonStrings = messageBuffer.ToString().Split("\n", StringSplitOptions.RemoveEmptyEntries);
            messageBuffer.Clear();
            if (ConfigurationManager.shared.config.debugSettings.debugMessages)
            {
                logger.Debug($"Message was splitted into {jsonStrings.Length} jsonObjects.");
            }
            for(int index = 0; index < jsonStrings.Length; index++)
            {
                string jsonString = jsonStrings[index];
                string trimedJsonString = jsonString.Trim('\r', '\n');
                if(!trimedJsonString.StartsWith("{"))
                {
                    if (ConfigurationManager.shared.config.debugSettings.debugMessages)
                    {
                        logger.Debug("Json string didnt start with a {.");
                    }
                    continue;
                }
                if(!trimedJsonString.EndsWith("}"))
                {
                    if (ConfigurationManager.shared.config.debugSettings.debugMessages)
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

                    if (ConfigurationManager.shared.config.debugSettings.debugMessages)
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
                        handlePayload(payload);
                    }
                }
                catch (JsonException)
                {
                    if(index == jsonStrings.Length - 1){
                        messageBuffer.Append(jsonString);
                    }
                }
            }

            if (ConfigurationManager.shared.config.debugSettings.debugMessages)
            {
                logger.Debug($"Message #{messageCount} was handled!");
            }
        }

        private void AddAccountAndSessionIfNeeded(Payload payload) {
            if (this.accountName != payload.account_name)
            {
                this.accountName = payload.account_name;
                getStatEntry();

                if (ConfigurationManager.shared.config.mysqlSettings.enabled)
                {
                    using(var context = connectionManager.GetContext()) {
                        Account account = context.Accounts.Where(a => a.Name == this.accountName).FirstOrDefault<Account>();
                        if (account == null)
                        {
                            account = new Account();
                            account.Name = this.accountName;
                            account.HashedName = "";
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
        }

        private Stats getStatEntry()
        {
            if (ConfigurationManager.shared.config.httpSettings.enabled)
            {
                return StatManager.sharedInstance.getEntry(accountName);
            } else
            {
                return null;
            }
        }

        private void handlePayload(Payload payload)
        {
            logger.Debug($"Payload with type {payload.getMethodType().ToString("g")}");
            switch (payload.getMethodType())
            {
                case Method.CheckAwardedBadges:
                    CheckAwardedBadgesOutProto badge = CheckAwardedBadgesOutProto.Parser.ParseFrom(payload.getDate());
                    logger.Debug($"Proto: {JsonSerializer.Serialize(badge)}");
                    break;
                case Method.Encounter:
                    EncounterOutProto encounterProto = EncounterOutProto.Parser.ParseFrom(payload.getDate());
                    logger.Debug($"Proto: {JsonSerializer.Serialize(encounterProto)}");
                    ProcessEncounter(payload.account_name, encounterProto);
                    break;
                case Method.CatchPokemon:
                    CatchPokemonOutProto catchPokemonProto = CatchPokemonOutProto.Parser.ParseFrom(payload.getDate());
                    logger.Debug($"Proto: {JsonSerializer.Serialize(catchPokemonProto)}");
                    ProcessCaughtPokemon(catchPokemonProto);
                    break;
                case Method.GymFeedPokemon:
                    GymFeedPokemonOutProto feedPokemonProto = GymFeedPokemonOutProto.Parser.ParseFrom(payload.getDate());
                    if (feedPokemonProto.Result == GymFeedPokemonOutProto.Types.Result.Success)
                    {
                        ProcessFeedBerry(payload.account_name, feedPokemonProto);
                    }
                    break;
                case Method.CompleteQuest:
                    CompleteQuestOutProto questProto = CompleteQuestOutProto.Parser.ParseFrom(payload.getDate());
                    if (questProto.Status == CompleteQuestOutProto.Types.Status.Success)
                    {
                        ProcessQuestRewards(payload.account_name, questProto.Quest.Quest.QuestRewards);
                    }
                    break;
                case Method.CompleteQuestStampCard:
                    CompleteQuestStampCardOutProto completeQuestStampCardProto = CompleteQuestStampCardOutProto.Parser.ParseFrom(payload.getDate());
                    if (completeQuestStampCardProto.Status == CompleteQuestStampCardOutProto.Types.Status.Success)
                    {
                        ProcessQuestRewards(payload.account_name, completeQuestStampCardProto.Reward);
                    }
                    break;
                case Method.GetHatchedEggs:
                    GetHatchedEggsOutProto getHatchedEggsProto = GetHatchedEggsOutProto.Parser.ParseFrom(payload.getDate());
                    if (getHatchedEggsProto.Success)
                    {
                        ProcessHatchedEggReward(payload.account_name, getHatchedEggsProto);
                    }
                    break;
                case Method.GetMapObjects:
                    GetMapObjectsOutProto mapProto = GetMapObjectsOutProto.Parser.ParseFrom(payload.getDate());
                    if (mapProto.Status == GetMapObjectsOutProto.Types.Status.Success)
                    {
                        if (ConfigurationManager.shared.config.rocketMapSettings.enabled)
                        {
                            RocketMap.RocketMapManager.shared.AddCells(mapProto.MapCell.ToList());
                            RocketMap.RocketMapManager.shared.AddWeather(mapProto.ClientWeather.ToList(), (int) mapProto.TimeOfDay);
                            foreach (var mapCell in mapProto.MapCell)
                            {
                                RocketMap.RocketMapManager.shared.AddForts(mapCell.Fort.ToList());
                            }
                        }
                    }
                    break;
                case Method.FortDetails:
                    FortDetailsOutProto fortDetailProto = FortDetailsOutProto.Parser.ParseFrom(payload.getDate());
                    if (ConfigurationManager.shared.config.rocketMapSettings.enabled)
                    {
                        RocketMap.RocketMapManager.shared.UpdateFortInformations(fortDetailProto);
                    }
                    break;
                case Method.GymGetInfo:
                    GymGetInfoOutProto gymProto = GymGetInfoOutProto.Parser.ParseFrom(payload.getDate());
                    if (gymProto.Result == GymGetInfoOutProto.Types.Result.Success)
                    {
                        if (ConfigurationManager.shared.config.rocketMapSettings.enabled)
                        {
                            RocketMap.RocketMapManager.shared.UpdateGymDetails(gymProto);
                        }
                    }
                    break;
                case Method.FortSearch:
                    FortSearchOutProto fortSearchProto = FortSearchOutProto.Parser.ParseFrom(payload.getDate());
                    if (fortSearchProto.Result == FortSearchOutProto.Types.Result.Success)
                    {
                        ProcessSpinnedFort(payload.account_name, fortSearchProto);
                        if (ConfigurationManager.shared.config.rocketMapSettings.enabled)
                        {
                            RocketMap.RocketMapManager.shared.AddQuest(fortSearchProto);
                        }
                    }
                    break;
                case Method.EvolvePokemon:
                    EvolvePokemonOutProto evolvePokemon = EvolvePokemonOutProto.Parser.ParseFrom(payload.getDate());
                    if(evolvePokemon.Result == EvolvePokemonOutProto.Types.Result.Success)
                    {
                        ProcessEvolvedPokemon(payload.account_name, evolvePokemon);
                    }
                    break;
                case Method.GetHoloholoInventory:
                    GetHoloholoInventoryOutProto holoInventory = GetHoloholoInventoryOutProto.Parser.ParseFrom(payload.getDate());
                    logger.Debug($"Proto: {JsonSerializer.Serialize(holoInventory)}");
                    ProcessHoloHoloInventory(payload.account_name, holoInventory);
                    break;
                case Method.InvasionBattleUpdate:
                    UpdateInvasionBattleOutProto updateBattle = UpdateInvasionBattleOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessUpdateInvasionBattle(payload.account_name, updateBattle);
                    break;
                case Method.InvasionEncounter:
                    InvasionEncounterOutProto invasionEncounter = InvasionEncounterOutProto.Parser.ParseFrom(payload.getDate());
                    if (invasionEncounter.EncounterPokemon != null) {
                        this.lastEncounterPokemon = new WildPokemonProto()
                        {
                            Pokemon = invasionEncounter.EncounterPokemon
                        };
                    }
                    break;
                case Method.AttackRaid:
                    AttackRaidBattleOutProto attackRaidBattle = AttackRaidBattleOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessAttackRaidBattle(payload.account_name, attackRaidBattle);
                    break;
                case Method.GetPlayer:
                    GetPlayerOutProto player = GetPlayerOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessPlayer(payload.account_name, player, int.Parse(payload.level));
                    break;
                default:
                    break;
            }
        }

        private void ProcessEncounter(string account_name, EncounterOutProto encounterProto)
        {
            if (encounterProto.Pokemon == null || encounterProto.Pokemon.Pokemon == null)
            {
                return;
            }

            if (ConfigurationManager.shared.config.rocketMapSettings.enabled)
            {
                RocketMap.RocketMapManager.shared.AddEncounter(encounterProto);
            }

            if (!ConfigurationManager.shared.config.encounterSettings.enabled) {
                return;
            }
            lastEncounterPokemon = encounterProto.Pokemon;
            EncounterManager.shared.AddEncounter(encounterProto);
        }

        private void ProcessAttackRaidBattle(string account_name, AttackRaidBattleOutProto attackRaidBattle)
        {
            if (attackRaidBattle.Result != AttackRaidBattleOutProto.Types.Result.Success)
            {
                return;
            }
            if (attackRaidBattle.BattleUpdate == null || attackRaidBattle.BattleUpdate.BattleLog == null || attackRaidBattle.BattleUpdate.BattleLog.BattleActions == null || attackRaidBattle.BattleUpdate.BattleLog.BattleActions.Count == 0)
            {
                return;
            }

            BattleActionProto lastEntry = attackRaidBattle.BattleUpdate.BattleLog.BattleActions[attackRaidBattle.BattleUpdate.BattleLog.BattleActions.Count - 1];
            if (lastEntry.BattleResults == null)
            {
                return;
            }

            // Get user
            BattleParticipantProto ownParticipant = lastEntry.BattleResults.Attackers.FirstOrDefault(attacker => attacker.TrainerPublicProfile.Name == account_name);
            if (ownParticipant != null)
            {
                int index = lastEntry.BattleResults.Attackers.IndexOf(ownParticipant);

                if (lastEntry.BattleResults.PostRaidEncounter != null && lastEntry.BattleResults.PostRaidEncounter.Count > 0)
                {
                    lastEncounterPokemon = new WildPokemonProto() {
                        Pokemon = lastEntry.BattleResults.PostRaidEncounter.First().Pokemon
                    };
                }

                if (ConfigurationManager.shared.config.httpSettings.enabled)
                {
                    Stats entry = getStatEntry();
                    entry.addXp(lastEntry.BattleResults.PlayerXpAwarded[index]);
                    int stardust = 0;
                    stardust += lastEntry.BattleResults.RaidItemRewards[index].LootItem.Sum(loot => loot.Stardust ? loot.Count : 0);
                    stardust += lastEntry.BattleResults.DefaultRaidItemRewards[index].LootItem.Sum(loot => loot.Stardust ? loot.Count : 0);
                    entry.addStardust(stardust);
                }

                if (ConfigurationManager.shared.config.mysqlSettings.enabled)
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
                    if(lastEntry.BattleResults.PlayerXpAwarded.Count > index)
                    {
                        xp = lastEntry.BattleResults.PlayerXpAwarded[index];
                    }

                    connectionManager.AddRaidToDatabase(dbSessionId, xp, stardust);
                }
            }
        }

        private void ProcessUpdateInvasionBattle(string account_name, UpdateInvasionBattleOutProto updateBattle)
        {
            if (updateBattle.Status != InvasionStatus.Types.Status.Success || updateBattle.Rewards == null)
            {
                return;
            }

            if (ConfigurationManager.shared.config.httpSettings.enabled)
            {
                Stats entry = getStatEntry();
                foreach (LootItemProto loot in updateBattle.Rewards.LootItem)
                {
                    switch (loot.TypeCase)
                    {
                        case LootItemProto.TypeOneofCase.Experience:
                            entry.addXp(loot.Count);
                            break;
                        case LootItemProto.TypeOneofCase.Stardust:
                            entry.addStardust(loot.Count);
                            break;
                        default:
                            break;
                    }
                }
            }

            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                connectionManager.AddRocketToDatabase(dbSessionId, updateBattle);
            }
        }

        private void ProcessHoloHoloInventory(string account_name, GetHoloholoInventoryOutProto holoInventory)
        {
            if (!ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                return;
            }
            if (holoInventory.InventoryDelta == null || holoInventory.InventoryDelta.InventoryItem == null)
            {
                return;
            }

            foreach (InventoryItemProto item in holoInventory.InventoryDelta.InventoryItem)
            {
                if (item.InventoryItemData != null)
                { 
                    if (item.InventoryItemData.Pokemon != null)
                    {
                        using (var context = connectionManager.GetContext()) {
                            PokemonProto pokemon = item.InventoryItemData.Pokemon;
                            context.Database.ExecuteSqlRaw($"UPDATE `SessionLogEntry` SET PokemonName=\"{pokemon.PokemonId.ToString("G")}\", Attack={pokemon.IndividualAttack}, Defense={pokemon.IndividualDefense}, Stamina={pokemon.IndividualStamina} WHERE PokemonUniqueId={pokemon.Id} ORDER BY Id");
                            if (pokemon.IndividualAttack == 15 && pokemon.IndividualDefense == 15 && pokemon.IndividualStamina == 15)
                            {
                                context.Database.ExecuteSqlRawAsync($"UPDATE `Session` SET MaxIV=MaxIV+1, LastUpdate=NOW() WHERE Id={dbSessionId} ORDER BY Id");
                            }
                        }
                    }
                    if (item.InventoryItemData.PlayerStats != null) {
                        connectionManager.UpdateLevelAndExp(accountId, item.InventoryItemData.PlayerStats);
                    }
                }
            }
        }

        private void ProcessEvolvedPokemon(string account_name, EvolvePokemonOutProto evolvePokemon)
        {
            if (ConfigurationManager.shared.config.httpSettings.enabled)
            {
                Stats entry = getStatEntry();
                entry.addXp(evolvePokemon.ExpAwarded);
            }

            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                connectionManager.AddEvolvePokemonToDatabase(dbSessionId, evolvePokemon);
            }
        }

        private void ProcessFeedBerry(string account_name, GymFeedPokemonOutProto feedPokemonProto)
        {
            if (ConfigurationManager.shared.config.httpSettings.enabled)
            {
                Stats entry = getStatEntry();
                entry.addXp(feedPokemonProto.XpAwarded);
                entry.addStardust(feedPokemonProto.StardustAwarded);
            }

            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                connectionManager.AddFeedBerryToDatabase(dbSessionId, feedPokemonProto);
            }
        }

        private void ProcessSpinnedFort(string account_name, FortSearchOutProto fortSearchProto)
        {
            if (ConfigurationManager.shared.config.httpSettings.enabled)
            {
                Stats entry = getStatEntry();
                entry.addSpinnedPokestop();
                entry.addXp(fortSearchProto.XpAwarded);
            }

            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                connectionManager.AddSpinnedFortToDatabase(dbSessionId, fortSearchProto);
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP session caught an error with code {error}");
        }

        private void ProcessQuestRewards(string acc, RepeatedField<QuestRewardProto> rewards)
        {
            if (ConfigurationManager.shared.config.httpSettings.enabled)
            {
                Stats entry = getStatEntry();
                foreach (QuestRewardProto reward in rewards)
                {
                    if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Exp)
                    {
                        entry.addXp(reward.Exp);
                    }
                    if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Stardust)
                    {
                        entry.addStardust(reward.Stardust);
                    }
                }
            }

            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                connectionManager.AddQuestToDatabase(dbSessionId, rewards);
            }
        }
        private void ProcessHatchedEggReward(string acc, GetHatchedEggsOutProto getHatchedEggsProto)
        {
            if (getHatchedEggsProto.HatchedPokemon.Count <= 0)
            {
                return;
            }
            if (ConfigurationManager.shared.config.httpSettings.enabled)
            {
                Stats entry = getStatEntry();

                entry.addXp(getHatchedEggsProto.ExpAwarded.Sum());
                entry.addStardust(getHatchedEggsProto.StardustAwarded.Sum());

                foreach (PokemonProto pokemon in getHatchedEggsProto.HatchedPokemon)
                {
                    if (pokemon.PokemonDisplay != null && pokemon.PokemonDisplay.Shiny)
                    {
                        entry.shinyPokemon++;
                    }
                }
            }

            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                connectionManager.AddHatchedEggToDatabase(dbSessionId, getHatchedEggsProto);
            }
        }
        private void ProcessPlayer(string acc, GetPlayerOutProto player, int level)
        {
            if (!player.Success)
            {
                return;
            }

            if (ConfigurationManager.shared.config.mysqlSettings.enabled)
            {
                connectionManager.AddPlayerInfoToDatabase(accountId, player, level);
            }
        }

        public void ProcessCaughtPokemon(CatchPokemonOutProto caughtPokemon)
        {
            Stats entry = getStatEntry();
            switch (caughtPokemon.Status)
            {
                case CatchPokemonOutProto.Types.Status.CatchSuccess:
                    if (entry != null)
                    {
                        entry.caughtPokemon++;
                        if (caughtPokemon.PokemonDisplay != null && caughtPokemon.PokemonDisplay.Shiny)
                        {
                            entry.shinyPokemon++;
                        }

                        entry.addXp(caughtPokemon.Scores.Exp.Sum());
                        entry.addStardust(caughtPokemon.Scores.Stardust.Sum());
                    }

                    if (ConfigurationManager.shared.config.mysqlSettings.enabled)
                    {
                        connectionManager.AddPokemonToDatabase(dbSessionId, caughtPokemon, null);
                    }
                    break;
                case CatchPokemonOutProto.Types.Status.CatchFlee:
                    if (entry != null)
                    {
                        entry.addXp(caughtPokemon.Scores.Exp.Sum());
                        entry.fleetPokemon++;
                    }

                    if (ConfigurationManager.shared.config.mysqlSettings.enabled)
                    {
                        connectionManager.AddPokemonToDatabase(dbSessionId, caughtPokemon, lastEncounterPokemon);
                    }
                    break;
            }
        }
    }
}
