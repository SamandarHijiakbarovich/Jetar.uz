using Jetar.Domain.Abstractions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;
using Jetar.Application.Interfaces;

namespace Jetar.Application.Services;

/// <summary>
/// Bitim ichidagi yozishma. Yangi xabar real vaqtda yetkazilishi uchun
/// <see cref="IChatBroadcaster"/> orqali SignalR hubiga uzatiladi.
/// </summary>
public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IChatBroadcaster _broadcaster;
    private readonly INotificationService _notifications;

    public ChatService(IUnitOfWork uow, IClock clock, IChatBroadcaster broadcaster, INotificationService notifications)
    {
        _uow = uow;
        _clock = clock;
        _broadcaster = broadcaster;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<MessageDto>> GetThreadAsync(Guid transactionId, Guid viewerId, bool isModerator, CancellationToken ct = default)
    {
        var tx = await _uow.Transactions.GetByIdAsync(transactionId, ct)
                 ?? throw AppException.NotFound("Bitim");

        EnsureParticipant(tx, viewerId, isModerator);

        var messages = await _uow.Messages.ListThreadAsync(transactionId, ct);

        return messages.Select(m => m.ToDto()).ToList();
    }

    public async Task<MessageDto> SendAsync(Guid senderId, SendMessageRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            throw new AppException("Xabar bo'sh bo'lishi mumkin emas.", 400, "empty_message");

        var tx = await _uow.Transactions.GetByIdAsync(request.TransactionId, ct)
                 ?? throw AppException.NotFound("Bitim");

        EnsureParticipant(tx, senderId, isModerator: false);

        if (tx.Status is TransactionStatus.Cancelled)
            throw AppException.Conflict("Bekor qilingan bitimda yozishib bo'lmaydi.");

        var message = new Message
        {
            TransactionId = tx.Id,
            SenderId = senderId,
            ReceiverId = tx.BuyerId == senderId ? tx.SellerId : tx.BuyerId,
            Text = request.Text?.Trim() ?? string.Empty,
            AttachmentUrl = request.AttachmentUrl,
            CreatedAt = _clock.UtcNow
        };

        await _uow.Messages.AddAsync(message, ct);
        await _uow.SaveChangesAsync(ct);

        message.Sender = await _uow.Users.GetByIdAsync(senderId, ct);
        var dto = message.ToDto();

        await _broadcaster.BroadcastAsync(tx.Id, dto, ct);
        await _notifications.NotifyAsync(message.ReceiverId, "Yangi xabar",
            $"Bitim {tx.EscrowCode} bo'yicha yangi xabar keldi.", ct);

        return dto;
    }

    public async Task MarkReadAsync(Guid transactionId, Guid viewerId, CancellationToken ct = default)
    {
        var unread = await _uow.Messages.ListUnreadAsync(transactionId, viewerId, ct);

        if (unread.Count == 0) return;

        foreach (var m in unread) m.IsRead = true;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<MessageDto> SendSystemAsync(Guid transactionId, string text, CancellationToken ct = default)
    {
        var tx = await _uow.Transactions.GetByIdAsync(transactionId, ct)
                 ?? throw AppException.NotFound("Bitim");

        var message = new Message
        {
            TransactionId = transactionId,
            SenderId = tx.SellerId,     // FK majburiy; IsSystem bayrog'i UI uchun asosiy belgi.
            ReceiverId = tx.BuyerId,
            Text = text,
            IsSystem = true,
            IsRead = false,
            CreatedAt = _clock.UtcNow
        };

        await _uow.Messages.AddAsync(message, ct);
        await _uow.SaveChangesAsync(ct);

        var dto = new MessageDto(message.Id, transactionId, Guid.Empty, "Jetar",
            text, null, true, false, message.CreatedAt);

        await _broadcaster.BroadcastAsync(transactionId, dto, ct);
        return dto;
    }

    private static void EnsureParticipant(Transaction tx, Guid userId, bool isModerator)
    {
        if (tx.BuyerId != userId && tx.SellerId != userId && !isModerator)
            throw AppException.Forbidden("Bu bitim chatiga kirish huquqingiz yo'q.");
    }
}

/// <summary>
/// Chatni real vaqtda tarqatish. API qatlamida SignalR hubi bilan ulanadi;
/// Infrastructure SignalR'ga bog'lanib qolmasligi uchun shu abstraksiya ishlatiladi.
/// </summary>
public interface IChatBroadcaster
{
    Task BroadcastAsync(Guid transactionId, MessageDto message, CancellationToken ct = default);
}

/// <summary>SignalR ulanmagan muhitlarda (masalan testlarda) ishlatiladi.</summary>
public class NullChatBroadcaster : IChatBroadcaster
{
    public Task BroadcastAsync(Guid transactionId, MessageDto message, CancellationToken ct = default)
        => Task.CompletedTask;
}
