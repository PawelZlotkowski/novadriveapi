// UnitTests/VehicleServiceTests.cs
namespace NovaDrive.UnitTests;

using FluentAssertions;
using Moq;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _repoMock = new();
    private readonly VehicleService _sut;

    public VehicleServiceTests()
    {
        _sut = new VehicleService(_repoMock.Object);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Vehicle MakeVehicle(
        string vin = "12345678901234567",
        string plate = "1-ABC-123",
        string model = "Tesla Model Y",
        VehicleType type = VehicleType.Standard) => new()
    {
        Id = Guid.NewGuid(),
        VIN = vin,
        LicensePlate = plate,
        Model = model,
        VehicleType = type,
        YearOfManufacture = 2024,
        IsActive = true
    };

    private static CreateVehicleRequest MakeRequest(
        string vin = "12345678901234567",
        string plate = "1-ABC-123",
        string model = "Tesla Model Y",
        string type = "Standard",
        int year = 2024) =>
        new(vin, plate, model, type, year, null, null);

    // ─── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsVehicleResponse()
    {
        var vehicle = MakeVehicle();
        _repoMock.Setup(r => r.GetByIdAsync(vehicle.Id)).ReturnsAsync(vehicle);

        var result = await _sut.GetByIdAsync(vehicle.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(vehicle.Id);
        result.VIN.Should().Be(vehicle.VIN);
        result.Model.Should().Be(vehicle.Model);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        await _sut.Invoking(s => s.GetByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedVehicle()
    {
        var request = MakeRequest();
        _repoMock.Setup(r => r.GetByVINAsync(request.VIN)).ReturnsAsync((Vehicle?)null);
        _repoMock.Setup(r => r.GetByLicensePlateAsync(request.LicensePlate)).ReturnsAsync((Vehicle?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync((Vehicle v) => v);

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.VIN.Should().Be(request.VIN);
        result.LicensePlate.Should().Be(request.LicensePlate);
        result.Model.Should().Be(request.Model);
    }

    [Fact]
    public async Task CreateAsync_DuplicateVIN_ThrowsInvalidOperationException()
    {
        var request = MakeRequest();
        _repoMock.Setup(r => r.GetByVINAsync(request.VIN)).ReturnsAsync(MakeVehicle());

        await _sut.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{request.VIN}*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateLicensePlate_ThrowsInvalidOperationException()
    {
        var request = MakeRequest();
        _repoMock.Setup(r => r.GetByVINAsync(request.VIN)).ReturnsAsync((Vehicle?)null);
        _repoMock.Setup(r => r.GetByLicensePlateAsync(request.LicensePlate)).ReturnsAsync(MakeVehicle());

        await _sut.Invoking(s => s.CreateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{request.LicensePlate}*");
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryOnce()
    {
        var request = MakeRequest();
        _repoMock.Setup(r => r.GetByVINAsync(request.VIN)).ReturnsAsync((Vehicle?)null);
        _repoMock.Setup(r => r.GetByLicensePlateAsync(request.LicensePlate)).ReturnsAsync((Vehicle?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync((Vehicle v) => v);

        await _sut.CreateAsync(request);

        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Vehicle>()), Times.Once);
    }

    // ─── UpdateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingVehicle_UpdatesFields()
    {
        var vehicle = MakeVehicle();
        _repoMock.Setup(r => r.GetByIdAsync(vehicle.Id)).ReturnsAsync(vehicle);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync((Vehicle v) => v);

        var updateRequest = new UpdateVehicleRequest("9-XYZ-999", "New Model S", "Luxury", true);
        var result = await _sut.UpdateAsync(vehicle.Id, updateRequest);

        result.Model.Should().Be("New Model S");
        result.LicensePlate.Should().Be("9-XYZ-999");
        result.VehicleType.Should().Be("Luxury");
    }

    [Fact]
    public async Task UpdateAsync_NonExistentVehicle_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        await _sut.Invoking(s => s.UpdateAsync(Guid.NewGuid(), new UpdateVehicleRequest(null, null, null, null)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_NullFields_DoesNotOverwriteExistingValues()
    {
        var vehicle = MakeVehicle(model: "Original Model", plate: "ORIG-000");
        _repoMock.Setup(r => r.GetByIdAsync(vehicle.Id)).ReturnsAsync(vehicle);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync((Vehicle v) => v);

        var updateRequest = new UpdateVehicleRequest(null, null, null, null);
        var result = await _sut.UpdateAsync(vehicle.Id, updateRequest);

        result.Model.Should().Be("Original Model");
        result.LicensePlate.Should().Be("ORIG-000");
    }

    // ─── DeleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingVehicle_CallsRepositoryDelete()
    {
        var vehicle = MakeVehicle();
        _repoMock.Setup(r => r.GetByIdAsync(vehicle.Id)).ReturnsAsync(vehicle);
        _repoMock.Setup(r => r.DeleteAsync(vehicle.Id)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(vehicle.Id);

        _repoMock.Verify(r => r.DeleteAsync(vehicle.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentVehicle_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        await _sut.Invoking(s => s.DeleteAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─── GetAllAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResponse()
    {
        var vehicles = new List<Vehicle> { MakeVehicle(), MakeVehicle(vin: "99999999999999999", plate: "2-DEF-456") };
        _repoMock.Setup(r => r.GetAllAsync(1, 20, null)).ReturnsAsync(vehicles);
        _repoMock.Setup(r => r.GetTotalCountAsync(null)).ReturnsAsync(2);

        var result = await _sut.GetAllAsync(1, 20);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByActiveStatus()
    {
        var activeVehicles = new List<Vehicle> { MakeVehicle() };
        _repoMock.Setup(r => r.GetAllAsync(1, 10, true)).ReturnsAsync(activeVehicles);
        _repoMock.Setup(r => r.GetTotalCountAsync(true)).ReturnsAsync(1);

        var result = await _sut.GetAllAsync(1, 10, isActive: true);

        result.Items.Should().HaveCount(1);
        _repoMock.Verify(r => r.GetAllAsync(1, 10, true), Times.Once);
    }

    // ─── SetActiveStatusAsync ────────────────────────────────────────────────

    [Fact]
    public async Task SetActiveStatusAsync_TogglesStatus_AndCallsUpdate()
    {
        var vehicle = MakeVehicle();
        vehicle.IsActive = true;
        _repoMock.Setup(r => r.GetByIdAsync(vehicle.Id)).ReturnsAsync(vehicle);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
            .ReturnsAsync((Vehicle v) => v);

        await _sut.SetActiveStatusAsync(vehicle.Id, false);

        _repoMock.Verify(r => r.UpdateAsync(It.Is<Vehicle>(v => v.IsActive == false)), Times.Once);
    }

    [Fact]
    public async Task SetActiveStatusAsync_NonExistentVehicle_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        await _sut.Invoking(s => s.SetActiveStatusAsync(Guid.NewGuid(), true))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
