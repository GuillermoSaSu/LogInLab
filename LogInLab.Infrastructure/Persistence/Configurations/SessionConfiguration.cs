using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogInLab.Infrastructure.Persistence.Configurations
{
    public class SessionConfiguration : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> builder)
        {
            builder.ToTable("sessions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.IpAddress)
                .HasMaxLength(45);

            builder.Property(s => s.UserAgent)
                .HasMaxLength(512);

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.ExpiresAt)
                .IsRequired();

            builder.HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => s.UserId);
        }
    }
}
