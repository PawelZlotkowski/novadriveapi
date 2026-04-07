// Application/Validators/CreatePassengerRequestValidator.cs
namespace NovaDrive.Application.Validators;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;

public class CreatePassengerRequestValidator : AbstractValidator<CreatePassengerRequest>
{
    public CreatePassengerRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100);

        RuleFor(x => x.HomeAddress)
            .NotEmpty().WithMessage("Home address is required")
            .MaximumLength(500);

        RuleFor(x => x.PreferredPaymentMethod)
            .NotEmpty()
            .Must(m => m is "CreditCard" or "PayPal" or "BankTransfer")
            .WithMessage("Payment method must be CreditCard, PayPal, or BankTransfer");
    }
}