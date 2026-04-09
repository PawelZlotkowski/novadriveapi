// GraphQL/Mutations/Mutation.cs
namespace NovaDrive.GraphQL.Mutations;

using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;
using HotChocolate.Authorization;

public class Mutation
{
    [Authorize(Policy = "AdminPolicy")]
    public async Task<VehicleResponse> CreateVehicle(
        IVehicleService service, CreateVehicleRequest input)
        => await service.CreateAsync(input);

    [Authorize(Policy = "AdminPolicy")]
    public async Task<SupportTicketResponse> UpdateTicketStatus(
        ISupportTicketService service,
        Guid id, string status, string? adminNotes)
    {
        var ticketStatus = Enum.Parse<TicketStatus>(status);
        return await service.UpdateStatusAsync(id, ticketStatus, adminNotes);
    }

    [Authorize(Policy = "AdminPolicy")]
    public async Task<DiscountCodeResponse> CreateDiscountCode(
        IDiscountCodeService service, CreateDiscountCodeRequest input)
        => await service.CreateAsync(input);
}