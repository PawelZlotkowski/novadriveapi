// Api/Endpoints/Public/DiscountCodeEndpoints.cs
namespace NovaDrive.Api.Endpoints.Public;

using NovaDrive.Application.Services;

public static class DiscountCodeEndpoints
{
    public static RouteGroupBuilder MapPublicDiscountCodeEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/public/discount-codes/validate
        group.MapPost("/validate", async (
            ValidateCodeRequest request,
            IDiscountCodeService discountCodeService) =>
        {
            var result = await discountCodeService.ValidateCodeAsync(request.Code, request.EstimatedRideValue);
            return Results.Ok(result);
        })
        .RequireAuthorization("PassengerPolicy")
        .WithTags("Discount Codes");

        return group;
    }
}

public record ValidateCodeRequest(string Code, decimal EstimatedRideValue);