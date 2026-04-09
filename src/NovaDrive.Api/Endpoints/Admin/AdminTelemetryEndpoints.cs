// Api/Endpoints/Admin/AdminTelemetryEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using NovaDrive.Application.Services;

public static class AdminTelemetryEndpoints
{
    public static RouteGroupBuilder MapAdminTelemetryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{vehicleId:guid}", async (
            Guid vehicleId, ITelemetryService service,
            DateTime? from, DateTime? to) =>
        {
            var fromDate = from ?? DateTime.UtcNow.AddHours(-24);
            var toDate = to ?? DateTime.UtcNow;
            return Results.Ok(await service.GetVehicleTelemetryAsync(vehicleId, fromDate, toDate));
        });

        group.MapGet("/{vehicleId:guid}/latest", async (Guid vehicleId, ITelemetryService service) =>
        {
            var latest = await service.GetLatestAsync(vehicleId);
            return latest is not null ? Results.Ok(latest) : Results.NotFound();
        });

        group.MapGet("/fleet-snapshot", async (ITelemetryService service) =>
            Results.Ok(await service.GetFleetSnapshotAsync()));

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Telemetry");
    }
}