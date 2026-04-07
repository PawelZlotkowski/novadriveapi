// Infrastructure/Persistence/Configurations/DiscountCodeConfiguration.cs
namespace NovaDrive.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaDrive.Domain.Models;

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(d => d.Code).IsUnique();

        builder.Property(d => d.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.Value).HasPrecision(10, 2);
        builder.Property(d => d.MinimumRideValue).HasPrecision(10, 2);
    }
}