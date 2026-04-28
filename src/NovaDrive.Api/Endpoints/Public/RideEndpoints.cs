// Api/Endpoints/Public/RideEndpoints.cs
namespace NovaDrive.Api.Endpoints.Public;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;
using NovaDrive.Api.Extensions;

public static class RideEndpoints
{
    public static RouteGroupBuilder MapPublicRideEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/public/rides — request a new ride
        group.MapPost("/", async (
            CreateRideRequest request,
            HttpContext context,
            IRideService rideService,
            IPassengerService passengerService,
            IValidator<CreateRideRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.UnprocessableEntity(validation.Errors);

            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var ride = await rideService.RequestRideAsync(passenger.Id, request);
            return Results.Created($"/api/public/rides/{ride.Id}", ride);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Rides");

        // GET /api/public/rides/active
        group.MapGet("/active", async (HttpContext context, IRideService rideService, IPassengerService passengerService) =>
        {
            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var ride = await rideService.GetActiveRideAsync(passenger.Id);
            return ride is not null ? Results.Ok(ride) : Results.NotFound(new { message = "No active ride" });
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Rides");

        // GET /api/public/rides/{id}
        group.MapGet("/{id:guid}", async (Guid id, IRideService rideService) =>
        {
            var ride = await rideService.GetByIdAsync(id);
            return Results.Ok(ride);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Rides");

        // POST /api/public/rides/{id}/cancel
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            HttpContext context,
            IRideService rideService,
            IPassengerService passengerService) =>
        {
            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var ride = await rideService.CancelRideAsync(id, passenger.Id);
            return Results.Ok(ride);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Rides");

        // GET /api/public/rides/history
        group.MapGet("/history", async (
            HttpContext context,
            IRideService rideService,
            IPassengerService passengerService,
            int page = 1, int pageSize = 10) =>
        {
            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var rides = await rideService.GetPassengerHistoryAsync(passenger.Id, page, pageSize);
            return Results.Ok(rides);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Rides");

        // POST /api/public/rides/estimate
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
        .WithTags("Rides");

        // GET /api/public/rides/{id}/invoice
        group.MapGet("/{id:guid}/invoice", async (Guid id, IInvoiceService invoiceService) =>
        {
            var pdf = await invoiceService.GenerateInvoicePdfAsync(id);
            return Results.File(pdf, "application/pdf", $"NovaDrive-Invoice-{id.ToString()[..8]}.pdf");
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Invoices");

        // GET /api/public/rides/active/vehicle-position
        // Returns the latest telemetry for the vehicle assigned to the passenger's active ride
        group.MapGet("/active/vehicle-position", async (
            HttpContext context,
            IRideService rideService,
            IPassengerService passengerService,
            ITelemetryService telemetryService) =>
        {
            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var ride = await rideService.GetActiveRideAsync(passenger.Id);
            if (ride is null || ride.VehicleId is null)
                return Results.NotFound(new { message = "No active ride with vehicle assigned" });

            var telemetry = await telemetryService.GetLatestAsync(ride.VehicleId.Value);
            if (telemetry is null)
                return Results.NotFound(new { message = "No telemetry available yet" });

            return Results.Ok(new
            {
                latitude         = telemetry.Latitude,
                longitude        = telemetry.Longitude,
                speedKmh         = telemetry.SpeedKmh,
                batteryPct       = telemetry.BatteryPercentage,
                timestamp        = telemetry.Timestamp,
                rideStatus       = ride.Status,
            });
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Rides");

        return group;
    }
}