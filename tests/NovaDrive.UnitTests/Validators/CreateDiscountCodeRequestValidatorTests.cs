// UnitTests/Validators/CreateDiscountCodeRequestValidatorTests.cs
namespace NovaDrive.UnitTests.Validators;

using FluentAssertions;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Validators;

public class CreateDiscountCodeRequestValidatorTests
{
    private readonly CreateDiscountCodeRequestValidator _validator = new();

    private static CreateDiscountCodeRequest Valid() =>
        new("SUMMER20", "Percentage", 20m, 0m, DateTime.UtcNow.AddDays(30), null);

    [Fact]
    public async Task ValidPercentageRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidFlatRequest_PassesValidation()
    {
        var request = new CreateDiscountCodeRequest("FLAT5", "Flat", 5m, 10m, DateTime.UtcNow.AddDays(7), 100);
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    // ─── Code ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmptyCode_FailsValidation()
    {
        var request = Valid() with { Code = "" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task LowercaseCode_FailsValidation()
    {
        var request = Valid() with { Code = "summer20" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorMessage.Contains("uppercase"));
    }

    [Fact]
    public async Task CodeWithSpecialChars_FailsValidation()
    {
        var request = Valid() with { Code = "SAVE-10" };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task CodeTooLong_FailsValidation()
    {
        var request = Valid() with { Code = new string('A', 51) };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    // ─── Type ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Percentage")]
    [InlineData("Flat")]
    public async Task ValidType_PassesValidation(string type)
    {
        var request = Valid() with { Type = type };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "Type");
    }

    [Theory]
    [InlineData("percentage")]
    [InlineData("fixed")]
    [InlineData("")]
    public async Task InvalidType_FailsValidation(string type)
    {
        var request = Valid() with { Type = type };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    // ─── Value ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ZeroValue_FailsValidation()
    {
        var request = Valid() with { Value = 0m };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }

    [Fact]
    public async Task NegativeValue_FailsValidation()
    {
        var request = Valid() with { Value = -5m };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PercentageOver100_FailsValidation()
    {
        var request = Valid() with { Type = "Percentage", Value = 101m };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value" && e.ErrorMessage.Contains("100%"));
    }

    [Fact]
    public async Task PercentageExactly100_PassesValidation()
    {
        var request = Valid() with { Type = "Percentage", Value = 100m };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "Value" && e.ErrorMessage.Contains("100%"));
    }

    [Fact]
    public async Task FlatOver100_PassesValidation()
    {
        // Flat discount is not capped at 100
        var request = Valid() with { Type = "Flat", Value = 500m };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    // ─── MinimumRideValue ─────────────────────────────────────────────────────

    [Fact]
    public async Task NegativeMinimumRideValue_FailsValidation()
    {
        var request = Valid() with { MinimumRideValue = -1m };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinimumRideValue");
    }

    [Fact]
    public async Task ZeroMinimumRideValue_PassesValidation()
    {
        var request = Valid() with { MinimumRideValue = 0m };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "MinimumRideValue");
    }

    // ─── ExpiresAt ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PastExpiryDate_FailsValidation()
    {
        var request = Valid() with { ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiresAt");
    }

    [Fact]
    public async Task FutureExpiryDate_PassesValidation()
    {
        var request = Valid() with { ExpiresAt = DateTime.UtcNow.AddYears(1) };
        var result = await _validator.ValidateAsync(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "ExpiresAt");
    }
}
