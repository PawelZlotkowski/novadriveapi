// Domain/Enums/VehicleType.cs
namespace NovaDrive.Domain.Enums;

public enum VehicleType
{
    Standard = 0,   // Sedan — 1.0x multiplier
    Van = 1,        // Cargo/Group — 1.5x multiplier
    Luxury = 2      // Premium — 2.2x multiplier
}