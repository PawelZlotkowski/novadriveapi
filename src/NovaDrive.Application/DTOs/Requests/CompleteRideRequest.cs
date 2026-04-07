// Application/DTOs/Requests/CompleteRideRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record CompleteRideRequest(
    decimal DistanceKm,
    decimal DurationMinutes
);