// IntegrationTests/Fixtures/MongoContainerFixture.cs
namespace NovaDrive.IntegrationTests.Fixtures;

using Testcontainers.MongoDb;

public class MongoContainerFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
