using System.Net;
using System.Net.Mail;
using Jetar.Application.Interfaces;
using Jetar.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jetar.Infrastructure.Services;

/// <summary>
/// SMTP orqali email yuboradi (System.Net.Mail — qo'shimcha paket kerak emas).
/// Test rejimida (<see cref="EmailOptions.Enabled"/> = false) haqiqiy email yubormaydi,
/// faqat log qiladi — kod javobda ko'rsatiladi.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IOptions<EmailOptions> opt, ILogger<SmtpEmailSender> log)
    {
        _opt = opt.Value;
        _log = log;
    }

    public bool Enabled => _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.Host);

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!Enabled)
        {
            _log.LogInformation("Email TEST rejimi — yuborilmadi. To={To} Subject={Subject}", toEmail, subject);
            return;
        }

        using var msg = new MailMessage
        {
            From = new MailAddress(_opt.FromEmail, _opt.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(toEmail);

        using var client = new SmtpClient(_opt.Host, _opt.Port)
        {
            EnableSsl = _opt.UseSsl,
            Credentials = new NetworkCredential(_opt.User, _opt.Password)
        };

        await client.SendMailAsync(msg, ct);
        _log.LogInformation("Email yuborildi: {To}", toEmail);
    }
}
