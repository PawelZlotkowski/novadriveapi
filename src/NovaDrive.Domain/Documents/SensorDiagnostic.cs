// Domain/Documents/SensorDiagnostic.cs
namespace NovaDrive.Domain.Documents;

using System.Text.Json;

public class SensorDiagnostic
{
    public string Id { get; set; } = string.Empty;
    public string VehicleId { get; set; } = string.Empty;
    public string SensorType { get; set; } = string.Empty;   // "Lidar", "Radar", "Camera"
    public string ErrorCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;     // "Info", "Warning", "Critical"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public JsonDocument? RawSensorData { get; set; }
}