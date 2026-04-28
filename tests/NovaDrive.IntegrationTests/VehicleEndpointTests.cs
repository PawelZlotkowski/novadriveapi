// IntegrationTests/VehicleEndpointTests.cs
namespace NovaDrive.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NovaDrive.IntegrationTests.Fixtures;

public class VehicleEndpointTests : IClassFixture<NovaDriveWebAppFactory>
{
    private readonly HttpClient _client;

    public VehicleEndpointTests(NovaDriveWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── POST /api/admin/vehicles ────────────────────────────────────────────

    [Fact]
    public async Task CreateVehicle_ValidRequest_Returns201Created()
    {
        var request = new
        {
            vIN = "WBA12345678901234",
            licensePlate = "1-TEST-001",
            model = "Tesla Model Y",
            vehicleType = "Standard",
            yearOfManufacture = 2024,
            latitude = (double?)null,
            longitude = (double?)null
        };

        var response = await _client.PostAsJsonAsync("/api/admin/vehicles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateVehicle_DuplicateVIN_Returns500OrConflict()
    {
        var request = new
        {
            vIN = "DUPLICATE12345678",
            licensePlate = "1-DUP-001",
            model = "Model S",
            vehicleType = "Luxury",
            yearOfManufacture = 2023
        };

        await _client.PostAsJsonAsync("/api/admin/vehicles", request);
        var secondResponse = await _client.PostAsJsonAsync("/api/admin/vehicles", request);

        // Should fail due to duplicate VIN — exception handler returns 500 or 409
        secondResponse.StatusCode.Should().NotBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateVehicle_InvalidVIN_Returns422UnprocessableEntity()
    {
        var request = new
        {
            vIN = "SHORT",          // only 5 chars
            licensePlate = "1-VAL-001",
            model = "Model X",
            vehicleType = "Standard",
            yearOfManufacture = 2024
        };

        var response = await _client.PostAsJsonAsync("/api/admin/vehicles", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateVehicle_InvalidVehicleType_Returns422UnprocessableEntity()
    {
        var request = new
        {
            vIN = "TYPTEST1234567890",
            licensePlate = "1-TYP-001",
            model = "Unknown Van",
            vehicleType = "Taxi",          // not a valid type
            yearOfManufacture = 2024
        };

        var response = await _client.PostAsJsonAsync("/api/admin/vehicles", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateVehicle_OldYear_Returns422UnprocessableEntity()
    {
        var request = new
        {
            vIN = "YRTEST1234567890X",
            licensePlate = "1-YR-001",
            model = "Old Car",
            vehicleType = "Standard",
            yearOfManufacture = 2010    // before 2015
        };

        var response = await _client.PostAsJsonAsync("/api/admin/vehicles", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ─── GET /api/admin/vehicles ─────────────────────────────────────────────

    [Fact]
    public async Task GetVehicles_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/admin/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetVehicles_ReturnsPaginatedResponse()
    {
        var response = await _client.GetAsync("/api/admin/vehicles?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<PaginatedTestResponse>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetVehicles_FilterByActive_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/admin/vehicles?isActive=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/admin/vehicles/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetVehicleById_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/admin/vehicles/{Guid.NewGuid()}");

        // Exception handler maps KeyNotFoundException → 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVehicleById_AfterCreate_ReturnsVehicle()
    {
        var vin = "GETTEST1234567890";
        var createRequest = new
        {
            vIN = vin,
            licensePlate = "1-GET-001",
            model = "Get Test Model",
            vehicleType = "Van",
            yearOfManufacture = 2023
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/vehicles", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<VehicleTestResponse>();
        created.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/api/admin/vehicles/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var vehicle = await getResponse.Content.ReadFromJsonAsync<VehicleTestResponse>();
        vehicle!.VIN.Should().Be(vin);
    }

    // ─── Helper records for deserialization ──────────────────────────────────

    private record PaginatedTestResponse(IEnumerable<object> Items, int Page, int PageSize, int TotalCount);
    private record VehicleTestResponse(Guid Id, string VIN, string LicensePlate, string Model, string VehicleType, int YearOfManufacture, bool IsActive);
}
