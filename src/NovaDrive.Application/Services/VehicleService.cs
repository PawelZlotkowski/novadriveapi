// Application/Services/VehicleService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Mappings;
using NovaDrive.Domain.Enums;
using NovaDrive.Infrastructure.Repositories;

public interface IVehicleService
{
    Task<VehicleResponse> GetByIdAsync(Guid id);
    Task<VehicleResponse> CreateAsync(CreateVehicleRequest request);
    Task<VehicleResponse> UpdateAsync(Guid id, UpdateVehicleRequest request);
    Task DeleteAsync(Guid id);
    Task<PaginatedResponse<VehicleResponse>> GetAllAsync(int page, int pageSize, bool? isActive = null);
    Task<VehicleResponse?> FindNearestAsync(double lat, double lng, VehicleType? type = null);
    Task SetActiveStatusAsync(Guid id, bool isActive);
    Task<FleetStatsResponse> GetFleetStatsAsync();
}

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehicleService(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<VehicleResponse> GetByIdAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle {id} not found");
        return vehicle.ToResponse();
    }

    public async Task<VehicleResponse> CreateAsync(CreateVehicleRequest request)
    {
        // Check for duplicate VIN
        var existing = await _vehicleRepository.GetByVINAsync(request.VIN);
        if (existing is not null)
            throw new InvalidOperationException($"Vehicle with VIN {request.VIN} already exists");

        var existingPlate = await _vehicleRepository.GetByLicensePlateAsync(request.LicensePlate);
        if (existingPlate is not null)
            throw new InvalidOperationException($"Vehicle with license plate {request.LicensePlate} already exists");

        var vehicle = request.ToEntity();
        await _vehicleRepository.CreateAsync(vehicle);
        return vehicle.ToResponse();
    }

    public async Task<VehicleResponse> UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle {id} not found");

        if (request.LicensePlate is not null) vehicle.LicensePlate = request.LicensePlate;
        if (request.Model is not null) vehicle.Model = request.Model;
        if (request.VehicleType is not null) vehicle.VehicleType = Enum.Parse<VehicleType>(request.VehicleType);
        if (request.IsActive.HasValue) vehicle.IsActive = request.IsActive.Value;

        await _vehicleRepository.UpdateAsync(vehicle);
        return vehicle.ToResponse();
    }

    public async Task DeleteAsync(Guid id)
    {
        _ = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle {id} not found");
        await _vehicleRepository.DeleteAsync(id);
    }

    public async Task<PaginatedResponse<VehicleResponse>> GetAllAsync(int page, int pageSize, bool? isActive = null)
    {
        var vehicles = await _vehicleRepository.GetAllAsync(page, pageSize, isActive);
        var total = await _vehicleRepository.GetTotalCountAsync(isActive);

        return new PaginatedResponse<VehicleResponse>
        {
            Items = vehicles.Select(v => v.ToResponse()),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<VehicleResponse?> FindNearestAsync(double lat, double lng, VehicleType? type = null)
    {
        var vehicle = await _vehicleRepository.GetNearestAvailableAsync(lat, lng, type);
        return vehicle?.ToResponse();
    }

    public async Task SetActiveStatusAsync(Guid id, bool isActive)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle {id} not found");

        vehicle.IsActive = isActive;
        await _vehicleRepository.UpdateAsync(vehicle);
    }

    public async Task<FleetStatsResponse> GetFleetStatsAsync()
    {
        var allVehicles = await _vehicleRepository.GetActiveVehiclesAsync();
        var vehicleList = allVehicles.ToList();
        var totalActive = vehicleList.Count;
        var totalInactive = await _vehicleRepository.GetTotalCountAsync(false);

        return new FleetStatsResponse(
            TotalVehicles: totalActive + totalInactive,
            ActiveVehicles: totalActive,
            InactiveVehicles: totalInactive,
            StandardCount: vehicleList.Count(v => v.VehicleType == VehicleType.Standard),
            VanCount: vehicleList.Count(v => v.VehicleType == VehicleType.Van),
            LuxuryCount: vehicleList.Count(v => v.VehicleType == VehicleType.Luxury),
            AverageBatteryPercentage: vehicleList
                .Where(v => v.CurrentBatteryPercentage.HasValue)
                .Select(v => v.CurrentBatteryPercentage!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            ActiveRides: 0  // Will be populated by RideService
        );
    }
}