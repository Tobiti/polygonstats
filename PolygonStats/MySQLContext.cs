using System;
using Microsoft.EntityFrameworkCore;
using PolygonStats.Configuration;
using PolygonStats.Models;

namespace PolygonStats
{
    class MySQLContext : DbContext
    {
        public DbSet<Encounter> Encounters { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<LogEntry> Logs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbConnectionString = ConfigurationManager.shared.config.mysqlSettings.dbConnectionString;
            optionsBuilder.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString), options => options.EnableRetryOnFailure());
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Default values for LogEntry
            modelBuilder.Entity<LogEntry>()
                .Property(l => l.XpReward)
                .HasDefaultValue(0);
            modelBuilder.Entity<LogEntry>()
                .Property(l => l.StardustReward)
                .HasDefaultValue(0);
            modelBuilder.Entity<LogEntry>()
                .Property(l => l.Shiny)
                .HasDefaultValue(false);
            modelBuilder.Entity<LogEntry>()
                .Property(l => l.Shadow)
                .HasDefaultValue(false);
            modelBuilder.Entity<LogEntry>()
                .HasIndex(l => new { l.PokemonUniqueId })
                .IsUnique(false);
        }
    }
}
