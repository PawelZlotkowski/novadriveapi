// Application/DTOs/Responses/MaintenanceLogResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record MaintenanceLogResponse(
    Guid Id,
    Guid VehicleId,
    string? VehicleModel,
    string? VehicleLicensePlate,
    DateTime ServiceDate,
    string Description,
    string TechnicianName,
    decimal Cost,
    int NextServiceMileage
);