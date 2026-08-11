using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Domain.Enums;

namespace Jetar.Tests.Integration;

/// <summary>Arxitektura hujjatidagi 01–08 qadamlarni haqiqiy API orqali tekshiradi.</summary>
public class EscrowFlowTests : IClassFixture<JetarWebFactory>
{
    private readonly JetarWebFactory _factory;
    private static readonly System.Text.Json.JsonSerializerOptions Json = JetarWebFactory.Json;

    public EscrowFlowTests(JetarWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_ishlaydi()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Elonlar_royxati_avtorizatsiyasiz_ochiladi()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/listings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Himoyalangan_endpoint_401_qaytaradi()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bir_xil_username_bilan_ikki_marta_royxatdan_otilmaydi()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest("Takror", "Sinovchi", "takror_user", "+998911110001", "jetar123", null);

        (await client.PostAsJsonAsync("/api/auth/register", request, Json)).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register",
            request with { Phone = "+998911110002" }, Json);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Nogri_parol_bilan_kirish_401()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Parol", "Sinovchi", "parol_test", "+998911110003", "jetar123", null), Json);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("parol_test", "notogri"), Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Toliq_escrow_oqimi_elondan_yakunlashgacha()
    {
        var (sellerClient, seller) = await _factory.CreateAuthenticatedClientAsync("sotuvchi_e2e", "+998911112201");
        var (buyerClient, buyer) = await _factory.CreateAuthenticatedClientAsync("xaridor_e2e", "+998911112202");

        // 1. Sotuvchi e'lon joylashtiradi
        var createResponse = await sellerClient.PostAsJsonAsync("/api/listings", new CreateListingRequest(
            GameType.EFootball,
            ListingType.Account,
            "eFootball 2025 — Legend akkaunt, 250+ o'yinchi",
            "Legend darajali akkaunt, Konami ID orqali bog'langan, pochta to'liq beriladi.",
            850_000,
            "ASIA",
            "Dream League",
            new List<string> { "GP", "Coins" },
            null,
            new Dictionary<string, string> { ["GP"] = "1.2M" }), Json);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var listing = await createResponse.Content.ReadFromJsonAsync<ListingDetailDto>(Json);
        listing!.Status.Should().Be(ListingStatus.Active);

        // 2. E'lon ommaviy ro'yxatda ko'rinadi
        var search = await _factory.CreateClient()
            .GetFromJsonAsync<PagedResult<ListingCardDto>>("/api/listings?game=efootball", Json);
        search!.Items.Should().Contain(l => l.Id == listing.Id);

        // 3. Xaridor bitim ochadi (02-qadam)
        var initiate = await buyerClient.PostAsJsonAsync("/api/transactions/initiate",
            new InitiateTransactionRequest(listing.Id), Json);
        initiate.EnsureSuccessStatusCode();

        var tx = await initiate.Content.ReadFromJsonAsync<TransactionDto>(Json);
        tx!.Status.Should().Be(TransactionStatus.Initiated);
        tx.CommissionAmount.Should().Be(68_000);
        tx.SellerPayout.Should().Be(782_000);
        tx.ViewerIsBuyer.Should().BeTrue();

        // 4. To'lov (03-qadam) — sandboxda darhol tasdiqlanadi
        var payResponse = await buyerClient.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest(tx.Id, PaymentMethod.Click), Json);
        payResponse.EnsureSuccessStatusCode();

        var payment = await payResponse.Content.ReadFromJsonAsync<PaymentDto>(Json);
        payment!.Status.Should().Be(PaymentStatus.Pending);
        payment.CheckoutUrl.Should().NotBeNullOrEmpty();

        var confirm = await buyerClient.PostAsJsonAsync("/api/payments/sandbox/confirm",
            new SandboxConfirmRequest(payment.ProviderPaymentId!, true), Json);
        confirm.EnsureSuccessStatusCode();

        // 5. Pul escrowda bloklandi (04-qadam)
        var afterPayment = await buyerClient.GetFromJsonAsync<TransactionDto>($"/api/transactions/{tx.Id}", Json);
        afterPayment!.Status.Should().Be(TransactionStatus.EscrowHeld);
        afterPayment.AutoReleaseAt.Should().NotBeNull();

        // 6. Sotuvchi akkaunt ma'lumotlarini yuboradi (05-qadam)
        var chatResponse = await sellerClient.PostAsJsonAsync("/api/chat/send",
            new SendMessageRequest(tx.Id, "Login: demo@jetar.uz / parol xabarda", null), Json);
        chatResponse.EnsureSuccessStatusCode();

        var credentials = await sellerClient.PostAsync($"/api/transactions/{tx.Id}/credentials-sent", null);
        credentials.EnsureSuccessStatusCode();

        // 7. Xaridor tasdiqlaydi — pul chiqariladi (07–08-qadam)
        var release = await buyerClient.PostAsync($"/api/transactions/{tx.Id}/release", null);
        release.EnsureSuccessStatusCode();

        var completed = await release.Content.ReadFromJsonAsync<TransactionDto>(Json);
        completed!.Status.Should().Be(TransactionStatus.Completed);
        completed.CompletedAt.Should().NotBeNull();

        // 8. Baho qoldiriladi va sotuvchi reytingi yangilanadi
        var rate = await buyerClient.PostAsJsonAsync($"/api/transactions/{tx.Id}/rating",
            new CreateRatingRequest(5, "Tez va ishonchli!"), Json);
        rate.EnsureSuccessStatusCode();

        var profile = await _factory.CreateClient().GetAsync($"/api/users/{seller.Username}");
        profile.StatusCode.Should().Be(HttpStatusCode.OK);

        // 9. Chat tarixida tizim xabarlari va sotuvchi xabari bor
        var thread = await buyerClient.GetFromJsonAsync<List<MessageDto>>($"/api/chat/{tx.Id}", Json);
        thread!.Should().Contain(m => m.IsSystem);
        thread.Should().Contain(m => !m.IsSystem && m.SenderId == seller.Id);

        buyer.Username.Should().Be("xaridor_e2e");
    }

    [Fact]
    public async Task Begona_foydalanuvchi_bitimni_kora_olmaydi()
    {
        var (sellerClient, _) = await _factory.CreateAuthenticatedClientAsync("sotuvchi_403", "+998911112301");
        var (buyerClient, _) = await _factory.CreateAuthenticatedClientAsync("xaridor_403", "+998911112302");
        var (strangerClient, _) = await _factory.CreateAuthenticatedClientAsync("begona_403", "+998911112303");

        var listing = await CreateListingAsync(sellerClient, "Begona test uchun akkaunt — Legend daraja");

        var initiate = await buyerClient.PostAsJsonAsync("/api/transactions/initiate",
            new InitiateTransactionRequest(listing.Id), Json);
        var tx = await initiate.Content.ReadFromJsonAsync<TransactionDto>(Json);

        var response = await strangerClient.GetAsync($"/api/transactions/{tx!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Oddiy_foydalanuvchi_admin_panelga_kira_olmaydi()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("oddiy_user", "+998911112401");

        var response = await client.GetAsync("/api/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Oz_elonini_sotib_olishga_urinish_400_qaytaradi()
    {
        var (sellerClient, _) = await _factory.CreateAuthenticatedClientAsync("ozi_sotib", "+998911112501");

        var listing = await CreateListingAsync(sellerClient, "O'z e'lonim — Legend akkaunt sinov uchun");

        var response = await sellerClient.PostAsJsonAsync("/api/transactions/initiate",
            new InitiateTransactionRequest(listing.Id), Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Qisqa_sarlavhali_elon_validatsiyadan_otmaydi()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("validator_test", "+998911112601");

        var response = await client.PostAsJsonAsync("/api/listings", new CreateListingRequest(
            GameType.EFootball, ListingType.Account, "Qisqa", "Juda qisqa tavsif", 850_000, "ASIA", "Dream League",
            null, null, null), Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<ListingDetailDto> CreateListingAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/listings", new CreateListingRequest(
            GameType.PubgMobile,
            ListingType.Account,
            title,
            "Batafsil tavsif: akkaunt darajasi, skinlar va bog'langan pochta to'liq beriladi.",
            600_000,
            "ASIA",
            "Conqueror",
            null, null, null), Json);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ListingDetailDto>(Json)
               ?? throw new InvalidOperationException("E'lon yaratilmadi.");
    }
}
