// Infrastructure/MongoDb/MongoDbContext.cs
namespace NovaDrive.Infrastructure.MongoDb;

using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using NovaDrive.Domain.Documents;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDb")
            ?? throw new ArgumentNullException("MongoDb connection string is missing");

        var databaseName = configuration["MongoDb:DatabaseName"] ?? "novadrive";

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);

        CreateIndexes();
    }

    public IMongoCollection<TelemetryData> Telemetry
        => _database.GetCollection<TelemetryData>("telemetry");

    public IMongoCollection<SensorDiagnostic> SensorDiagnostics
        => _database.GetCollection<SensorDiagnostic>("sensor_diagnostics");

    private void CreateIndexes()
    {
        // Telemetry: index on VehicleId + Timestamp (descending)
        var telemetryIndexBuilder = Builders<TelemetryData>.IndexKeys;
        var telemetryIndex = telemetryIndexBuilder
            .Ascending(t => t.VehicleId)
            .Descending(t => t.Timestamp);

        Telemetry.Indexes.CreateOne(
            new CreateIndexModel<TelemetryData>(telemetryIndex));

        // Optional: TTL index to auto-delete old telemetry after 90 days
        var ttlIndex = telemetryIndexBuilder.Ascending(t => t.Timestamp);
        Telemetry.Indexes.CreateOne(
            new CreateIndexModel<TelemetryData>(
                ttlIndex,
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(90) }));

        // Sensor diagnostics: index on VehicleId + Timestamp
        var sensorIndexBuilder = Builders<SensorDiagnostic>.IndexKeys;
        var sensorIndex = sensorIndexBuilder
            .Ascending(s => s.VehicleId)
            .Descending(s => s.Timestamp);

        SensorDiagnostics.Indexes.CreateOne(
            new CreateIndexModel<SensorDiagnostic>(sensorIndex));

        // Sensor diagnostics: index on Severity for critical alerts
        var severityIndex = sensorIndexBuilder
            .Ascending(s => s.Severity)
            .Descending(s => s.Timestamp);

        SensorDiagnostics.Indexes.CreateOne(
            new CreateIndexModel<SensorDiagnostic>(severityIndex));
    }
}