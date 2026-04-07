// Application/DTOs/Requests/UpdateVehicleRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record UpdateVehicleRequest(
    string? LicensePlate,
    string? Model,
    string? VehicleType,
    bool? IsActive
);