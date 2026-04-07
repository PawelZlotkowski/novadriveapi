// Infrastructure/Persistence/Configurations/RideConfiguration.cs
namespace NovaDrive.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaDrive.Domain.Models;

public class RideConfiguration : IEntityTypeConfiguration<Ride>
{
    public void Configure(EntityTypeBuilder<Ride> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.DepartureAddress).IsRequired().HasMaxLength(500);
        builder.Property(r => r.DestinationAddress).IsRequired().HasMaxLength(500);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.FinalPrice).HasPrecision(10, 2);
        builder.Property(r => r.VatAmount).HasPrecision(10, 2);
        builder.Property(r => r.SubtotalBeforeVat).HasPrecision(10, 2);
        builder.Property(r => r.DistanceKm).HasPrecision(10, 2);
        builder.Property(r => r.DurationMinutes).HasPrecision(10, 2);

        builder.HasOne(r => r.Payment)
            .WithOne(p => p.Ride)
            .HasForeignKey<Payment>(p => p.RideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.DiscountCode)
            .WithMany()
            .HasForeignKey(r => r.DiscountCodeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RequestedAt);
    }
}