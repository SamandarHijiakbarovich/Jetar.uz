namespace Jetar.Application.Contracts;

public record SendMessageRequest(Guid TransactionId, string Text, string? AttachmentUrl);

public record MessageDto(
    Guid Id,
    Guid TransactionId,
    Guid SenderId,
    string SenderUsername,
    string Text,
    string? AttachmentUrl,
    bool IsSystem,
    bool IsRead,
    DateTimeOffset CreatedAt);
