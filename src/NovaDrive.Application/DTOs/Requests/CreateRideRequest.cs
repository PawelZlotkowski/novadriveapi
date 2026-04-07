// Application/DTOs/Requests/CreateRideRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record CreateRideRequest(
    string DepartureAddress,
    double DepartureLatitude,
    double DepartureLongitude,
    string DestinationAddress,
    double DestinationLatitude,
    double DestinationLongitude,
    string? VehicleType,       // optional preference
    string? DiscountCode       // optional promo code
);