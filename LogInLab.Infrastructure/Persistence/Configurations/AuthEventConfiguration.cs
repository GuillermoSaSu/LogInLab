using LogInLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogInLab.Infrastructure.Persistence.Configurations
{
    public class AuthEventConfiguration : IEntityTypeConfiguration<AuthEvent>
    {
        public void Configure(EntityTypeBuilder<AuthEvent> builder)
        {
            builder.ToTable("auth_events");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.EventType)
                .HasConversion<string>()
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.Email)
                .HasMaxLength(256);

            builder.Property(e => e.IpAddress)
                .HasMaxLength(45);

            builder.Property(e => e.UserAgent)
                .HasMaxLength(512);

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(e => e.UserId);
            builder.HasIndex(e => e.CreatedAt);
        }
    }
}
