// Api/Endpoints/Admin/AdminPaymentEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;

public static class AdminPaymentEndpoints
{
    public static RouteGroupBuilder MapAdminPaymentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IPaymentService service, int page = 1, int pageSize = 20, string? status = null) =>
        {
            PaymentStatus? statusFilter = status is not null ? Enum.Parse<PaymentStatus>(status) : null;
            return Results.Ok(await service.GetAllAsync(page, pageSize, statusFilter));
        });

        group.MapGet("/{id:guid}", async (Guid id, IPaymentService service) =>
            Results.Ok(await service.GetByIdAsync(id)));

        group.MapPost("/{id:guid}/refund", async (Guid id, IPaymentService service) =>
            Results.Ok(await service.RefundPaymentAsync(id)));

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Payments");
    }
}