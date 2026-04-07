// Application/Validators/CreateDiscountCodeRequestValidator.cs
namespace NovaDrive.Application.Validators;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;

public class CreateDiscountCodeRequestValidator : AbstractValidator<CreateDiscountCodeRequest>
{
    public CreateDiscountCodeRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z0-9]+$").WithMessage("Code must be uppercase alphanumeric");

        RuleFor(x => x.Type)
            .Must(t => t is "Percentage" or "Flat")
            .WithMessage("Type must be Percentage or Flat");

        RuleFor(x => x.Value).GreaterThan(0);

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.Type == "Percentage")
            .WithMessage("Percentage discount cannot exceed 100%");

        RuleFor(x => x.MinimumRideValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTime.UtcNow);
    }
}