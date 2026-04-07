// Infrastructure/Persistence/NovaDriveDbContext.cs
namespace NovaDrive.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Models;

public class NovaDriveDbContext : DbContext
{
    public NovaDriveDbContext(DbContextOptions<NovaDriveDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NovaDriveDbContext).Assembly);
    }
}