// Infrastructure/Persistence/Configurations/VehicleConfiguration.cs
namespace NovaDrive.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaDrive.Domain.Models;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VIN).IsRequired().HasMaxLength(17);
        builder.HasIndex(v => v.VIN).IsUnique();

        builder.Property(v => v.LicensePlate).IsRequired().HasMaxLength(20);
        builder.HasIndex(v => v.LicensePlate).IsUnique();

        builder.Property(v => v.Model).IsRequired().HasMaxLength(100);

        builder.Property(v => v.VehicleType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasMany(v => v.Rides)
            .WithOne(r => r.Vehicle)
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(v => v.MaintenanceLogs)
            .WithOne(m => m.Vehicle)
            .HasForeignKey(m => m.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}