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

        private Dictionary<ClientSession, Stats> statDictionary = new Dictionary<ClientSession, Stats>();

        public Stats getEntry(ClientSession session)
        {
            if (!statDictionary.ContainsKey(session))
            {
                statDictionary.Add(session, new Stats());
            }
            return statDictionary[session];
        }

        internal void removeEntry(ClientSession session)
        {
            statDictionary.Remove(session);
        }

        public Dictionary<ClientSession, Stats> getAllEntries()
        {
            return statDictionary;
        }
    }
}
