// GraphQL/Queries/Query.cs
namespace NovaDrive.GraphQL.Queries;

using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Services;
using NovaDrive.Domain.Enums;
using HotChocolate.Authorization;

public class Query
{
    // ── Vehicles ──
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IEnumerable<VehicleResponse>> GetVehicles(
        [Service] IVehicleService service,
        int page = 1, int pageSize = 20, bool? isActive = null)
    {
        var result = await service.GetAllAsync(page, pageSize, isActive);
        return result.Items;
    }

    [Authorize(Policy = "AdminPolicy")]
    public async Task<VehicleResponse> GetVehicle(
        [Service] IVehicleService service, Guid id)
        => await service.GetByIdAsync(id);

    [Authorize(Policy = "AdminPolicy")]
    public async Task<FleetStatsResponse> GetFleetStats(
        [Service] IVehicleService service)
        => await service.GetFleetStatsAsync();

    // ── Rides ──
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IEnumerable<RideResponse>> GetRides(
        [Service] IRideService service,
        int page = 1, int pageSize = 20, string? status = null)
    {
        RideStatus? s = status is not null ? Enum.Parse<RideStatus>(status) : null;
        var result = await service.GetAllAsync(page, pageSize, s);
        return result.Items;
    }

    [Authorize(Policy = "AdminPolicy")]
    public async Task<RideResponse> GetRide(
        [Service] IRideService service, Guid id)
        => await service.GetByIdAsync(id);

    // ── Passengers ──
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IEnumerable<PassengerResponse>> GetPassengers(
        [Service] IPassengerService service,
        int page = 1, int pageSize = 20)
    {
        var result = await service.GetAllAsync(page, pageSize);
        return result.Items;
    }

    // ── Telemetry ──
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IEnumerable<TelemetryResponse>> GetTelemetry(
        [Service] ITelemetryService service,
        Guid vehicleId, DateTime? from, DateTime? to)
    {
        return await service.GetVehicleTelemetryAsync(
            vehicleId,
            from ?? DateTime.UtcNow.AddHours(-24),
            to ?? DateTime.UtcNow);
    }

    [Authorize(Policy = "AdminPolicy")]
    public async Task<IEnumerable<TelemetryResponse>> GetFleetSnapshot(
        [Service] ITelemetryService service)
        => await service.GetFleetSnapshotAsync();

    // ── Support Tickets ──
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IEnumerable<SupportTicketResponse>> GetSupportTickets(
        [Service] ISupportTicketService service,
        int page = 1, int pageSize = 20,
        string? status = null, string? priority = null)
    {
        TicketStatus? s = status is not null ? Enum.Parse<TicketStatus>(status) : null;
        TicketPriority? p = priority is not null ? Enum.Parse<TicketPriority>(priority) : null;
        var result = await service.GetAllAsync(page, pageSize, s, p);
        return result.Items;
    }

    // ── Payments ──
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IEnumerable<PaymentResponse>> GetPayments(
        [Service] IPaymentService service,
        int page = 1, int pageSize = 20, string? status = null)
    {
        PaymentStatus? s = status is not null ? Enum.Parse<PaymentStatus>(status) : null;
        var result = await service.GetAllAsync(page, pageSize, s);
        return result.Items;
    }
}