using System;

namespace PolygonStats
{
    class Stats
    {
        public Stats() => ConnectionTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        public Stats(string acc) : this() => AccountName = acc;
        private float Days => (Now - ConnectionTimestamp) / 3600f / 24f;
        private float Hours => (Now - ConnectionTimestamp) / 3600f;
        private static long Now => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        public long ShinyPokemon { get; set; } = 0;
        public string AccountName { get; set; } = null;
        public long ConnectionTimestamp { get; set; } = 0;
        public long CaughtPokemon { get; set; } = 0;
        public long XpTotal { get; set; } = 0;
        public long StardustTotal { get; set; } = 0;
        public long SpinnedPokestops { get; set; } = 0;
        public long FleetPokemon { get; set; } = 0;

        public int XpPerHour => (int)(XpTotal / Hours);

        public int StardustPerHour => (int)(StardustTotal / Hours);

        public int XpPerDay => (int)(XpTotal / Days);

        public int StardustPerDay => (int)(StardustTotal / Days);

        public void AddStardust(long stardust) => StardustTotal += stardust;

        public void AddXp(long xp) => XpTotal += xp;

        public void AddSpinnedPokestop() => SpinnedPokestops++;

        internal int CaughtPokemonPerDay => (int)(CaughtPokemon / Days);

        internal int SpinnedPokestopsPerDay => (int)(SpinnedPokestops / Days);
    }
}
