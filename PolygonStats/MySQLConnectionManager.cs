using Google.Protobuf.Collections;
using POGOProtos.Rpc;
using PolygonStats.Models;
using System;
using System.Linq;
namespace PolygonStats
{
    class MySQLConnectionManager
    {
        public MySQLContext GetContext() {
            return new MySQLContext();
        }

        public Session GetSession(MySQLContext context, int id) {
            return context.Sessions.Where(s => s.Id == id).FirstOrDefault<Session>();
        }

        public void AddLogEntry(Session session, LogEntry log) {

            switch(log.LogEntryType) {
                case LogEntryType.Pokemon:
                    if (log.PokemonUniqueId != 0) {
                        session.Account.CaughtPokemon += 1;
                        session.Account.ShinyPokemon += log.Shiny ? 1 : 0;
                    } else {
                        session.Account.EscapedPokemon += 1;
                    }
                    break;
                case LogEntryType.Egg:
                    session.Account.ShinyPokemon += log.Shiny ? 1 : 0;
                    break;
                case LogEntryType.Rocket:
                    session.Account.Rockets += 1;
                    break;
                case LogEntryType.Raid:
                    session.Account.Raids += 1;
                    break;
            }
            session.Account.TotalXp += log.XpReward;
            session.Account.TotalStardust += log.StardustReward;
            session.LogEntrys.Add(log);
        }

        public void AddPokemonToDatabase(int dbSessionId, CatchPokemonOutProto catchedPokemon)
        {
            using (var context = new MySQLContext()) {
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
                pokemonLogEntry.XpReward = catchedPokemon.Scores.Exp.Sum();
                pokemonLogEntry.StardustReward = catchedPokemon.Scores.Stardust.Sum();
                this.AddLogEntry(dbSession, pokemonLogEntry);
                context.SaveChanges();
            }
        }

        public void AddFeedBerryToDatabase(int dbSessionId, GymFeedPokemonOutProto gymFeedPokemonProto)
        {
            using (var context = new MySQLContext()) {
                Session dbSession = this.GetSession(context, dbSessionId);
                LogEntry feedBerryLogEntry = new LogEntry { LogEntryType = LogEntryType.FeedBerry, timestamp = DateTime.UtcNow };

                feedBerryLogEntry.XpReward = gymFeedPokemonProto.XpAwarded;
                feedBerryLogEntry.StardustReward = gymFeedPokemonProto.StardustAwarded;
                feedBerryLogEntry.CandyAwarded = gymFeedPokemonProto.NumCandyAwarded;

                this.AddLogEntry(dbSession, feedBerryLogEntry);
                context.SaveChanges();
            }
        }

        public void AddQuestToDatabase(int dbSessionId, RepeatedField<QuestRewardProto> rewards)
        {
            using (var context = new MySQLContext()) {
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
                this.AddLogEntry(dbSession, questLogEntry);
                context.SaveChanges();
            }
        }

        public void AddHatchedEggToDatabase(int dbSessionId, GetHatchedEggsOutProto getHatchedEggsProto)
        {
            using (var context = new MySQLContext()) {
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
                    this.AddLogEntry(dbSession, eggLogEntry);
                }

                context.SaveChanges();
            }
        }

        public void AddSpinnedFortToDatabase(int dbSessionId, FortSearchOutProto fortSearchProto)
        {
            using (var context = new MySQLContext()) {
                Session dbSession = this.GetSession(context, dbSessionId);
                LogEntry fortLogEntry = new LogEntry { LogEntryType = LogEntryType.Fort, timestamp = DateTime.UtcNow };

                fortLogEntry.XpReward = fortSearchProto.XpAwarded;

                this.AddLogEntry(dbSession, fortLogEntry);
                context.SaveChanges();
            }
        }

        public void AddEvolvePokemonToDatabase(int dbSessionId, EvolvePokemonOutProto evolvePokemon)
        {
            using (var context = new MySQLContext()) {
                Session dbSession = this.GetSession(context, dbSessionId);
                LogEntry evolveLogEntry = new LogEntry { LogEntryType = LogEntryType.EvolvePokemon, timestamp = DateTime.UtcNow };

                evolveLogEntry.XpReward = evolvePokemon.ExpAwarded;
                evolveLogEntry.CandyAwarded = evolvePokemon.CandyAwarded;
                evolveLogEntry.PokemonName = evolvePokemon.EvolvedPokemon.PokemonId;
                evolveLogEntry.Attack = evolvePokemon.EvolvedPokemon.IndividualAttack;
                evolveLogEntry.Defense = evolvePokemon.EvolvedPokemon.IndividualDefense;
                evolveLogEntry.Stamina = evolvePokemon.EvolvedPokemon.IndividualStamina;
                evolveLogEntry.PokemonUniqueId = evolvePokemon.EvolvedPokemon.Id;

                this.AddLogEntry(dbSession, evolveLogEntry);
                context.SaveChanges();
            }
        }

        internal void AddRocketToDatabase(int dbSessionId, UpdateInvasionBattleOutProto updateBattle)
        {
            using (var context = new MySQLContext()) {
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

                this.AddLogEntry(dbSession, rocketLogEntry);
                context.SaveChanges();
            }
        }

        internal void AddRaidToDatabase(int dbSessionId, int xp, int stardust)
        {
            using (var context = new MySQLContext()) {
                Session dbSession = this.GetSession(context, dbSessionId);
                LogEntry raidLogEntry = new LogEntry { LogEntryType = LogEntryType.Raid, timestamp = DateTime.UtcNow };

                raidLogEntry.XpReward = xp;
                raidLogEntry.StardustReward = stardust;
                this.AddLogEntry(dbSession, raidLogEntry);
                context.SaveChanges();
            }
        }
    }
}
