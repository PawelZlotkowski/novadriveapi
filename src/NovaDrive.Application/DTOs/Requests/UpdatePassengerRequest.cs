// Application/DTOs/Requests/UpdatePassengerRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record UpdatePassengerRequest(
    string? FirstName,
    string? LastName,
    string? HomeAddress,
    string? PreferredPaymentMethod
);