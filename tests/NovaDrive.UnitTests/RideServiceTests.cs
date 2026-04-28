// UnitTests/RideServiceTests.cs
namespace NovaDrive.UnitTests;

using FluentAssertions;
using Moq;
using NovaDrive.Application.DTOs.Pricing;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public class RideServiceTests
{
    private readonly Mock<IRideRepository> _rideRepoMock = new();
    private readonly Mock<IPassengerRepository> _passengerRepoMock = new();
    private readonly Mock<IVehicleRepository> _vehicleRepoMock = new();
    private readonly Mock<IDiscountCodeRepository> _discountRepoMock = new();
    private readonly Mock<IPricingService> _pricingServiceMock = new();
    private readonly RideService _sut;

    public RideServiceTests()
    {
        _sut = new RideService(
            _rideRepoMock.Object,
            _passengerRepoMock.Object,
            _vehicleRepoMock.Object,
            _discountRepoMock.Object,
            _pricingServiceMock.Object);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Passenger MakePassenger(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        FirstName = "John",
        LastName = "Doe",
        LoyaltyPoints = 0,
        User = new User { Id = Guid.NewGuid(), Email = "john@example.com" }
    };

    private static Vehicle MakeVehicle() => new()
    {
        Id = Guid.NewGuid(),
        VIN = "12345678901234567",
        LicensePlate = "1-ABC-123",
        Model = "Tesla Model 3",
        VehicleType = VehicleType.Standard,
        YearOfManufacture = 2024,
        IsActive = true
    };

    private static Ride MakeRide(Guid passengerId, Guid? vehicleId = null, RideStatus status = RideStatus.Requested) => new()
    {
        Id = Guid.NewGuid(),
        PassengerId = passengerId,
        VehicleId = vehicleId,
        DepartureAddress = "123 Start St",
        DestinationAddress = "456 End Ave",
        DepartureLatitude = 51.5,
        DepartureLongitude = 4.4,
        DestinationLatitude = 51.6,
        DestinationLongitude = 4.5,
        Status = status,
        RequestedAt = DateTime.UtcNow,
        Passenger = MakePassenger(passengerId)
    };

    private static CreateRideRequest MakeCreateRequest(string? vehicleType = null, string? discountCode = null) =>
        new("123 Start St", 51.5, 4.4, "456 End Ave", 51.6, 4.5, vehicleType, discountCode);

    // ─── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingRide_ReturnsResponse()
    {
        var passengerId = Guid.NewGuid();
        var ride = MakeRide(passengerId);
        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(ride.Id)).ReturnsAsync(ride);

        var result = await _sut.GetByIdAsync(ride.Id);

        result.Id.Should().Be(ride.Id);
        result.PassengerId.Should().Be(passengerId);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((Ride?)null);

        await _sut.Invoking(s => s.GetByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── RequestRideAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RequestRideAsync_ValidRequest_CreatesRide()
    {
        var passenger = MakePassenger();
        var vehicle = MakeVehicle();
        var request = MakeCreateRequest();
        var createdRide = MakeRide(passenger.Id, vehicle.Id);

        _passengerRepoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);
        _rideRepoMock.Setup(r => r.GetActiveRideByPassengerAsync(passenger.Id)).ReturnsAsync((Ride?)null);
        _vehicleRepoMock.Setup(r => r.GetNearestAvailableAsync(It.IsAny<double>(), It.IsAny<double>(), null))
            .ReturnsAsync(vehicle);
        _discountRepoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((DiscountCode?)null);
        _rideRepoMock.Setup(r => r.CreateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);
        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(createdRide);

        var result = await _sut.RequestRideAsync(passenger.Id, request);

        result.Should().NotBeNull();
        result.PassengerId.Should().Be(passenger.Id);
        _rideRepoMock.Verify(r => r.CreateAsync(It.IsAny<Ride>()), Times.Once);
    }

    [Fact]
    public async Task RequestRideAsync_PassengerNotFound_ThrowsKeyNotFoundException()
    {
        _passengerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Passenger?)null);

        await _sut.Invoking(s => s.RequestRideAsync(Guid.NewGuid(), MakeCreateRequest()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RequestRideAsync_PassengerHasActiveRide_ThrowsInvalidOperationException()
    {
        var passenger = MakePassenger();
        _passengerRepoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);
        _rideRepoMock.Setup(r => r.GetActiveRideByPassengerAsync(passenger.Id))
            .ReturnsAsync(MakeRide(passenger.Id));

        await _sut.Invoking(s => s.RequestRideAsync(passenger.Id, MakeCreateRequest()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active ride*");
    }

    [Fact]
    public async Task RequestRideAsync_NoVehicleAvailable_RideCreatedWithoutVehicle()
    {
        var passenger = MakePassenger();
        var createdRide = MakeRide(passenger.Id, vehicleId: null);

        _passengerRepoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);
        _rideRepoMock.Setup(r => r.GetActiveRideByPassengerAsync(passenger.Id)).ReturnsAsync((Ride?)null);
        _vehicleRepoMock.Setup(r => r.GetNearestAvailableAsync(It.IsAny<double>(), It.IsAny<double>(), null))
            .ReturnsAsync((Vehicle?)null);
        _rideRepoMock.Setup(r => r.CreateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);
        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(createdRide);

        var result = await _sut.RequestRideAsync(passenger.Id, MakeCreateRequest());

        result.VehicleId.Should().BeNull();
    }

    [Fact]
    public async Task RequestRideAsync_WithPreferredVehicleType_PassesTypeToRepository()
    {
        var passenger = MakePassenger();
        var vehicle = MakeVehicle();
        var request = MakeCreateRequest(vehicleType: "Luxury");
        var createdRide = MakeRide(passenger.Id, vehicle.Id);

        _passengerRepoMock.Setup(r => r.GetByIdAsync(passenger.Id)).ReturnsAsync(passenger);
        _rideRepoMock.Setup(r => r.GetActiveRideByPassengerAsync(passenger.Id)).ReturnsAsync((Ride?)null);
        _vehicleRepoMock.Setup(r => r.GetNearestAvailableAsync(It.IsAny<double>(), It.IsAny<double>(), VehicleType.Luxury))
            .ReturnsAsync(vehicle);
        _rideRepoMock.Setup(r => r.CreateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);
        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(createdRide);

        await _sut.RequestRideAsync(passenger.Id, request);

        _vehicleRepoMock.Verify(r => r.GetNearestAvailableAsync(
            It.IsAny<double>(), It.IsAny<double>(), VehicleType.Luxury), Times.Once);
    }

    // ─── CancelRideAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CancelRideAsync_RequestedRide_ChangeStatusToCancelled()
    {
        var passengerId = Guid.NewGuid();
        var ride = MakeRide(passengerId, status: RideStatus.Requested);

        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(ride.Id)).ReturnsAsync(ride);
        _rideRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);

        var result = await _sut.CancelRideAsync(ride.Id, passengerId);

        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelRideAsync_EnRouteRide_CanBeCancelled()
    {
        var passengerId = Guid.NewGuid();
        var ride = MakeRide(passengerId, status: RideStatus.EnRoute);

        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(ride.Id)).ReturnsAsync(ride);
        _rideRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);

        var result = await _sut.CancelRideAsync(ride.Id, passengerId);

        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelRideAsync_CompletedRide_ThrowsInvalidOperationException()
    {
        var passengerId = Guid.NewGuid();
        var ride = MakeRide(passengerId, status: RideStatus.Completed);

        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(ride.Id)).ReturnsAsync(ride);

        await _sut.Invoking(s => s.CancelRideAsync(ride.Id, passengerId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel*");
    }

    [Fact]
    public async Task CancelRideAsync_DifferentPassenger_ThrowsInvalidOperationException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var ride = MakeRide(ownerId);

        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(ride.Id)).ReturnsAsync(ride);

        await _sut.Invoking(s => s.CancelRideAsync(ride.Id, otherId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*own rides*");
    }

    [Fact]
    public async Task CancelRideAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((Ride?)null);

        await _sut.Invoking(s => s.CancelRideAsync(Guid.NewGuid(), Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── StartRideAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task StartRideAsync_RequestedRide_ChangesStatusToEnRoute()
    {
        var passengerId = Guid.NewGuid();
        var ride = MakeRide(passengerId, status: RideStatus.Requested);

        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(ride.Id)).ReturnsAsync(ride);
        _rideRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);

        var result = await _sut.StartRideAsync(ride.Id);

        result.Status.Should().Be("EnRoute");
    }

    [Fact]
    public async Task StartRideAsync_AlreadyEnRoute_ThrowsInvalidOperationException()
    {
        var passengerId = Guid.NewGuid();
        var ride = MakeRide(passengerId, status: RideStatus.EnRoute);

        _rideRepoMock.Setup(r => r.GetByIdWithDetailsAsync(ride.Id)).ReturnsAsync(ride);

        await _sut.Invoking(s => s.StartRideAsync(ride.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── GetPassengerHistoryAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPassengerHistoryAsync_ReturnsPagedHistory()
    {
        var passengerId = Guid.NewGuid();
        var rides = new List<Ride>
        {
            MakeRide(passengerId, status: RideStatus.Completed),
            MakeRide(passengerId, status: RideStatus.Completed)
        };
        _rideRepoMock.Setup(r => r.GetByPassengerIdAsync(passengerId, 1, 10)).ReturnsAsync(rides);
        _rideRepoMock.Setup(r => r.GetTotalCountByPassengerAsync(passengerId)).ReturnsAsync(2);

        var result = await _sut.GetPassengerHistoryAsync(passengerId, 1, 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }
}
