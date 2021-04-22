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
        public long caughtPokemon = 0;
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

        public int getXpPerDay()
        {
            long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            float days = (((now - connectionTimestamp) / 60f) / 60f) / 24f;

            return (int)(xpTotal / days);
        }

        public int getStardustPerDay()
        {
            long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            float days = (((now - connectionTimestamp) / 60f) / 60f) / 24f;

            return (int)(stardustTotal / days);
        }

        public void addStardust(long stardust)
        {
            stardustTotal = stardust;
        }

        public void addXp(long xp)
        {
            xpTotal = xp;
        }

        public void addSpinnedPokestop()
        {
            spinnedPokestops += 1;
        }

        internal int getCaughtPokemonPerDay()
        {
            long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            float days = (((now - connectionTimestamp) / 60f) / 60f) / 24f;

            return (int)(caughtPokemon / days);
        }

        internal int getSpinnedPokestopsPerDay()
        {
            long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            float days = (((now - connectionTimestamp) / 60f) / 60f) / 24f;

            return (int)(spinnedPokestops / days);
        }
    }
}
