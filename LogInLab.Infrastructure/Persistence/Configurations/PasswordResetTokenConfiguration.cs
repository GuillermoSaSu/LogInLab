using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogInLab.Infrastructure.Persistence.Configurations
{
    public class PasswordResetTokenConfiguration :IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("password_reset_tokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength((64));

            builder.HasIndex(x => x.TokenHash)
                .IsUnique();

            builder.Property(x => x.CreatedAt)
                .IsRequired();
            
            builder.Property(x => x.ExipresAt)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
        }
    }
}
