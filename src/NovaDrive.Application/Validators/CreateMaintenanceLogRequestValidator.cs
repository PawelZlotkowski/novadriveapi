// Application/Validators/CreateMaintenanceLogRequestValidator.cs
namespace NovaDrive.Application.Validators;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;

public class CreateMaintenanceLogRequestValidator : AbstractValidator<CreateMaintenanceLogRequest>
{
    public CreateMaintenanceLogRequestValidator()
    {
        RuleFor(x => x.ServiceDate).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow.AddDays(1));
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TechnicianName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NextServiceMileage).GreaterThan(0);
    }
}