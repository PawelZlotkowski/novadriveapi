// Infrastructure/Repositories/VehicleRepository.cs
namespace NovaDrive.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task<Vehicle?> GetByVINAsync(string vin);
    Task<Vehicle?> GetByLicensePlateAsync(string licensePlate);
    Task<Vehicle> CreateAsync(Vehicle vehicle);
    Task<Vehicle> UpdateAsync(Vehicle vehicle);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Vehicle>> GetAllAsync(int page, int pageSize, bool? isActive = null);
    Task<int> GetTotalCountAsync(bool? isActive = null);
    Task<Vehicle?> GetNearestAvailableAsync(double latitude, double longitude, VehicleType? type = null);
    Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync();
    Task UpdateLocationAsync(Guid id, double latitude, double longitude);
    Task UpdateTelemetryAsync(Guid id, double latitude, double longitude, double batteryPct);
    Task AddMileageAsync(Guid id, decimal distanceKm);
}

public class VehicleRepository : IVehicleRepository
{
    private readonly NovaDriveDbContext _context;

    public VehicleRepository(NovaDriveDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
        => await _context.Vehicles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle with ID {id} not found.");

    public async Task<Vehicle?> GetByVINAsync(string vin)
        => await _context.Vehicles.FirstOrDefaultAsync(v => v.VIN == vin);

    public async Task<Vehicle?> GetByLicensePlateAsync(string licensePlate)
        => await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == licensePlate);

    public async Task<Vehicle> CreateAsync(Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    public async Task<Vehicle> UpdateAsync(Vehicle vehicle)
    {
        vehicle.UpdatedAt = DateTime.UtcNow;
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    public async Task DeleteAsync(Guid id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle is not null)
        {
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync(int page, int pageSize, bool? isActive = null)
    {
        var query = _context.Vehicles.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(v => v.IsActive == isActive.Value);

        return await query
            .OrderBy(v => v.Model)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(bool? isActive = null)
    {
        var query = _context.Vehicles.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(v => v.IsActive == isActive.Value);
        return await query.CountAsync();
    }

    public async Task<Vehicle?> GetNearestAvailableAsync(double latitude, double longitude, VehicleType? type = null)
    {
        var query = _context.Vehicles
            .Where(v => v.IsActive
                && v.CurrentLatitude.HasValue
                && v.CurrentLongitude.HasValue);

        if (type.HasValue)
            query = query.Where(v => v.VehicleType == type.Value);

        // Exclude vehicles currently on a ride
        var busyVehicleIds = await _context.Rides
            .Where(r => r.Status == RideStatus.Requested || r.Status == RideStatus.EnRoute)
            .Select(r => r.VehicleId)
            .ToListAsync();

        query = query.Where(v => !busyVehicleIds.Contains(v.Id));

        // Simple Euclidean distance approximation (fine for nearby)
        return await query
            .OrderBy(v =>
                Math.Pow(v.CurrentLatitude!.Value - latitude, 2) +
                Math.Pow(v.CurrentLongitude!.Value - longitude, 2))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync()
        => await _context.Vehicles.Where(v => v.IsActive).ToListAsync();

    public async Task UpdateLocationAsync(Guid id, double latitude, double longitude)
    {
        await _context.Vehicles
            .Where(v => v.Id == id)
            .ExecuteUpdateAsync(v => v
                .SetProperty(x => x.CurrentLatitude, latitude)
                .SetProperty(x => x.CurrentLongitude, longitude)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
    }

    public async Task UpdateTelemetryAsync(Guid id, double latitude, double longitude, double batteryPct)
    {
        await _context.Vehicles
            .Where(v => v.Id == id)
            .ExecuteUpdateAsync(v => v
                .SetProperty(x => x.CurrentLatitude, latitude)
                .SetProperty(x => x.CurrentLongitude, longitude)
                .SetProperty(x => x.CurrentBatteryPercentage, batteryPct)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
    }

    public async Task AddMileageAsync(Guid id, decimal distanceKm)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle is null) return;
        vehicle.CurrentMileage += (int)Math.Round(distanceKm);
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}