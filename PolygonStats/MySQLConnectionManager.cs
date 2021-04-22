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
            pokemonLogEntry.CandyAwarded = catchedPokemon.Scores.Candy.Sum();
            dbSession.LogEntrys.Add(pokemonLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();
        }

        public void AddFeedBerryToDatabase(Session dbSession, GymFeedPokemonOutProto gymFeedPokemonProto)
        {
            LogEntry feedBerryLogEntry = new LogEntry { LogEntryType = LogEntryType.FeedBerry, timestamp = DateTime.UtcNow };

            feedBerryLogEntry.XpReward = gymFeedPokemonProto.XpAwarded;
            feedBerryLogEntry.StardustReward = gymFeedPokemonProto.StardustAwarded;
            feedBerryLogEntry.CandyAwarded = gymFeedPokemonProto.NumCandyAwarded;

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
                if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Candy)
                {
                    questLogEntry.CandyAwarded += reward.Candy.Amount;
                    questLogEntry.PokedexId = (int) reward.Candy.PokemonId;
                }
            }
            dbSession.LogEntrys.Add(questLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();
        }

        public void AddHatchedEggToDatabase(Session dbSession, GetHatchedEggsOutProto getHatchedEggsProto)
        {
            for(int index = 0; index < getHatchedEggsProto.HatchedPokemon.Count; index++)
            {
                LogEntry eggLogEntry = new LogEntry { LogEntryType = LogEntryType.Egg, timestamp = DateTime.UtcNow, XpReward = 0, StardustReward = 0 };
                eggLogEntry.XpReward = getHatchedEggsProto.ExpAwarded[index];
                eggLogEntry.StardustReward = getHatchedEggsProto.StardustAwarded[index];
                eggLogEntry.CandyAwarded = getHatchedEggsProto.CandyAwarded[index];
                eggLogEntry.PokedexId = getHatchedEggsProto.HatchedPokemon[index].DisplayPokemonId;
                dbSession.LogEntrys.Add(eggLogEntry);
            }

            MySQLConnectionManager.shared.GetContext().SaveChanges();

        }

        public void AddSpinnedFortToDatabase(Session dbSession, FortSearchOutProto fortSearchProto)
        {
            LogEntry fortLogEntry = new LogEntry { LogEntryType = LogEntryType.Fort, timestamp = DateTime.UtcNow };

            fortLogEntry.XpReward = fortSearchProto.XpAwarded;

            dbSession.LogEntrys.Add(fortLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();
        }

        public void AddEvolvePokemonToDatabase(Session dbSession, EvolvePokemonOutProto evolvePokemon)
        {
            LogEntry evolveLogEntry = new LogEntry { LogEntryType = LogEntryType.EvolvePokemon, timestamp = DateTime.UtcNow };

            evolveLogEntry.XpReward = evolvePokemon.ExpAwarded;
            evolveLogEntry.CandyAwarded = evolvePokemon.CandyAwarded;
            evolveLogEntry.PokedexId = evolvePokemon.EvolvedPokemon.DisplayPokemonId;

            dbSession.LogEntrys.Add(evolveLogEntry);
            MySQLConnectionManager.shared.GetContext().SaveChanges();
        }
    }
}
