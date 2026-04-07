// Application/DTOs/Responses/PassengerResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record PassengerResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string HomeAddress,
    int LoyaltyPoints,
    string PreferredPaymentMethod,
    DateTime CreatedAt
);