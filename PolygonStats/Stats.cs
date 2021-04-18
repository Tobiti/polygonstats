using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonStats
{
    class Stats
    {
        public string accountName = null;
        public long connectionTimestamp = 0;
        public long catchedPokemon = 0;
        public long xpTotal = 0;
        public long stardustTotal = 0;
        public long spinnedPokestops = 0;
        public long fleetPokemon = 0;
        public long shinyPokemon = 0;

        public Stats()
        {
            this.connectionTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }
        public Stats(string acc)
        {
            this.connectionTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            this.accountName = acc;
        }

        public int getXpPerHour()
        {
            long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            float hours = ((now - connectionTimestamp) / 60f) / 60f;

            return (int)(xpTotal / hours);
        }

        public int getStardustPerHour()
        {
            long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            float hours = ((now - connectionTimestamp) / 60f) / 60f;

            return (int)(stardustTotal / hours);
        }

        public void addStardust(long stardust)
        {
            Interlocked.Add(ref stardustTotal, stardust);
        }

        public void addXp(long xp)
        {
            Interlocked.Add(ref xpTotal, xp);
        }

        public void addSpinnedPokestop()
        {
            Interlocked.Increment(ref this.spinnedPokestops);
        }
    }
}
