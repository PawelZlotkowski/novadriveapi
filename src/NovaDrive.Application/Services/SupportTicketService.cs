// Application/Services/SupportTicketService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Mappings;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public interface ISupportTicketService
{
    Task<SupportTicketResponse> CreateAsync(Guid passengerId, CreateSupportTicketRequest request);
    Task<SupportTicketResponse> GetByIdAsync(Guid id);
    Task<PaginatedResponse<SupportTicketResponse>> GetByPassengerAsync(Guid passengerId, int page, int pageSize);
    Task<PaginatedResponse<SupportTicketResponse>> GetAllAsync(int page, int pageSize, TicketStatus? status = null, TicketPriority? priority = null);
    Task<SupportTicketResponse> UpdateStatusAsync(Guid id, TicketStatus status, string? adminNotes = null);
    Task<SupportTicketResponse> UpdatePriorityAsync(Guid id, TicketPriority priority);
}

public class SupportTicketService : ISupportTicketService
{
    private readonly ISupportTicketRepository _ticketRepo;

    public SupportTicketService(ISupportTicketRepository ticketRepo)
    {
        _ticketRepo = ticketRepo;
    }

    public async Task<SupportTicketResponse> CreateAsync(Guid passengerId, CreateSupportTicketRequest request)
    {
        var ticket = new SupportTicket
        {
            PassengerId = passengerId,
            RideId = request.RideId,
            Subject = request.Subject,
            Description = request.Description,
            Priority = Enum.Parse<TicketPriority>(request.Priority)
        };

        await _ticketRepo.CreateAsync(ticket);
        var created = await _ticketRepo.GetByIdWithDetailsAsync(ticket.Id);
        return created!.ToResponse();
    }

    public async Task<SupportTicketResponse> GetByIdAsync(Guid id)
    {
        var ticket = await _ticketRepo.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Support ticket {id} not found");
        return ticket.ToResponse();
    }

    public async Task<PaginatedResponse<SupportTicketResponse>> GetByPassengerAsync(Guid passengerId, int page, int pageSize)
    {
        var tickets = await _ticketRepo.GetByPassengerIdAsync(passengerId, page, pageSize);
        var total = await _ticketRepo.GetCountByPassengerAsync(passengerId);

        return new PaginatedResponse<SupportTicketResponse>
        {
            Items = tickets.Select(t => t.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<PaginatedResponse<SupportTicketResponse>> GetAllAsync(int page, int pageSize, TicketStatus? status = null, TicketPriority? priority = null)
    {
        var tickets = await _ticketRepo.GetAllAsync(page, pageSize, status, priority);
        var total = await _ticketRepo.GetTotalCountAsync(status, priority);

        return new PaginatedResponse<SupportTicketResponse>
        {
            Items = tickets.Select(t => t.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<SupportTicketResponse> UpdateStatusAsync(Guid id, TicketStatus status, string? adminNotes = null)
    {
        var ticket = await _ticketRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Support ticket {id} not found");

        ticket.Status = status;
        if (adminNotes is not null) ticket.AdminNotes = adminNotes;
        if (status == TicketStatus.Resolved) ticket.ResolvedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);
        var updated = await _ticketRepo.GetByIdWithDetailsAsync(id);
        return updated!.ToResponse();
    }

    public async Task<SupportTicketResponse> UpdatePriorityAsync(Guid id, TicketPriority priority)
    {
        var ticket = await _ticketRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Support ticket {id} not found");

        ticket.Priority = priority;
        await _ticketRepo.UpdateAsync(ticket);
        var updated = await _ticketRepo.GetByIdWithDetailsAsync(id);
        return updated!.ToResponse();
    }
}