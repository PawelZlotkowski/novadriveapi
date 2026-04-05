// Domain/Models/SupportTicket.cs
namespace NovaDrive.Domain.Models;

using NovaDrive.Domain.Enums;

public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PassengerId { get; set; }
    public Guid? RideId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? AdminNotes { get; set; }

    // Navigation
    public Passenger Passenger { get; set; } = null!;
    public Ride? Ride { get; set; }
}