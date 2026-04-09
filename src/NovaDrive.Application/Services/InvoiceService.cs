// Application/Services/InvoiceService.cs
namespace NovaDrive.Application.Services;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using NovaDrive.Infrastructure.Repositories;
using NovaDrive.Infrastructure.External;

public interface IInvoiceService
{
    Task<byte[]> GenerateInvoicePdfAsync(Guid rideId);
    Task SendInvoiceEmailAsync(Guid rideId);
}

public class InvoiceService : IInvoiceService
{
    private readonly IRideRepository _rideRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IEmailService _emailService;

    public InvoiceService(
        IRideRepository rideRepository,
        IPaymentRepository paymentRepository,
        IPassengerRepository passengerRepository,
        IEmailService emailService)
    {
        _rideRepository = rideRepository;
        _paymentRepository = paymentRepository;
        _passengerRepository = passengerRepository;
        _emailService = emailService;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid rideId)
    {
        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId)
            ?? throw new KeyNotFoundException($"Ride {rideId} not found");

        var payment = await _paymentRepository.GetByRideIdAsync(rideId);
        var passenger = ride.Passenger;

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("NOVA DRIVE").Bold().FontSize(24).FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Autonomous Mobility Invoice").FontSize(12).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    // Invoice info
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Invoice To:").Bold();
                            c.Item().Text($"{passenger.FirstName} {passenger.LastName}");
                            c.Item().Text(passenger.User?.Email ?? "");
                            c.Item().Text(passenger.HomeAddress);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Invoice #: INV-{ride.Id.ToString()[..8].ToUpper()}");
                            c.Item().Text($"Date: {DateTime.UtcNow:yyyy-MM-dd}");
                            c.Item().Text($"Ride Date: {ride.RequestedAt:yyyy-MM-dd HH:mm}");
                        });
                    });

                    col.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Ride details
                    col.Item().Text("Ride Details").Bold().FontSize(14);
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Cell().Text("From:").Bold();
                        table.Cell().Text(ride.DepartureAddress);
                        table.Cell().Text("To:").Bold();
                        table.Cell().Text(ride.DestinationAddress);
                        table.Cell().Text("Distance:").Bold();
                        table.Cell().Text($"{ride.DistanceKm:F1} km");
                        table.Cell().Text("Duration:").Bold();
                        table.Cell().Text($"{ride.DurationMinutes:F0} min");
                        table.Cell().Text("Vehicle:").Bold();
                        table.Cell().Text($"{ride.Vehicle?.Model} ({ride.Vehicle?.LicensePlate})");
                    });

                    col.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Price breakdown
                    col.Item().Text("Price Breakdown").Bold().FontSize(14);
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Cell().Text("Subtotal (excl. VAT):");
                        table.Cell().AlignRight().Text($"€{ride.SubtotalBeforeVat:F2}");
                        table.Cell().Text("VAT (21%):");
                        table.Cell().AlignRight().Text($"€{ride.VatAmount:F2}");

                        table.Cell().PaddingTop(5).Text("Total:").Bold().FontSize(14);
                        table.Cell().PaddingTop(5).AlignRight()
                            .Text($"€{ride.FinalPrice:F2}").Bold().FontSize(14);
                    });

                    if (payment is not null)
                    {
                        col.Item().PaddingTop(10).Text($"Payment Status: {payment.Status}")
                            .FontColor(Colors.Green.Darken2);
                        col.Item().Text($"Transaction Ref: {payment.TransactionReference}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Nova Drive — Autonomous Mobility as a Service | ")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span("Thank you for riding with us!")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task SendInvoiceEmailAsync(Guid rideId)
    {
        var ride = await _rideRepository.GetByIdWithDetailsAsync(rideId)
            ?? throw new KeyNotFoundException($"Ride {rideId} not found");

        var pdf = await GenerateInvoicePdfAsync(rideId);
        var email = ride.Passenger.User?.Email
            ?? throw new InvalidOperationException("Passenger email not found");

        var htmlBody = $@"
            <h2>Your Nova Drive Receipt</h2>
            <p>Hi {ride.Passenger.FirstName},</p>
            <p>Thank you for your ride! Please find your invoice attached.</p>
            <p><strong>From:</strong> {ride.DepartureAddress}<br/>
               <strong>To:</strong> {ride.DestinationAddress}<br/>
               <strong>Total:</strong> €{ride.FinalPrice:F2}</p>
            <p>Safe travels,<br/>The Nova Drive Team</p>";

        await _emailService.SendEmailAsync(
            email,
            $"Nova Drive Invoice - Ride {ride.Id.ToString()[..8].ToUpper()}",
            htmlBody,
            pdf,
            $"NovaDrive-Invoice-{ride.Id.ToString()[..8].ToUpper()}.pdf");
    }
}