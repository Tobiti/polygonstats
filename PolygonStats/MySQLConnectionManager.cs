using Google.Protobuf.Collections;
using POGOProtos.Rpc;
using PolygonStats.Configuration;
using PolygonStats.Models;
using System;
using System.Linq;
namespace PolygonStats
{
    class MySQLConnectionManager
    {
        private static MySQLConnectionManager _mysqlManager;
        public static MySQLConnectionManager shared
        {
            get
            {
                if (_mysqlManager == null)
                {
                    _mysqlManager = new MySQLConnectionManager();
                }
                return _mysqlManager;
            }
        }

        private MySQLContext context;

        public MySQLConnectionManager()
        {
            context = new MySQLContext();
        }

        public MySQLContext GetContext()
        {
            return context;
        }

        public void AddPokemonToDatabase(Session dbSession, CatchPokemonOutProto catchedPokemon)
        {
            LogEntry pokemonLogEntry = new LogEntry { LogEntryType = LogEntryType.Pokemon, CaughtSuccess = catchedPokemon.Status == CatchPokemonOutProto.Types.Status.CatchSuccess, timestamp = DateTime.UtcNow };
            if (catchedPokemon.Status == CatchPokemonOutProto.Types.Status.CatchSuccess)
            {
                pokemonLogEntry.PokedexId = (int)catchedPokemon.DisplayPokedexId;
                if (catchedPokemon.PokemonDisplay != null)
                {
                    pokemonLogEntry.Shiny = catchedPokemon.PokemonDisplay.Shiny;
                }
            }
            pokemonLogEntry.XpReward = catchedPokemon.Scores.Exp.Sum();
            pokemonLogEntry.StardustReward = catchedPokemon.Scores.Stardust.Sum();
            dbSession.LogEntrys.Add(pokemonLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();
        }

        public void AddFeedBerryToDatabase(Session dbSession, GymFeedPokemonOutProto gymFeedPokemonProto)
        {
            LogEntry feedBerryLogEntry = new LogEntry { LogEntryType = LogEntryType.FeedBerry, timestamp = DateTime.UtcNow };

            feedBerryLogEntry.XpReward = gymFeedPokemonProto.XpAwarded;
            feedBerryLogEntry.StardustReward = gymFeedPokemonProto.StardustAwarded;

            dbSession.LogEntrys.Add(feedBerryLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();
        }

        public void AddQuestToDatabase(Session dbSession, RepeatedField<QuestRewardProto> rewards)
        {
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
            }
            dbSession.LogEntrys.Add(questLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();
        }

        internal void AddHatchedEggToDatabase(Session dbSession, GetHatchedEggsOutProto getHatchedEggsProto)
        {
            LogEntry eggLogEntry = new LogEntry { LogEntryType = LogEntryType.Egg, timestamp = DateTime.UtcNow, XpReward = 0, StardustReward = 0 };

            eggLogEntry.XpReward = getHatchedEggsProto.ExpAwarded.Sum();
            eggLogEntry.StardustReward = getHatchedEggsProto.StardustAwarded.Sum();

            dbSession.LogEntrys.Add(eggLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();

        }

        internal void AddSpinnedFortToDatabase(Session dbSession, FortSearchOutProto fortSearchProto)
        {
            LogEntry fortLogEntry = new LogEntry { LogEntryType = LogEntryType.Fort, timestamp = DateTime.UtcNow };

            fortLogEntry.XpReward = fortSearchProto.XpAwarded;

            dbSession.LogEntrys.Add(fortLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();
        }
    }
}
