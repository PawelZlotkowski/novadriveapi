// Application/DTOs/Responses/PaymentResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record PaymentResponse(
    Guid Id,
    Guid RideId,
    decimal Amount,
    string Currency,
    string Status,
    string TransactionReference,
    DateTime CreatedAt,
    DateTime? PaidAt
);