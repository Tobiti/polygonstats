using POGOProtos.Rpc;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static POGOProtos.Rpc.PokemonDisplayProto.Types;

namespace PolygonStats.Models
{
    public enum LogEntryType
    {
        Pokemon,
        Quest,
        Egg,
        Fort,
        FeedBerry,
        EvolvePokemon,
        Rocket,
        Raid
    }

    [Table("SessionLogEntry")]
    class LogEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SessionId { get; set; }
        public Session Session { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(24)")]
        public LogEntryType LogEntryType { get; set; }

        public bool CaughtSuccess { get; set; }

        public ulong PokemonUniqueId { get; set; }

        [Column(TypeName = "nvarchar(24)")]
        public HoloPokemonId PokemonName { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Stamina { get; set; }

        public int XpReward { get; set; }

        public int StardustReward { get; set; }

        public int CandyAwarded { get; set; }

        public bool Shiny { get; set; }

        public bool Shadow { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public Costume Costume { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public Form Form { get; set; }

        [Required]
        public DateTime timestamp { get; set; }
    }
}
