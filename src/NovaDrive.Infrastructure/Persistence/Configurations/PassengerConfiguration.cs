// Infrastructure/Persistence/Configurations/PassengerConfiguration.cs
namespace NovaDrive.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaDrive.Domain.Models;

public class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.HomeAddress).IsRequired().HasMaxLength(500);

        builder.Property(p => p.PreferredPaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasMany(p => p.Rides)
            .WithOne(r => r.Passenger)
            .HasForeignKey(r => r.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.SupportTickets)
            .WithOne(t => t.Passenger)
            .HasForeignKey(t => t.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}