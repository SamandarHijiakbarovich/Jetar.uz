using Jetar.Domain.Abstractions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;
using Jetar.Application.Interfaces;
using Jetar.Application.Options;
using Jetar.Domain.Specifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jetar.Application.Services;

/// <summary>
/// Escrow bitimining barcha holat o'tishlari shu yerda. Boshqa hech qaysi servis
/// <see cref="Transaction.Status"/> ni to'g'ridan-to'g'ri o'zgartirmaydi.
/// </summary>
public class EscrowService : IEscrowService
{
    private readonly IUnitOfWork _uow;
    private readonly PlatformOptions _platform;
    private readonly IClock _clock;
    private readonly INotificationService _notifications;
    private readonly IChatService _chat;
    private readonly ILogger<EscrowService> _log;

    public EscrowService(
        IUnitOfWork uow,
        IOptions<PlatformOptions> platform,
        IClock clock,
        INotificationService notifications,
        IChatService chat,
        ILogger<EscrowService> log)
    {
        _uow = uow;
        _platform = platform.Value;
        _clock = clock;
        _notifications = notifications;
        _chat = chat;
        _log = log;
    }

    // 01–02 ─────────────────────────────────────────────────────────────────
    public async Task<Transaction> InitiateAsync(Guid listingId, Guid buyerId, CancellationToken ct = default)
    {
        var listing = await _uow.Listings.FirstOrDefaultAsync(new ListingWithSellerSpec(listingId), ct)
            ?? throw AppException.NotFound("E'lon");

        if (listing.SellerId == buyerId)
            throw new AppException("O'z e'loningizni sotib ololmaysiz.", 400, "self_purchase");

        var buyer = await _uow.Users.GetByIdAsync(buyerId, ct)
            ?? throw AppException.NotFound("Xaridor");

        if (buyer.IsBlocked)
            throw AppException.Forbidden("Hisobingiz bloklangan.");

        // Bir xaridorda bitta e'lon bo'yicha bir vaqtda faqat bitta ochiq bitim bo'ladi.
        // Bu tekshiruv holat tekshiruvidan oldin turadi: e'lon allaqachon shu bitim
        // tufayli Reserved bo'lgani uchun takroriy so'rov xatoga uchramasligi kerak.
        var existing = await _uow.Transactions.FirstOrDefaultAsync(
            t => t.ListingId == listingId && t.BuyerId == buyerId && OpenStatuses.Contains(t.Status), ct);
        if (existing != null) return existing;

        if (listing.Status != ListingStatus.Active)
            throw AppException.Conflict("Bu e'lon hozir sotuvda emas.");

        var commission = Math.Round(listing.Price * _platform.CommissionRate, 0, MidpointRounding.AwayFromZero);

        var tx = new Transaction
        {
            ListingId = listing.Id,
            BuyerId = buyerId,
            SellerId = listing.SellerId,
            Amount = listing.Price,
            CommissionAmount = commission,
            SellerPayout = listing.Price - commission,
            Status = TransactionStatus.Initiated,
            EscrowCode = GenerateEscrowCode(),
            CreatedAt = _clock.UtcNow
        };

        listing.Status = ListingStatus.Reserved;
        listing.UpdatedAt = _clock.UtcNow;

        await _uow.Transactions.AddAsync(tx, ct);
        await _uow.SaveChangesAsync(ct);

        _log.LogInformation("Escrow bitimi ochildi {Code} listing={ListingId} buyer={BuyerId}",
            tx.EscrowCode, listingId, buyerId);

        await _chat.SendSystemAsync(tx.Id, $"Bitim ochildi. Escrow kodi: {tx.EscrowCode}", ct);
        await _notifications.NotifyTransactionAsync(tx, "initiated", ct);

        return tx;
    }

    // 04 ────────────────────────────────────────────────────────────────────
    public async Task<Transaction> HoldAsync(Guid transactionId, CancellationToken ct = default)
    {
        var tx = await LoadAsync(transactionId, ct);

        if (tx.Status == TransactionStatus.EscrowHeld) return tx;

        Require(tx, TransactionStatus.Initiated, TransactionStatus.AwaitingPayment);

        tx.Status = TransactionStatus.EscrowHeld;
        tx.EscrowHeldAt = _clock.UtcNow;
        tx.AutoReleaseAt = _clock.UtcNow.AddHours(_platform.AutoReleaseHours);

        await _uow.SaveChangesAsync(ct);

        _log.LogInformation("Escrow BLOKLANDI {Code} summa={Amount}", tx.EscrowCode, tx.Amount);

        await _chat.SendSystemAsync(tx.Id,
            "To'lov qabul qilindi — pul Escrow hisobida bloklandi. Sotuvchi akkaunt ma'lumotlarini yuborishi mumkin.", ct);
        await _notifications.NotifyTransactionAsync(tx, "escrow_held", ct);

        return tx;
    }

    // 05 ────────────────────────────────────────────────────────────────────
    public async Task<Transaction> MarkCredentialsSentAsync(Guid transactionId, Guid sellerId, CancellationToken ct = default)
    {
        var tx = await LoadAsync(transactionId, ct);

        if (tx.SellerId != sellerId)
            throw AppException.Forbidden("Faqat sotuvchi bu amalni bajara oladi.");

        Require(tx, TransactionStatus.EscrowHeld);

        tx.Status = TransactionStatus.CredentialsSent;
        tx.SellerConfirmed = true;
        await _uow.SaveChangesAsync(ct);

        await _chat.SendSystemAsync(tx.Id,
            "Sotuvchi akkaunt ma'lumotlarini yubordi. Xaridor tekshirib, tasdiqlashi kerak.", ct);
        await _notifications.NotifyTransactionAsync(tx, "credentials_sent", ct);

        return tx;
    }

    // 07–08 ─────────────────────────────────────────────────────────────────
    public async Task<Transaction> ReleaseAsync(Guid transactionId, Guid buyerId, CancellationToken ct = default)
    {
        var tx = await LoadAsync(transactionId, ct);

        if (tx.BuyerId != buyerId)
            throw AppException.Forbidden("Faqat xaridor bitimni tasdiqlay oladi.");

        Require(tx, TransactionStatus.EscrowHeld, TransactionStatus.CredentialsSent, TransactionStatus.BuyerVerifying);

        await CompleteAsync(tx, "Xaridor tasdiqladi — pul sotuvchiga chiqarildi.", ct);
        return tx;
    }

    public async Task<int> ReleaseExpiredAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        var due = await _uow.Transactions.ListAsync(new DueForAutoReleaseSpec(now), ct);

        foreach (var tx in due)
        {
            await CompleteAsync(tx,
                $"Xaridor {_platform.AutoReleaseHours} soat ichida javob bermadi — pul avtomatik sotuvchiga chiqarildi.", ct);
            _log.LogInformation("Avto-release bajarildi {Code}", tx.EscrowCode);
        }

        return due.Count;
    }

    // 09 ────────────────────────────────────────────────────────────────────
    public async Task<Dispute> OpenDisputeAsync(Guid transactionId, Guid userId, OpenDisputeRequest request, CancellationToken ct = default)
    {
        var tx = await LoadAsync(transactionId, ct);

        if (tx.BuyerId != userId && tx.SellerId != userId)
            throw AppException.Forbidden("Bu bitim sizga tegishli emas.");

        if (tx.Status is TransactionStatus.Completed or TransactionStatus.Refunded or TransactionStatus.Cancelled)
            throw AppException.Conflict("Yakunlangan bitim bo'yicha nizo ocholmaysiz.");

        var already = await _uow.Disputes.FirstOrDefaultAsync(d => d.TransactionId == transactionId, ct);
        if (already != null) throw AppException.Conflict("Bu bitim bo'yicha nizo allaqachon ochilgan.");

        var dispute = new Dispute
        {
            TransactionId = tx.Id,
            OpenedById = userId,
            Reason = request.Reason,
            EvidenceUrls = request.EvidenceUrls ?? new List<string>(),
            Status = DisputeStatus.Open,
            CreatedAt = _clock.UtcNow
        };

        tx.Status = TransactionStatus.Disputed;
        // Nizo davomida avto-release ishlamaydi — pul muzlatiladi.
        tx.AutoReleaseAt = null;

        await _uow.Disputes.AddAsync(dispute, ct);
        await _uow.SaveChangesAsync(ct);

        _log.LogWarning("Nizo ochildi {Code} sabab={Reason}", tx.EscrowCode, request.Reason);

        await _chat.SendSystemAsync(tx.Id, "Nizo ochildi. Pul escrowda muzlatildi, moderator 24 soat ichida ko'rib chiqadi.", ct);
        await _notifications.NotifyTransactionAsync(tx, "dispute_opened", ct);

        return dispute;
    }

    // 11 ────────────────────────────────────────────────────────────────────
    public async Task<Transaction> ResolveDisputeAsync(Guid disputeId, Guid moderatorId, ResolveDisputeRequest request, CancellationToken ct = default)
    {
        var dispute = await _uow.Disputes.FirstOrDefaultAsync(new DisputeWithTransactionSpec(disputeId), ct)
            ?? throw AppException.NotFound("Nizo");

        if (dispute.Status is DisputeStatus.ResolvedForBuyer or DisputeStatus.ResolvedForSeller)
            throw AppException.Conflict("Bu nizo allaqachon hal qilingan.");

        var tx = dispute.Transaction ?? throw AppException.NotFound("Bitim");

        dispute.Status = request.FavourBuyer ? DisputeStatus.ResolvedForBuyer : DisputeStatus.ResolvedForSeller;
        dispute.ResolvedById = moderatorId;
        dispute.ResolutionNote = request.Note;
        dispute.ResolvedAt = _clock.UtcNow;

        if (request.FavourBuyer)
        {
            await ApplyRefundAsync(tx, request.Note ?? "Nizo xaridor foydasiga hal qilindi.", ct);
        }
        else
        {
            await CompleteAsync(tx, request.Note ?? "Nizo sotuvchi foydasiga hal qilindi — pul chiqarildi.", ct);
        }

        await _uow.SaveChangesAsync(ct);
        return tx;
    }

    // Bekor qilish / qaytarish ────────────────────────────────────────────────
    public async Task<Transaction> CancelAsync(Guid transactionId, Guid userId, string? reason, CancellationToken ct = default)
    {
        var tx = await LoadAsync(transactionId, ct);

        if (tx.BuyerId != userId && tx.SellerId != userId)
            throw AppException.Forbidden("Bu bitim sizga tegishli emas.");

        if (tx.Status is TransactionStatus.Completed or TransactionStatus.Refunded or TransactionStatus.Cancelled)
            throw AppException.Conflict("Bitim allaqachon yopilgan.");

        if (tx.Status is TransactionStatus.EscrowHeld or TransactionStatus.CredentialsSent or TransactionStatus.BuyerVerifying)
            throw AppException.Conflict("Pul escrowda — bekor qilish uchun nizo oching yoki sotuvchi bilan kelishing.");

        tx.Status = TransactionStatus.Cancelled;
        tx.CancelledAt = _clock.UtcNow;
        tx.CancelReason = reason;
        ReleaseListing(tx);

        await _uow.SaveChangesAsync(ct);
        await _notifications.NotifyTransactionAsync(tx, "cancelled", ct);

        return tx;
    }

    public async Task<Transaction> RefundAsync(Guid transactionId, string reason, CancellationToken ct = default)
    {
        var tx = await LoadAsync(transactionId, ct);
        await ApplyRefundAsync(tx, reason, ct);
        await _uow.SaveChangesAsync(ct);
        return tx;
    }

    public async Task<int> CancelStaleUnpaidAsync(CancellationToken ct = default)
    {
        var cutoff = _clock.UtcNow.AddMinutes(-_platform.UnpaidTransactionTimeoutMinutes);

        var stale = await _uow.Transactions.ListAsync(new StaleUnpaidSpec(cutoff), ct);

        foreach (var tx in stale)
        {
            tx.Status = TransactionStatus.Cancelled;
            tx.CancelledAt = _clock.UtcNow;
            tx.CancelReason = "To'lov muddati o'tdi.";
            ReleaseListing(tx);
        }

        if (stale.Count > 0) await _uow.SaveChangesAsync(ct);
        return stale.Count;
    }

    // ── Ichki yordamchi metodlar ─────────────────────────────────────────────

    private static readonly TransactionStatus[] OpenStatuses =
    {
        TransactionStatus.Initiated,
        TransactionStatus.AwaitingPayment,
        TransactionStatus.EscrowHeld,
        TransactionStatus.CredentialsSent,
        TransactionStatus.BuyerVerifying,
        TransactionStatus.Disputed
    };

    private async Task<Transaction> LoadAsync(Guid id, CancellationToken ct)
        => await _uow.Transactions.FirstOrDefaultAsync(new TransactionWithDetailsSpec(id), ct)
           ?? throw AppException.NotFound("Bitim");

    private static void Require(Transaction tx, params TransactionStatus[] allowed)
    {
        if (!allowed.Contains(tx.Status))
            throw AppException.Conflict(
                $"Bitim holati '{StatusLabels.Get(tx.Status)}' — bu amalni bajarib bo'lmaydi.");
    }

    /// <summary>08 — pulni sotuvchiga chiqaradi, komissiya ushlanadi, statistikani yangilaydi.</summary>
    private async Task CompleteAsync(Transaction tx, string systemMessage, CancellationToken ct)
    {
        tx.Status = TransactionStatus.Completed;
        tx.BuyerConfirmed = true;
        tx.CompletedAt = _clock.UtcNow;
        tx.AutoReleaseAt = null;

        if (tx.Listing != null)
        {
            tx.Listing.Status = ListingStatus.Sold;
            tx.Listing.SoldAt = _clock.UtcNow;
        }

        var seller = tx.Seller ?? await _uow.Users.GetByIdAsync(tx.SellerId, ct);
        if (seller != null) seller.TotalSales++;

        var buyer = tx.Buyer ?? await _uow.Users.GetByIdAsync(tx.BuyerId, ct);
        if (buyer != null) buyer.TotalPurchases++;

        await _uow.SaveChangesAsync(ct);

        _log.LogInformation("Escrow chiqarildi {Code} payout={Payout} komissiya={Commission}",
            tx.EscrowCode, tx.SellerPayout, tx.CommissionAmount);

        await _chat.SendSystemAsync(tx.Id, systemMessage, ct);
        await _notifications.NotifyTransactionAsync(tx, "completed", ct);
    }

    private async Task ApplyRefundAsync(Transaction tx, string reason, CancellationToken ct)
    {
        if (tx.Status == TransactionStatus.Refunded) return;

        tx.Status = TransactionStatus.Refunded;
        tx.CancelReason = reason;
        tx.CancelledAt = _clock.UtcNow;
        tx.AutoReleaseAt = null;
        ReleaseListing(tx);

        var payments = await _uow.Payments.ListAsync(new PaidPaymentsForTransactionSpec(tx.Id), ct);

        foreach (var p in payments)
        {
            p.Status = PaymentStatus.Refunded;
            p.RefundedAt = _clock.UtcNow;
        }

        await _uow.SaveChangesAsync(ct);

        await _chat.SendSystemAsync(tx.Id, $"Pul xaridorga qaytarildi. {reason}", ct);
        await _notifications.NotifyTransactionAsync(tx, "refunded", ct);
    }

    /// <summary>Bitim yopilganda e'lonni yana sotuvga qaytaradi.</summary>
    private void ReleaseListing(Transaction tx)
    {
        if (tx.Listing is { Status: ListingStatus.Reserved })
        {
            tx.Listing.Status = ListingStatus.Active;
            tx.Listing.UpdatedAt = _clock.UtcNow;
        }
    }

    private static string GenerateEscrowCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var suffix = new char[6];
        for (var i = 0; i < suffix.Length; i++)
            suffix[i] = alphabet[Random.Shared.Next(alphabet.Length)];
        return $"JT-{new string(suffix)}";
    }
}

/// <summary>Holat kodlarini o'zbekcha yorliqqa aylantiradi — UI shu matnlarni ko'rsatadi.</summary>
public static class StatusLabels
{
    public static string Get(TransactionStatus status) => status switch
    {
        TransactionStatus.Initiated => "Ochildi",
        TransactionStatus.AwaitingPayment => "To'lov kutilmoqda",
        TransactionStatus.EscrowHeld => "Escrow",
        TransactionStatus.CredentialsSent => "Ma'lumot yuborildi",
        TransactionStatus.BuyerVerifying => "Tekshirilmoqda",
        TransactionStatus.Completed => "Yakunlandi",
        TransactionStatus.Disputed => "Nizo",
        TransactionStatus.Refunded => "Qaytarildi",
        TransactionStatus.Cancelled => "Bekor qilindi",
        _ => status.ToString()
    };
}
