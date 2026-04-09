// Api/Endpoints/Admin/AdminMaintenanceByVehicleEndpoints.cs
//NOTE: These are mapped under /api/admin/vehicles/{vehicleId}/maintenance in Program.cs
// But for simplicity, we map them separately and include the vehicleId in the route
namespace NovaDrive.Api.Endpoints.Admin;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;

public static class AdminMaintenanceByVehicleEndpoints
{
    // Call from Program.cs:
    // app.MapGroup("/api/admin/vehicles/{vehicleId}/maintenance").MapVehicleMaintenanceEndpoints();

    public static void AddMaintenanceVehicleRoutes(this RouteGroupBuilder adminMaintenanceGroup)
    {
        // These are added to the existing maintenance group with vehicle-scoped paths

        // GET /api/admin/maintenance/vehicle/{vehicleId}
        adminMaintenanceGroup.MapGet("/vehicle/{vehicleId:guid}", async (
            Guid vehicleId, IMaintenanceService service, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.GetByVehicleAsync(vehicleId, page, pageSize)));

        // POST /api/admin/maintenance/vehicle/{vehicleId}
        adminMaintenanceGroup.MapPost("/vehicle/{vehicleId:guid}", async (
            Guid vehicleId,
            CreateMaintenanceLogRequest request,
            IMaintenanceService service,
            IValidator<CreateMaintenanceLogRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) return Results.UnprocessableEntity(validation.Errors);
            var log = await service.CreateAsync(vehicleId, request);
            return Results.Created($"/api/admin/maintenance/{log.Id}", log);
        });

        // PUT /api/admin/maintenance/{id}
        adminMaintenanceGroup.MapPut("/{id:guid}", async (
            Guid id,
            CreateMaintenanceLogRequest request,
            IMaintenanceService service,
            IValidator<CreateMaintenanceLogRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) return Results.UnprocessableEntity(validation.Errors);
            return Results.Ok(await service.UpdateAsync(id, request));
        });

        // DELETE /api/admin/maintenance/{id}
        adminMaintenanceGroup.MapDelete("/{id:guid}", async (Guid id, IMaintenanceService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        });
    }
}