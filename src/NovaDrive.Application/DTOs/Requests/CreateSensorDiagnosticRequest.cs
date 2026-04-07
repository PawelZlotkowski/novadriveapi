// Application/DTOs/Requests/CreateSensorDiagnosticRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

using System.Text.Json;

public record CreateSensorDiagnosticRequest(
    Guid VehicleId,
    string SensorType,       // "Lidar", "Radar", "Camera"
    string ErrorCode,
    string Severity,         // "Info", "Warning", "Critical"
    JsonDocument? RawSensorData
);