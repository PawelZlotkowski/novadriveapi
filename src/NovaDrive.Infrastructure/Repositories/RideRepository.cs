// Infrastructure/Repositories/RideRepository.cs
namespace NovaDrive.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;

public interface IRideRepository
{
    Task<Ride?> GetByIdAsync(Guid id);
    Task<Ride?> GetByIdWithDetailsAsync(Guid id);
    Task<Ride> CreateAsync(Ride ride);
    Task<Ride> UpdateAsync(Ride ride);
    Task<IEnumerable<Ride>> GetByPassengerIdAsync(Guid passengerId, int page, int pageSize);
    Task<int> GetTotalCountByPassengerAsync(Guid passengerId);
    Task<IEnumerable<Ride>> GetAllAsync(int page, int pageSize, RideStatus? status = null);
    Task<int> GetTotalCountAsync(RideStatus? status = null);
    Task<IEnumerable<Ride>> GetActiveRidesAsync();
    Task<Ride?> GetActiveRideByPassengerAsync(Guid passengerId);
}

public class RideRepository : IRideRepository
{
    private readonly NovaDriveDbContext _context;

    public RideRepository(NovaDriveDbContext context)
    {
        _context = context;
    }

    public async Task<Ride?> GetByIdAsync(Guid id)
        => await _context.Rides.FindAsync(id)
            ?? throw new KeyNotFoundException($"Ride with ID {id} not found.");

    public async Task<Ride?> GetByIdWithDetailsAsync(Guid id)
        => await _context.Rides
            .Include(r => r.Passenger).ThenInclude(p => p.User)
            .Include(r => r.Vehicle)
            .Include(r => r.Payment)
            .Include(r => r.DiscountCode)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Ride> CreateAsync(Ride ride)
    {
        _context.Rides.Add(ride);
        await _context.SaveChangesAsync();
        return ride;
    }

    public async Task<Ride> UpdateAsync(Ride ride)
    {
        _context.Rides.Update(ride);
        await _context.SaveChangesAsync();
        return ride;
    }

    public async Task<IEnumerable<Ride>> GetByPassengerIdAsync(Guid passengerId, int page, int pageSize)
        => await _context.Rides
            .Include(r => r.Vehicle)
            .Include(r => r.Payment)
            .Where(r => r.PassengerId == passengerId)
            .OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetTotalCountByPassengerAsync(Guid passengerId)
        => await _context.Rides.CountAsync(r => r.PassengerId == passengerId);

    public async Task<IEnumerable<Ride>> GetAllAsync(int page, int pageSize, RideStatus? status = null)
    {
        var query = _context.Rides
            .Include(r => r.Passenger)
            .Include(r => r.Vehicle)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(RideStatus? status = null)
    {
        var query = _context.Rides.AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        return await query.CountAsync();
    }

    public async Task<IEnumerable<Ride>> GetActiveRidesAsync()
        => await _context.Rides
            .Include(r => r.Passenger)
            .Include(r => r.Vehicle)
            .Where(r => r.Status == RideStatus.Requested || r.Status == RideStatus.EnRoute)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

    public async Task<Ride?> GetActiveRideByPassengerAsync(Guid passengerId)
        => await _context.Rides
            .Include(r => r.Vehicle)
            .Where(r => r.PassengerId == passengerId
                && (r.Status == RideStatus.Requested || r.Status == RideStatus.EnRoute))
            .FirstOrDefaultAsync();
}