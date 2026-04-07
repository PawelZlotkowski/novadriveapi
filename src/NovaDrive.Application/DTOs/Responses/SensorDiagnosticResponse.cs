// Application/DTOs/Responses/SensorDiagnosticResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record SensorDiagnosticResponse(
    string Id,
    string VehicleId,
    string SensorType,
    string ErrorCode,
    string Severity,
    DateTime Timestamp,
    object? RawSensorData
);