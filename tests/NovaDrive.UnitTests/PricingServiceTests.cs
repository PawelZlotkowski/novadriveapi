// UnitTests/PricingServiceTests.cs
namespace NovaDrive.UnitTests;

using NovaDrive.Application.DTOs.Pricing;
using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;
using FluentAssertions;

public class PricingServiceTests
{
    private readonly PricingService _sut = new();

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static PricingRequest BaseRequest(
        decimal distanceKm = 10m,
        decimal durationMinutes = 20m,
        VehicleType vehicleType = VehicleType.Standard,
        DateTime? startTime = null,
        int loyaltyPoints = 0,
        DiscountCodeInfo? discountCode = null) => new()
    {
        DistanceKm = distanceKm,
        DurationMinutes = durationMinutes,
        VehicleType = vehicleType,
        RideStartTime = startTime ?? new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc),
        PassengerLoyaltyPoints = loyaltyPoints,
        DiscountCode = discountCode
    };

    private static DiscountCodeInfo ActiveCode(
        DiscountType type = DiscountType.Percentage,
        decimal value = 10m,
        decimal minRideValue = 0m) => new()
    {
        Id = Guid.NewGuid(),
        Code = "TEST10",
        Type = type,
        Value = value,
        MinimumRideValue = minRideValue,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    // ─── Base Price ──────────────────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_BaseComponents_AreCorrect()
    {
        // 2.50 + (10 * 1.10) + (20 * 0.30) = 2.50 + 11 + 6 = 19.50
        var result = _sut.CalculatePrice(BaseRequest());

        result.BasePrice.Should().Be(19.50m);
    }

    [Fact]
    public void CalculatePrice_ZeroDistanceAndDuration_ReturnsMinimumFare()
    {
        var result = _sut.CalculatePrice(BaseRequest(distanceKm: 0m, durationMinutes: 0m));

        // BasePrice = 2.50, after VAT = 2.50 * 1.21 = 3.025 → capped at minimum fare 5.00
        result.FinalPrice.Should().Be(5.00m);
    }

    // ─── Vehicle Multiplier ──────────────────────────────────────────────────

    [Theory]
    [InlineData(VehicleType.Standard, 1.0)]
    [InlineData(VehicleType.Van, 1.5)]
    [InlineData(VehicleType.Luxury, 2.2)]
    public void CalculatePrice_VehicleMultiplier_IsApplied(VehicleType type, double multiplier)
    {
        var result = _sut.CalculatePrice(BaseRequest(vehicleType: type));

        result.VehicleMultiplier.Should().Be((decimal)multiplier);
        result.AfterMultiplier.Should().Be(result.BasePrice * (decimal)multiplier);
    }

    [Fact]
    public void CalculatePrice_LuxuryVehicle_IsMoreExpensiveThanStandard()
    {
        var standard = _sut.CalculatePrice(BaseRequest(vehicleType: VehicleType.Standard));
        var luxury = _sut.CalculatePrice(BaseRequest(vehicleType: VehicleType.Luxury));

        luxury.FinalPrice.Should().BeGreaterThan(standard.FinalPrice);
    }

    // ─── Night Surcharge ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(22, true)]   // exactly 22:00 → night
    [InlineData(23, true)]
    [InlineData(0, true)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]   // exactly 06:00 → day
    [InlineData(12, false)]
    [InlineData(21, false)]
    public void CalculatePrice_NightSurcharge_CorrectlyDetectsNightTime(int hour, bool expectNight)
    {
        var time = new DateTime(2025, 6, 15, hour, 0, 0, DateTimeKind.Utc);
        var result = _sut.CalculatePrice(BaseRequest(startTime: time));

        result.IsNightRate.Should().Be(expectNight);
        if (expectNight)
            result.NightSurcharge.Should().BeGreaterThan(0m);
        else
            result.NightSurcharge.Should().Be(0m);
    }

    [Fact]
    public void CalculatePrice_NightSurcharge_Is15Percent()
    {
        var nightTime = new DateTime(2025, 6, 15, 23, 0, 0, DateTimeKind.Utc);
        var result = _sut.CalculatePrice(BaseRequest(startTime: nightTime));

        result.NightSurcharge.Should().Be(result.AfterMultiplier * 0.15m);
    }

    // ─── Loyalty Discount ────────────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_LoyaltyPoints_BelowThreshold_NoDiscount()
    {
        var result = _sut.CalculatePrice(BaseRequest(loyaltyPoints: 99));

        result.LoyaltyDiscount.Should().Be(0m);
        result.LoyaltyPointsUsed.Should().Be(0);
    }

    [Fact]
    public void CalculatePrice_LoyaltyPoints_Exactly100_GivesOneEuroDiscount()
    {
        var result = _sut.CalculatePrice(BaseRequest(loyaltyPoints: 100));

        result.LoyaltyDiscount.Should().Be(1.00m);
        result.LoyaltyPointsUsed.Should().Be(100);
    }

    [Fact]
    public void CalculatePrice_LoyaltyPoints_250Points_Gives2EuroDiscount()
    {
        // 250 / 100 = 2 blocks → €2 discount
        var result = _sut.CalculatePrice(BaseRequest(loyaltyPoints: 250));

        result.LoyaltyDiscount.Should().Be(2.00m);
        result.LoyaltyPointsUsed.Should().Be(200);
    }

    [Fact]
    public void CalculatePrice_LoyaltyDiscount_CappedAt20Percent()
    {
        // Very large loyalty points — discount must not exceed 20% of subtotal
        var result = _sut.CalculatePrice(BaseRequest(loyaltyPoints: 100_000));

        var maxAllowed = result.SubtotalBeforeDiscounts * 0.20m;
        result.LoyaltyDiscount.Should().BeLessThanOrEqualTo(maxAllowed);
    }

    // ─── Discount Code ───────────────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_PercentageCode_ReducesSubtotal()
    {
        var code = ActiveCode(type: DiscountType.Percentage, value: 10m);
        var withCode = _sut.CalculatePrice(BaseRequest(discountCode: code));
        var withoutCode = _sut.CalculatePrice(BaseRequest());

        withCode.CodeDiscount.Should().BeGreaterThan(0m);
        withCode.FinalPrice.Should().BeLessThan(withoutCode.FinalPrice);
    }

    [Fact]
    public void CalculatePrice_PercentageCode_10Percent_IsCorrect()
    {
        var code = ActiveCode(type: DiscountType.Percentage, value: 10m);
        var result = _sut.CalculatePrice(BaseRequest(discountCode: code));

        var expectedCodeDiscount = result.SubtotalBeforeDiscounts * 0.10m;
        result.CodeDiscount.Should().BeApproximately(expectedCodeDiscount, 0.001m);
    }

    [Fact]
    public void CalculatePrice_FlatCode_SubtractsFixedAmount()
    {
        var code = ActiveCode(type: DiscountType.Flat, value: 5m);
        var result = _sut.CalculatePrice(BaseRequest(discountCode: code));

        result.CodeDiscount.Should().Be(5.00m);
        result.CodeApplied.Should().Be("TEST10");
    }

    [Fact]
    public void CalculatePrice_FlatCode_CannotExceedSubtotal()
    {
        // Flat code larger than the ride cost
        var code = ActiveCode(type: DiscountType.Flat, value: 9999m);
        var result = _sut.CalculatePrice(BaseRequest(distanceKm: 0.1m, durationMinutes: 0.1m, discountCode: code));

        result.SubtotalBeforeVat.Should().Be(0m);
        result.FinalPrice.Should().Be(5.00m); // minimum fare
    }

    [Fact]
    public void CalculatePrice_ExpiredCode_IsNotApplied()
    {
        var expiredCode = new DiscountCodeInfo
        {
            Id = Guid.NewGuid(),
            Code = "EXPIRED",
            Type = DiscountType.Percentage,
            Value = 20m,
            MinimumRideValue = 0m,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        var withExpired = _sut.CalculatePrice(BaseRequest(discountCode: expiredCode));
        var withoutCode = _sut.CalculatePrice(BaseRequest());

        withExpired.CodeDiscount.Should().Be(0m);
        withExpired.FinalPrice.Should().Be(withoutCode.FinalPrice);
    }

    [Fact]
    public void CalculatePrice_CodeWithMinimumRideValue_NotApplied_WhenSubtotalTooLow()
    {
        var code = ActiveCode(minRideValue: 1000m); // ride will never reach €1000
        var result = _sut.CalculatePrice(BaseRequest(distanceKm: 1m, durationMinutes: 5m, discountCode: code));

        result.CodeDiscount.Should().Be(0m);
    }

    [Fact]
    public void CalculatePrice_CodeWithMinimumRideValue_Applied_WhenSubtotalMeetsThreshold()
    {
        var code = ActiveCode(type: DiscountType.Flat, value: 2m, minRideValue: 5m);
        var result = _sut.CalculatePrice(BaseRequest(distanceKm: 20m, durationMinutes: 40m, discountCode: code));

        result.CodeDiscount.Should().Be(2m);
    }

    // ─── VAT ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_VatRate_Is21Percent()
    {
        var result = _sut.CalculatePrice(BaseRequest());

        result.VatRate.Should().Be(0.21m);
        result.VatAmount.Should().BeApproximately(result.SubtotalBeforeVat * 0.21m, 0.01m);
    }

    [Fact]
    public void CalculatePrice_FinalPrice_EqualsSubtotalPlusVat()
    {
        var result = _sut.CalculatePrice(BaseRequest());

        var expected = result.SubtotalBeforeVat + result.VatAmount;
        result.FinalPrice.Should().Be(Math.Round(expected, 2, MidpointRounding.AwayFromZero));
    }

    // ─── Minimum Fare ────────────────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_VeryShortRide_ReturnsMinimumFare()
    {
        var result = _sut.CalculatePrice(BaseRequest(distanceKm: 0.001m, durationMinutes: 0.001m));

        result.FinalPrice.Should().BeGreaterThanOrEqualTo(5.00m);
    }

    [Fact]
    public void CalculatePrice_NormalRide_FinalPriceExceedsMinimumFare()
    {
        var result = _sut.CalculatePrice(BaseRequest(distanceKm: 20m, durationMinutes: 30m));

        result.FinalPrice.Should().BeGreaterThan(5.00m);
    }

    // ─── Rounding ────────────────────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_FinalPrice_IsRoundedToTwoDecimalPlaces()
    {
        var result = _sut.CalculatePrice(BaseRequest(distanceKm: 7.333m, durationMinutes: 13.7m));

        var decimals = BitConverter.GetBytes(decimal.GetBits(result.FinalPrice)[3])[2];
        decimals.Should().BeLessThanOrEqualTo(2);
    }

    // ─── Combined Discounts ───────────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_LoyaltyAndCode_BothApplied()
    {
        var code = ActiveCode(type: DiscountType.Flat, value: 3m);
        var result = _sut.CalculatePrice(BaseRequest(loyaltyPoints: 200, discountCode: code));

        result.LoyaltyDiscount.Should().BeGreaterThan(0m);
        result.CodeDiscount.Should().Be(3m);
    }

    [Fact]
    public void CalculatePrice_CodeAppliedAfterLoyaltyDiscount()
    {
        // Percentage code is applied to the running total after loyalty, not the original subtotal
        var code = ActiveCode(type: DiscountType.Percentage, value: 50m);
        var withLoyalty = _sut.CalculatePrice(BaseRequest(loyaltyPoints: 300, discountCode: code));
        var noLoyalty = _sut.CalculatePrice(BaseRequest(loyaltyPoints: 0, discountCode: code));

        // When loyalty reduces the base, the 50% code applies to a smaller amount
        withLoyalty.CodeDiscount.Should().BeLessThanOrEqualTo(noLoyalty.CodeDiscount);
    }
}
