using FluentAssertions;
using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Infrastructure.Services;

namespace Jetar.Tests;

public class RatingServiceTests
{
    [Fact]
    public async Task Baho_faqat_yakunlangan_bitimga_qoldiriladi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var act = () => h.Ratings.CreateAsync(tx.Id, buyer.Id, new CreateRatingRequest(5, "Zo'r"));

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task Baho_ortacha_reytingni_qayta_hisoblaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");

        var tx1 = await h.HeldTransactionAsync(seller, buyer);
        await h.Escrow.ReleaseAsync(tx1.Id, buyer.Id);
        await h.Ratings.CreateAsync(tx1.Id, buyer.Id, new CreateRatingRequest(5, "Tez va ishonchli"));

        var tx2 = await h.HeldTransactionAsync(seller, buyer, 400_000);
        await h.Escrow.ReleaseAsync(tx2.Id, buyer.Id);
        await h.Ratings.CreateAsync(tx2.Id, buyer.Id, new CreateRatingRequest(4, "Yaxshi"));

        var reloaded = (await h.Db.Users.FindAsync(seller.Id))!;
        reloaded.RatingCount.Should().Be(2);
        reloaded.Rating.Should().Be(4.5m);
    }

    [Fact]
    public async Task Bir_bitimga_ikki_marta_baho_qoldirilmaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");

        var tx = await h.HeldTransactionAsync(seller, buyer);
        await h.Escrow.ReleaseAsync(tx.Id, buyer.Id);
        await h.Ratings.CreateAsync(tx.Id, buyer.Id, new CreateRatingRequest(5, null));

        var act = () => h.Ratings.CreateAsync(tx.Id, buyer.Id, new CreateRatingRequest(1, null));

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 409);
    }
}

public class ChatServiceTests
{
    [Fact]
    public async Task Begona_foydalanuvchi_chatni_ocholmaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var stranger = h.AddUser("sardor_pubg");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var act = () => h.Chat.GetThreadAsync(tx.Id, stranger.Id, isModerator: false);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 403);
    }

    [Fact]
    public async Task Moderator_har_qanday_chatni_kora_oladi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var moderator = h.AddUser("jetar_mod");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var thread = await h.Chat.GetThreadAsync(tx.Id, moderator.Id, isModerator: true);

        // Escrow o'zgarishlari tizim xabari sifatida yoziladi.
        thread.Should().NotBeEmpty();
        thread.Should().OnlyContain(m => m.IsSystem);
    }

    [Fact]
    public async Task Xabar_qabul_qiluvchiga_yonaltiriladi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var sent = await h.Chat.SendAsync(seller.Id,
            new SendMessageRequest(tx.Id, "Akkaunt logini: demo@jetar.uz", null));

        sent.SenderUsername.Should().Be("valisher");

        var stored = h.Db.Messages.First(m => !m.IsSystem);
        stored.ReceiverId.Should().Be(buyer.Id);
    }

    [Fact]
    public async Task Bosh_xabar_qabul_qilinmaydi()
    {
        using var h = new TestHarness();
        var seller = h.AddUser("valisher");
        var buyer = h.AddUser("alisher_uz");
        var tx = await h.HeldTransactionAsync(seller, buyer);

        var act = () => h.Chat.SendAsync(buyer.Id, new SendMessageRequest(tx.Id, "   ", null));

        await act.Should().ThrowAsync<AppException>().Where(e => e.Code == "empty_message");
    }

    [Fact]
    public void Telefon_raqami_normallashtiriladi()
    {
        AuthService.NormalizePhone("+998 90 123 45 67").Should().Be("+998901234567");
        AuthService.NormalizePhone("901234567").Should().Be("+998901234567");
        AuthService.NormalizePhone("998901234567").Should().Be("+998901234567");
    }
}
