// Application/Validators/CreateRideRequestValidator.cs
namespace NovaDrive.Application.Validators;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;

public class CreateRideRequestValidator : AbstractValidator<CreateRideRequest>
{
    public CreateRideRequestValidator()
    {
        RuleFor(x => x.DepartureAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DestinationAddress).NotEmpty().MaximumLength(500);

        RuleFor(x => x.DepartureLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.DepartureLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DestinationLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.DestinationLongitude).InclusiveBetween(-180, 180);

        RuleFor(x => x.VehicleType)
            .Must(t => t is null or "Standard" or "Van" or "Luxury")
            .WithMessage("Vehicle type must be Standard, Van, or Luxury");
    }
}