using Microsoft.EntityFrameworkCore;
using PolygonStats.Configuration;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolygonStats.RocketMap
{
    [Table("trs_spawn")]
    public class Spawnpoint
    {
        [Key]
        public long spawnpoint { get; set; }
        public int spawndef { get; set; }
        public string calc_endminsec { get; set; }
    }

    class RocketMapContext : DbContext
    {
        public DbSet<Spawnpoint> Spawnpoints { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbConnectionString = ConfigurationManager.Shared.Config.MadExport.ConnectionString;
            optionsBuilder.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString), options => options.EnableRetryOnFailure());
        }
    }
}
