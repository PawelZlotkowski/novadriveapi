// Infrastructure/Persistence/Configurations/UserConfiguration.cs
namespace NovaDrive.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaDrive.Domain.Models;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Auth0Id).IsUnique();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512);

        builder.HasOne(u => u.Passenger)
            .WithOne(p => p.User)
            .HasForeignKey<Passenger>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}