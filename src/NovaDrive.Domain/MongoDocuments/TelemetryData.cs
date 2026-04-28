// Domain/Documents/TelemetryData.cs
namespace NovaDrive.Domain.Documents;

public class TelemetryData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string VehicleId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedKmh { get; set; }
    public double BatteryPercentage { get; set; }
    public double HardwareTemperatureCelsius { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}