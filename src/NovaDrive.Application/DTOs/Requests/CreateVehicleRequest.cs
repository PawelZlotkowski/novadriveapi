// Application/DTOs/Requests/CreateVehicleRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record CreateVehicleRequest(
    string VIN,
    string LicensePlate,
    string Model,
    string VehicleType,    // "Standard", "Van", "Luxury"
    int YearOfManufacture,
    double? Latitude,
    double? Longitude
);