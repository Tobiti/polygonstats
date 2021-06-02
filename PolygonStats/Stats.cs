namespace PolygonStats
{
    class Stats
    {
        private string accountName = null;
        private long _connectionTimestamp = 0;
        private long _caughtPokemon = 0;
        private long _xpTotal = 0;
        private long _stardustTotal = 0;
        private long _spinnedPokestops = 0;
        private long _fleetPokemon = 0;
        private long _shinyPokemon = 0;

        public Stats() => ConnectionTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        public Stats(string acc) : this() => AccountName = acc;
        private float Days => (Now - ConnectionTimestamp) / 60f / 60f / 24f;
        private float Hours => (Now - ConnectionTimestamp) / 60f / 60f;
        private long Now => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        public Int64 ShinyPokemon { get => _shinyPokemon; set => _shinyPokemon = value; }
        public String AccountName { get => accountName; set => accountName = value; }
        public Int64 ConnectionTimestamp { get => _connectionTimestamp; set => _connectionTimestamp = value; }
        public Int64 CaughtPokemon { get => _caughtPokemon; set => _caughtPokemon = value; }
        public Int64 XpTotal { get => _xpTotal; set => _xpTotal = value; }
        public Int64 StardustTotal { get => _stardustTotal; set => _stardustTotal = value; }
        public Int64 SpinnedPokestops { get => _spinnedPokestops; set => _spinnedPokestops = value; }
        public Int64 FleetPokemon { get => _fleetPokemon; set => _fleetPokemon = value; }

        public int XpPerHour => (int)(XpTotal / Hours);

        public int StardustPerHour => (int)(StardustTotal / Hours);

        public int XpPerDay => (int)(XpTotal / Days);

        public int StardustPerDay => (int)(StardustTotal / Days);

        public void AddStardust(long stardust) => StardustTotal += stardust;

        public void AddXp(long xp) => XpTotal += xp;

        public void AddSpinnedPokestop() => SpinnedPokestops += 1;

        internal int CaughtPokemonPerDay => (int)(CaughtPokemon / Days);

        internal int SpinnedPokestopsPerDay => (int)(SpinnedPokestops / Days);
    }
}
