// Application/DTOs/Requests/CreateTelemetryRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record CreateTelemetryRequest(
    Guid VehicleId,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double BatteryPercentage,
    double HardwareTemperatureCelsius
);