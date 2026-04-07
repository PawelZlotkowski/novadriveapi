// Application/Validators/CreateVehicleRequestValidator.cs
namespace NovaDrive.Application.Validators;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;

public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.VIN)
            .NotEmpty().WithMessage("VIN is required")
            .Length(17).WithMessage("VIN must be exactly 17 characters");

        RuleFor(x => x.LicensePlate)
            .NotEmpty().WithMessage("License plate is required")
            .MaximumLength(20);

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Vehicle model is required")
            .MaximumLength(100);

        RuleFor(x => x.VehicleType)
            .NotEmpty()
            .Must(t => t is "Standard" or "Van" or "Luxury")
            .WithMessage("Vehicle type must be Standard, Van, or Luxury");

        RuleFor(x => x.YearOfManufacture)
            .InclusiveBetween(2015, DateTime.UtcNow.Year + 1)
            .WithMessage("Year of manufacture must be between 2015 and next year");
    }
}