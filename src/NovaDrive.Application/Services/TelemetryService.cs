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

        // Also update vehicle's current location and battery
        await _vehicleRepo.UpdateLocationAsync(request.VehicleId, request.Latitude, request.Longitude);
    }

    public async Task RecordBatchAsync(IEnumerable<CreateTelemetryRequest> requests)
    {
        var batch = requests.Select(r => new TelemetryData
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