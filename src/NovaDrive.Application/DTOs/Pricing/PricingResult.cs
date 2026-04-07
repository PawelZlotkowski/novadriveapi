// Application/DTOs/Pricing/PricingResult.cs
namespace NovaDrive.Application.DTOs.Pricing;

public class PricingResult
{
    public decimal BasePrice { get; set; }
    public decimal VehicleMultiplier { get; set; }
    public decimal AfterMultiplier { get; set; }
    public bool IsNightRate { get; set; }
    public decimal NightSurcharge { get; set; }
    public decimal SubtotalBeforeDiscounts { get; set; }
    public decimal LoyaltyDiscount { get; set; }
    public int LoyaltyPointsUsed { get; set; }
    public decimal CodeDiscount { get; set; }
    public string? CodeApplied { get; set; }
    public decimal SubtotalBeforeVat { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal FinalPrice { get; set; }
}