// Domain/Models/Vehicle.cs
namespace NovaDrive.Domain.Models;

using NovaDrive.Domain.Enums;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string VIN { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; } = VehicleType.Standard;
    public int YearOfManufacture { get; set; }
    public bool IsActive { get; set; } = true;
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public double? CurrentBatteryPercentage { get; set; }
    public int CurrentMileage { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Ride> Rides { get; set; } = new List<Ride>();
    public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
}