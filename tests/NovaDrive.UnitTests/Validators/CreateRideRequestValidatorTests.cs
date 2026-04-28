// UnitTests/Validators/CreateRideRequestValidatorTests.cs
namespace NovaDrive.UnitTests.Validators;

using FluentAssertions;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Validators;

public class CreateRideRequestValidatorTests
{
    private readonly CreateRideRequestValidator _validator = new();

    private static CreateRideRequest Valid() =>
        new("123 Start Street", 51.5074, 4.3528, "456 End Avenue", 51.6033, 4.4900, null, null);

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidRequest_WithVehicleType_PassesValidation()
    {
        var request = Valid() with { VehicleType = "Van" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    // ─── Addresses ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EmptyDepartureAddress_FailsValidation()
    {
        var request = Valid() with { DepartureAddress = "" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DepartureAddress");
    }

    [Fact]
    public async Task EmptyDestinationAddress_FailsValidation()
    {
        var request = Valid() with { DestinationAddress = "" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinationAddress");
    }

    [Fact]
    public async Task TooLongDepartureAddress_FailsValidation()
    {
        var request = Valid() with { DepartureAddress = new string('A', 501) };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    // ─── Coordinates ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    [InlineData(90.0)]
    public async Task ValidDepartureLat_PassesValidation(double lat)
    {
        var request = Valid() with { DepartureLatitude = lat };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "DepartureLatitude");
    }

    [Theory]
    [InlineData(-91.0)]
    [InlineData(91.0)]
    public async Task OutOfRangeDepartureLat_FailsValidation(double lat)
    {
        var request = Valid() with { DepartureLatitude = lat };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DepartureLatitude");
    }

    [Theory]
    [InlineData(-180.0)]
    [InlineData(0.0)]
    [InlineData(180.0)]
    public async Task ValidDepartureLon_PassesValidation(double lon)
    {
        var request = Valid() with { DepartureLongitude = lon };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "DepartureLongitude");
    }

    [Theory]
    [InlineData(-181.0)]
    [InlineData(181.0)]
    public async Task OutOfRangeDepartureLon_FailsValidation(double lon)
    {
        var request = Valid() with { DepartureLongitude = lon };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DepartureLongitude");
    }

    [Theory]
    [InlineData(-91.0)]
    [InlineData(91.0)]
    public async Task OutOfRangeDestinationLat_FailsValidation(double lat)
    {
        var request = Valid() with { DestinationLatitude = lat };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinationLatitude");
    }

    [Theory]
    [InlineData(-181.0)]
    [InlineData(181.0)]
    public async Task OutOfRangeDestinationLon_FailsValidation(double lon)
    {
        var request = Valid() with { DestinationLongitude = lon };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinationLongitude");
    }

    // ─── VehicleType ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Standard")]
    [InlineData("Van")]
    [InlineData("Luxury")]
    public async Task ValidVehicleType_PassesValidation(string type)
    {
        var request = Valid() with { VehicleType = type };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "VehicleType");
    }

    [Fact]
    public async Task NullVehicleType_PassesValidation()
    {
        // Vehicle type is optional
        var request = Valid() with { VehicleType = null };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "VehicleType");
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("Taxi")]
    [InlineData("Bus")]
    public async Task InvalidVehicleType_FailsValidation(string type)
    {
        var request = Valid() with { VehicleType = type };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VehicleType");
    }
}
