using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogInLab.Infrastructure.Persistence.Configurations
{
    public class WebAuthnCredentialConfiguration : IEntityTypeConfiguration<WebAuthnCredential>
    {
        public void Configure(EntityTypeBuilder<WebAuthnCredential> builder)
        {
            builder.ToTable("webauthn_credentials");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.CredentialId)
                .IsRequired();

            builder.Property(c => c.PublicKey)
                .IsRequired();

            builder.Property(c => c.DeviceName)
                .HasMaxLength(100);

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => c.UserId);

            //Should be unique globaly not for every user.
            builder.HasIndex(c => c.CredentialId)
                .IsUnique();
        }
    }
}
