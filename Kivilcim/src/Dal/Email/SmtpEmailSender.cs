using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace Kivilcim.Dal.Email;

public class EmailOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "noreply@kivilcim.local";
    public string FromName { get; set; } = "Kıvılcım";
    /// <summary>SMTP yokken e-postaların (.eml) yazılacağı klasör.</summary>
    public string PickupDirectory { get; set; } = "App_Data/mail";

    public bool UsesSmtp => !string.IsNullOrWhiteSpace(Host);
}

/// <summary>SMTP ayarlıysa gerçek e-posta yollar; değilse klasöre .eml dosyası yazar.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.From, _options.FromName, Encoding.UTF8),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = htmlMessage,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        message.To.Add(email);

        using var client = new SmtpClient();
        if (_options.UsesSmtp)
        {
            client.Host = _options.Host!;
            client.Port = _options.Port;
            client.EnableSsl = _options.EnableSsl;
            if (!string.IsNullOrEmpty(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }
        }
        else
        {
            var dir = Path.IsPathRooted(_options.PickupDirectory)
                ? _options.PickupDirectory
                : Path.Combine(AppContext.BaseDirectory, _options.PickupDirectory);
            Directory.CreateDirectory(dir);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = dir;
        }

        await client.SendMailAsync(message);
    }
}
