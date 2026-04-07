// Infrastructure/Persistence/Configurations/PaymentConfiguration.cs
namespace NovaDrive.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaDrive.Domain.Models;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        builder.Property(p => p.TransactionReference).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.TransactionReference).IsUnique();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
    }
}