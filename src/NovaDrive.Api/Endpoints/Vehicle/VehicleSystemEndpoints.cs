// Api/Endpoints/Vehicle/VehicleSystemEndpoints.cs
namespace NovaDrive.Api.Endpoints.Vehicle;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.Services;

public static class VehicleSystemEndpoints
{
    public static RouteGroupBuilder MapVehicleSystemEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/vehicle/telemetry
        group.MapPost("/telemetry", async (
            CreateTelemetryRequest request,
            ITelemetryService service,
            IValidator<CreateTelemetryRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) return Results.UnprocessableEntity(validation.Errors);
            await service.RecordTelemetryAsync(request);
            return Results.Accepted();
        });

        // POST /api/vehicle/telemetry/batch
        group.MapPost("/telemetry/batch", async (
            List<CreateTelemetryRequest> requests,
            ITelemetryService service) =>
        {
            await service.RecordBatchAsync(requests);
            return Results.Accepted(value: new { count = requests.Count });
        });

        // POST /api/vehicle/diagnostics
        group.MapPost("/diagnostics", async (
            CreateSensorDiagnosticRequest request,
            ISensorDiagnosticService service) =>
        {
            await service.RecordDiagnosticAsync(request);
            return Results.Accepted();
        });

        // POST /api/vehicle/rides/{id}/start
        group.MapPost("/rides/{id:guid}/start", async (Guid id, IRideService rideService) =>
        {
            var ride = await rideService.StartRideAsync(id);
            return Results.Ok(ride);
        });

        // POST /api/vehicle/rides/{id}/complete
        group.MapPost("/rides/{id:guid}/complete", async (
            Guid id,
            CompleteRideRequest request,
            IRideService rideService,
            IPaymentService paymentService,
            IInvoiceService invoiceService) =>
        {
            // 1. Complete ride + calculate price
            var ride = await rideService.CompleteRideAsync(id, request);

            // 2. Create payment
            var payment = await paymentService.CreatePaymentForRideAsync(
                ride.Id, ride.FinalPrice ?? 0, "EUR");

            // 3. Process payment (simulated)
            await paymentService.ProcessPaymentAsync(payment.Id);

            // 4. Send invoice email (fire and forget in production)
            try
            {
                await invoiceService.SendInvoiceEmailAsync(ride.Id);
            }
            catch (Exception)
            {
                // Log but don't fail the ride completion
            }

            return Results.Ok(new { ride, payment });
        });

        // Note: API key auth is handled by ApiKeyMiddleware
        return group.WithTags("Vehicle System");
    }
}