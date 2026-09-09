using Jetar.Domain.Entities;
using Jetar.Domain.Enums;

namespace Jetar.Domain.Abstractions;

// Aggregate'ga xos repository interfeyslari. Maxsus so'rov metodlari shu yerga
// qo'shiladi; aks holda generic IRepository<T> amallarini meros oladi.

public interface IUserRepository : IRepository<User> { }

public interface IListingRepository : IRepository<Listing>
{
    /// <summary>Aktiv e'lonlar soni — o'yin turi bo'yicha guruhlangan.</summary>
    Task<IReadOnlyList<(GameType Game, int Count)>> CountActiveByGameAsync(CancellationToken ct = default);

    /// <summary>Aktiv e'lonlar soni — e'lon turi bo'yicha guruhlangan.</summary>
    Task<IReadOnlyList<(ListingType Type, int Count)>> CountActiveByTypeAsync(CancellationToken ct = default);
}

public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>Shartga mos bitimlar bo'yicha ushlangan komissiya yig'indisi.</summary>
    Task<decimal> SumCommissionAsync(System.Linq.Expressions.Expression<Func<Transaction, bool>> predicate, CancellationToken ct = default);

    /// <summary>Sotuvchi yakunlangan bitimlardan jami ishlab topgan summasi (payout).</summary>
    Task<decimal> TotalEarnedAsync(Guid sellerId, CancellationToken ct = default);
}

public interface IPaymentRepository : IRepository<Payment> { }

public interface IMessageRepository : IRepository<Message>
{
    /// <summary>Bitim yozishmasi — jo'natuvchisi bilan, vaqt bo'yicha o'sish tartibida.</summary>
    Task<IReadOnlyList<Message>> ListThreadAsync(Guid transactionId, CancellationToken ct = default);

    /// <summary>Qabul qiluvchining o'qilmagan xabarlari (belgilash uchun, kuzatiladi).</summary>
    Task<IReadOnlyList<Message>> ListUnreadAsync(Guid transactionId, Guid receiverId, CancellationToken ct = default);
}

public interface IRatingRepository : IRepository<Rating>
{
    /// <summary>Foydalanuvchi olgan baholar — beruvchisi bilan, eng yangisidan.</summary>
    Task<IReadOnlyList<Rating>> ListForUserAsync(Guid userId, int limit, CancellationToken ct = default);

    /// <summary>Foydalanuvchi olgan barcha ballar (o'rtacha reytingni qayta hisoblash uchun).</summary>
    Task<IReadOnlyList<int>> ScoresForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Berilgan bitimlar ichidan foydalanuvchi baho qoldirganlarining id'lari.</summary>
    Task<IReadOnlyList<Guid>> RatedTransactionIdsAsync(IReadOnlyCollection<Guid> transactionIds, Guid fromUserId, CancellationToken ct = default);
}

public interface IDisputeRepository : IRepository<Dispute> { }

public interface IBoostRequestRepository : IRepository<BoostRequest>
{
    /// <summary>Tasdiqlangan boost so'rovlari bo'yicha jami to'lov summasi (ixtiyoriy sanadan boshlab).</summary>
    Task<decimal> SumApprovedAmountAsync(DateTimeOffset? since = null, CancellationToken ct = default);
}
