namespace Jetar.Domain.Abstractions;

/// <summary>
/// Bitta tranzaksiya doirasidagi barcha repository'larni jamlaydi va o'zgarishlarni
/// yagona nuqtadan saqlaydi. (Unit of Work pattern.) EF Core'da DbContext'ning o'zi
/// UoW vazifasini bajaradi — bu abstraksiya servislarni Infrastructure'dan ajratadi.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IListingRepository Listings { get; }
    ITransactionRepository Transactions { get; }
    IPaymentRepository Payments { get; }
    IMessageRepository Messages { get; }
    IRatingRepository Ratings { get; }
    IDisputeRepository Disputes { get; }
    IBoostRequestRepository BoostRequests { get; }

    /// <summary>Kuzatilgan barcha o'zgarishlarni bazaga yozadi. O'zgargan qatorlar sonini qaytaradi.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Bir nechta SaveChanges'ni bitta atomik tranzaksiyaga o'raydi.</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
