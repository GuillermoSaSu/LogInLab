using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogInLab.Infrastructure.Persistence.Configurations
{
    public class BackupCodeConfiguration : IEntityTypeConfiguration<BackupCode>
    {
        public void Configure(EntityTypeBuilder<BackupCode> builder)
        {
            builder.ToTable("backup_code");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.CodeHash)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            builder.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(b => b.UserId);
        }
    }
}
