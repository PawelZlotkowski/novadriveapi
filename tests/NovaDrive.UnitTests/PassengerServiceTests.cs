// UnitTests/PassengerServiceTests.cs
namespace NovaDrive.UnitTests;

using FluentAssertions;
using Moq;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public class PassengerServiceTests
{
    private readonly Mock<IPassengerRepository> _repoMock = new();
    private readonly PassengerService _sut;

    public PassengerServiceTests()
    {
        _sut = new PassengerService(_repoMock.Object);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Passenger MakePassenger(
        Guid? id = null,
        Guid? userId = null,
        string firstName = "John",
        string lastName = "Doe",
        int loyaltyPoints = 0) => new()
    {
        Id = id ?? Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        FirstName = firstName,
        LastName = lastName,
        HomeAddress = "123 Main St",
        LoyaltyPoints = loyaltyPoints,
        PreferredPaymentMethod = PaymentMethod.CreditCard,
        User = new User { Id = userId ?? Guid.NewGuid(), Email = "john@example.com" }
    };

    private static CreatePassengerRequest MakeRequest(
        string firstName = "Jane",
        string lastName = "Smith",
        string address = "456 Oak Ave",
        string paymentMethod = "CreditCard") =>
        new(firstName, lastName, address, paymentMethod);

    // ─── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingPassenger_ReturnsResponse()
    {
        var passenger = MakePassenger();
        _repoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);

        var result = await _sut.GetByIdAsync(passenger.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(passenger.Id);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Passenger?)null);

        await _sut.Invoking(s => s.GetByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── GetByUserIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_ExistingUserId_ReturnsResponse()
    {
        var passenger = MakePassenger();
        _repoMock.Setup(r => r.GetByUserIdAsync(passenger.UserId)).ReturnsAsync(passenger);

        var result = await _sut.GetByUserIdAsync(passenger.UserId);

        result.UserId.Should().Be(passenger.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_NonExistentUserId_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync((Passenger?)null);

        await _sut.Invoking(s => s.GetByUserIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NewUser_CreatesPassenger()
    {
        var userId = Guid.NewGuid();
        var request = MakeRequest();
        var createdPassenger = MakePassenger(userId: userId, firstName: request.FirstName, lastName: request.LastName);

        _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Passenger?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Passenger>()))
            .ReturnsAsync((Passenger p) => p);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(createdPassenger);

        var result = await _sut.CreateAsync(request, userId);

        result.Should().NotBeNull();
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Passenger>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ExistingProfile_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(MakePassenger(userId: userId));

        await _sut.Invoking(s => s.CreateAsync(MakeRequest(), userId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    // ─── UpdateProfileAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileAsync_ValidUpdate_ChangesFirstAndLastName()
    {
        var passenger = MakePassenger(firstName: "Old", lastName: "Name");
        _repoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Passenger>()))
            .ReturnsAsync((Passenger p) => p);

        var updateRequest = new UpdatePassengerRequest("New", "Name", null, null);
        var result = await _sut.UpdateProfileAsync(passenger.Id, updateRequest);

        result.FirstName.Should().Be("New");
        result.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistentPassenger_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Passenger?)null);

        await _sut.Invoking(s => s.UpdateProfileAsync(Guid.NewGuid(), new UpdatePassengerRequest(null, null, null, null)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_NullFields_KeepsExistingValues()
    {
        var passenger = MakePassenger(firstName: "Unchanged", lastName: "Person");
        _repoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Passenger>()))
            .ReturnsAsync((Passenger p) => p);

        var result = await _sut.UpdateProfileAsync(passenger.Id, new UpdatePassengerRequest(null, null, null, null));

        result.FirstName.Should().Be("Unchanged");
        result.LastName.Should().Be("Person");
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesPreferredPaymentMethod()
    {
        var passenger = MakePassenger();
        _repoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Passenger>()))
            .ReturnsAsync((Passenger p) => p);

        var updateRequest = new UpdatePassengerRequest(null, null, null, "PayPal");
        var result = await _sut.UpdateProfileAsync(passenger.Id, updateRequest);

        result.PreferredPaymentMethod.Should().Be("PayPal");
    }

    // ─── GetAllAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedPassengers()
    {
        var passengers = new List<Passenger> { MakePassenger(), MakePassenger() };
        _repoMock.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync(passengers);
        _repoMock.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(2);

        var result = await _sut.GetAllAsync(1, 20);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    // ─── GetLoyaltyPointsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetLoyaltyPointsAsync_ReturnsCorrectPointCount()
    {
        var passenger = MakePassenger(loyaltyPoints: 350);
        _repoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);

        var points = await _sut.GetLoyaltyPointsAsync(passenger.Id);

        points.Should().Be(350);
    }

    [Fact]
    public async Task GetLoyaltyPointsAsync_NonExistentPassenger_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Passenger?)null);

        await _sut.Invoking(s => s.GetLoyaltyPointsAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
