// Application/Mappings/MappingExtensions.cs
namespace NovaDrive.Application.Mappings;

using NovaDrive.Domain.Models;
using NovaDrive.Domain.Documents;
using NovaDrive.Domain.Enums;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.DTOs.Requests;

public static class MappingExtensions
{
    // --- Passenger ---
    public static PassengerResponse ToResponse(this Passenger p) => new(
        p.Id, p.UserId, p.User?.Email ?? "", p.FirstName, p.LastName,
        p.HomeAddress, p.LoyaltyPoints, p.PreferredPaymentMethod.ToString(), p.CreatedAt);

    public static Passenger ToEntity(this CreatePassengerRequest r, Guid userId) => new()
    {
        UserId = userId,
        FirstName = r.FirstName,
        LastName = r.LastName,
        HomeAddress = r.HomeAddress,
        PreferredPaymentMethod = Enum.Parse<PaymentMethod>(r.PreferredPaymentMethod)
    };

    // --- Vehicle ---
    public static VehicleResponse ToResponse(this Vehicle v) => new(
        v.Id, v.VIN, v.LicensePlate, v.Model, v.VehicleType.ToString(),
        v.YearOfManufacture, v.IsActive, v.CurrentLatitude, v.CurrentLongitude,
        v.CurrentBatteryPercentage, v.CurrentMileage);

    public static Vehicle ToEntity(this CreateVehicleRequest r) => new()
    {
        VIN = r.VIN,
        LicensePlate = r.LicensePlate,
        Model = r.Model,
        VehicleType = Enum.Parse<VehicleType>(r.VehicleType),
        YearOfManufacture = r.YearOfManufacture,
        CurrentLatitude = r.Latitude,
        CurrentLongitude = r.Longitude
    };

    // --- Ride ---
    public static RideResponse ToResponse(this Ride r) => new(
        r.Id, r.PassengerId,
        r.Passenger is not null ? $"{r.Passenger.FirstName} {r.Passenger.LastName}" : "",
        r.VehicleId, r.Vehicle?.Model, r.Vehicle?.LicensePlate,
        r.DepartureAddress, r.DepartureLatitude, r.DepartureLongitude,
        r.DestinationAddress, r.DestinationLatitude, r.DestinationLongitude,
        r.Vehicle?.VehicleType.ToString(),
        r.Status.ToString(),
        r.RequestedAt, r.StartedAt, r.CompletedAt,
        r.DistanceKm, r.DurationMinutes, r.FinalPrice, r.VatAmount,
        r.DiscountCode?.Code);

    // --- Payment ---
    public static PaymentResponse ToResponse(this Payment p) => new(
        p.Id, p.RideId, p.Amount, p.Currency, p.Status.ToString(),
        p.TransactionReference, p.CreatedAt, p.PaidAt);

    // --- MaintenanceLog ---
    public static MaintenanceLogResponse ToResponse(this MaintenanceLog m) => new(
        m.Id, m.VehicleId, m.Vehicle?.Model, m.Vehicle?.LicensePlate,
        m.ServiceDate, m.Description, m.TechnicianName, m.Cost, m.NextServiceMileage);

    // --- SupportTicket ---
    public static SupportTicketResponse ToResponse(this SupportTicket t) => new(
        t.Id, t.PassengerId,
        t.Passenger is not null ? $"{t.Passenger.FirstName} {t.Passenger.LastName}" : "",
        t.RideId, t.Subject, t.Description,
        t.Priority.ToString(), t.Status.ToString(),
        t.CreatedAt, t.ResolvedAt, t.AdminNotes);

    // --- DiscountCode ---
    public static DiscountCodeResponse ToResponse(this DiscountCode d) => new(
        d.Id, d.Code, d.Type.ToString(), d.Value, d.MinimumRideValue,
        d.ExpiresAt, d.IsActive, d.MaxUses, d.TimesUsed);

    // --- Telemetry ---
    public static TelemetryResponse ToResponse(this TelemetryData t) => new(
        t.VehicleId, t.Latitude, t.Longitude, t.SpeedKmh,
        t.BatteryPercentage, t.HardwareTemperatureCelsius, t.Timestamp);

    // --- SensorDiagnostic ---
    public static SensorDiagnosticResponse ToResponse(this SensorDiagnostic s) => new(
        s.Id, s.VehicleId, s.SensorType, s.ErrorCode,
        s.Severity, s.Timestamp, null); // RawSensorData converted separately
}