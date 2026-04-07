// Infrastructure/Repositories/PaymentRepository.cs
namespace NovaDrive.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByRideIdAsync(Guid rideId);
    Task<Payment?> GetByTransactionReferenceAsync(string reference);
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
    Task<IEnumerable<Payment>> GetByPassengerIdAsync(Guid passengerId, int page, int pageSize);
    Task<int> GetCountByPassengerAsync(Guid passengerId);
    Task<IEnumerable<Payment>> GetAllAsync(int page, int pageSize, PaymentStatus? status = null);
    Task<int> GetTotalCountAsync(PaymentStatus? status = null);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly NovaDriveDbContext _context;

    public PaymentRepository(NovaDriveDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
        => await _context.Payments
            .Include(p => p.Ride)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Payment?> GetByRideIdAsync(Guid rideId)
        => await _context.Payments.FirstOrDefaultAsync(p => p.RideId == rideId);

    public async Task<Payment?> GetByTransactionReferenceAsync(string reference)
        => await _context.Payments.FirstOrDefaultAsync(p => p.TransactionReference == reference);

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<IEnumerable<Payment>> GetByPassengerIdAsync(Guid passengerId, int page, int pageSize)
        => await _context.Payments
            .Include(p => p.Ride)
            .Where(p => p.Ride.PassengerId == passengerId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountByPassengerAsync(Guid passengerId)
        => await _context.Payments.CountAsync(p => p.Ride.PassengerId == passengerId);

    public async Task<IEnumerable<Payment>> GetAllAsync(int page, int pageSize, PaymentStatus? status = null)
    {
        var query = _context.Payments.Include(p => p.Ride).AsQueryable();
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(PaymentStatus? status = null)
    {
        var query = _context.Payments.AsQueryable();
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);
        return await query.CountAsync();
    }
}