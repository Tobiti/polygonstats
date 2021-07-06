using Google.Protobuf.Collections;
using POGOProtos.Rpc;
using PolygonStats.Models;
using System;
using static System.Linq.Queryable;
using static System.Linq.Enumerable;
using Serilog;

namespace PolygonStats
{
    class MySQLConnectionManager : IDisposable
    {
        private MySQLContext context = new MySQLContext();

        private int queryCount = 0;

        public MySQLContext GetContext() {
            return context;
        }

        internal MySQLContext GetOwnContext()
        {
            return new MySQLContext();
        }

        public Session GetSession(MySQLContext context, int id) {
            return context.Sessions.Where(s => s.Id == id).FirstOrDefault<Session>();
        }

        public Account GetAccount(MySQLContext context, int accountId) {
            return context.Accounts.Where(a => a.Id == accountId).Single();
        }

        public void AddLogEntry(MySQLContext context, Session session, LogEntry log) {
            session.LogEntrys.Add(log);
        }

        public void AddEncounterToDatabase(EncounterOutProto encounterProto) {
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

            SaveChanges();
        }

        public void AddPokemonToDatabase(int dbSessionId, CatchPokemonOutProto catchedPokemon, WildPokemonProto lastEncounter)
        {
            Session dbSession = this.GetSession(context, dbSessionId);
            LogEntry pokemonLogEntry = new LogEntry { LogEntryType = LogEntryType.Pokemon, CaughtSuccess = catchedPokemon.Status == CatchPokemonOutProto.Types.Status.CatchSuccess, timestamp = DateTime.UtcNow };
            if (catchedPokemon.Status == CatchPokemonOutProto.Types.Status.CatchSuccess)
            {
                if (catchedPokemon.PokemonDisplay != null)
                {
                    pokemonLogEntry.Shiny = catchedPokemon.PokemonDisplay.Shiny;
                }
                pokemonLogEntry.PokemonUniqueId = catchedPokemon.CapturedPokemonId;
                pokemonLogEntry.CandyAwarded = catchedPokemon.Scores.Candy.Sum();
            }
            if (catchedPokemon.Status == CatchPokemonOutProto.Types.Status.CatchFlee && lastEncounter != null)
            {
                if (lastEncounter.Pokemon.PokemonDisplay != null)
                {
                    pokemonLogEntry.Shiny = lastEncounter.Pokemon.PokemonDisplay.Shiny;
                }
                pokemonLogEntry.PokemonName = lastEncounter.Pokemon.PokemonId;
            }
            pokemonLogEntry.XpReward = catchedPokemon.Scores.Exp.Sum();
            pokemonLogEntry.StardustReward = catchedPokemon.Scores.Stardust.Sum();
            this.AddLogEntry(context, dbSession, pokemonLogEntry);

            SaveChanges();
        }

        public void AddFeedBerryToDatabase(int dbSessionId, GymFeedPokemonOutProto gymFeedPokemonProto)
        {
            Session dbSession = this.GetSession(context, dbSessionId);
            LogEntry feedBerryLogEntry = new LogEntry { LogEntryType = LogEntryType.FeedBerry, timestamp = DateTime.UtcNow };

            feedBerryLogEntry.XpReward = gymFeedPokemonProto.XpAwarded;
            feedBerryLogEntry.StardustReward = gymFeedPokemonProto.StardustAwarded;
            feedBerryLogEntry.CandyAwarded = gymFeedPokemonProto.NumCandyAwarded;

            this.AddLogEntry(context, dbSession, feedBerryLogEntry);

            SaveChanges();
        }

        public void AddQuestToDatabase(int dbSessionId, RepeatedField<QuestRewardProto> rewards)
        {
            Session dbSession = this.GetSession(context, dbSessionId);
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
            this.AddLogEntry(context, dbSession, questLogEntry);

            SaveChanges();
        }

        public void AddHatchedEggToDatabase(int dbSessionId, GetHatchedEggsOutProto getHatchedEggsProto)
        {
            Session dbSession = this.GetSession(context, dbSessionId);
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
                }
                this.AddLogEntry(context, dbSession, eggLogEntry);
            }

            SaveChanges();
        }

        public void AddSpinnedFortToDatabase(int dbSessionId, FortSearchOutProto fortSearchProto)
        {
            Session dbSession = this.GetSession(context, dbSessionId);
            LogEntry fortLogEntry = new LogEntry { LogEntryType = LogEntryType.Fort, timestamp = DateTime.UtcNow };

            fortLogEntry.XpReward = fortSearchProto.XpAwarded;

            this.AddLogEntry(context, dbSession, fortLogEntry);

            SaveChanges();
        }

        public void AddEvolvePokemonToDatabase(int dbSessionId, EvolvePokemonOutProto evolvePokemon)
        {
            Session dbSession = this.GetSession(context, dbSessionId);
            LogEntry evolveLogEntry = new LogEntry { LogEntryType = LogEntryType.EvolvePokemon, timestamp = DateTime.UtcNow };

            evolveLogEntry.XpReward = evolvePokemon.ExpAwarded;
            evolveLogEntry.CandyAwarded = evolvePokemon.CandyAwarded;
            evolveLogEntry.PokemonName = evolvePokemon.EvolvedPokemon.PokemonId;
            evolveLogEntry.Attack = evolvePokemon.EvolvedPokemon.IndividualAttack;
            evolveLogEntry.Defense = evolvePokemon.EvolvedPokemon.IndividualDefense;
            evolveLogEntry.Stamina = evolvePokemon.EvolvedPokemon.IndividualStamina;
            evolveLogEntry.PokemonUniqueId = evolvePokemon.EvolvedPokemon.Id;

            this.AddLogEntry(context, dbSession, evolveLogEntry);

            SaveChanges();
        }

        internal void AddRocketToDatabase(int dbSessionId, UpdateInvasionBattleOutProto updateBattle)
        {
            Session dbSession = this.GetSession(context, dbSessionId);
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

            this.AddLogEntry(context, dbSession, rocketLogEntry);

            SaveChanges();
        }

        internal void AddRaidToDatabase(int dbSessionId, int xp, int stardust)
        {
            Session dbSession = this.GetSession(context, dbSessionId);
            LogEntry raidLogEntry = new LogEntry { LogEntryType = LogEntryType.Raid, timestamp = DateTime.UtcNow };

            raidLogEntry.XpReward = xp;
            raidLogEntry.StardustReward = stardust;
            this.AddLogEntry(context, dbSession, raidLogEntry);

            SaveChanges();
        }

        internal void AddPlayerInfoToDatabase(int dbSessionId, GetPlayerOutProto player, int level)
        {
            if (player.Player == null) {
                return;
            }

            Session dbSession = this.GetSession(context, dbSessionId);
            Account dbAccount = this.GetAccount(context, dbSession.AccountId);
            dbAccount.Team = player.Player.Team;
            dbAccount.Level = level;
            var currency = player.Player.CurrencyBalance.FirstOrDefault(c => c.CurrencyType.Equals("POKECOIN"));
            if (currency != null) {
                dbAccount.Pokecoins = currency.Quantity;
            }
            currency = player.Player.CurrencyBalance.FirstOrDefault(c => c.CurrencyType.Equals("STARDUST"));
            if (currency != null) {
                dbAccount.Stardust = currency.Quantity;
            }

            SaveChanges();
        }

        internal void UpdateLevelAndExp(int dbSessionId, PlayerStatsProto playerStats)
        {
            if (playerStats == null) {
                return;
            }

            Session dbSession = this.GetSession(context, dbSessionId);
            if (dbSession != null)
            {
                Account dbAccount = this.GetAccount(context, dbSession.AccountId);
                dbAccount.Level = playerStats.Level;
                dbAccount.Experience = (int)playerStats.Experience;
                dbAccount.NextLevelExp = playerStats.NextLevelExp;
            }

            SaveChanges();
        }

        public void SaveChanges()
        {
            queryCount++;
            if(queryCount > 50)
            {
                queryCount = 0;
                context.SaveChanges();
                context.Dispose();
                context = new MySQLContext();
            }
        }

        public void Dispose()
        {
            context.SaveChanges();
            context.Dispose();
        }
    }
}
