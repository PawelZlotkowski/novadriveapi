// Api/Endpoints/Admin/AdminDiagnosticEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using NovaDrive.Application.Services;

public static class AdminDiagnosticEndpoints
{
    public static RouteGroupBuilder MapAdminDiagnosticEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{vehicleId:guid}", async (
            Guid vehicleId, ISensorDiagnosticService service,
            int page = 1, int pageSize = 20) =>
            Results.Ok(await service.GetByVehicleAsync(vehicleId, page, pageSize)));

        group.MapGet("/critical", async (ISensorDiagnosticService service, int limit = 50) =>
            Results.Ok(await service.GetCriticalAlertsAsync(limit)));

        group.MapGet("/", async (
            ISensorDiagnosticService service,
            string? severity = null, int page = 1, int pageSize = 20) =>
        {
            if (severity is not null)
                return Results.Ok(await service.GetBySeverityAsync(severity, page, pageSize));
            return Results.Ok(await service.GetBySeverityAsync("Critical", page, pageSize));
        });

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Diagnostics");
    }
}