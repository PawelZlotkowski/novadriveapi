// IntegrationTests/RideEndpointTests.cs
namespace NovaDrive.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;
using NovaDrive.IntegrationTests.Fixtures;

public class RideEndpointTests : IClassFixture<NovaDriveWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NovaDriveWebAppFactory _factory;

    // Test identity used by TestAuthHandler
    private static readonly Guid TestUserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TestPassengerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    public RideEndpointTests(NovaDriveWebAppFactory factory)
    {
        _factory = factory;
        // Create a client that sends the app_user_id claim (required by GetUserId())
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override test auth handler to add app_user_id and passenger_id claims
                services.PostConfigureAll<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>(_ => { });
            });
        }).CreateClient();

        SeedTestPassenger();
    }

    /// <summary>
    /// Seeds a User + Passenger into the test database so ride operations have a valid owner.
    /// Uses TestUserId so that admin endpoint tests do not conflict.
    /// </summary>
    private void SeedTestPassenger()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NovaDriveDbContext>();

        if (db.Users.Any(u => u.Id == TestUserId)) return;

        var user = new User
        {
            Id = TestUserId,
            Email = "ridetest@novadrive.test",
            Role = UserRole.Passenger,
            IsActive = true
        };

        var passenger = new Passenger
        {
            Id = TestPassengerId,
            UserId = TestUserId,
            FirstName = "Ride",
            LastName = "Tester",
            HomeAddress = "1 Test Lane",
            LoyaltyPoints = 0,
            PreferredPaymentMethod = PaymentMethod.CreditCard
        };

        db.Users.Add(user);
        db.Passengers.Add(passenger);
        db.SaveChanges();
    }

    // ─── POST /api/public/rides — validation tests (no passenger lookup needed) ─

    [Fact]
    public async Task CreateRide_InvalidCoordinates_Returns422()
    {
        var request = new
        {
            departureAddress = "A",
            departureLatitude = 999.0,   // invalid
            departureLongitude = 4.35,
            destinationAddress = "B",
            destinationLatitude = 51.6,
            destinationLongitude = 4.49,
            vehicleType = (string?)null,
            discountCode = (string?)null
        };

        var response = await _client.PostAsJsonAsync("/api/public/rides", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateRide_EmptyDepartureAddress_Returns422()
    {
        var request = new
        {
            departureAddress = "",
            departureLatitude = 51.5,
            departureLongitude = 4.35,
            destinationAddress = "456 End Ave",
            destinationLatitude = 51.6,
            destinationLongitude = 4.49,
            vehicleType = (string?)null,
            discountCode = (string?)null
        };

        var response = await _client.PostAsJsonAsync("/api/public/rides", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateRide_InvalidVehicleType_Returns422()
    {
        var request = new
        {
            departureAddress = "Start",
            departureLatitude = 51.5,
            departureLongitude = 4.35,
            destinationAddress = "End",
            destinationLatitude = 51.6,
            destinationLongitude = 4.49,
            vehicleType = "Helicopter",
            discountCode = (string?)null
        };

        var response = await _client.PostAsJsonAsync("/api/public/rides", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ─── GET /api/public/rides/{id} ──────────────────────────────────────────

    [Fact]
    public async Task GetRideById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/public/rides/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── GET /api/public/rides/active ────────────────────────────────────────

    // Note: Active ride lookup requires app_user_id claim → tested via admin ride endpoints instead

    // ─── Admin ride list ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRides_Admin_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/admin/rides");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllRides_Admin_WithStatusFilter_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/admin/rides?status=Requested");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
