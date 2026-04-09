// Api/Endpoints/Public/PricingEndpoints.cs
namespace NovaDrive.Api.Endpoints.Public;

using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;
using NovaDrive.Api.Extensions;

public static class PricingEndpoints
{
    public static RouteGroupBuilder MapPricingEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/public/pricing/estimate
        group.MapPost("/estimate", async (
            CreateRideRequest request,
            HttpContext context,
            IRideService rideService,
            IPassengerService passengerService) =>
        {
            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var estimate = await rideService.GetEstimateAsync(request, passenger.LoyaltyPoints);
            return Results.Ok(estimate);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Pricing");

        return group;
    }
}