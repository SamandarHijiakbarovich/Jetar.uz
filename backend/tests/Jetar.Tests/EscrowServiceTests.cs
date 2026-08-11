using FluentAssertions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Jetar.Tests;

public class EscrowServiceTests
{
    [Fact]
    public async Task Initiate_bitim_ochadi_va_elonni_rezerv_qiladi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var listing = h.AddListing(seller, 850_000);

        var tx = await h.Escrow.InitiateAsync(listing.Id, buyer.Id);

        tx.Status.Should().Be(TransactionStatus.Initiated);
        tx.EscrowCode.Should().StartWith("JT-");
        tx.Amount.Should().Be(850_000);

        // 8% komissiya — dizaynda ko'rsatilgan stavka.
        tx.CommissionAmount.Should().Be(68_000);
        tx.SellerPayout.Should().Be(782_000);

        (await h.Db.Listings.FindAsync(listing.Id))!.Status.Should().Be(ListingStatus.Reserved);
    }

    [Fact]
    public async Task Initiate_oz_elonini_sotib_olishga_ruxsat_bermaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var listing = h.AddListing(seller);

        var act = () => h.Escrow.InitiateAsync(listing.Id, seller.Id);

        await act.Should().ThrowAsync<AppException>().Where(e => e.Code == "self_purchase");
    }

    [Fact]
    public async Task Initiate_ikkinchi_marta_chaqirilsa_ayni_bitimni_qaytaradi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var listing = h.AddListing(seller);

        var first = await h.Escrow.InitiateAsync(listing.Id, buyer.Id);
        var second = await h.Escrow.InitiateAsync(listing.Id, buyer.Id);

        second.Id.Should().Be(first.Id);
        (await h.Db.Transactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Hold_pulni_bloklaydi_va_avto_release_vaqtini_belgilaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");

        var tx = await h.HeldTransactionAsync(seller, buyer);

        tx.Status.Should().Be(TransactionStatus.EscrowHeld);
        tx.EscrowHeldAt.Should().Be(h.Clock.UtcNow);
        tx.AutoReleaseAt.Should().Be(h.Clock.UtcNow.AddHours(72));
        h.Notifications.Events.Should().Contain("escrow_held");
    }

    [Fact]
    public async Task Release_faqat_xaridor_tomonidan_bajariladi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var act = () => h.Escrow.ReleaseAsync(tx.Id, seller.Id);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 403);
    }

    [Fact]
    public async Task Release_bitimni_yakunlaydi_va_statistikani_yangilaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        await h.Escrow.MarkCredentialsSentAsync(tx.Id, seller.Id);
        var released = await h.Escrow.ReleaseAsync(tx.Id, buyer.Id);

        released.Status.Should().Be(TransactionStatus.Completed);
        released.BuyerConfirmed.Should().BeTrue();
        released.CompletedAt.Should().NotBeNull();

        (await h.Db.Users.FindAsync(seller.Id))!.TotalSales.Should().Be(1);
        (await h.Db.Users.FindAsync(buyer.Id))!.TotalPurchases.Should().Be(1);
        (await h.Db.Listings.FirstAsync()).Status.Should().Be(ListingStatus.Sold);
    }

    [Fact]
    public async Task Release_tolov_qilinmagan_bitimda_ishlamaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var listing = h.AddListing(seller);
        var tx = await h.Escrow.InitiateAsync(listing.Id, buyer.Id);

        var act = () => h.Escrow.ReleaseAsync(tx.Id, buyer.Id);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task ReleaseExpired_muddat_otgach_pulni_avtomatik_chiqaradi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);
        await h.Escrow.MarkCredentialsSentAsync(tx.Id, seller.Id);

        // Muddat hali tugamagan
        h.Clock.Advance(TimeSpan.FromHours(71));
        (await h.Escrow.ReleaseExpiredAsync()).Should().Be(0);

        h.Clock.Advance(TimeSpan.FromHours(2));
        (await h.Escrow.ReleaseExpiredAsync()).Should().Be(1);

        (await h.Db.Transactions.FindAsync(tx.Id))!.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public async Task Nizo_ochilganda_avto_release_toxtaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);
        await h.Escrow.MarkCredentialsSentAsync(tx.Id, seller.Id);

        await h.Escrow.OpenDisputeAsync(tx.Id, buyer.Id,
            new OpenDisputeRequest("Akkaunt ma'lumotlari noto'g'ri — login ishlamayapti.", null));

        var reloaded = (await h.Db.Transactions.FindAsync(tx.Id))!;
        reloaded.Status.Should().Be(TransactionStatus.Disputed);
        reloaded.AutoReleaseAt.Should().BeNull();

        h.Clock.Advance(TimeSpan.FromDays(30));
        (await h.Escrow.ReleaseExpiredAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Nizo_ikki_marta_ochilmaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var request = new OpenDisputeRequest("Skinlar e'londa ko'rsatilganidan kam.", null);
        await h.Escrow.OpenDisputeAsync(tx.Id, buyer.Id, request);

        var act = () => h.Escrow.OpenDisputeAsync(tx.Id, buyer.Id, request);
        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task Nizo_xaridor_foydasiga_hal_qilinsa_pul_qaytariladi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var moderator = h.AddUser("jetar_mod");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var dispute = await h.Escrow.OpenDisputeAsync(tx.Id, buyer.Id,
            new OpenDisputeRequest("Sotuvchi akkauntni qaytarib olishga urinmoqda.", null));

        await h.Escrow.ResolveDisputeAsync(dispute.Id, moderator.Id,
            new ResolveDisputeRequest(FavourBuyer: true, Note: "Dalillar xaridor foydasiga."));

        var reloaded = (await h.Db.Transactions.FindAsync(tx.Id))!;
        reloaded.Status.Should().Be(TransactionStatus.Refunded);

        // E'lon yana sotuvga qaytadi.
        (await h.Db.Listings.FirstAsync()).Status.Should().Be(ListingStatus.Active);
        (await h.Db.Users.FindAsync(seller.Id))!.TotalSales.Should().Be(0);
    }

    [Fact]
    public async Task Nizo_sotuvchi_foydasiga_hal_qilinsa_pul_chiqariladi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var moderator = h.AddUser("jetar_mod");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var dispute = await h.Escrow.OpenDisputeAsync(tx.Id, buyer.Id,
            new OpenDisputeRequest("Akkaunt tavsifga mos emas deb hisoblayman.", null));

        await h.Escrow.ResolveDisputeAsync(dispute.Id, moderator.Id,
            new ResolveDisputeRequest(FavourBuyer: false, Note: "Akkaunt tavsifga mos."));

        (await h.Db.Transactions.FindAsync(tx.Id))!.Status.Should().Be(TransactionStatus.Completed);
        (await h.Db.Users.FindAsync(seller.Id))!.TotalSales.Should().Be(1);
    }

    [Fact]
    public async Task Escrowdagi_bitim_oddiy_bekor_qilinmaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var act = () => h.Escrow.CancelAsync(tx.Id, buyer.Id, "Fikrimdan qaytdim");

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task Tolanmagan_bitim_muddat_otgach_bekor_qilinadi_va_elon_ozod_boladi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var listing = h.AddListing(seller);
        await h.Escrow.InitiateAsync(listing.Id, buyer.Id);

        h.Clock.Advance(TimeSpan.FromMinutes(61));
        (await h.Escrow.CancelStaleUnpaidAsync()).Should().Be(1);

        (await h.Db.Listings.FindAsync(listing.Id))!.Status.Should().Be(ListingStatus.Active);
        (await h.Db.Transactions.FirstAsync()).Status.Should().Be(TransactionStatus.Cancelled);
    }

    [Fact]
    public async Task Komissiya_yaxlitlanadi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");

        // 333 333 * 0.08 = 26 666.64 → 26 667
        var tx = await h.HeldTransactionAsync(seller, buyer, 333_333);

        tx.CommissionAmount.Should().Be(26_667);
        (tx.CommissionAmount + tx.SellerPayout).Should().Be(tx.Amount);
    }
}
