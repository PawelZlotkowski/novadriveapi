// Infrastructure/External/EmailSender.cs
namespace NovaDrive.Infrastructure.External;

using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null);
}

public class EmailSender : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody,
        byte[]? attachment = null, string? attachmentName = null)
    {
        var smtpSettings = _configuration.GetSection("Smtp");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Nova Drive", smtpSettings["From"]));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };

        if (attachment is not null && attachmentName is not null)
        {
            builder.Attachments.Add(attachmentName, attachment, ContentType.Parse("application/pdf"));
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            smtpSettings["Host"],
            int.Parse(smtpSettings["Port"]!),
            MailKit.Security.SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            smtpSettings["Username"],
            smtpSettings["Password"]);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}