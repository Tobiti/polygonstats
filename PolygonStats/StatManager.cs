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

        public Stats getEntry(string sessionId)
        {
            if (!statDictionary.ContainsKey(sessionId))
            {
                statDictionary.Add(sessionId, new Stats());
            }
            return statDictionary[sessionId];
        }

        internal void removeEntry(string accName)
        {
            statDictionary.Remove(accName);
        }

        public Dictionary<string, Stats> getAllEntries()
        {
            return statDictionary;
        }
    }
}
