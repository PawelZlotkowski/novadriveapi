// Application/Validators/CreateSupportTicketRequestValidator.cs
namespace NovaDrive.Application.Validators;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;

public class CreateSupportTicketRequestValidator : AbstractValidator<CreateSupportTicketRequest>
{
    public CreateSupportTicketRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Priority)
            .NotEmpty()
            .Must(p => p is "Low" or "Medium" or "High" or "Critical")
            .WithMessage("Priority must be Low, Medium, High, or Critical");
    }
}