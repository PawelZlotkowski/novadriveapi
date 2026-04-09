// Api/Endpoints/Admin/AdminSupportTicketEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;

public static class AdminSupportTicketEndpoints
{
    public static RouteGroupBuilder MapAdminSupportTicketEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            ISupportTicketService service,
            int page = 1, int pageSize = 20,
            string? status = null, string? priority = null) =>
        {
            TicketStatus? s = status is not null ? Enum.Parse<TicketStatus>(status) : null;
            TicketPriority? p = priority is not null ? Enum.Parse<TicketPriority>(priority) : null;
            return Results.Ok(await service.GetAllAsync(page, pageSize, s, p));
        });

        group.MapGet("/{id:guid}", async (Guid id, ISupportTicketService service) =>
            Results.Ok(await service.GetByIdAsync(id)));

        group.MapPatch("/{id:guid}/status", async (
            Guid id, UpdateTicketStatusRequest request, ISupportTicketService service) =>
        {
            var status = Enum.Parse<TicketStatus>(request.Status);
            return Results.Ok(await service.UpdateStatusAsync(id, status, request.AdminNotes));
        });

        group.MapPatch("/{id:guid}/priority", async (
            Guid id, UpdateTicketPriorityRequest request, ISupportTicketService service) =>
        {
            var priority = Enum.Parse<TicketPriority>(request.Priority);
            return Results.Ok(await service.UpdatePriorityAsync(id, priority));
        });

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Support Tickets");
    }
}

public record UpdateTicketStatusRequest(string Status, string? AdminNotes);
public record UpdateTicketPriorityRequest(string Priority);