// Application/Services/PricingService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs.Pricing;
using NovaDrive.Domain.Enums;

public interface IPricingService
{
    PricingResult CalculatePrice(PricingRequest request);
}

public class PricingService : IPricingService
{
    // Constants
    private const decimal BaseRate = 2.50m;
    private const decimal PricePerKm = 1.10m;
    private const decimal PricePerMinute = 0.30m;
    private const decimal NightSurchargeRate = 0.15m;
    private const decimal VatRate = 0.21m;
    private const decimal MinimumFare = 5.00m;
    private const decimal LoyaltyDiscountPer100Points = 1.00m;
    private const decimal MaxLoyaltyDiscountPercentage = 0.20m;

    public PricingResult CalculatePrice(PricingRequest request)
    {
        var result = new PricingResult { VatRate = VatRate };

        // Step 1: Base price
        result.BasePrice = BaseRate
            + (request.DistanceKm * PricePerKm)
            + (request.DurationMinutes * PricePerMinute);

        // Step 2: Vehicle multiplier
        result.VehicleMultiplier = GetVehicleMultiplier(request.VehicleType);
        result.AfterMultiplier = result.BasePrice * result.VehicleMultiplier;

        // Step 3: Night surcharge (22:00 - 06:00)
        result.IsNightRate = IsNightTime(request.RideStartTime);
        result.NightSurcharge = result.IsNightRate
            ? result.AfterMultiplier * NightSurchargeRate
            : 0m;

        result.SubtotalBeforeDiscounts = result.AfterMultiplier + result.NightSurcharge;
        var runningTotal = result.SubtotalBeforeDiscounts;

        // Step 4: Loyalty discount (€1 per 100 points, max 20% of current total)
        if (request.PassengerLoyaltyPoints >= 100)
        {
            var loyaltyBlocks = request.PassengerLoyaltyPoints / 100;
            var maxLoyaltyDiscount = runningTotal * MaxLoyaltyDiscountPercentage;
            var rawLoyaltyDiscount = loyaltyBlocks * LoyaltyDiscountPer100Points;

            result.LoyaltyDiscount = Math.Min(rawLoyaltyDiscount, maxLoyaltyDiscount);
            result.LoyaltyPointsUsed = (int)(result.LoyaltyDiscount / LoyaltyDiscountPer100Points) * 100;

            runningTotal -= result.LoyaltyDiscount;
        }

        // Step 5: Discount code
        if (request.DiscountCode is not null)
        {
            var code = request.DiscountCode;
            bool isValid = DateTime.UtcNow < code.ExpiresAt
                && runningTotal >= code.MinimumRideValue;

            if (isValid)
            {
                result.CodeApplied = code.Code;

                result.CodeDiscount = code.Type switch
                {
                    DiscountType.Percentage => runningTotal * (code.Value / 100m),
                    DiscountType.Flat => Math.Min(code.Value, runningTotal),
                    _ => 0m
                };

                runningTotal -= result.CodeDiscount;
            }
        }

        // Ensure non-negative before VAT
        runningTotal = Math.Max(0m, runningTotal);

        // Step 6: VAT
        result.SubtotalBeforeVat = runningTotal;
        result.VatAmount = runningTotal * VatRate;

        // Step 7: Final price
        var finalPrice = runningTotal + result.VatAmount;

        // Step 8: Minimum fare
        finalPrice = Math.Max(finalPrice, MinimumFare);

        // Step 9: Round to 2 decimal places
        result.FinalPrice = Math.Round(finalPrice, 2, MidpointRounding.AwayFromZero);
        result.VatAmount = Math.Round(result.VatAmount, 2, MidpointRounding.AwayFromZero);

        return result;
    }

    private static decimal GetVehicleMultiplier(VehicleType type) => type switch
    {
        VehicleType.Standard => 1.0m,
        VehicleType.Van => 1.5m,
        VehicleType.Luxury => 2.2m,
        _ => 1.0m
    };

    private static bool IsNightTime(DateTime time)
    {
        var hour = time.Hour;
        return hour >= 22 || hour < 6;
    }
}