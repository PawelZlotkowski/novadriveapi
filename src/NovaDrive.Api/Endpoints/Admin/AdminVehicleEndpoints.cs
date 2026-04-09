// Api/Endpoints/Admin/AdminVehicleEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;

public static class AdminVehicleEndpoints
{
    public static RouteGroupBuilder MapAdminVehicleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IVehicleService service, int page = 1, int pageSize = 20, bool? isActive = null) =>
            Results.Ok(await service.GetAllAsync(page, pageSize, isActive)));

        group.MapGet("/{id:guid}", async (Guid id, IVehicleService service) =>
            Results.Ok(await service.GetByIdAsync(id)));

        group.MapPost("/", async (CreateVehicleRequest request, IVehicleService service, IValidator<CreateVehicleRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) return Results.UnprocessableEntity(validation.Errors);
            var vehicle = await service.CreateAsync(request);
            return Results.Created($"/api/admin/vehicles/{vehicle.Id}", vehicle);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateVehicleRequest request, IVehicleService service) =>
            Results.Ok(await service.UpdateAsync(id, request)));

        group.MapDelete("/{id:guid}", async (Guid id, IVehicleService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, ChangeStatusRequest request, IVehicleService service) =>
        {
            await service.SetActiveStatusAsync(id, request.IsActive);
            return Results.Ok(new { id, request.IsActive });
        });

        group.MapGet("/stats", async (IVehicleService service) =>
            Results.Ok(await service.GetFleetStatsAsync()));

        group.MapGet("/{id:guid}/location", async (Guid id, IVehicleService service) =>
        {
            var vehicle = await service.GetByIdAsync(id);
            return Results.Ok(new { vehicle.CurrentLatitude, vehicle.CurrentLongitude, vehicle.CurrentBatteryPercentage });
        });

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Vehicles");
    }
}