﻿using System;
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

namespace PolygonStats
{
    class ClientSession : TcpSession
    {
        private StringBuilder messageBuffer = new StringBuilder();
        private string accountName = null;
        private MySQLConnectionManager connectionManager = new MySQLConnectionManager();
        private int dbSessionId = -1;

        private int messageCount = 0;
        private ILogger fileLogger;

        public ClientSession(TcpServer server) : base(server) { 
            fileLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File($"logs/sessions/{Id}.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        protected override void OnConnected()
        {
            //Console.WriteLine($"{DateTime.Now.ToString("dd.MM.yy HH:mm")}: Polygon TCP session with Id {Id} connected!");
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
                        dbSession.EndTime = DateTime.UtcNow;
                        context.SaveChanges();
                    }
                }
            }

        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string currentMessage = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            
            fileLogger.Debug($"Message #{messageCount++} was received!");

            messageBuffer.Append(currentMessage);
            var jsonStrings = messageBuffer.ToString().Split("\n", StringSplitOptions.RemoveEmptyEntries);
            messageBuffer.Clear();
            for(int index = 0; index < jsonStrings.Length; index++)
            {
                string jsonString = jsonStrings[index];
                string trimedJsonString = jsonString.Trim('\r', '\n');
                if(!trimedJsonString.StartsWith("{"))
                {
                    continue;
                }
                if(!trimedJsonString.EndsWith("}")) {
                    if(index == jsonStrings.Length - 1){
                        messageBuffer.Append(jsonString);
                    }
                    continue;
                }
                try
                {
                    MessageObject message = JsonSerializer.Deserialize<MessageObject>(trimedJsonString);
                    
                    fileLogger.Debug($"Handle JsonObject #{index} with {message.payloads.Count} payloads.");
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
            
            fileLogger.Debug($"Message #{messageCount++} was handled!");
        }

        private void AddAccountAndSessionIfNeeded(Payload payload) {
            if (this.accountName != payload.account_name)
            {
                this.accountName = payload.account_name;
                getStatEntry();

                if (ConfigurationManager.shared.config.mysqlSettings.enabled)
                {
                    using(var context = connectionManager.GetContext()) {
                        Account acc = context.Accounts.Where(a => a.Name == this.accountName).FirstOrDefault<Account>();
                        if (acc == null)
                        {
                            acc = new Account();
                            acc.Name = this.accountName;
                            acc.HashedName = "";
                            //TODO: Add hashed name
                            //acc.HashedName =  this.accountName.get
                            context.Accounts.Add(acc);
                        }
                        Log.Information($"User {this.accountName} with sessionId {Id} has connected.");
                        Session dbSession = new Session { StartTime = DateTime.UtcNow, LogEntrys = new List<LogEntry>() };
                        acc.Sessions.Add(dbSession);
                        context.SaveChanges();

                        dbSessionId = dbSession.Id;
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
            fileLogger.Debug($"Payload with type {payload.getMethodType().ToString("g")}");
            switch (payload.getMethodType())
            {
                case Method.Encounter:
                    EncounterOutProto encounterProto = EncounterOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessEncounter(payload.account_name, encounterProto);
                    break;
                case Method.CatchPokemon:
                    CatchPokemonOutProto catchPokemonProto = CatchPokemonOutProto.Parser.ParseFrom(payload.getDate());
                    //Console.WriteLine($"Pokemon {catchPokemonProto.DisplayPokedexId.ToString("G")} Status: {catchPokemonProto.Status.ToString("G")}.");
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
                case Method.FortSearch:
                    FortSearchOutProto fortSearchProto = FortSearchOutProto.Parser.ParseFrom(payload.getDate());
                    if (fortSearchProto.Result == FortSearchOutProto.Types.Result.Success)
                    {
                        ProcessSpinnedFort(payload.account_name, fortSearchProto);
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
                    ProcessHoloHoloInventory(payload.account_name, holoInventory);
                    break;
                case Method.InvasionBattleUpdate:
                    UpdateInvasionBattleOutProto updateBattle = UpdateInvasionBattleOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessUpdateInvasionBattle(payload.account_name, updateBattle);
                    break;
                case Method.AttackRaid:
                    AttackRaidBattleOutProto attackRaidBattle = AttackRaidBattleOutProto.Parser.ParseFrom(payload.getDate());
                    ProcessAttackRaidBattle(payload.account_name, attackRaidBattle);
                    break;
                default:
                    break;
            }
        }

        private void ProcessEncounter(string account_name, EncounterOutProto encounterProto)
        {
            if (!ConfigurationManager.shared.config.encounterSettings.enabled || encounterProto.Pokemon == null || encounterProto.Pokemon.Pokemon == null) {
                return;
            }
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
                            Session dbSession = connectionManager.GetSession(context, dbSessionId);
                            PokemonProto pokemon = item.InventoryItemData.Pokemon;
                            LogEntry log = context.Logs.SingleOrDefault(l => l.PokemonUniqueId == pokemon.Id);
                            if (log != null)
                            {
                                log.PokemonName = pokemon.PokemonId;
                                log.Attack = pokemon.IndividualAttack;
                                log.Defense = pokemon.IndividualDefense;
                                log.Stamina = pokemon.IndividualStamina;
                                context.SaveChanges();
                            }
                        }
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
                        connectionManager.AddPokemonToDatabase(dbSessionId, caughtPokemon);
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
                        connectionManager.AddPokemonToDatabase(dbSessionId, caughtPokemon);
                    }
                    break;
            }
        }
    }
}
