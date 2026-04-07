// Application/Validators/CreateTelemetryRequestValidator.cs
namespace NovaDrive.Application.Validators;

using FluentValidation;
using NovaDrive.Application.DTOs.Requests;

public class CreateTelemetryRequestValidator : AbstractValidator<CreateTelemetryRequest>
{
    public CreateTelemetryRequestValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.SpeedKmh).InclusiveBetween(0, 300);
        RuleFor(x => x.BatteryPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.HardwareTemperatureCelsius).InclusiveBetween(-40, 120);
    }
}