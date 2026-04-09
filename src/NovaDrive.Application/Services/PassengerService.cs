// Application/Services/PassengerService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs;
using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Mappings;
using NovaDrive.Infrastructure.Repositories;

public interface IPassengerService
{
    Task<PassengerResponse> GetByIdAsync(Guid id);
    Task<PassengerResponse> GetByUserIdAsync(Guid userId);
    Task<PassengerResponse> CreateAsync(CreatePassengerRequest request, Guid userId);
    Task<PassengerResponse> UpdateProfileAsync(Guid passengerId, UpdatePassengerRequest request);
    Task<PaginatedResponse<PassengerResponse>> GetAllAsync(int page, int pageSize);
    Task<int> GetLoyaltyPointsAsync(Guid passengerId);
    Task AdjustLoyaltyPointsAsync(Guid passengerId, int points);
}

public class PassengerService : IPassengerService
{
    private readonly IPassengerRepository _passengerRepository;

    public PassengerService(IPassengerRepository passengerRepository)
    {
        _passengerRepository = passengerRepository;
    }

    public async Task<PassengerResponse> GetByIdAsync(Guid id)
    {
        var passenger = await _passengerRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Passenger {id} not found");
        return passenger.ToResponse();
    }

    public async Task<PassengerResponse> GetByUserIdAsync(Guid userId)
    {
        var passenger = await _passengerRepository.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"Passenger profile for user {userId} not found");
        return passenger.ToResponse();
    }

    public async Task<PassengerResponse> CreateAsync(CreatePassengerRequest request, Guid userId)
    {
        var existing = await _passengerRepository.GetByUserIdAsync(userId);
        if (existing is not null)
            throw new InvalidOperationException("Passenger profile already exists for this user");

        var passenger = request.ToEntity(userId);
        await _passengerRepository.CreateAsync(passenger);

        var created = await _passengerRepository.GetByIdAsync(passenger.Id);
        return created!.ToResponse();
    }

    public async Task<PassengerResponse> UpdateProfileAsync(Guid passengerId, UpdatePassengerRequest request)
    {
        var passenger = await _passengerRepository.GetByIdAsync(passengerId)
            ?? throw new KeyNotFoundException($"Passenger {passengerId} not found");

        if (request.FirstName is not null) passenger.FirstName = request.FirstName;
        if (request.LastName is not null) passenger.LastName = request.LastName;
        if (request.HomeAddress is not null) passenger.HomeAddress = request.HomeAddress;
        if (request.PreferredPaymentMethod is not null)
            passenger.PreferredPaymentMethod = Enum.Parse<Domain.Enums.PaymentMethod>(request.PreferredPaymentMethod);

        await _passengerRepository.UpdateAsync(passenger);
        return passenger.ToResponse();
    }

    public async Task<PaginatedResponse<PassengerResponse>> GetAllAsync(int page, int pageSize)
    {
        var passengers = await _passengerRepository.GetAllAsync(page, pageSize);
        var total = await _passengerRepository.GetTotalCountAsync();

        return new PaginatedResponse<PassengerResponse>
        {
            Items = passengers.Select(p => p.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<int> GetLoyaltyPointsAsync(Guid passengerId)
    {
        var passenger = await _passengerRepository.GetByIdAsync(passengerId)
            ?? throw new KeyNotFoundException($"Passenger {passengerId} not found");
        return passenger.LoyaltyPoints;
    }

    public async Task AdjustLoyaltyPointsAsync(Guid passengerId, int points)
    {
        if (points > 0)
            await _passengerRepository.AddLoyaltyPointsAsync(passengerId, points);
        else
            await _passengerRepository.DeductLoyaltyPointsAsync(passengerId, Math.Abs(points));
    }
}