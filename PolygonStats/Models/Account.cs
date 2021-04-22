using System.Collections.Generic;
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

        public IList<Session> Sessions { get; set; } = new List<Session>();
    }
}
