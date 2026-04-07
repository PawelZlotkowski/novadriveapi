// Infrastructure/MongoDb/SensorDiagnosticRepository.cs
namespace NovaDrive.Infrastructure.MongoDb;

using MongoDB.Driver;
using NovaDrive.Domain.Documents;

public interface ISensorDiagnosticRepository
{
    Task InsertAsync(SensorDiagnostic diagnostic);
    Task<IEnumerable<SensorDiagnostic>> GetByVehicleIdAsync(string vehicleId, int page, int pageSize);
    Task<long> GetCountByVehicleAsync(string vehicleId);
    Task<IEnumerable<SensorDiagnostic>> GetBySeverityAsync(string severity, int page, int pageSize);
    Task<IEnumerable<SensorDiagnostic>> GetBySensorTypeAsync(string sensorType, int page, int pageSize);
    Task<IEnumerable<SensorDiagnostic>> GetRecentCriticalAsync(int limit = 50);
}

public class SensorDiagnosticRepository : ISensorDiagnosticRepository
{
    private readonly IMongoCollection<SensorDiagnostic> _collection;

    public SensorDiagnosticRepository(MongoDbContext context)
    {
        _collection = context.SensorDiagnostics;
    }

    public async Task InsertAsync(SensorDiagnostic diagnostic)
        => await _collection.InsertOneAsync(diagnostic);

    public async Task<IEnumerable<SensorDiagnostic>> GetByVehicleIdAsync(string vehicleId, int page, int pageSize)
    {
        var filter = Builders<SensorDiagnostic>.Filter.Eq(s => s.VehicleId, vehicleId);
        return await _collection
            .Find(filter)
            .SortByDescending(s => s.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<long> GetCountByVehicleAsync(string vehicleId)
    {
        var filter = Builders<SensorDiagnostic>.Filter.Eq(s => s.VehicleId, vehicleId);
        return await _collection.CountDocumentsAsync(filter);
    }

    public async Task<IEnumerable<SensorDiagnostic>> GetBySeverityAsync(string severity, int page, int pageSize)
    {
        var filter = Builders<SensorDiagnostic>.Filter.Eq(s => s.Severity, severity);
        return await _collection
            .Find(filter)
            .SortByDescending(s => s.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<SensorDiagnostic>> GetBySensorTypeAsync(string sensorType, int page, int pageSize)
    {
        var filter = Builders<SensorDiagnostic>.Filter.Eq(s => s.SensorType, sensorType);
        return await _collection
            .Find(filter)
            .SortByDescending(s => s.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<SensorDiagnostic>> GetRecentCriticalAsync(int limit = 50)
    {
        var filter = Builders<SensorDiagnostic>.Filter.Eq(s => s.Severity, "Critical");
        return await _collection
            .Find(filter)
            .SortByDescending(s => s.Timestamp)
            .Limit(limit)
            .ToListAsync();
    }
}