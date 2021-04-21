using System;
using Microsoft.EntityFrameworkCore;
using PolygonStats.Models;

namespace PolygonStats
{
    class MySQLContext : DbContext
    {
        public MySQLContext(DbContextOptions<MySQLContext> options) : base(options) { }
        public DbSet<Account> User { get; set; }
    }
}
