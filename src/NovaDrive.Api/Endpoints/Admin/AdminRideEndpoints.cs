// Api/Endpoints/Admin/AdminRideEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;

public static class AdminRideEndpoints
{
    public static RouteGroupBuilder MapAdminRideEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IRideService service, int page = 1, int pageSize = 20, string? status = null) =>
        {
            RideStatus? statusFilter = status is not null ? Enum.Parse<RideStatus>(status) : null;
            return Results.Ok(await service.GetAllAsync(page, pageSize, statusFilter));
        });

        group.MapGet("/{id:guid}", async (Guid id, IRideService service) =>
            Results.Ok(await service.GetByIdAsync(id)));

        group.MapGet("/active", async (IRideService service) =>
            Results.Ok(await service.GetAllAsync(1, 100, RideStatus.EnRoute)));

        // POST /api/admin/rides/{id}/invoice/send
        group.MapPost("/{id:guid}/invoice/send", async (Guid id, IInvoiceService invoiceService) =>
        {
            await invoiceService.SendInvoiceEmailAsync(id);
            return Results.Ok(new { message = "Invoice sent successfully" });
        });

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Rides");
    }
}