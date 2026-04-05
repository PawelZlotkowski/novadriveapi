// Domain/Models/Payment.cs
namespace NovaDrive.Domain.Models;

using NovaDrive.Domain.Enums;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RideId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    // Navigation
    public Ride Ride { get; set; } = null!;
}