using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using POGOProtos.Rpc;

namespace PolygonStats.Models
{

    [Table("Encounter")]
    class Encounter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public ulong EncounterId { get; internal set; }
        
        [Required]
        [Column(TypeName = "nvarchar(30)")]
        public HoloPokemonId PokemonName { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public PokemonDisplayProto.Types.Form Form { get; set; }

        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Stamina { get; set; }

        public double Longitude {get; set; }
        public double Latitude {get; set; }

        [Required]
        public DateTime timestamp { get; set; }
    }
}
