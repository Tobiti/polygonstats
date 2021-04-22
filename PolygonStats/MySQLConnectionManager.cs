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
                if (catchedPokemon.PokemonDisplay != null)
                {
                    pokemonLogEntry.Shiny = catchedPokemon.PokemonDisplay.Shiny;
                }
                pokemonLogEntry.PokemonUniqueId = catchedPokemon.CapturedPokemonId;
                pokemonLogEntry.CandyAwarded = catchedPokemon.Scores.Candy.Sum();
            }
            pokemonLogEntry.XpReward = catchedPokemon.Scores.Exp.Sum();
            pokemonLogEntry.StardustReward = catchedPokemon.Scores.Stardust.Sum();
            dbSession.LogEntrys.Add(pokemonLogEntry);
            SaveChanges();
        }

        public void AddFeedBerryToDatabase(Session dbSession, GymFeedPokemonOutProto gymFeedPokemonProto)
        {
            LogEntry feedBerryLogEntry = new LogEntry { LogEntryType = LogEntryType.FeedBerry, timestamp = DateTime.UtcNow };

            feedBerryLogEntry.XpReward = gymFeedPokemonProto.XpAwarded;
            feedBerryLogEntry.StardustReward = gymFeedPokemonProto.StardustAwarded;
            feedBerryLogEntry.CandyAwarded = gymFeedPokemonProto.NumCandyAwarded;

            dbSession.LogEntrys.Add(feedBerryLogEntry);
            SaveChanges();
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
                    questLogEntry.PokemonName = reward.Candy.PokemonId;
                }
            }
            dbSession.LogEntrys.Add(questLogEntry);
            SaveChanges();
        }

        public void AddHatchedEggToDatabase(Session dbSession, GetHatchedEggsOutProto getHatchedEggsProto)
        {
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
                dbSession.LogEntrys.Add(eggLogEntry);
            }

            SaveChanges();

        }

        public void AddSpinnedFortToDatabase(Session dbSession, FortSearchOutProto fortSearchProto)
        {
            LogEntry fortLogEntry = new LogEntry { LogEntryType = LogEntryType.Fort, timestamp = DateTime.UtcNow };

            fortLogEntry.XpReward = fortSearchProto.XpAwarded;

            dbSession.LogEntrys.Add(fortLogEntry);
            SaveChanges();
        }

        public void AddEvolvePokemonToDatabase(Session dbSession, EvolvePokemonOutProto evolvePokemon)
        {
            LogEntry evolveLogEntry = new LogEntry { LogEntryType = LogEntryType.EvolvePokemon, timestamp = DateTime.UtcNow };

            evolveLogEntry.XpReward = evolvePokemon.ExpAwarded;
            evolveLogEntry.CandyAwarded = evolvePokemon.CandyAwarded;
            evolveLogEntry.PokemonName = evolvePokemon.EvolvedPokemon.PokemonId;
            evolveLogEntry.Attack = evolvePokemon.EvolvedPokemon.IndividualAttack;
            evolveLogEntry.Defense = evolvePokemon.EvolvedPokemon.IndividualDefense;
            evolveLogEntry.Stamina = evolvePokemon.EvolvedPokemon.IndividualStamina;
            evolveLogEntry.PokemonUniqueId = evolvePokemon.EvolvedPokemon.Id;

            dbSession.LogEntrys.Add(evolveLogEntry);
            SaveChanges();
        }

        internal void SaveChanges()
        {
            SaveChanges();
        }
    }
}
