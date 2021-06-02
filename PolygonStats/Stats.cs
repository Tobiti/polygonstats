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

        public Stats() => connectionTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        public Stats(string acc) : this() => accountName = acc;
        private float Days => (Now - connectionTimestamp) / 60f / 60f / 24f;
        private float Hours => (Now - connectionTimestamp) / 60f / 60f;
        private long Now => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        public int getXpPerHour() => (int)(xpTotal / Hours);

        public int getStardustPerHour() => (int)(stardustTotal / Hours);

        public int getXpPerDay() => (int)(xpTotal / Days);

        public int getStardustPerDay() => (int)(stardustTotal / Days);

        public void addStardust(long stardust) => stardustTotal += stardust;

        public void addXp(long xp) => xpTotal += xp;

        public void addSpinnedPokestop() => spinnedPokestops += 1;

        internal int getCaughtPokemonPerDay() => (int)(caughtPokemon / Days);

        internal int getSpinnedPokestopsPerDay() => (int)(spinnedPokestops / Days);
    }
}
