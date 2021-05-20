using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [DefaultValue(0)]
        public int TotalXp { get; set; }

        [DefaultValue(0)]
        public int TotalStardust { get; set; }

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

        public IList<Session> Sessions { get; set; } = new List<Session>();
    }
}
