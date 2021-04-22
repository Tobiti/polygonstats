using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolygonStats.Models
{
    public enum LogEntryType
    {
        Pokemon,
        Quest,
        Egg,
        Fort,
        FeedBerry,
        EvolvePokemon
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

        public int PokedexId { get; set; }

        public int XpReward { get; set; }

        public int StardustReward { get; set; }

        public int CandyAwarded { get; set; }

        public bool Shiny { get; set; }

        [Required]
        public DateTime timestamp { get; set; }
    }
}
