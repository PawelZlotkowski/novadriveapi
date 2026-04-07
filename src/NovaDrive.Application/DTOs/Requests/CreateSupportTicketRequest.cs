// Application/DTOs/Requests/CreateSupportTicketRequest.cs
namespace NovaDrive.Application.DTOs.Requests;

public record CreateSupportTicketRequest(
    Guid? RideId,
    string Subject,
    string Description,
    string Priority   // "Low", "Medium", "High", "Critical"
);