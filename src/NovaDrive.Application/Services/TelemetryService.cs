// Application/Services/TelemetryService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Mappings;
using NovaDrive.Domain.Documents;
using NovaDrive.Infrastructure.MongoDb;
using NovaDrive.Infrastructure.Repositories;

public interface ITelemetryService
{
    Task RecordTelemetryAsync(CreateTelemetryRequest request);
    Task RecordBatchAsync(IEnumerable<CreateTelemetryRequest> requests);
    Task<IEnumerable<TelemetryResponse>> GetVehicleTelemetryAsync(Guid vehicleId, DateTime from, DateTime to);
    Task<TelemetryResponse?> GetLatestAsync(Guid vehicleId);
    Task<IEnumerable<TelemetryResponse>> GetFleetSnapshotAsync();
}

public class TelemetryService : ITelemetryService
{
    private readonly ITelemetryRepository _telemetryRepo;
    private readonly IVehicleRepository _vehicleRepo;

    public TelemetryService(ITelemetryRepository telemetryRepo, IVehicleRepository vehicleRepo)
    {
        _telemetryRepo = telemetryRepo;
        _vehicleRepo = vehicleRepo;
    }

    public async Task RecordTelemetryAsync(CreateTelemetryRequest request)
    {
        var data = new TelemetryData
        {
            VehicleId = request.VehicleId.ToString(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SpeedKmh = request.SpeedKmh,
            BatteryPercentage = request.BatteryPercentage,
            HardwareTemperatureCelsius = request.HardwareTemperatureCelsius,
            Timestamp = DateTime.UtcNow
        };

        await _telemetryRepo.InsertAsync(data);

        // Sync latest position + battery to PostgreSQL for admin view and nearest-vehicle queries
        await _vehicleRepo.UpdateTelemetryAsync(request.VehicleId, request.Latitude, request.Longitude, request.BatteryPercentage);
    }

    public async Task RecordBatchAsync(IEnumerable<CreateTelemetryRequest> requests)
    {
        var list = requests.ToList();

        var batch = list.Select(r => new TelemetryData
        {
            VehicleId = r.VehicleId.ToString(),
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            SpeedKmh = r.SpeedKmh,
            BatteryPercentage = r.BatteryPercentage,
            HardwareTemperatureCelsius = r.HardwareTemperatureCelsius,
            Timestamp = DateTime.UtcNow
        });

        await _telemetryRepo.InsertBatchAsync(batch);

        // Sync latest position + battery to PostgreSQL sequentially (DbContext is not thread-safe)
        foreach (var r in list)
            await _vehicleRepo.UpdateTelemetryAsync(r.VehicleId, r.Latitude, r.Longitude, r.BatteryPercentage);
    }

    public async Task<IEnumerable<TelemetryResponse>> GetVehicleTelemetryAsync(Guid vehicleId, DateTime from, DateTime to)
    {
        var data = await _telemetryRepo.GetByVehicleIdAsync(vehicleId.ToString(), from, to);
        return data.Select(d => d.ToResponse());
    }

    public async Task<TelemetryResponse?> GetLatestAsync(Guid vehicleId)
    {
        var data = await _telemetryRepo.GetLatestByVehicleAsync(vehicleId.ToString());
        return data?.ToResponse();
    }

    public async Task<IEnumerable<TelemetryResponse>> GetFleetSnapshotAsync()
    {
        var data = await _telemetryRepo.GetLatestForAllVehiclesAsync();
        return data.Select(d => d.ToResponse());
    }
}