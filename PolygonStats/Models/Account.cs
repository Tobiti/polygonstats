using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using POGOProtos.Rpc;

namespace PolygonStats.Models
{

    [Table("Account")]
    class Account
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string HashedName { get; set; }

        [DefaultValue(1)]
        public int Level { get; set; }

        [Column(TypeName = "nvarchar(24)")]
        public Team Team { get; set; }

        [DefaultValue(0)]
        public int Pokecoins { get; set; }

        [DefaultValue(0)]
        public int Experience { get; set; }

        [DefaultValue(0)]
        public long NextLevelExp { get; set; }

        [DefaultValue(0)]
        public int Stardust { get; set; }

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

        public IList<Session> Sessions { get; set; } = new List<Session>();
    }
}
