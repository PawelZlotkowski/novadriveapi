// IntegrationTests/DiscountCodeEndpointTests.cs
namespace NovaDrive.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NovaDrive.IntegrationTests.Fixtures;

public class DiscountCodeEndpointTests : IClassFixture<NovaDriveWebAppFactory>
{
    private readonly HttpClient _client;

    public DiscountCodeEndpointTests(NovaDriveWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── POST /api/admin/discount-codes ──────────────────────────────────────

    [Fact]
    public async Task CreateDiscountCode_ValidPercentage_Returns201Created()
    {
        var request = new
        {
            code = "SUMMER25",
            type = "Percentage",
            value = 25m,
            minimumRideValue = 0m,
            expiresAt = DateTime.UtcNow.AddDays(30),
            maxUses = (int?)null
        };

        var response = await _client.PostAsJsonAsync("/api/admin/discount-codes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateDiscountCode_ValidFlat_Returns201Created()
    {
        var request = new
        {
            code = "FLAT10OFF",
            type = "Flat",
            value = 10m,
            minimumRideValue = 20m,
            expiresAt = DateTime.UtcNow.AddMonths(3),
            maxUses = 500
        };

        var response = await _client.PostAsJsonAsync("/api/admin/discount-codes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateDiscountCode_DuplicateCode_ReturnsServerError()
    {
        var request = new
        {
            code = "DUPLICATE",
            type = "Flat",
            value = 5m,
            minimumRideValue = 0m,
            expiresAt = DateTime.UtcNow.AddDays(10),
            maxUses = (int?)null
        };

        await _client.PostAsJsonAsync("/api/admin/discount-codes", request);
        var second = await _client.PostAsJsonAsync("/api/admin/discount-codes", request);

        second.StatusCode.Should().NotBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateDiscountCode_LowercaseCode_Returns422()
    {
        var request = new
        {
            code = "lowercase",
            type = "Percentage",
            value = 10m,
            minimumRideValue = 0m,
            expiresAt = DateTime.UtcNow.AddDays(10),
            maxUses = (int?)null
        };

        var response = await _client.PostAsJsonAsync("/api/admin/discount-codes", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateDiscountCode_PercentageOver100_Returns422()
    {
        var request = new
        {
            code = "TOOHIGH",
            type = "Percentage",
            value = 110m,
            minimumRideValue = 0m,
            expiresAt = DateTime.UtcNow.AddDays(10),
            maxUses = (int?)null
        };

        var response = await _client.PostAsJsonAsync("/api/admin/discount-codes", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateDiscountCode_PastExpiry_Returns422()
    {
        var request = new
        {
            code = "EXPIRED",
            type = "Flat",
            value = 5m,
            minimumRideValue = 0m,
            expiresAt = DateTime.UtcNow.AddDays(-1),
            maxUses = (int?)null
        };

        var response = await _client.PostAsJsonAsync("/api/admin/discount-codes", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ─── GET /api/admin/discount-codes ───────────────────────────────────────

    [Fact]
    public async Task GetDiscountCodes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/admin/discount-codes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDiscountCodes_AfterCreating_IncludesNewCode()
    {
        var request = new
        {
            code = "LISTTEST",
            type = "Percentage",
            value = 15m,
            minimumRideValue = 0m,
            expiresAt = DateTime.UtcNow.AddDays(60),
            maxUses = (int?)null
        };

        await _client.PostAsJsonAsync("/api/admin/discount-codes", request);

        var response = await _client.GetAsync("/api/admin/discount-codes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("LISTTEST");
    }

    // ─── GET /api/admin/discount-codes/{id} ──────────────────────────────────

    [Fact]
    public async Task GetDiscountCodeById_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/admin/discount-codes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDiscountCodeById_AfterCreate_ReturnsCode()
    {
        var request = new
        {
            code = "GETBYID1",
            type = "Flat",
            value = 3m,
            minimumRideValue = 0m,
            expiresAt = DateTime.UtcNow.AddDays(14),
            maxUses = (int?)null
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/discount-codes", request);
        var created = await createResponse.Content.ReadFromJsonAsync<DiscountCodeTestResponse>();

        var getResponse = await _client.GetAsync($"/api/admin/discount-codes/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var code = await getResponse.Content.ReadFromJsonAsync<DiscountCodeTestResponse>();
        code!.Code.Should().Be("GETBYID1");
    }

    // ─── Helper records ───────────────────────────────────────────────────────

    private record DiscountCodeTestResponse(Guid Id, string Code, string Type, decimal Value, bool IsActive);
}
