// Api/Endpoints/Admin/AdminMaintenanceEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;

public static class AdminMaintenanceEndpoints
{
    public static RouteGroupBuilder MapAdminMaintenanceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IMaintenanceService service, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.GetAllAsync(page, pageSize)));

        group.MapGet("/{id:guid}", async (Guid id, IMaintenanceService service) =>
            Results.Ok(await service.GetByIdAsync(id)));

        group.MapGet("/overdue", async (IMaintenanceService service) =>
            Results.Ok(await service.GetOverdueServicesAsync()));

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Maintenance");
    }
}