// Api/Endpoints/Admin/AdminPassengerEndpoints.cs
namespace NovaDrive.Api.Endpoints.Admin;

using NovaDrive.Application.Services;

public static class AdminPassengerEndpoints
{
    public static RouteGroupBuilder MapAdminPassengerEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IPassengerService service, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.GetAllAsync(page, pageSize)));

        group.MapGet("/{id:guid}", async (Guid id, IPassengerService service) =>
            Results.Ok(await service.GetByIdAsync(id)));

        group.MapPut("/{id:guid}/loyalty", async (Guid id, AdjustLoyaltyRequest request, IPassengerService service) =>
        {
            await service.AdjustLoyaltyPointsAsync(id, request.Points);
            return Results.Ok(new { passengerId = id, adjustedBy = request.Points });
        });

        return group.RequireAuthorization("AdminPolicy").WithTags("Admin - Passengers");
    }
}

public record AdjustLoyaltyRequest(int Points);