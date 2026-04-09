// Api/Endpoints/Public/SupportTicketEndpoints.cs
namespace NovaDrive.Api.Endpoints.Public;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;
using NovaDrive.Api.Extensions;

public static class SupportTicketEndpoints
{
    public static RouteGroupBuilder MapPublicSupportTicketEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/public/support-tickets
        group.MapPost("/", async (
            CreateSupportTicketRequest request,
            HttpContext context,
            ISupportTicketService ticketService,
            IPassengerService passengerService,
            IValidator<CreateSupportTicketRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.UnprocessableEntity(validation.Errors);

            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var ticket = await ticketService.CreateAsync(passenger.Id, request);
            return Results.Created($"/api/public/support-tickets/{ticket.Id}", ticket);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Support Tickets");

        // GET /api/public/support-tickets
        group.MapGet("/", async (
            HttpContext context,
            ISupportTicketService ticketService,
            IPassengerService passengerService,
            int page = 1, int pageSize = 10) =>
        {
            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var tickets = await ticketService.GetByPassengerAsync(passenger.Id, page, pageSize);
            return Results.Ok(tickets);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Support Tickets");

        // GET /api/public/support-tickets/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISupportTicketService ticketService) =>
        {
            var ticket = await ticketService.GetByIdAsync(id);
            return Results.Ok(ticket);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Support Tickets");

        return group;
    }
}