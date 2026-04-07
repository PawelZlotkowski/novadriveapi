// Application/DTOs/Requests/CreateMaintenanceLogRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record CreateMaintenanceLogRequest(
    DateTime ServiceDate,
    string Description,
    string TechnicianName,
    decimal Cost,
    int NextServiceMileage
);