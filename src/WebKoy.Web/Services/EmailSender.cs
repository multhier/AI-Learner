using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WebKoy.Web.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}

public class SmtpOptions
{
    public const string Section = "Smtp";
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@localhost";
}

/// <summary>Smtp:Host ayarlıysa SMTP ile gönderir; değilse e-postayı loga yazar (geliştirme).</summary>
public class EmailSender(IOptions<SmtpOptions> options, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.Host))
        {
            logger.LogWarning("SMTP ayarlı değil. E-posta gönderilmedi. Alıcı: {To}, Konu: {Subject}\n{Body}", email, subject, htmlMessage);
            return;
        }

        using var client = new SmtpClient(o.Host, o.Port) { EnableSsl = o.EnableSsl };
        if (!string.IsNullOrEmpty(o.User)) client.Credentials = new NetworkCredential(o.User, o.Password);
        using var msg = new MailMessage(o.From, email, subject, htmlMessage) { IsBodyHtml = true };
        await client.SendMailAsync(msg);
    }
}
