// UnitTests/Validators/CreateVehicleRequestValidatorTests.cs
namespace NovaDrive.UnitTests.Validators;

using FluentAssertions;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Validators;

public class CreateVehicleRequestValidatorTests
{
    private readonly CreateVehicleRequestValidator _validator = new();

    private static CreateVehicleRequest Valid() =>
        new("12345678901234567", "1-ABC-123", "Tesla Model Y", "Standard", 2024, null, null);

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(Valid());

        result.IsValid.Should().BeTrue();
    }

    // ─── VIN ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmptyVIN_FailsValidation()
    {
        var request = Valid() with { VIN = "" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VIN");
    }

    [Theory]
    [InlineData("1234567890")]       // too short (10)
    [InlineData("123456789012345678")] // too long (18)
    public async Task IncorrectVINLength_FailsValidation(string vin)
    {
        var request = Valid() with { VIN = vin };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VIN" && e.ErrorMessage.Contains("17"));
    }

    [Fact]
    public async Task VINExactly17Chars_PassesValidation()
    {
        var request = Valid() with { VIN = "ABCDEFGHJKLMNPRS1" };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "VIN");
    }

    // ─── LicensePlate ────────────────────────────────────────────────────────

    [Fact]
    public async Task EmptyLicensePlate_FailsValidation()
    {
        var request = Valid() with { LicensePlate = "" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LicensePlate");
    }

    [Fact]
    public async Task LicensePlateTooLong_FailsValidation()
    {
        var request = Valid() with { LicensePlate = new string('A', 21) };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    // ─── Model ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmptyModel_FailsValidation()
    {
        var request = Valid() with { Model = "" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Model");
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

    [Theory]
    [InlineData("standard")]   // lowercase
    [InlineData("STANDARD")]   // allcaps
    [InlineData("Taxi")]
    [InlineData("")]
    public async Task InvalidVehicleType_FailsValidation(string type)
    {
        var request = Valid() with { VehicleType = type };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VehicleType");
    }

    // ─── YearOfManufacture ────────────────────────────────────────────────────

    [Fact]
    public async Task Year2015_PassesValidation()
    {
        var request = Valid() with { YearOfManufacture = 2015 };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "YearOfManufacture");
    }

    [Fact]
    public async Task YearBefore2015_FailsValidation()
    {
        var request = Valid() with { YearOfManufacture = 2014 };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "YearOfManufacture");
    }

    [Fact]
    public async Task YearNextYear_PassesValidation()
    {
        var request = Valid() with { YearOfManufacture = DateTime.UtcNow.Year + 1 };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "YearOfManufacture");
    }

    [Fact]
    public async Task YearTwoYearsAhead_FailsValidation()
    {
        var request = Valid() with { YearOfManufacture = DateTime.UtcNow.Year + 2 };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "YearOfManufacture");
    }
}
