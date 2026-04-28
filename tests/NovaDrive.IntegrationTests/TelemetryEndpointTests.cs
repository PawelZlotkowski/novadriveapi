// IntegrationTests/TelemetryEndpointTests.cs
namespace NovaDrive.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NovaDrive.IntegrationTests.Fixtures;

/// <summary>
/// Tests for /api/vehicle/telemetry which uses API-key middleware (not JWT).
/// The factory sets ApiKeys:VehicleSystem = "test-api-key".
/// </summary>
public class TelemetryEndpointTests : IClassFixture<NovaDriveWebAppFactory>
{
    private const string TestApiKey = "test-api-key";
    private readonly HttpClient _client;

    public TelemetryEndpointTests(NovaDriveWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object ValidTelemetryPayload(Guid? vehicleId = null) => new
    {
        vehicleId = vehicleId ?? Guid.NewGuid(),
        latitude = 51.5074,
        longitude = 4.3528,
        speedKmh = 45.0,
        batteryPercentage = 82.5,
        hardwareTemperatureCelsius = 38.2
    };

    // ─── POST /api/vehicle/telemetry ─────────────────────────────────────────

    [Fact]
    public async Task PostTelemetry_WithValidApiKey_Returns202Accepted()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/vehicle/telemetry");
        request.Headers.Add("X-Api-Key", TestApiKey);
        request.Content = JsonContent.Create(ValidTelemetryPayload());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostTelemetry_WithoutApiKey_Returns401()
    {
        // No X-Api-Key header
        var response = await _client.PostAsJsonAsync("/api/vehicle/telemetry", ValidTelemetryPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostTelemetry_WithWrongApiKey_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/vehicle/telemetry");
        request.Headers.Add("X-Api-Key", "totally-wrong-key");
        request.Content = JsonContent.Create(ValidTelemetryPayload());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostTelemetry_InvalidLatitude_Returns422()
    {
        var invalidPayload = new
        {
            vehicleId = Guid.NewGuid(),
            latitude = 999.0,        // invalid
            longitude = 4.35,
            speedKmh = 30.0,
            batteryPercentage = 70.0,
            hardwareTemperatureCelsius = 35.0
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/vehicle/telemetry");
        request.Headers.Add("X-Api-Key", TestApiKey);
        request.Content = JsonContent.Create(invalidPayload);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ─── POST /api/vehicle/telemetry/batch ───────────────────────────────────

    [Fact]
    public async Task PostTelemetryBatch_WithValidApiKey_Returns202Accepted()
    {
        var batch = new[]
        {
            ValidTelemetryPayload(),
            ValidTelemetryPayload()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/vehicle/telemetry/batch");
        request.Headers.Add("X-Api-Key", TestApiKey);
        request.Content = JsonContent.Create(batch);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostTelemetryBatch_WithoutApiKey_Returns401()
    {
        var batch = new[] { ValidTelemetryPayload() };
        var response = await _client.PostAsJsonAsync("/api/vehicle/telemetry/batch", batch);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
