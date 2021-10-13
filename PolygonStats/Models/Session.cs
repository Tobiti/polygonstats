using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolygonStats.Models
{
    [Table("Session")]
    class Session
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        [DefaultValue(0)]
        public int TotalGainedXp { get; set; }

        [DefaultValue(0)]
        public int TotalGainedStardust { get; set; }

        [DefaultValue(0)]
        public int TotalMinutes { get; set; }

        [DefaultValue(0)]
        public int CaughtPokemon { get; set; }

        [DefaultValue(0)]
        public int EscapedPokemon { get; set; }

        [DefaultValue(0)]
        public int ShinyPokemon { get; set; }

        [DefaultValue(0)]
        public int Pokestops { get; set; }

        [DefaultValue(0)]
        public int Rockets { get; set; }

        [DefaultValue(0)]
        public int Raids { get; set; }

        [DefaultValue(0)]
        public int MaxIV { get; set; }

        [DefaultValue(0)]
        public int Shadow { get; set; }
        public DateTime LastUpdate { get; set; }

        public IList<LogEntry> LogEntrys { get; set; } = new List<LogEntry>();
    }
}
