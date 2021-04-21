using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolygonStats.Models
{
    [Table("Pokemon")]
    class CaughtPokemon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }
        public Session Session { get; set; }

        [Required]
        public int PokedexId { get; set; }

        [Required]
        public int XpReward { get; set; }

        [Required]
        public int StardustReward { get; set; }

        [Required]
        public bool Shiny { get; set; }

        [Required]
        public DateTime timestamp { get; set; }
    }
}
