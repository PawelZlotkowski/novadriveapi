namespace NovaDrive.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;

public interface IPassengerRepository
{
    Task<Passenger?> GetByIdAsync(Guid id);
    Task<Passenger?> GetByUserIdAsync(Guid userId);
    Task<Passenger> CreateAsync(Passenger passenger);
    Task<Passenger> UpdateAsync(Passenger passenger);
    Task<IEnumerable<Passenger>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task AddLoyaltyPointsAsync(Guid passengerId, int points);
    Task DeductLoyaltyPointsAsync(Guid passengerId, int points);
    
}

public class PassengerRepository : IPassengerRepository
{
    private readonly NovaDriveDbContext _context;

    public PassengerRepository(NovaDriveDbContext context)
    {
        _context = context;
    }

    public async Task<Passenger?> GetByIdAsync(Guid id)
        => await _context.Passengers
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Passenger?> GetByUserIdAsync(Guid userId)
        => await _context.Passengers
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task<Passenger> CreateAsync(Passenger passenger)
    {
        _context.Passengers.Add(passenger);
        await _context.SaveChangesAsync();
        return passenger;
    }

    public async Task<Passenger> UpdateAsync(Passenger passenger)
    {
        passenger.UpdatedAt = DateTime.UtcNow;
        _context.Passengers.Update(passenger);
        await _context.SaveChangesAsync();
        return passenger;
    }

    public async Task<IEnumerable<Passenger>> GetAllAsync(int page, int pageSize)
        => await _context.Passengers
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetTotalCountAsync()
        => await _context.Passengers.CountAsync();

    public async Task AddLoyaltyPointsAsync(Guid passengerId, int points)
    {
        await _context.Passengers
            .Where(p => p.Id == passengerId)
            .ExecuteUpdateAsync(p => p.SetProperty(
                x => x.LoyaltyPoints,
                x => x.LoyaltyPoints + points));
    }

    public async Task DeductLoyaltyPointsAsync(Guid passengerId, int points)
    {
        await _context.Passengers
            .Where(p => p.Id == passengerId)
            .ExecuteUpdateAsync(p => p.SetProperty(
                x => x.LoyaltyPoints,
                x => x.LoyaltyPoints - points));
    }
}