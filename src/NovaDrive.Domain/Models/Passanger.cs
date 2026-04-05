// Domain/Models/Passenger.cs
namespace NovaDrive.Domain.Models;

using NovaDrive.Domain.Enums;

public class Passenger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string HomeAddress { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; } = 0;
    public PaymentMethod PreferredPaymentMethod { get; set; } = PaymentMethod.CreditCard;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Ride> Rides { get; set; } = new List<Ride>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
}