// Api/Endpoints/Public/PaymentEndpoints.cs
namespace NovaDrive.Api.Endpoints.Public;

using NovaDrive.Application.Services;
using NovaDrive.Api.Extensions;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPublicPaymentEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/public/payments/history
        group.MapGet("/history", async (
            HttpContext context,
            IPaymentService paymentService,
            IPassengerService passengerService,
            int page = 1, int pageSize = 10) =>
        {
            var userId = context.User.GetUserId();
            var passenger = await passengerService.GetByUserIdAsync(userId);
            var payments = await paymentService.GetPassengerPaymentsAsync(passenger.Id, page, pageSize);
            return Results.Ok(payments);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Payments");

        // GET /api/public/payments/{id}
        group.MapGet("/{id:guid}", async (Guid id, IPaymentService paymentService) =>
        {
            var payment = await paymentService.GetByIdAsync(id);
            return Results.Ok(payment);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Payments");

        return group;
    }
}