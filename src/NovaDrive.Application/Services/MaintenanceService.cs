// Application/Services/MaintenanceService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Mappings;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public interface IMaintenanceService
{
    Task<MaintenanceLogResponse> GetByIdAsync(Guid id);
    Task<MaintenanceLogResponse> CreateAsync(Guid vehicleId, CreateMaintenanceLogRequest request);
    Task<MaintenanceLogResponse> UpdateAsync(Guid id, CreateMaintenanceLogRequest request);
    Task DeleteAsync(Guid id);
    Task<PaginatedResponse<MaintenanceLogResponse>> GetByVehicleAsync(Guid vehicleId, int page, int pageSize);
    Task<PaginatedResponse<MaintenanceLogResponse>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<MaintenanceLogResponse>> GetOverdueServicesAsync();
}

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceLogRepository _maintenanceRepo;
    private readonly IVehicleRepository _vehicleRepo;

    public MaintenanceService(IMaintenanceLogRepository maintenanceRepo, IVehicleRepository vehicleRepo)
    {
        _maintenanceRepo = maintenanceRepo;
        _vehicleRepo = vehicleRepo;
    }

    public async Task<MaintenanceLogResponse> GetByIdAsync(Guid id)
    {
        var log = await _maintenanceRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Maintenance log {id} not found");
        return log.ToResponse();
    }

    public async Task<MaintenanceLogResponse> CreateAsync(Guid vehicleId, CreateMaintenanceLogRequest request)
    {
        _ = await _vehicleRepo.GetByIdAsync(vehicleId)
            ?? throw new KeyNotFoundException($"Vehicle {vehicleId} not found");

        var log = new MaintenanceLog
        {
            VehicleId = vehicleId,
            ServiceDate = request.ServiceDate,
            Description = request.Description,
            TechnicianName = request.TechnicianName,
            Cost = request.Cost,
            NextServiceMileage = request.NextServiceMileage
        };

        await _maintenanceRepo.CreateAsync(log);
        var created = await _maintenanceRepo.GetByIdAsync(log.Id);
        return created!.ToResponse();
    }

    public async Task<MaintenanceLogResponse> UpdateAsync(Guid id, CreateMaintenanceLogRequest request)
    {
        var log = await _maintenanceRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Maintenance log {id} not found");

        log.ServiceDate = request.ServiceDate;
        log.Description = request.Description;
        log.TechnicianName = request.TechnicianName;
        log.Cost = request.Cost;
        log.NextServiceMileage = request.NextServiceMileage;

        await _maintenanceRepo.UpdateAsync(log);
        return log.ToResponse();
    }

    public async Task DeleteAsync(Guid id)
    {
        _ = await _maintenanceRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Maintenance log {id} not found");
        await _maintenanceRepo.DeleteAsync(id);
    }

    public async Task<PaginatedResponse<MaintenanceLogResponse>> GetByVehicleAsync(Guid vehicleId, int page, int pageSize)
    {
        var logs = await _maintenanceRepo.GetByVehicleIdAsync(vehicleId, page, pageSize);
        var total = await _maintenanceRepo.GetCountByVehicleAsync(vehicleId);

        return new PaginatedResponse<MaintenanceLogResponse>
        {
            Items = logs.Select(l => l.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<PaginatedResponse<MaintenanceLogResponse>> GetAllAsync(int page, int pageSize)
    {
        var logs = await _maintenanceRepo.GetAllAsync(page, pageSize);
        var total = await _maintenanceRepo.GetTotalCountAsync();

        return new PaginatedResponse<MaintenanceLogResponse>
        {
            Items = logs.Select(l => l.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<IEnumerable<MaintenanceLogResponse>> GetOverdueServicesAsync()
    {
        // Get all vehicles and check which are overdue
        var logs = await _maintenanceRepo.GetOverdueAsync(int.MaxValue);
        return logs.Select(l => l.ToResponse());
    }
}