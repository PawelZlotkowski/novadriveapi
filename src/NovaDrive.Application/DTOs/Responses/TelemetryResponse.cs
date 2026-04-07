// Application/DTOs/Responses/TelemetryResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record TelemetryResponse(
    string VehicleId,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double BatteryPercentage,
    double HardwareTemperatureCelsius,
    DateTime Timestamp
);