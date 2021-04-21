using System;
using System.Collections.Generic;
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

        public IList<CaughtPokemon> CaughtPokemons { get; set; }
        public IList<SpinnedFort> SpinnedForts { get; set; }
        public IList<FinishedQuest> FinishedQuests { get; set; }
        public IList<HatchedEgg> HatchedEggs { get; set; }
    }
}
