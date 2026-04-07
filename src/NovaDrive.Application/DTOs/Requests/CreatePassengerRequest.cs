// Application/DTOs/Requests/CreatePassengerRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record CreatePassengerRequest(
    string FirstName,
    string LastName,
    string HomeAddress,
    string PreferredPaymentMethod  // "CreditCard", "PayPal", "BankTransfer"
);