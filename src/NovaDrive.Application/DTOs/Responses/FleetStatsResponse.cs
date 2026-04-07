// Application/DTOs/Responses/FleetStatsResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record FleetStatsResponse(
    int TotalVehicles,
    int ActiveVehicles,
    int InactiveVehicles,
    int StandardCount,
    int VanCount,
    int LuxuryCount,
    double AverageBatteryPercentage,
    int ActiveRides
);