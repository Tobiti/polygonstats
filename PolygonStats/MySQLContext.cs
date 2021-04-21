using System;
using Microsoft.EntityFrameworkCore;
using PolygonStats.Configuration;
using PolygonStats.Models;

namespace PolygonStats
{
    class MySQLContext : DbContext
    {
        public DbSet<Account> User { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbConnectionString = ConfigurationManager.shared.config.mysqlSettings.dbConnectionString;
            optionsBuilder.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString));
        }
    }
}
