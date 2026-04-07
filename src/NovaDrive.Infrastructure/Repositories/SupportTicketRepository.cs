// Infrastructure/Repositories/SupportTicketRepository.cs
namespace NovaDrive.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Persistence;

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id);
    Task<SupportTicket?> GetByIdWithDetailsAsync(Guid id);
    Task<SupportTicket> CreateAsync(SupportTicket ticket);
    Task<SupportTicket> UpdateAsync(SupportTicket ticket);
    Task<IEnumerable<SupportTicket>> GetByPassengerIdAsync(Guid passengerId, int page, int pageSize);
    Task<int> GetCountByPassengerAsync(Guid passengerId);
    Task<IEnumerable<SupportTicket>> GetAllAsync(int page, int pageSize, TicketStatus? status = null, TicketPriority? priority = null);
    Task<int> GetTotalCountAsync(TicketStatus? status = null, TicketPriority? priority = null);
}

public class SupportTicketRepository : ISupportTicketRepository
{
    private readonly NovaDriveDbContext _context;

    public SupportTicketRepository(NovaDriveDbContext context)
    {
        _context = context;
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id)
        => await _context.SupportTickets.FindAsync(id);

    public async Task<SupportTicket?> GetByIdWithDetailsAsync(Guid id)
        => await _context.SupportTickets
            .Include(t => t.Passenger).ThenInclude(p => p.User)
            .Include(t => t.Ride)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<SupportTicket> CreateAsync(SupportTicket ticket)
    {
        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicket> UpdateAsync(SupportTicket ticket)
    {
        _context.SupportTickets.Update(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public async Task<IEnumerable<SupportTicket>> GetByPassengerIdAsync(Guid passengerId, int page, int pageSize)
        => await _context.SupportTickets
            .Where(t => t.PassengerId == passengerId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountByPassengerAsync(Guid passengerId)
        => await _context.SupportTickets.CountAsync(t => t.PassengerId == passengerId);

    public async Task<IEnumerable<SupportTicket>> GetAllAsync(int page, int pageSize, TicketStatus? status = null, TicketPriority? priority = null)
    {
        var query = _context.SupportTickets
            .Include(t => t.Passenger)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(TicketStatus? status = null, TicketPriority? priority = null)
    {
        var query = _context.SupportTickets.AsQueryable();
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);
        return await query.CountAsync();
    }
}