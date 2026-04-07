// Application/DTOs/Pricing/PricingRequest.cs
namespace NovaDrive.Application.DTOs.Pricing;

using NovaDrive.Domain.Enums;

public class PricingRequest
{
    public decimal DistanceKm { get; set; }
    public decimal DurationMinutes { get; set; }
    public VehicleType VehicleType { get; set; }
    public DateTime RideStartTime { get; set; }
    public int PassengerLoyaltyPoints { get; set; }
    public DiscountCodeInfo? DiscountCode { get; set; }
}

public class DiscountCodeInfo
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumRideValue { get; set; }
    public DateTime ExpiresAt { get; set; }
}