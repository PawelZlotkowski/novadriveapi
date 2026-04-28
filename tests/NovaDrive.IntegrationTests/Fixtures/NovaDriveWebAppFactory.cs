// IntegrationTests/Fixtures/NovaDriveWebAppFactory.cs
namespace NovaDrive.IntegrationTests.Fixtures;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaDrive.Infrastructure.Persistence;

public class NovaDriveWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres = new();
    private readonly MongoContainerFixture _mongo = new();

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();
        await _mongo.InitializeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ── Replace PostgreSQL with test container ──────────────────────
            var pgDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<NovaDriveDbContext>));
            if (pgDescriptor is not null) services.Remove(pgDescriptor);

            services.AddDbContext<NovaDriveDbContext>(options =>
                options.UseNpgsql(_postgres.ConnectionString));

            // ── Replace JWT authentication with test scheme ─────────────────
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // Schema is created by MigrateAsync() in Program.cs startup — no EnsureCreated needed.
        });

        // Override MongoDB connection string via configuration
        builder.UseSetting("ConnectionStrings:MongoDb", _mongo.ConnectionString);

        // Override API key for vehicle system tests
        builder.UseSetting("ApiKeys:VehicleSystem", "test-api-key");
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _mongo.DisposeAsync();
        await base.DisposeAsync();
    }
}
