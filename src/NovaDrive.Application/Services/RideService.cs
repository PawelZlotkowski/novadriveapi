// Application/Services/RideService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs;
using NovaDrive.Application.DTOs.Pricing;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Mappings;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public interface IRideService
{
    Task<RideResponse> GetByIdAsync(Guid id);
    Task<RideResponse> RequestRideAsync(Guid passengerId, CreateRideRequest request);
    Task<RideResponse?> GetActiveRideAsync(Guid passengerId);
    Task<RideResponse> CancelRideAsync(Guid rideId, Guid passengerId);
    Task<PaginatedResponse<RideResponse>> GetPassengerHistoryAsync(Guid passengerId, int page, int pageSize);
    Task<PricingResult> GetEstimateAsync(CreateRideRequest request, int loyaltyPoints);
    Task<RideResponse> StartRideAsync(Guid rideId);
    Task<RideResponse> CompleteRideAsync(Guid rideId, CompleteRideRequest request);
    Task<PaginatedResponse<RideResponse>> GetAllAsync(int page, int pageSize, RideStatus? status = null);
}

public class RideService : IRideService
{
    private readonly IRideRepository _rideRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDiscountCodeRepository _discountCodeRepository;
    private readonly IPricingService _pricingService;

    public RideService(
        IRideRepository rideRepository,
        IPassengerRepository passengerRepository,
        IVehicleRepository vehicleRepository,
        IDiscountCodeRepository discountCodeRepository,
        IPricingService pricingService)
    {
        _rideRepository = rideRepository;
        _passengerRepository = passengerRepository;
        _vehicleRepository = vehicleRepository;
        _discountCodeRepository = discountCodeRepository;
        _pricingService = pricingService;
    }

    public async Task<RideResponse> GetByIdAsync(Guid id)
    {
        var ride = await _rideRepository.GetByIdWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Ride {id} not found");
        return ride.ToResponse();
    }

    public async Task<RideResponse> RequestRideAsync(Guid passengerId, CreateRideRequest request)
    {
        var passenger = await _passengerRepository.GetByIdAsync(passengerId)
            ?? throw new KeyNotFoundException($"Passenger {passengerId} not found");

        // Prevent multiple active rides
        var activeRide = await _rideRepository.GetActiveRideByPassengerAsync(passengerId);
        if (activeRide is not null)
            throw new InvalidOperationException("Passenger already has an active ride");

        var ride = new Ride
        {
            PassengerId = passengerId,
            DepartureAddress = request.DepartureAddress,
            DepartureLatitude = request.DepartureLatitude,
            DepartureLongitude = request.DepartureLongitude,
            DestinationAddress = request.DestinationAddress,
            DestinationLatitude = request.DestinationLatitude,
            DestinationLongitude = request.DestinationLongitude,
            Status = RideStatus.Requested
        };

        // Try to find nearest available vehicle
        VehicleType? preferredType = request.VehicleType is not null
            ? Enum.Parse<VehicleType>(request.VehicleType)
            : null;

        var vehicle = await _vehicleRepository.GetNearestAvailableAsync(
            request.DepartureLatitude, request.DepartureLongitude, preferredType);

        if (vehicle is not null)
            ride.VehicleId = vehicle.Id;

        // Handle discount code
        if (request.DiscountCode is not null)
        {
            var discountCode = await _discountCodeRepository.GetByCodeAsync(request.DiscountCode);
            if (discountCode is not null)
                ride.DiscountCodeId = discountCode.Id;
        }

        await _rideRepository.CreateAsync(ride);

        // Reload with navigation properties
        var created = await _rideRepository.GetByIdWithDetailsAsync(ride.Id);
        return created!.ToResponse();
    }

    public async Task<RideResponse?> GetActiveRideAsync(Guid passengerId)
    {
        var ride = await _rideRepository.GetActiveRideByPassengerAsync(passengerId);
        return ride?.ToResponse();
    }

    public async Task<RideResponse> CancelRideAsync(Guid rideId, Guid passengerId)
    {
        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId)
            ?? throw new KeyNotFoundException($"Ride {rideId} not found");

        if (ride.PassengerId != passengerId)
            throw new InvalidOperationException("You can only cancel your own rides");

        if (ride.Status is not (RideStatus.Requested or RideStatus.EnRoute))
            throw new InvalidOperationException($"Cannot cancel a ride with status {ride.Status}");

        ride.Status = RideStatus.Cancelled;
        await _rideRepository.UpdateAsync(ride);

        return ride.ToResponse();
    }

    public async Task<PaginatedResponse<RideResponse>> GetPassengerHistoryAsync(Guid passengerId, int page, int pageSize)
    {
        var rides = await _rideRepository.GetByPassengerIdAsync(passengerId, page, pageSize);
        var total = await _rideRepository.GetTotalCountByPassengerAsync(passengerId);

        return new PaginatedResponse<RideResponse>
        {
            Items = rides.Select(r => r.ToResponse()),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PricingResult> GetEstimateAsync(CreateRideRequest request, int loyaltyPoints)
    {
        var vehicleType = request.VehicleType is not null
            ? Enum.Parse<VehicleType>(request.VehicleType)
            : VehicleType.Standard;

        // Estimate distance from coordinates (Haversine)
        var distanceKm = CalculateDistanceKm(
            request.DepartureLatitude, request.DepartureLongitude,
            request.DestinationLatitude, request.DestinationLongitude);

        // Estimate duration (~30 km/h average city speed)
        var durationMinutes = (distanceKm / 30m) * 60m;

        DiscountCodeInfo? discountCodeInfo = null;
        if (request.DiscountCode is not null)
        {
            var code = await _discountCodeRepository.GetByCodeAsync(request.DiscountCode);
            if (code is not null && code.IsActive && DateTime.UtcNow < code.ExpiresAt)
            {
                discountCodeInfo = new DiscountCodeInfo
                {
                    Id = code.Id,
                    Code = code.Code,
                    Type = code.Type,
                    Value = code.Value,
                    MinimumRideValue = code.MinimumRideValue,
                    ExpiresAt = code.ExpiresAt
                };
            }
        }

        var pricingRequest = new PricingRequest
        {
            DistanceKm = distanceKm,
            DurationMinutes = durationMinutes,
            VehicleType = vehicleType,
            RideStartTime = DateTime.UtcNow,
            PassengerLoyaltyPoints = loyaltyPoints,
            DiscountCode = discountCodeInfo
        };

        return _pricingService.CalculatePrice(pricingRequest);
    }

    public async Task<RideResponse> StartRideAsync(Guid rideId)
    {
        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId)
            ?? throw new KeyNotFoundException($"Ride {rideId} not found");

        if (ride.Status != RideStatus.Requested)
            throw new InvalidOperationException($"Cannot start a ride with status {ride.Status}");

        ride.Status = RideStatus.EnRoute;
        ride.StartedAt = DateTime.UtcNow;

        await _rideRepository.UpdateAsync(ride);
        return ride.ToResponse();
    }

    public async Task<RideResponse> CompleteRideAsync(Guid rideId, CompleteRideRequest request)
    {
        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId)
            ?? throw new KeyNotFoundException($"Ride {rideId} not found");

        if (ride.Status != RideStatus.EnRoute)
            throw new InvalidOperationException($"Cannot complete a ride with status {ride.Status}");

        ride.DistanceKm = request.DistanceKm;
        ride.DurationMinutes = request.DurationMinutes;

        // Get passenger loyalty points for pricing
        var passenger = await _passengerRepository.GetByIdAsync(ride.PassengerId);
        var loyaltyPoints = passenger?.LoyaltyPoints ?? 0;

        var vehicleType = ride.Vehicle?.VehicleType ?? VehicleType.Standard;

        // Build discount code info if applicable
        DiscountCodeInfo? discountCodeInfo = null;
        if (ride.DiscountCodeId.HasValue && ride.DiscountCode is not null)
        {
            var code = ride.DiscountCode;
            discountCodeInfo = new DiscountCodeInfo
            {
                Id = code.Id,
                Code = code.Code,
                Type = code.Type,
                Value = code.Value,
                MinimumRideValue = code.MinimumRideValue,
                ExpiresAt = code.ExpiresAt
            };
        }

        var pricingRequest = new PricingRequest
        {
            DistanceKm = request.DistanceKm,
            DurationMinutes = request.DurationMinutes,
            VehicleType = vehicleType,
            RideStartTime = ride.StartedAt ?? ride.RequestedAt,
            PassengerLoyaltyPoints = loyaltyPoints,
            DiscountCode = discountCodeInfo
        };

        var pricing = _pricingService.CalculatePrice(pricingRequest);

        ride.FinalPrice = pricing.FinalPrice;
        ride.VatAmount = pricing.VatAmount;
        ride.SubtotalBeforeVat = pricing.SubtotalBeforeVat;
        ride.Status = RideStatus.Completed;
        ride.CompletedAt = DateTime.UtcNow;

        await _rideRepository.UpdateAsync(ride);

        // Update vehicle mileage
        if (ride.VehicleId.HasValue && request.DistanceKm > 0)
            await _vehicleRepository.AddMileageAsync(ride.VehicleId.Value, request.DistanceKm);

        // Increment discount code usage
        if (ride.DiscountCodeId.HasValue && discountCodeInfo is not null)
            await _discountCodeRepository.IncrementUsageAsync(ride.DiscountCodeId.Value);

        // Award loyalty points (1 point per €1 spent)
        if (passenger is not null)
        {
            var pointsEarned = (int)Math.Floor(pricing.FinalPrice);
            if (pointsEarned > 0)
                await _passengerRepository.AddLoyaltyPointsAsync(passenger.Id, pointsEarned);

            // Deduct used loyalty points
            if (pricing.LoyaltyPointsUsed > 0)
                await _passengerRepository.DeductLoyaltyPointsAsync(passenger.Id, pricing.LoyaltyPointsUsed);
        }

        return ride.ToResponse();
    }

    public async Task<PaginatedResponse<RideResponse>> GetAllAsync(int page, int pageSize, RideStatus? status = null)
    {
        var rides = await _rideRepository.GetAllAsync(page, pageSize, status);
        var total = await _rideRepository.GetTotalCountAsync(status);

        return new PaginatedResponse<RideResponse>
        {
            Items = rides.Select(r => r.ToResponse()),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private static decimal CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Earth radius in km
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (decimal)(R * c);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
