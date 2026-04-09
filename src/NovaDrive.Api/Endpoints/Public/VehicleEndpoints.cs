// Api/Endpoints/Public/VehicleEndpoints.cs
namespace NovaDrive.Api.Endpoints.Public;

using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;

public static class VehicleEndpoints
{
    public static RouteGroupBuilder MapPublicVehicleEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/public/vehicles/nearest?lat=...&lng=...&type=...
        group.MapGet("/nearest", async (
            double lat, double lng, string? type,
            IVehicleService vehicleService) =>
        {
            VehicleType? vehicleType = type is not null ? Enum.Parse<VehicleType>(type) : null;
            var vehicle = await vehicleService.FindNearestAsync(lat, lng, vehicleType);
            return vehicle is not null
                ? Results.Ok(vehicle)
                : Results.NotFound(new { message = "No available vehicles nearby" });
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Vehicles");

        return group;
    }
}