using Jetar.Core.Contracts;
using Jetar.Core.Entities;
using Jetar.Core.Enums;
using Jetar.Core.Interfaces;
using Jetar.Core.Options;
using Jetar.Infrastructure.Data;
using Jetar.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jetar.Tests;

/// <summary>Vaqtni qo'lda siljitish uchun.</summary>
public class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

public class FakeNotifications : INotificationService
{
    public List<string> Events { get; } = new();

    public Task NotifyAsync(Guid userId, string title, string body, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task NotifyTransactionAsync(Transaction transaction, string eventKey, CancellationToken ct = default)
    {
        Events.Add(eventKey);
        return Task.CompletedTask;
    }
}

/// <summary>Har bir test uchun izolyatsiya qilingan baza va servislar.</summary>
public sealed class TestHarness : IDisposable
{
    public AppDbContext Db { get; }
    public FakeClock Clock { get; } = new();
    public FakeNotifications Notifications { get; } = new();
    public EscrowService Escrow { get; }
    public ChatService Chat { get; }
    public RatingService Ratings { get; }

    public PlatformOptions Platform { get; } = new()
    {
        CommissionRate = 0.08m,
        AutoReleaseHours = 72,
        UnpaidTransactionTimeoutMinutes = 60
    };

    public TestHarness()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"jetar-{Guid.NewGuid()}")
            .Options;

        Db = new AppDbContext(options);

        Chat = new ChatService(Db, Clock, new NullChatBroadcaster(), Notifications);
        Ratings = new RatingService(Db, Clock);

        Escrow = new EscrowService(
            Db,
            Options.Create(Platform),
            Clock,
            Notifications,
            Chat,
            NullLogger<EscrowService>.Instance);
    }

    public User AddUser(string username)
    {
        var user = new User
        {
            Username = username,
            Phone = $"+9989{Random.Shared.Next(10_000_000, 99_999_999)}",
            PasswordHash = "x",
            CreatedAt = Clock.UtcNow
        };

        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    public Listing AddListing(User seller, decimal price = 850_000, ListingStatus status = ListingStatus.Active)
    {
        var listing = new Listing
        {
            SellerId = seller.Id,
            GameType = GameType.EFootball,
            Title = "eFootball 2025 — Legend akkaunt",
            Description = "Test uchun yaratilgan e'lon tavsifi.",
            Price = price,
            ServerRegion = "ASIA",
            RankLevel = "Dream League",
            Status = status,
            CreatedAt = Clock.UtcNow
        };

        Db.Listings.Add(listing);
        Db.SaveChanges();
        return listing;
    }

    /// <summary>Bitimni "pul escrowda" holatiga olib keladi.</summary>
    public async Task<Transaction> HeldTransactionAsync(User seller, User buyer, decimal price = 850_000)
    {
        var listing = AddListing(seller, price);
        var tx = await Escrow.InitiateAsync(listing.Id, buyer.Id);
        return await Escrow.HoldAsync(tx.Id);
    }

    public void Dispose() => Db.Dispose();
}
