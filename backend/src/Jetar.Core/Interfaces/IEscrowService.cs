using Jetar.Core.Contracts;
using Jetar.Core.Entities;

namespace Jetar.Core.Interfaces;

/// <summary>
/// Escrow — bitim yadrosi. Arxitektura hujjatidagi 01–11 qadamlarni boshqaradi.
/// Har bir metod holat o'tishini tekshiradi va noto'g'ri o'tishda <see cref="Common.AppException"/> tashlaydi.
/// </summary>
public interface IEscrowService
{
    /// <summary>02 — bitim yaratiladi, escrow kodi generatsiya qilinadi, e'lon rezerv qilinadi.</summary>
    Task<Transaction> InitiateAsync(Guid listingId, Guid buyerId, CancellationToken ct = default);

    /// <summary>04 — to'lov tasdiqlandi, pul BLOKLANADI (status: EscrowHeld).</summary>
    Task<Transaction> HoldAsync(Guid transactionId, CancellationToken ct = default);

    /// <summary>05 — sotuvchi akkaunt ma'lumotlarini yubordi deb belgilaydi.</summary>
    Task<Transaction> MarkCredentialsSentAsync(Guid transactionId, Guid sellerId, CancellationToken ct = default);

    /// <summary>07–08 — xaridor tasdiqlaydi, pul sotuvchiga chiqariladi, komissiya ushlanadi.</summary>
    Task<Transaction> ReleaseAsync(Guid transactionId, Guid buyerId, CancellationToken ct = default);

    /// <summary>Avto-release: xaridor belgilangan muddatda javob bermasa, tizim o'zi chiqaradi.</summary>
    Task<int> ReleaseExpiredAsync(CancellationToken ct = default);

    /// <summary>09 — nizo ochiladi, pul muzlatiladi.</summary>
    Task<Dispute> OpenDisputeAsync(Guid transactionId, Guid userId, OpenDisputeRequest request, CancellationToken ct = default);

    /// <summary>11 — moderator qarori: pul xaridorga qaytariladi yoki sotuvchiga chiqariladi.</summary>
    Task<Transaction> ResolveDisputeAsync(Guid disputeId, Guid moderatorId, ResolveDisputeRequest request, CancellationToken ct = default);

    /// <summary>To'lovgacha bekor qilish (xaridor yoki sotuvchi).</summary>
    Task<Transaction> CancelAsync(Guid transactionId, Guid userId, string? reason, CancellationToken ct = default);

    /// <summary>Pulni xaridorga qaytarish (nizo yoki bekor qilish natijasida).</summary>
    Task<Transaction> RefundAsync(Guid transactionId, string reason, CancellationToken ct = default);

    /// <summary>To'lanmagan bitimlarni muddati o'tgach bekor qiladi.</summary>
    Task<int> CancelStaleUnpaidAsync(CancellationToken ct = default);
}
