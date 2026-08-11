namespace Jetar.Core.Entities;

/// <summary>Bitim ichidagi xaridor–sotuvchi yozishmasi (SignalR orqali real-time).</summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid SenderId { get; set; }
    public User? Sender { get; set; }

    public Guid ReceiverId { get; set; }
    public User? Receiver { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? AttachmentUrl { get; set; }

    /// <summary>Tizim tomonidan yozilgan xabar (escrow holati o'zgardi va h.k.).</summary>
    public bool IsSystem { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
