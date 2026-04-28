// Infrastructure/Persistence/DataSeeder.cs
// Populates the database with realistic Warsaw-based mock data.
// Called from Program.cs during development startup.
namespace NovaDrive.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using NovaDrive.Domain.Enums;
using NovaDrive.Domain.Models;

public static class DataSeeder
{
    public static async Task SeedAsync(NovaDriveDbContext db)
    {
        // Only seed if the database is completely empty
        if (await db.Users.AnyAsync()) return;

        // ── Stable IDs so foreign keys can cross-reference ──────────────────
        var userIds     = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        var passengerIds= Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        var vehicleIds  = Enumerable.Range(0, 12).Select(_ => Guid.NewGuid()).ToArray();
        var rideIds     = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray();
        var discountIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();

        // ── Users ────────────────────────────────────────────────────────────
        var users = new[]
        {
            NewUser(userIds[0], "anna.kowalska@novadrive.com",   UserRole.Passenger),
            NewUser(userIds[1], "piotr.nowak@novadrive.com",     UserRole.Passenger),
            NewUser(userIds[2], "maria.wisniewska@novadrive.com",UserRole.Passenger),
            NewUser(userIds[3], "jan.wojcik@novadrive.com",      UserRole.Passenger),
            NewUser(userIds[4], "katarzyna.kowalczyk@novadrive.com", UserRole.Passenger),
            NewUser(userIds[5], "tomasz.kaminski@novadrive.com", UserRole.Passenger),
            NewUser(userIds[6], "agnieszka.lewandowska@novadrive.com", UserRole.Passenger),
            NewUser(userIds[7], "admin@novadrive.com",            UserRole.Admin),
        };
        await db.Users.AddRangeAsync(users);

        // ── Passengers ───────────────────────────────────────────────────────
        var passengers = new[]
        {
            NewPassenger(passengerIds[0], userIds[0], "Anna",      "Kowalska",    "ul. Marszałkowska 12, Warszawa",     320, PaymentMethod.CreditCard),
            NewPassenger(passengerIds[1], userIds[1], "Piotr",     "Nowak",       "ul. Nowy Świat 45, Warszawa",        150, PaymentMethod.PayPal),
            NewPassenger(passengerIds[2], userIds[2], "Maria",     "Wiśniewska",  "ul. Puławska 22, Warszawa",          870, PaymentMethod.CreditCard),
            NewPassenger(passengerIds[3], userIds[3], "Jan",       "Wójcik",      "ul. Grochowska 89, Warszawa",         40, PaymentMethod.BankTransfer),
            NewPassenger(passengerIds[4], userIds[4], "Katarzyna", "Kowalczyk",   "ul. Słowackiego 7, Warszawa",        210, PaymentMethod.PayPal),
            NewPassenger(passengerIds[5], userIds[5], "Tomasz",    "Kamiński",    "ul. Mołdawska 3, Warszawa",          510, PaymentMethod.CreditCard),
            NewPassenger(passengerIds[6], userIds[6], "Agnieszka", "Lewandowska", "ul. Aleje Jerozolimskie 128, Warszawa", 95, PaymentMethod.CreditCard),
        };
        await db.Passengers.AddRangeAsync(passengers);

        // ── Vehicles (Kortrijk, Belgium area) ───────────────────────────────
        var vehicles = new[]
        {
            // Standard
            NewVehicle(vehicleIds[0],  "WD1ND00001", "1-ABC-001", "Tesla Model 3",        VehicleType.Standard, 2022, true,  50.8282, 3.2523, 88.0, 14200), // city centre
            NewVehicle(vehicleIds[1],  "WD1ND00002", "1-ABC-002", "BMW i3",               VehicleType.Standard, 2021, true,  50.8238, 3.2645, 72.5, 28700), // station
            NewVehicle(vehicleIds[2],  "WD1ND00003", "1-ABC-003", "Renault Zoe",          VehicleType.Standard, 2020, true,  50.8072, 3.2634, 55.0, 41300), // Hoog Kortrijk
            NewVehicle(vehicleIds[3],  "WD1ND00004", "1-ABC-004", "Volkswagen ID.3",      VehicleType.Standard, 2023, true,  50.8533, 3.2542, 91.0,  8900), // Gullegem
            NewVehicle(vehicleIds[4],  "WD1ND00005", "1-ABC-005", "Nissan Leaf",          VehicleType.Standard, 2021, false, 50.8597, 3.2229, 30.0, 55100), // Heule
            // Vans
            NewVehicle(vehicleIds[5],  "WD1ND00006", "1-VAN-001", "Ford E-Transit",       VehicleType.Van,      2023, true,  50.8218, 3.2458, 68.0, 12600), // Howest campus
            NewVehicle(vehicleIds[6],  "WD1ND00007", "1-VAN-002", "Mercedes eVito",       VehicleType.Van,      2022, true,  50.8535, 3.2842, 45.0, 22300), // Kuurne
            NewVehicle(vehicleIds[7],  "WD1ND00008", "1-VAN-003", "Volkswagen ID. Buzz",  VehicleType.Van,      2024, true,  50.8052, 3.1791, 95.0,  3100), // Wevelgem
            // Luxury
            NewVehicle(vehicleIds[8],  "WD1ND00009", "1-LUX-001", "Tesla Model S",        VehicleType.Luxury,   2023, true,  50.8278, 3.2642, 82.0,  9800), // Markt
            NewVehicle(vehicleIds[9],  "WD1ND00010", "1-LUX-002", "BMW iX",               VehicleType.Luxury,   2023, true,  50.8390, 3.2350, 77.0, 18400), // north Kortrijk
            NewVehicle(vehicleIds[10], "WD1ND00011", "1-LUX-003", "Mercedes EQS",         VehicleType.Luxury,   2024, false, 50.8100, 3.2450, 60.0, 11500), // south Kortrijk
            NewVehicle(vehicleIds[11], "WD1ND00012", "1-LUX-004", "Audi e-tron GT",       VehicleType.Luxury,   2022, true,  50.8510, 3.2750, 89.0, 21700), // northeast
        };
        await db.Vehicles.AddRangeAsync(vehicles);

        // ── Discount Codes ───────────────────────────────────────────────────
        var discountCodes = new[]
        {
            NewDiscount(discountIds[0], "WELCOME20",  DiscountType.Percentage, 20m,  0m,     DateTime.UtcNow.AddMonths(6),  500, 0),
            NewDiscount(discountIds[1], "FLATFIVE",   DiscountType.Flat,        5m, 20m,     DateTime.UtcNow.AddMonths(3),  200, 47),
            NewDiscount(discountIds[2], "SUMMER2025", DiscountType.Percentage, 15m, 30m,     DateTime.UtcNow.AddMonths(2), 1000, 132),
            NewDiscount(discountIds[3], "EXPIRED10",  DiscountType.Percentage, 10m,  0m,     DateTime.UtcNow.AddDays(-30),   null, 88, isActive: false),
        };
        await db.DiscountCodes.AddRangeAsync(discountCodes);

        // ── Rides (Kortrijk, Belgium area) ───────────────────────────────────
        var rides = new List<Ride>
        {
            // Completed rides
            CompletedRide(rideIds[0],  passengerIds[0], vehicleIds[0],  "Howest, Graaf Karel de Goedelaan 5",  50.8218, 3.2458, "Station Kortrijk, Stationsplein 4",    50.8238, 3.2645, DateTime.UtcNow.AddDays(-1),   2.4m, 10m, 14.50m),
            CompletedRide(rideIds[1],  passengerIds[1], vehicleIds[8],  "Markt, Kortrijk",                     50.8278, 3.2642, "Hoog Kortrijk Shopping",               50.8072, 3.2634, DateTime.UtcNow.AddDays(-2),   3.1m, 12m, 18.20m),
            CompletedRide(rideIds[2],  passengerIds[2], vehicleIds[1],  "Station Kortrijk",                    50.8238, 3.2645, "AZ Groeninge, President Kennedylaan",  50.8195, 3.2743, DateTime.UtcNow.AddDays(-2),   1.8m,  8m, 12.80m),
            CompletedRide(rideIds[3],  passengerIds[3], vehicleIds[5],  "Kuurne, Brugsesteenweg 60",           50.8535, 3.2842, "Kortrijk city centre",                 50.8282, 3.2523, DateTime.UtcNow.AddDays(-3),   4.5m, 16m, 22.40m),
            CompletedRide(rideIds[4],  passengerIds[4], vehicleIds[9],  "Wevelgem, Vanackerestraat 12",        50.8052, 3.1791, "Kortrijk Station",                     50.8238, 3.2645, DateTime.UtcNow.AddDays(-4),   8.2m, 18m, 38.90m),
            CompletedRide(rideIds[5],  passengerIds[5], vehicleIds[2],  "Heule, Doorniksesteenweg 1",          50.8597, 3.2229, "Howest, Kortrijk",                     50.8218, 3.2458, DateTime.UtcNow.AddDays(-5),   5.3m, 14m, 27.60m),
            CompletedRide(rideIds[6],  passengerIds[0], vehicleIds[11], "Hotel Focus, Doorniksesteenweg",      50.8190, 3.2580, "Kortrijk Expo, Doorniksesteenweg 216", 50.8101, 3.2551, DateTime.UtcNow.AddDays(-6),   3.8m, 11m, 42.00m),
            CompletedRide(rideIds[7],  passengerIds[6], vehicleIds[3],  "Gullegem, Gentsesteenweg 5",          50.8533, 3.2542, "AZ Groeninge Hospital",                50.8195, 3.2743, DateTime.UtcNow.AddDays(-7),   6.1m, 20m, 31.50m),
            CompletedRide(rideIds[8],  passengerIds[1], vehicleIds[0],  "Kortrijk Station",                    50.8238, 3.2645, "Howest, Graaf Karel de Goedelaan 5",   50.8218, 3.2458, DateTime.UtcNow.AddDays(-8),   2.1m,  9m, 13.00m),
            CompletedRide(rideIds[9],  passengerIds[2], vehicleIds[6],  "Markt, Kortrijk",                     50.8278, 3.2642, "Kuurne, Brugsesteenweg 60",            50.8535, 3.2842, DateTime.UtcNow.AddDays(-9),   4.9m, 15m, 24.10m),
            CompletedRide(rideIds[10], passengerIds[3], vehicleIds[8],  "Wevelgem Station",                    50.8052, 3.1791, "Kortrijk city centre",                 50.8282, 3.2523, DateTime.UtcNow.AddDays(-10),  7.4m, 22m, 41.50m),
            CompletedRide(rideIds[11], passengerIds[4], vehicleIds[1],  "Heule, Stationsstraat 14",            50.8597, 3.2229, "Markt, Kortrijk",                      50.8278, 3.2642, DateTime.UtcNow.AddDays(-11),  4.7m, 16m, 23.80m),
            // Cancelled
            CancelledRide(rideIds[12], passengerIds[5], "Howest, Kortrijk",          50.8218, 3.2458, "Wevelgem, Vanackerestraat 12",  50.8052, 3.1791, DateTime.UtcNow.AddDays(-3)),
            CancelledRide(rideIds[13], passengerIds[6], "Gullegem, Gentsesteenweg 5",50.8533, 3.2542, "Kortrijk Station",              50.8238, 3.2645, DateTime.UtcNow.AddDays(-5)),
            // In progress / requested
            RequestedRide(rideIds[14], passengerIds[0], "Howest, Graaf Karel de Goedelaan 5",50.8218, 3.2458, "Markt, Kortrijk",              50.8278, 3.2642),
            RequestedRide(rideIds[15], passengerIds[1], "Kuurne, Brugsesteenweg 60",         50.8535, 3.2842, "Kortrijk Station",             50.8238, 3.2645),
            EnRouteRide(rideIds[16],   passengerIds[2], vehicleIds[7], "Station Kortrijk",    50.8238, 3.2645, "Wevelgem, Vanackerestraat 12", 50.8052, 3.1791),
            EnRouteRide(rideIds[17],   passengerIds[3], vehicleIds[3], "Heule, Stationsstraat",50.8597, 3.2229, "Kortrijk Expo",               50.8101, 3.2551),
            RequestedRide(rideIds[18], passengerIds[4], "Gullegem, Gentsesteenweg 5", 50.8533, 3.2542, "AZ Groeninge Hospital",        50.8195, 3.2743),
            RequestedRide(rideIds[19], passengerIds[5], "Markt, Kortrijk",            50.8278, 3.2642, "Howest, Graaf Karel de Goedelaan 5", 50.8218, 3.2458),
        };
        await db.Set<Ride>().AddRangeAsync(rides);

        // ── Maintenance Logs ──────────────────────────────────────────────────
        var maintenance = new[]
        {
            NewMaintenance(vehicleIds[4],  DateTime.UtcNow.AddDays(-45), "Full service + tyre rotation",          "Marek Kowalski",  450.00m, 60000),
            NewMaintenance(vehicleIds[10], DateTime.UtcNow.AddDays(-20), "Battery health check + software update", "Jan Nowak",       180.00m, 15000),
            NewMaintenance(vehicleIds[2],  DateTime.UtcNow.AddDays(-10), "Brake pad replacement",                  "Piotr Zając",     320.00m, 50000),
            NewMaintenance(vehicleIds[0],  DateTime.UtcNow.AddDays(-5),  "Annual inspection + wiper blades",       "Marek Kowalski",  210.00m, 30000),
            NewMaintenance(vehicleIds[5],  DateTime.UtcNow.AddDays(-60), "Suspension check + wheel alignment",     "Adam Wróbel",     560.00m, 25000),
            NewMaintenance(vehicleIds[8],  DateTime.UtcNow.AddDays(-90), "Full service + cabin filter",            "Jan Nowak",       390.00m, 20000),
        };
        await db.MaintenanceLogs.AddRangeAsync(maintenance);

        // ── Support Tickets ───────────────────────────────────────────────────
        var tickets = new[]
        {
            NewTicket(passengerIds[0], rideIds[6],  "Incorrect charge on invoice",  "I was charged €88 but the app showed €75 before booking.", TicketPriority.High,   TicketStatus.Open),
            NewTicket(passengerIds[1], rideIds[1],  "Driver took wrong route",      "Driver went via Praga adding 10 min to the trip.",           TicketPriority.Medium, TicketStatus.InProgress, "Investigating GPS logs"),
            NewTicket(passengerIds[2], null,        "App crash on booking screen",  "App crashes when I try to book a Luxury vehicle.",           TicketPriority.High,   TicketStatus.Open),
            NewTicket(passengerIds[3], rideIds[3],  "Left item in vehicle",         "I left my laptop bag — licence WI 67890.",                   TicketPriority.Critical,TicketStatus.InProgress, "Contacted driver, arranging return"),
            NewTicket(passengerIds[4], rideIds[4],  "Vehicle was not clean",        "Interior had visible dirt on seats.",                         TicketPriority.Low,    TicketStatus.Resolved, "Apology sent, 50 loyalty points added"),
            NewTicket(passengerIds[5], null,        "Discount code WELCOME20 not applied", "Used WELCOME20 but full price was charged.",           TicketPriority.Medium, TicketStatus.Closed, "Refund processed"),
        };
        await db.SupportTickets.AddRangeAsync(tickets);

        // ── Payments (one per completed ride) ────────────────────────────────
        var completedRides = rides.Where(r => r.Status == RideStatus.Completed).ToArray();
        var payments = completedRides.Select((r, i) => new Payment
        {
            Id = Guid.NewGuid(),
            RideId = r.Id,
            Amount = r.FinalPrice ?? 0,
            Currency = "EUR",
            Status = PaymentStatus.Successful,
            TransactionReference = $"TXN-{i + 1:D4}-{r.Id.ToString()[..8].ToUpper()}",
            CreatedAt = r.CompletedAt ?? r.RequestedAt,
            PaidAt = r.CompletedAt,
        }).ToArray();
        await db.Payments.AddRangeAsync(payments);

        await db.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User NewUser(Guid id, string email, UserRole role) => new()
    {
        Id = id, Email = email, Role = role, IsActive = true,
        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 180)),
    };

    private static Passenger NewPassenger(Guid id, Guid userId, string first, string last, string address, int points, PaymentMethod pm) => new()
    {
        Id = id, UserId = userId, FirstName = first, LastName = last,
        HomeAddress = address, LoyaltyPoints = points, PreferredPaymentMethod = pm,
        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 180)),
        UpdatedAt = DateTime.UtcNow,
    };

    private static Vehicle NewVehicle(Guid id, string vin, string plate, string model, VehicleType type, int year, bool active, double lat, double lng, double battery, int mileage) => new()
    {
        Id = id, VIN = vin, LicensePlate = plate, Model = model,
        VehicleType = type, YearOfManufacture = year, IsActive = active,
        CurrentLatitude = lat, CurrentLongitude = lng,
        CurrentBatteryPercentage = battery, CurrentMileage = mileage,
        CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(60, 400)),
        UpdatedAt = DateTime.UtcNow,
    };

    private static DiscountCode NewDiscount(Guid id, string code, DiscountType type, decimal value, decimal minRide, DateTime expires, int? maxUses, int used, bool isActive = true) => new()
    {
        Id = id, Code = code, Type = type, Value = value,
        MinimumRideValue = minRide, ExpiresAt = expires,
        MaxUses = maxUses, TimesUsed = used, IsActive = isActive,
        CreatedAt = DateTime.UtcNow.AddMonths(-3),
    };

    private static Ride CompletedRide(Guid id, Guid passengerId, Guid vehicleId, string depAddr, double depLat, double depLng, string destAddr, double destLat, double destLng, DateTime requestedAt, decimal distKm, decimal durMin, decimal price) => new()
    {
        Id = id, PassengerId = passengerId, VehicleId = vehicleId,
        DepartureAddress = depAddr, DepartureLatitude = depLat, DepartureLongitude = depLng,
        DestinationAddress = destAddr, DestinationLatitude = destLat, DestinationLongitude = destLng,
        Status = RideStatus.Completed,
        RequestedAt = requestedAt,
        StartedAt   = requestedAt.AddMinutes(4),
        CompletedAt = requestedAt.AddMinutes(4 + (double)durMin),
        DistanceKm  = distKm, DurationMinutes = durMin,
        FinalPrice  = price,
        VatAmount   = Math.Round(price / 1.21m * 0.21m, 2),
        SubtotalBeforeVat = Math.Round(price / 1.21m, 2),
    };

    private static Ride CancelledRide(Guid id, Guid passengerId, string depAddr, double depLat, double depLng, string destAddr, double destLat, double destLng, DateTime requestedAt) => new()
    {
        Id = id, PassengerId = passengerId,
        DepartureAddress = depAddr, DepartureLatitude = depLat, DepartureLongitude = depLng,
        DestinationAddress = destAddr, DestinationLatitude = destLat, DestinationLongitude = destLng,
        Status = RideStatus.Cancelled, RequestedAt = requestedAt,
    };

    private static Ride RequestedRide(Guid id, Guid passengerId, string depAddr, double depLat, double depLng, string destAddr, double destLat, double destLng) => new()
    {
        Id = id, PassengerId = passengerId,
        DepartureAddress = depAddr, DepartureLatitude = depLat, DepartureLongitude = depLng,
        DestinationAddress = destAddr, DestinationLatitude = destLat, DestinationLongitude = destLng,
        Status = RideStatus.Requested, RequestedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(2, 15)),
    };

    private static Ride EnRouteRide(Guid id, Guid passengerId, Guid vehicleId, string depAddr, double depLat, double depLng, string destAddr, double destLat, double destLng) => new()
    {
        Id = id, PassengerId = passengerId, VehicleId = vehicleId,
        DepartureAddress = depAddr, DepartureLatitude = depLat, DepartureLongitude = depLng,
        DestinationAddress = destAddr, DestinationLatitude = destLat, DestinationLongitude = destLng,
        Status = RideStatus.EnRoute,
        RequestedAt = DateTime.UtcNow.AddMinutes(-10),
        StartedAt   = DateTime.UtcNow.AddMinutes(-6),
    };

    private static MaintenanceLog NewMaintenance(Guid vehicleId, DateTime date, string desc, string tech, decimal cost, int nextMileage) => new()
    {
        Id = Guid.NewGuid(), VehicleId = vehicleId,
        ServiceDate = date, Description = desc, TechnicianName = tech,
        Cost = cost, NextServiceMileage = nextMileage,
        CreatedAt = date,
    };

    private static SupportTicket NewTicket(Guid passengerId, Guid? rideId, string subject, string desc, TicketPriority priority, TicketStatus status, string? adminNotes = null) => new()
    {
        Id = Guid.NewGuid(), PassengerId = passengerId, RideId = rideId,
        Subject = subject, Description = desc, Priority = priority, Status = status,
        AdminNotes = adminNotes,
        CreatedAt  = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 14)),
        ResolvedAt = status is TicketStatus.Resolved or TicketStatus.Closed ? DateTime.UtcNow.AddDays(-1) : null,
    };
}
