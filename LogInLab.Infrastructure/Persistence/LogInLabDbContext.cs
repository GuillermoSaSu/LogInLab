using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogInLab.Infrastructure.Persistence
{
    public class LogInLabDbContext : DbContext
    {
        public LogInLabDbContext(DbContextOptions<LogInLabDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        public DbSet<Session> Sessions => Set<Session>();

        public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

        public DbSet<MfaSecret> MfaSecrets => Set<MfaSecret>();

        public DbSet<BackupCode> BackupCodes => Set<BackupCode>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LogInLabDbContext).Assembly);
        }
    }
}
