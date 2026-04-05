// Domain/Models/User.cs
namespace NovaDrive.Domain.Models;

using NovaDrive.Domain.Enums;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Passenger;
    public string? Auth0Id { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Passenger? Passenger { get; set; }
}