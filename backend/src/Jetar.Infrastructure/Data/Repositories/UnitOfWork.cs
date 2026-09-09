using Jetar.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Jetar.Infrastructure.Data.Repositories;

/// <summary>
/// Barcha repository'larni bitta <see cref="AppDbContext"/> atrofida jamlaydi.
/// Repository'lar dangasa (lazy) yaratiladi va bir xil kontekstni bo'lishadi.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db) => _db = db;

    private IUserRepository? _users;
    public IUserRepository Users => _users ??= new UserRepository(_db);

    private IListingRepository? _listings;
    public IListingRepository Listings => _listings ??= new ListingRepository(_db);

    private ITransactionRepository? _transactions;
    public ITransactionRepository Transactions => _transactions ??= new TransactionRepository(_db);

    private IPaymentRepository? _payments;
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_db);

    private IMessageRepository? _messages;
    public IMessageRepository Messages => _messages ??= new MessageRepository(_db);

    private IRatingRepository? _ratings;
    public IRatingRepository Ratings => _ratings ??= new RatingRepository(_db);

    private IDisputeRepository? _disputes;
    public IDisputeRepository Disputes => _disputes ??= new DisputeRepository(_db);

    private IBoostRequestRepository? _boostRequests;
    public IBoostRequestRepository BoostRequests => _boostRequests ??= new BoostRequestRepository(_db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await action(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}
