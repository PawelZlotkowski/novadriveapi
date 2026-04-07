// Application/DTOs/Responses/RideResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record RideResponse(
    Guid Id,
    Guid PassengerId,
    string PassengerName,
    Guid? VehicleId,
    string? VehicleModel,
    string? VehicleLicensePlate,
    string DepartureAddress,
    string DestinationAddress,
    string Status,
    DateTime RequestedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    decimal? DistanceKm,
    decimal? DurationMinutes,
    decimal? FinalPrice,
    decimal? VatAmount,
    string? DiscountCodeUsed
);