using Google.Protobuf.Collections;
using POGOProtos.Rpc;
using PolygonStats.Models;
using System;
using static System.Linq.Queryable;
using static System.Linq.Enumerable;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace PolygonStats
{
    class MySQLConnectionManager
    {
        public MySQLContext GetContext() {
            return new MySQLContext();
        }

        public Session GetSession(MySQLContext context, int id)
        {
            return context.Sessions.Where(s => s.Id == id).FirstOrDefault<Session>();
        }

        public void AddLogEntry(MySQLContext context, int SessionId, LogEntry log) {
            log.SessionId = SessionId;

            //Update session stats
            context.Database.ExecuteSqlRaw( $"UPDATE `Session` SET TotalGainedXp=TotalGainedXp+\"{log.XpReward}\", TotalGainedStardust=TotalGainedStardust+{log.StardustReward}, " +
                                            $"CaughtPokemon=CaughtPokemon+{((log.LogEntryType == LogEntryType.Pokemon && log.CaughtSuccess) ? 1 : 0)}, EscapedPokemon=EscapedPokemon+{((log.LogEntryType == LogEntryType.Pokemon && !log.CaughtSuccess) ? 1 : 0)}, " +
                                            $"ShinyPokemon=ShinyPokemon+{((log.Shiny) ? 1 : 0)}, Shadow=Shadow+{((log.Shadow) ? 1 : 0)}, Pokestops=Pokestops+{(log.LogEntryType == LogEntryType.Fort ? 1 : 0)}, " +
                                            $"Rockets=Rockets+{(log.LogEntryType == LogEntryType.Rocket ? 1 : 0)}, Raids=Raids+{(log.LogEntryType == LogEntryType.Raid ? 1 : 0)}, " +
                                            $"LastUpdate=UTC_TIMESTAMP(), EndTime=UTC_TIMESTAMP(), TotalMinutes=TIMESTAMPDIFF(MINUTE, StartTime, UTC_TIMESTAMP()) WHERE Id={SessionId} ORDER BY Id");
            context.Logs.Add(log);
        }

        public void AddEncounterToDatabase(EncounterOutProto encounterProto, MySQLContext context) {
            if (context.Encounters.Where(e => e.EncounterId == encounterProto.Pokemon.EncounterId).FirstOrDefault() != null ) {
                return;
            }

            Encounter encounter = new Encounter();
            encounter.EncounterId = encounterProto.Pokemon.EncounterId;
            encounter.PokemonName = encounterProto.Pokemon.Pokemon.PokemonId;
            encounter.Form = encounterProto.Pokemon.Pokemon.PokemonDisplay.Form;
            encounter.Stamina = encounterProto.Pokemon.Pokemon.IndividualStamina;
            encounter.Attack = encounterProto.Pokemon.Pokemon.IndividualAttack;
            encounter.Defense = encounterProto.Pokemon.Pokemon.IndividualDefense;
            encounter.Latitude = encounterProto.Pokemon.Latitude;
            encounter.Longitude = encounterProto.Pokemon.Longitude;
            encounter.timestamp = DateTime.UtcNow;

            context.Encounters.Add(encounter);
        }

        public void AddPokemonToDatabase(int dbSessionId, CatchPokemonOutProto catchedPokemon, WildPokemonProto lastEncounter)
        {
            using (var context = new MySQLContext()) {
                LogEntry pokemonLogEntry = new LogEntry { LogEntryType = LogEntryType.Pokemon, CaughtSuccess = catchedPokemon.Status == CatchPokemonOutProto.Types.Status.CatchSuccess, timestamp = DateTime.UtcNow };
                if (catchedPokemon.Status == CatchPokemonOutProto.Types.Status.CatchSuccess)
                {
                    if (catchedPokemon.PokemonDisplay1 != null)
                    {
                        pokemonLogEntry.Shiny = catchedPokemon.PokemonDisplay1.Shiny;
                        pokemonLogEntry.Shadow = catchedPokemon.PokemonDisplay1.Alignment == PokemonDisplayProto.Types.Alignment.Shadow;
                        pokemonLogEntry.Form = catchedPokemon.PokemonDisplay1.Form;
                        pokemonLogEntry.Costume = catchedPokemon.PokemonDisplay1.Costume;
                    }
                    //TODO: Needs look better.
                    if (catchedPokemon.PokemonDisplay2 != null)
                    {
                        pokemonLogEntry.Shiny = catchedPokemon.PokemonDisplay2.Shiny;
                        pokemonLogEntry.Shadow = catchedPokemon.PokemonDisplay2.Alignment == PokemonDisplayProto.Types.Alignment.Shadow;
                        pokemonLogEntry.Form = catchedPokemon.PokemonDisplay2.Form;
                        pokemonLogEntry.Costume = catchedPokemon.PokemonDisplay2.Costume;
                    }

                    pokemonLogEntry.PokemonUniqueId = catchedPokemon.CapturedPokemonId;
                    pokemonLogEntry.CandyAwarded = catchedPokemon.Scores.Candy.Sum();
                }
                if (catchedPokemon.Status == CatchPokemonOutProto.Types.Status.CatchFlee && lastEncounter != null)
                {
                    if (lastEncounter.Pokemon.PokemonDisplay != null)
                    {
                        pokemonLogEntry.Shiny = lastEncounter.Pokemon.PokemonDisplay.Shiny;
                        pokemonLogEntry.Shadow = lastEncounter.Pokemon.PokemonDisplay.Alignment == PokemonDisplayProto.Types.Alignment.Shadow;
                        pokemonLogEntry.Form = lastEncounter.Pokemon.PokemonDisplay.Form;
                        pokemonLogEntry.Costume = lastEncounter.Pokemon.PokemonDisplay.Costume;
                    }
                    pokemonLogEntry.PokemonName = lastEncounter.Pokemon.PokemonId;
                }
                pokemonLogEntry.XpReward = catchedPokemon.Scores.Exp.Sum();
                pokemonLogEntry.StardustReward = catchedPokemon.Scores.Stardust.Sum();
                this.AddLogEntry(context, dbSessionId, pokemonLogEntry);
                context.SaveChanges();
            }
        }

        public void AddFeedBerryToDatabase(int dbSessionId, GymFeedPokemonOutProto gymFeedPokemonProto)
        {
            using (var context = new MySQLContext()) {
                LogEntry feedBerryLogEntry = new LogEntry { LogEntryType = LogEntryType.FeedBerry, timestamp = DateTime.UtcNow };

                feedBerryLogEntry.XpReward = gymFeedPokemonProto.XpAwarded;
                feedBerryLogEntry.StardustReward = gymFeedPokemonProto.StardustAwarded;
                feedBerryLogEntry.CandyAwarded = gymFeedPokemonProto.NumCandyAwarded;

                this.AddLogEntry(context, dbSessionId, feedBerryLogEntry);
                context.SaveChanges();
            }
        }

        public void AddQuestToDatabase(int dbSessionId, RepeatedField<QuestRewardProto> rewards)
        {
            using (var context = new MySQLContext()) {
                LogEntry questLogEntry = new LogEntry { LogEntryType = LogEntryType.Quest, timestamp = DateTime.UtcNow, XpReward = 0, StardustReward = 0 };
                foreach (QuestRewardProto reward in rewards)
                {
                    if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Exp)
                    {
                        questLogEntry.XpReward += reward.Exp;
                    }
                    if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Stardust)
                    {
                        questLogEntry.StardustReward += reward.Stardust;
                    }
                    if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Candy)
                    {
                        questLogEntry.CandyAwarded += reward.Candy.Amount;
                        questLogEntry.PokemonName = reward.Candy.PokemonId;
                    }
                }
                this.AddLogEntry(context, dbSessionId, questLogEntry);
                context.SaveChanges();
            }
        }

        public void AddHatchedEggToDatabase(int dbSessionId, GetHatchedEggsOutProto getHatchedEggsProto)
        {
            using (var context = new MySQLContext()) {
                for(int index = 0; index < getHatchedEggsProto.HatchedPokemon.Count; index++)
                {
                    LogEntry eggLogEntry = new LogEntry { LogEntryType = LogEntryType.Egg, timestamp = DateTime.UtcNow, XpReward = 0, StardustReward = 0 };
                    eggLogEntry.XpReward = getHatchedEggsProto.ExpAwarded[index];
                    eggLogEntry.StardustReward = getHatchedEggsProto.StardustAwarded[index];
                    eggLogEntry.CandyAwarded = getHatchedEggsProto.CandyAwarded[index];
                    eggLogEntry.PokemonName = getHatchedEggsProto.HatchedPokemon[index].PokemonId;
                    eggLogEntry.Attack = getHatchedEggsProto.HatchedPokemon[index].IndividualAttack;
                    eggLogEntry.Defense = getHatchedEggsProto.HatchedPokemon[index].IndividualDefense;
                    eggLogEntry.Stamina = getHatchedEggsProto.HatchedPokemon[index].IndividualStamina;
                    eggLogEntry.PokemonUniqueId = getHatchedEggsProto.HatchedPokemon[index].Id;
                    if (getHatchedEggsProto.HatchedPokemon[index].PokemonDisplay != null)
                    {
                        eggLogEntry.Shiny = getHatchedEggsProto.HatchedPokemon[index].PokemonDisplay.Shiny;
                        eggLogEntry.Shadow = getHatchedEggsProto.HatchedPokemon[index].PokemonDisplay.Alignment == PokemonDisplayProto.Types.Alignment.Shadow;
                        eggLogEntry.Form = getHatchedEggsProto.HatchedPokemon[index].PokemonDisplay.Form;
                        eggLogEntry.Costume = getHatchedEggsProto.HatchedPokemon[index].PokemonDisplay.Costume;
                    }
                    this.AddLogEntry(context, dbSessionId, eggLogEntry);
                }

                context.SaveChanges();
            }
        }

        public void AddSpinnedFortToDatabase(int dbSessionId, FortSearchOutProto fortSearchProto)
        {
            using (var context = new MySQLContext()) {
                LogEntry fortLogEntry = new LogEntry { LogEntryType = LogEntryType.Fort, timestamp = DateTime.UtcNow };

                fortLogEntry.XpReward = fortSearchProto.XpAwarded;

                this.AddLogEntry(context, dbSessionId, fortLogEntry);
                context.SaveChanges();
            }
        }

        public void AddEvolvePokemonToDatabase(int dbSessionId, EvolvePokemonOutProto evolvePokemon)
        {
            using (var context = new MySQLContext()) {
                LogEntry evolveLogEntry = new LogEntry { LogEntryType = LogEntryType.EvolvePokemon, timestamp = DateTime.UtcNow };

                evolveLogEntry.XpReward = evolvePokemon.ExpAwarded;
                evolveLogEntry.CandyAwarded = evolvePokemon.CandyAwarded;
                evolveLogEntry.PokemonName = evolvePokemon.EvolvedPokemon.PokemonId;
                evolveLogEntry.Attack = evolvePokemon.EvolvedPokemon.IndividualAttack;
                evolveLogEntry.Defense = evolvePokemon.EvolvedPokemon.IndividualDefense;
                evolveLogEntry.Stamina = evolvePokemon.EvolvedPokemon.IndividualStamina;
                evolveLogEntry.PokemonUniqueId = evolvePokemon.EvolvedPokemon.Id;
                if (evolvePokemon.EvolvedPokemon.PokemonDisplay != null)
                {
                    evolveLogEntry.Shadow = evolvePokemon.EvolvedPokemon.PokemonDisplay.Alignment == PokemonDisplayProto.Types.Alignment.Shadow;
                    evolveLogEntry.Form = evolvePokemon.EvolvedPokemon.PokemonDisplay.Form;
                    evolveLogEntry.Costume = evolvePokemon.EvolvedPokemon.PokemonDisplay.Costume;
                }

                this.AddLogEntry(context, dbSessionId, evolveLogEntry);
                context.SaveChanges();
            }
        }

        internal void AddRocketToDatabase(int dbSessionId, UpdateInvasionBattleOutProto updateBattle)
        {
            using (var context = new MySQLContext()) {
                LogEntry rocketLogEntry = new LogEntry { LogEntryType = LogEntryType.Rocket, timestamp = DateTime.UtcNow };

                rocketLogEntry.XpReward = 0;
                rocketLogEntry.StardustReward = 0;
                rocketLogEntry.CandyAwarded = 0;
                foreach (LootItemProto loot in updateBattle.Rewards.LootItem)
                {
                    switch(loot.TypeCase)
                    {
                        case LootItemProto.TypeOneofCase.Experience:
                            rocketLogEntry.XpReward += loot.Count;
                            break;
                        case LootItemProto.TypeOneofCase.Stardust:
                            rocketLogEntry.StardustReward += loot.Count;
                            break;
                        case LootItemProto.TypeOneofCase.PokemonCandy:
                            rocketLogEntry.CandyAwarded += loot.Count;
                            rocketLogEntry.PokemonName = loot.PokemonCandy;
                            break;
                    }
                }

                this.AddLogEntry(context, dbSessionId, rocketLogEntry);
                context.SaveChanges();
            }
        }

        internal void AddRaidToDatabase(int dbSessionId, int xp, int stardust)
        {
            using (var context = new MySQLContext()) {
                LogEntry raidLogEntry = new LogEntry { LogEntryType = LogEntryType.Raid, timestamp = DateTime.UtcNow };

                raidLogEntry.XpReward = xp;
                raidLogEntry.StardustReward = stardust;
                this.AddLogEntry(context, dbSessionId, raidLogEntry);
                context.SaveChanges();
            }
        }

        internal void AddPlayerInfoToDatabase(int accountId, GetPlayerOutProto player, int level)
        {
            if (player.Player == null) {
                return;
            }

            using (var context = new MySQLContext())
            {
                var pokecoins = 0;
                var stardust = 0;
                var currency = player.Player.CurrencyBalance.FirstOrDefault(c => c.CurrencyType.Equals("POKECOIN"));
                if (currency != null) {
                    pokecoins = currency.Quantity;
                }
                currency = player.Player.CurrencyBalance.FirstOrDefault(c => c.CurrencyType.Equals("STARDUST"));
                if (currency != null) {
                    stardust = currency.Quantity;
                }
                try
                {
                    context.Database.ExecuteSqlRaw($"UPDATE `Account` SET Team=\"{player.Player.Team}\", Level={level}, Pokecoins={pokecoins}, Stardust={stardust} WHERE Id={accountId} ORDER BY Id");
                } catch (Exception e)
                {
                    Log.Information(e.Message);
                    Log.Information(e.InnerException.Message);
                }
            }
        }

        internal void UpdateLevelAndExp(int accountId, PlayerStatsProto playerStats)
        {
            if (playerStats == null) {
                return;
            }

            using (var context = new MySQLContext())
            {
                try
                {
                    context.Database.ExecuteSqlRaw($"UPDATE `Account` SET Level={playerStats.Level},Experience={(int)playerStats.Experience},NextLevelExp={playerStats.NextLevelExp} WHERE Id={accountId} ORDER BY Id");
                }
                catch (Exception e)
                {
                    Log.Information(e.Message);
                    Log.Information(e.InnerException.Message);
                }
            }
        }
    }
}
