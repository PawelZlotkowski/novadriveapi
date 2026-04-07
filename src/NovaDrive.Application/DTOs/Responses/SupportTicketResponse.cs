// Application/DTOs/Responses/SupportTicketResponse.cs
namespace NovaDrive.Application.DTOs.Responses;

public record SupportTicketResponse(
    Guid Id,
    Guid PassengerId,
    string PassengerName,
    Guid? RideId,
    string Subject,
    string Description,
    string Priority,
    string Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    string? AdminNotes
);