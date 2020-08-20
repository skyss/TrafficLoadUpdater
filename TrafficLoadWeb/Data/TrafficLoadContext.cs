using Microsoft.EntityFrameworkCore;
using TrafficLoadWeb.Models;

namespace TrafficLoadWeb.Data
{
    public class TrafficLoadContext : DbContext
    {

        public TrafficLoadContext(DbContextOptions<TrafficLoadContext> options) : base(options)
        {
        }

        public DbSet<TurModel> Turer { get; set; }
        public DbSet<StopPoint> Stopp { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TurModel>().HasMany(c => c.History);
        }

    }
}
