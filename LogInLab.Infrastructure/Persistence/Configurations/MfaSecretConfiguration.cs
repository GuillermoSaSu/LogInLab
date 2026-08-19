using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogInLab.Infrastructure.Persistence.Configurations
{
    public class MfaSecretConfiguration : IEntityTypeConfiguration<MfaSecret>
    {
        public void Configure(EntityTypeBuilder<MfaSecret> builder)
        {
            builder.ToTable("mfa_secret");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.SecretKeyEncripted)
                .IsRequired();

            builder.Property(m => m.IsActive)
                .HasDefaultValue(false);

            builder.Property(m => m.CreatedAt)
                .IsRequired();

            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => m.UserId)
                .IsUnique();
        }
    }
}
