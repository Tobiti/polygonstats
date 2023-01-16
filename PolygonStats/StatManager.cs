using System.Collections.Generic;

namespace PolygonStats
{
    class StatManager
    {
        private static StatManager _sharedInstance;
        public static StatManager sharedInstance
        {
            get
            {
                if (_sharedInstance == null)
                {
                    _sharedInstance = new StatManager();
                }
                return _sharedInstance;
            }
        }

        private Dictionary<string, Stats> statDictionary = new Dictionary<string, Stats>();

        public Stats getEntry(string acc)
        {
            if (!statDictionary.ContainsKey(acc))
            {
                statDictionary.Add(acc, new Stats(acc));
            }
            return statDictionary[acc];
        }

        internal void removeEntry(string acc)
        {
            if (statDictionary.ContainsKey(acc))
            {
                statDictionary.Remove(acc);
            }
        }

        public Dictionary<string, Stats> getAllEntries()
        {
            return statDictionary;
        }

        public bool containsAccount(string acc)
        {
            return statDictionary.ContainsKey(acc);
        }
    }
}
