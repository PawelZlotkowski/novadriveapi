// Application/Services/PaymentService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.Mappings;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;
using NovaDrive.Infrastructure.Repositories;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentForRideAsync(Guid rideId, decimal amount, string currency);
    Task<PaymentResponse> ProcessPaymentAsync(Guid paymentId);
    Task<PaymentResponse> RefundPaymentAsync(Guid paymentId);
    Task<PaymentResponse> GetByIdAsync(Guid id);
    Task<PaymentResponse?> GetByRideIdAsync(Guid rideId);
    Task<PaginatedResponse<PaymentResponse>> GetPassengerPaymentsAsync(Guid passengerId, int page, int pageSize);
    Task<PaginatedResponse<PaymentResponse>> GetAllAsync(int page, int pageSize, PaymentStatus? status = null);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRideRepository _rideRepository;

    public PaymentService(IPaymentRepository paymentRepository, IRideRepository rideRepository)
    {
        _paymentRepository = paymentRepository;
        _rideRepository = rideRepository;
    }

    public async Task<PaymentResponse> CreatePaymentForRideAsync(Guid rideId, decimal amount, string currency)
    {
        var ride = await _rideRepository.GetByIdAsync(rideId)
            ?? throw new KeyNotFoundException($"Ride {rideId} not found");

        var existing = await _paymentRepository.GetByRideIdAsync(rideId);
        if (existing is not null)
            throw new InvalidOperationException("Payment already exists for this ride");

        var payment = new Payment
        {
            RideId = rideId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            TransactionReference = $"TXN-{Guid.NewGuid():N}"[..24].ToUpper()
        };

        await _paymentRepository.CreateAsync(payment);
        return payment.ToResponse();
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found");

        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can be processed");

        // Simulate payment processing (always succeeds for now)
        payment.Status = PaymentStatus.Successful;
        payment.PaidAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment);
        return payment.ToResponse();
    }

    public async Task<PaymentResponse> RefundPaymentAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found");

        if (payment.Status != PaymentStatus.Successful)
            throw new InvalidOperationException("Only successful payments can be refunded");

        payment.Status = PaymentStatus.Refunded;
        await _paymentRepository.UpdateAsync(payment);
        return payment.ToResponse();
    }

    public async Task<PaymentResponse> GetByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found");
        return payment.ToResponse();
    }

    public async Task<PaymentResponse?> GetByRideIdAsync(Guid rideId)
    {
        var payment = await _paymentRepository.GetByRideIdAsync(rideId);
        return payment?.ToResponse();
    }

    public async Task<PaginatedResponse<PaymentResponse>> GetPassengerPaymentsAsync(Guid passengerId, int page, int pageSize)
    {
        var payments = await _paymentRepository.GetByPassengerIdAsync(passengerId, page, pageSize);
        var total = await _paymentRepository.GetCountByPassengerAsync(passengerId);

        return new PaginatedResponse<PaymentResponse>
        {
            Items = payments.Select(p => p.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<PaginatedResponse<PaymentResponse>> GetAllAsync(int page, int pageSize, PaymentStatus? status = null)
    {
        var payments = await _paymentRepository.GetAllAsync(page, pageSize, status);
        var total = await _paymentRepository.GetTotalCountAsync(status);

        return new PaginatedResponse<PaymentResponse>
        {
            Items = payments.Select(p => p.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }
}