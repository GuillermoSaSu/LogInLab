using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogInLab.Infrastructure.Persistence
{
    public class LogInLabDbContext : DbContext
    {
        public LogInLabDbContext(DbContextOptions<LogInLabDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LogInLabDbContext).Assembly);
        }
    }
}
