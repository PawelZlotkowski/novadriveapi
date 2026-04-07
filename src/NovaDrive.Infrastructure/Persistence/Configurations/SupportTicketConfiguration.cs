// Infrastructure/Persistence/Configurations/SupportTicketConfiguration.cs
namespace NovaDrive.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaDrive.Domain.Models;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Subject).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(5000);
        builder.Property(t => t.AdminNotes).HasMaxLength(5000);

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
    }
}