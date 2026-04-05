// Domain/Models/Ride.cs
namespace NovaDrive.Domain.Models;

using NovaDrive.Domain.Enums;

public class Ride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PassengerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string DepartureAddress { get; set; } = string.Empty;
    public double DepartureLatitude { get; set; }
    public double DepartureLongitude { get; set; }
    public string DestinationAddress { get; set; } = string.Empty;
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public RideStatus Status { get; set; } = RideStatus.Requested;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal? DurationMinutes { get; set; }
    public Guid? DiscountCodeId { get; set; }
    public decimal? FinalPrice { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? SubtotalBeforeVat { get; set; }

    // Navigation
    public Passenger Passenger { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
    public DiscountCode? DiscountCode { get; set; }
    public Payment? Payment { get; set; }
}