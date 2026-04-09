// Api/Endpoints/Public/PassengerEndpoints.cs
namespace NovaDrive.Api.Endpoints.Public;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;
using NovaDrive.Api.Extensions;

public static class PassengerEndpoints
{
    public static RouteGroupBuilder MapPublicPassengerEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/public/passengers/me
        group.MapGet("/me", async (HttpContext context, IPassengerService service) =>
        {
            var userId = context.User.GetUserId();
            var passenger = await service.GetByUserIdAsync(userId);
            return Results.Ok(passenger);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Passengers");

        // POST /api/public/passengers
        group.MapPost("/", async (
            CreatePassengerRequest request,
            HttpContext context,
            IPassengerService service,
            IValidator<CreatePassengerRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.UnprocessableEntity(validation.Errors);

            var userId = context.User.GetUserId();
            var passenger = await service.CreateAsync(request, userId);
            return Results.Created($"/api/public/passengers/me", passenger);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Passengers");

        // PUT /api/public/passengers/me
        group.MapPut("/me", async (
            UpdatePassengerRequest request,
            HttpContext context,
            IPassengerService service) =>
        {
            var userId = context.User.GetUserId();
            var current = await service.GetByUserIdAsync(userId);
            var updated = await service.UpdateProfileAsync(current.Id, request);
            return Results.Ok(updated);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Passengers");

        // GET /api/public/passengers/me/loyalty
        group.MapGet("/me/loyalty", async (HttpContext context, IPassengerService service) =>
        {
            var userId = context.User.GetUserId();
            var current = await service.GetByUserIdAsync(userId);
            var points = await service.GetLoyaltyPointsAsync(current.Id);
            return Results.Ok(new { loyaltyPoints = points });
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Passengers");

        return group;
    }
}