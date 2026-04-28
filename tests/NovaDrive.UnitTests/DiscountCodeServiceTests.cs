// UnitTests/DiscountCodeServiceTests.cs
namespace NovaDrive.UnitTests;

using FluentAssertions;
using Moq;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public class DiscountCodeServiceTests
{
    private readonly Mock<IDiscountCodeRepository> _repoMock = new();
    private readonly DiscountCodeService _sut;

    public DiscountCodeServiceTests()
    {
        _sut = new DiscountCodeService(_repoMock.Object);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static DiscountCode MakeCode(
        string code = "SAVE10",
        DiscountType type = DiscountType.Percentage,
        decimal value = 10m,
        decimal minValue = 0m,
        bool isActive = true,
        int? maxUses = null,
        int timesUsed = 0,
        DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Type = type,
        Value = value,
        MinimumRideValue = minValue,
        IsActive = isActive,
        MaxUses = maxUses,
        TimesUsed = timesUsed,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30)
    };

    private static CreateDiscountCodeRequest MakeRequest(
        string code = "SAVE10",
        string type = "Percentage",
        decimal value = 10m,
        decimal minValue = 0m,
        int? maxUses = null) =>
        new(code, type, value, minValue, DateTime.UtcNow.AddDays(30), maxUses);

    // ─── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDiscountCodeResponse()
    {
        var request = MakeRequest();
        _repoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((DiscountCode?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<DiscountCode>()))
            .ReturnsAsync((DiscountCode dc) => dc);

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be(request.Code.ToUpper());
        result.Type.Should().Be("Percentage");
        result.Value.Should().Be(10m);
    }

    [Fact]
    public async Task CreateAsync_CodeStoredAsUppercase()
    {
        var request = MakeRequest(code: "lowercase");
        _repoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((DiscountCode?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<DiscountCode>()))
            .ReturnsAsync((DiscountCode dc) => dc);

        var result = await _sut.CreateAsync(request);

        result.Code.Should().Be("LOWERCASE");
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        var request = MakeRequest();
        _repoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync(MakeCode());

        await _sut.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{request.Code}*");
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryOnce()
    {
        var request = MakeRequest();
        _repoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((DiscountCode?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<DiscountCode>()))
            .ReturnsAsync((DiscountCode dc) => dc);

        await _sut.CreateAsync(request);

        _repoMock.Verify(r => r.CreateAsync(It.IsAny<DiscountCode>()), Times.Once);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingCode_ReturnsResponse()
    {
        var code = MakeCode();
        _repoMock.Setup(r => r.GetByIdAsync(code.Id)).ReturnsAsync(code);

        var result = await _sut.GetByIdAsync(code.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(code.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((DiscountCode?)null);

        await _sut.Invoking(s => s.GetByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── ValidateCodeAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateCodeAsync_ValidCode_ReturnsIsValidTrue()
    {
        var code = MakeCode();
        _repoMock.Setup(r => r.GetByCodeAsync(code.Code)).ReturnsAsync(code);

        var result = await _sut.ValidateCodeAsync(code.Code, 20m);

        result.IsValid.Should().BeTrue();
        result.Reason.Should().BeNull();
        result.DiscountCode.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateCodeAsync_CodeNotFound_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((DiscountCode?)null);

        var result = await _sut.ValidateCodeAsync("NOTEXIST", 20m);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateCodeAsync_InactiveCode_ReturnsFalse()
    {
        var code = MakeCode(isActive: false);
        _repoMock.Setup(r => r.GetByCodeAsync(code.Code)).ReturnsAsync(code);

        var result = await _sut.ValidateCodeAsync(code.Code, 20m);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("inactive");
    }

    [Fact]
    public async Task ValidateCodeAsync_ExpiredCode_ReturnsFalse()
    {
        var code = MakeCode(expiresAt: DateTime.UtcNow.AddDays(-1));
        _repoMock.Setup(r => r.GetByCodeAsync(code.Code)).ReturnsAsync(code);

        var result = await _sut.ValidateCodeAsync(code.Code, 20m);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("expired");
    }

    [Fact]
    public async Task ValidateCodeAsync_MaxUsesReached_ReturnsFalse()
    {
        var code = MakeCode(maxUses: 5, timesUsed: 5);
        _repoMock.Setup(r => r.GetByCodeAsync(code.Code)).ReturnsAsync(code);

        var result = await _sut.ValidateCodeAsync(code.Code, 20m);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("usage limit");
    }

    [Fact]
    public async Task ValidateCodeAsync_UsedCountBelowMax_ReturnsValid()
    {
        var code = MakeCode(maxUses: 10, timesUsed: 9);
        _repoMock.Setup(r => r.GetByCodeAsync(code.Code)).ReturnsAsync(code);

        var result = await _sut.ValidateCodeAsync(code.Code, 20m);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCodeAsync_RideValueBelowMinimum_ReturnsFalse()
    {
        var code = MakeCode(minValue: 50m);
        _repoMock.Setup(r => r.GetByCodeAsync(code.Code)).ReturnsAsync(code);

        var result = await _sut.ValidateCodeAsync(code.Code, currentRideValue: 30m);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("50");
    }

    [Fact]
    public async Task ValidateCodeAsync_RideValueExactlyAtMinimum_ReturnsValid()
    {
        var code = MakeCode(minValue: 50m);
        _repoMock.Setup(r => r.GetByCodeAsync(code.Code)).ReturnsAsync(code);

        var result = await _sut.ValidateCodeAsync(code.Code, currentRideValue: 50m);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCodeAsync_NoMaxUses_NeverReachesLimit()
    {
        var code = MakeCode(maxUses: null, timesUsed: 99999);
        _repoMock.Setup(r => r.GetByCodeAsync(code.Code)).ReturnsAsync(code);

        var result = await _sut.ValidateCodeAsync(code.Code, 20m);

        result.IsValid.Should().BeTrue();
    }
}
