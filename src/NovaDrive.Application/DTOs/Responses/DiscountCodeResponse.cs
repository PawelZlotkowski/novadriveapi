// Application/DTOs/Responses/DiscountCodeResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record DiscountCodeResponse(
    Guid Id,
    string Code,
    string Type,
    decimal Value,
    decimal MinimumRideValue,
    DateTime ExpiresAt,
    bool IsActive,
    int? MaxUses,
    int TimesUsed
);