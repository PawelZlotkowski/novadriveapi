// Infrastructure/Persistence/Configurations/MaintenanceLogConfiguration.cs
namespace NovaDrive.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaDrive.Domain.Models;

public class MaintenanceLogConfiguration : IEntityTypeConfiguration<MaintenanceLog>
{
    public void Configure(EntityTypeBuilder<MaintenanceLog> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Description).IsRequired().HasMaxLength(2000);
        builder.Property(m => m.TechnicianName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Cost).HasPrecision(10, 2);

        builder.HasIndex(m => m.VehicleId);
    }
}