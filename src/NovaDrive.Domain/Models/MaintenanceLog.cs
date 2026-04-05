// Domain/Models/MaintenanceLog.cs
namespace NovaDrive.Domain.Models;

public class MaintenanceLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public DateTime ServiceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int NextServiceMileage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Vehicle Vehicle { get; set; } = null!;
}