using System.Net.Http.Json;
using Jetar.Domain.Abstractions;
using Jetar.Domain.Entities;
using Jetar.Application.Interfaces;
using Jetar.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jetar.Infrastructure.Services;

/// <summary>
/// Bildirishnomalar. Telegram bot tokeni berilgan bo'lsa xabar yuboradi,
/// aks holda faqat log yozadi — MVP shu holatda ham to'liq ishlaydi.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly TelegramOptions _telegram;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<NotificationService> _log;

    public NotificationService(
        IUnitOfWork uow,
        IOptions<TelegramOptions> telegram,
        IHttpClientFactory http,
        ILogger<NotificationService> log)
    {
        _uow = uow;
        _telegram = telegram.Value;
        _http = http;
        _log = log;
    }

    public async Task NotifyAsync(Guid userId, string title, string body, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null) return;

        _log.LogInformation("Bildirishnoma → {Username}: {Title} — {Body}", user.Username, title, body);

        if (!_telegram.Enabled || string.IsNullOrWhiteSpace(_telegram.BotToken)) return;
        if (!user.NotifyTelegram || user.TelegramChatId is null) return;

        try
        {
            var client = _http.CreateClient(nameof(NotificationService));
            var url = $"https://api.telegram.org/bot{_telegram.BotToken}/sendMessage";

            await client.PostAsJsonAsync(url, new
            {
                chat_id = user.TelegramChatId,
                text = $"*{title}*\n{body}",
                parse_mode = "Markdown"
            }, ct);
        }
        catch (Exception ex)
        {
            // Bildirishnoma yuborilmasligi biznes oqimini to'xtatmaydi.
            _log.LogWarning(ex, "Telegram bildirishnomasi yuborilmadi ({UserId})", userId);
        }
    }

    public async Task NotifyTransactionAsync(Transaction transaction, string eventKey, CancellationToken ct = default)
    {
        var (title, buyerText, sellerText) = Describe(eventKey, transaction);

        if (buyerText != null) await NotifyAsync(transaction.BuyerId, title, buyerText, ct);
        if (sellerText != null) await NotifyAsync(transaction.SellerId, title, sellerText, ct);
    }

    private static (string Title, string? Buyer, string? Seller) Describe(string key, Transaction t) => key switch
    {
        "initiated" => ("Bitim ochildi",
            $"{t.EscrowCode} — to'lovni amalga oshiring.",
            $"{t.EscrowCode} — akkauntingizga xaridor topildi."),

        "escrow_held" => ("Pul escrowda",
            $"{t.EscrowCode} — pul bloklandi, sotuvchidan ma'lumot kutilmoqda.",
            $"{t.EscrowCode} — to'lov kelib tushdi, akkaunt ma'lumotlarini yuboring."),

        "credentials_sent" => ("Akkaunt ma'lumotlari yuborildi",
            $"{t.EscrowCode} — akkauntni tekshirib, tasdiqlang.",
            null),

        "completed" => ("Bitim yakunlandi",
            $"{t.EscrowCode} — bitim muvaffaqiyatli yakunlandi.",
            $"{t.EscrowCode} — {t.SellerPayout:N0} so'm hisobingizga o'tkaziladi."),

        "dispute_opened" => ("Nizo ochildi",
            $"{t.EscrowCode} — nizo ko'rib chiqilmoqda.",
            $"{t.EscrowCode} — nizo ko'rib chiqilmoqda."),

        "refunded" => ("Pul qaytarildi",
            $"{t.EscrowCode} — to'lov sizga qaytarildi.",
            $"{t.EscrowCode} — bitim bekor qilindi, pul xaridorga qaytarildi."),

        "cancelled" => ("Bitim bekor qilindi",
            $"{t.EscrowCode} — bitim bekor qilindi.",
            $"{t.EscrowCode} — bitim bekor qilindi."),

        _ => ("Jetar", null, null)
    };
}
