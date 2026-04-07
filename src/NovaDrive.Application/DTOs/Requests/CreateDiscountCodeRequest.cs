// Application/DTOs/Requests/CreateDiscountCodeRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record CreateDiscountCodeRequest(
    string Code,
    string Type,            // "Percentage" or "Flat"
    decimal Value,
    decimal MinimumRideValue,
    DateTime ExpiresAt,
    int? MaxUses
);