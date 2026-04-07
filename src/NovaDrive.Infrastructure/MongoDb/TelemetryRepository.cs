// Infrastructure/MongoDb/TelemetryRepository.cs
namespace NovaDrive.Infrastructure.MongoDb;

using MongoDB.Driver;
using NovaDrive.Domain.Documents;

public interface ITelemetryRepository
{
    Task InsertAsync(TelemetryData data);
    Task InsertBatchAsync(IEnumerable<TelemetryData> batch);
    Task<IEnumerable<TelemetryData>> GetByVehicleIdAsync(string vehicleId, DateTime from, DateTime to, int limit = 100);
    Task<TelemetryData?> GetLatestByVehicleAsync(string vehicleId);
    Task<IEnumerable<TelemetryData>> GetLatestForAllVehiclesAsync();
    Task<long> GetCountByVehicleAsync(string vehicleId);
    Task DeleteOlderThanAsync(DateTime cutoff);
}

public class TelemetryRepository : ITelemetryRepository
{
    private readonly IMongoCollection<TelemetryData> _collection;

    public TelemetryRepository(MongoDbContext context)
    {
        _collection = context.Telemetry;
    }

    public async Task InsertAsync(TelemetryData data)
        => await _collection.InsertOneAsync(data);

    public async Task InsertBatchAsync(IEnumerable<TelemetryData> batch)
        => await _collection.InsertManyAsync(batch);

    public async Task<IEnumerable<TelemetryData>> GetByVehicleIdAsync(
        string vehicleId, DateTime from, DateTime to, int limit = 100)
    {
        var filter = Builders<TelemetryData>.Filter.And(
            Builders<TelemetryData>.Filter.Eq(t => t.VehicleId, vehicleId),
            Builders<TelemetryData>.Filter.Gte(t => t.Timestamp, from),
            Builders<TelemetryData>.Filter.Lte(t => t.Timestamp, to));

        return await _collection
            .Find(filter)
            .SortByDescending(t => t.Timestamp)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<TelemetryData?> GetLatestByVehicleAsync(string vehicleId)
    {
        var filter = Builders<TelemetryData>.Filter.Eq(t => t.VehicleId, vehicleId);
        return await _collection
            .Find(filter)
            .SortByDescending(t => t.Timestamp)
            .Limit(1)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TelemetryData>> GetLatestForAllVehiclesAsync()
    {
        // Aggregation: group by VehicleId, take the latest per vehicle
        var pipeline = _collection.Aggregate()
            .SortByDescending(t => t.Timestamp)
            .Group(t => t.VehicleId, g => new TelemetryData
            {
                Id = g.First().Id,
                VehicleId = g.Key,
                Latitude = g.First().Latitude,
                Longitude = g.First().Longitude,
                SpeedKmh = g.First().SpeedKmh,
                BatteryPercentage = g.First().BatteryPercentage,
                HardwareTemperatureCelsius = g.First().HardwareTemperatureCelsius,
                Timestamp = g.First().Timestamp
            });

        return await pipeline.ToListAsync();
    }

    public async Task<long> GetCountByVehicleAsync(string vehicleId)
    {
        var filter = Builders<TelemetryData>.Filter.Eq(t => t.VehicleId, vehicleId);
        return await _collection.CountDocumentsAsync(filter);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff)
    {
        var filter = Builders<TelemetryData>.Filter.Lt(t => t.Timestamp, cutoff);
        await _collection.DeleteManyAsync(filter);
    }
}