// Application/DTOs/Responses/VehicleResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record VehicleResponse(
    Guid Id,
    string VIN,
    string LicensePlate,
    string Model,
    string VehicleType,
    int YearOfManufacture,
    bool IsActive,
    double? CurrentLatitude,
    double? CurrentLongitude,
    double? CurrentBatteryPercentage,
    int CurrentMileage
);