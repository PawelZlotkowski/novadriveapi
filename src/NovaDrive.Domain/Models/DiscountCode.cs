// Domain/Models/DiscountCode.cs
namespace NovaDrive.Domain.Models;

using NovaDrive.Domain.Enums;

public class DiscountCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumRideValue { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int? MaxUses { get; set; }
    public int TimesUsed { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}