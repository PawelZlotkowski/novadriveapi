// Infrastructure/Repositories/MaintenanceLogRepository.cs
namespace NovaDrive.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;

public interface IMaintenanceLogRepository
{
    Task<MaintenanceLog?> GetByIdAsync(Guid id);
    Task<MaintenanceLog> CreateAsync(MaintenanceLog log);
    Task<MaintenanceLog> UpdateAsync(MaintenanceLog log);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<MaintenanceLog>> GetByVehicleIdAsync(Guid vehicleId, int page, int pageSize);
    Task<int> GetCountByVehicleAsync(Guid vehicleId);
    Task<IEnumerable<MaintenanceLog>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<IEnumerable<MaintenanceLog>> GetOverdueAsync(int currentMileageThreshold);
}

public class MaintenanceLogRepository : IMaintenanceLogRepository
{
    private readonly NovaDriveDbContext _context;

    public MaintenanceLogRepository(NovaDriveDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenanceLog?> GetByIdAsync(Guid id)
        => await _context.MaintenanceLogs
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<MaintenanceLog> CreateAsync(MaintenanceLog log)
    {
        _context.MaintenanceLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<MaintenanceLog> UpdateAsync(MaintenanceLog log)
    {
        _context.MaintenanceLogs.Update(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task DeleteAsync(Guid id)
    {
        var log = await _context.MaintenanceLogs.FindAsync(id);
        if (log is not null)
        {
            _context.MaintenanceLogs.Remove(log);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<MaintenanceLog>> GetByVehicleIdAsync(Guid vehicleId, int page, int pageSize)
        => await _context.MaintenanceLogs
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.ServiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountByVehicleAsync(Guid vehicleId)
        => await _context.MaintenanceLogs.CountAsync(m => m.VehicleId == vehicleId);

    public async Task<IEnumerable<MaintenanceLog>> GetAllAsync(int page, int pageSize)
        => await _context.MaintenanceLogs
            .Include(m => m.Vehicle)
            .OrderByDescending(m => m.ServiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetTotalCountAsync()
        => await _context.MaintenanceLogs.CountAsync();

    public async Task<IEnumerable<MaintenanceLog>> GetOverdueAsync(int currentMileageThreshold)
        => await _context.MaintenanceLogs
            .Include(m => m.Vehicle)
            .Where(m => m.NextServiceMileage <= currentMileageThreshold)
            .OrderBy(m => m.NextServiceMileage)
            .ToListAsync();
}