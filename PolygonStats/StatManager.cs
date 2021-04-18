using POGOProtos.Rpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonStats
{
    class StatManager
    {
        private static StatManager _sharedInstance;
        public static StatManager sharedInstance
        {
            get
            {
                if(_sharedInstance == null)
                {
                    _sharedInstance = new StatManager();
                }
                return _sharedInstance;
            }
        }

        private Dictionary<string, Stats> statDictionary = new Dictionary<string, Stats>();

        public Stats getEntry(string account)
        {
            if (!statDictionary.ContainsKey(account))
            {
                statDictionary.Add(account, new Stats());
            }
            return statDictionary[account];
        }

        internal void removeEntry(string accName)
        {
            statDictionary.Remove(accName);
        }

        public void addCatchedPokemon(string account, CatchPokemonOutProto catchedPokemon)
        {
            Stats entry = getEntry(account);
            switch (catchedPokemon.Status)
            {
                case CatchPokemonOutProto.Types.Status.CatchSuccess:
                    Interlocked.Increment(ref entry.catchedPokemon);
                    if (catchedPokemon.PokemonDisplay.Shiny)
                    {
                        Interlocked.Increment(ref entry.shinyPokemon);
                    }

                    entry.addXp(catchedPokemon.Scores.Exp.Sum());
                    entry.addStardust(catchedPokemon.Scores.Stardust.Sum());
                    break;
                case CatchPokemonOutProto.Types.Status.CatchEscape:
                case CatchPokemonOutProto.Types.Status.CatchFlee:
                    Interlocked.Increment(ref entry.fleetPokemon);
                    break;
            }
        }

        public Dictionary<string, Stats> getAllEntries()
        {
            return statDictionary;
        }
    }
}
