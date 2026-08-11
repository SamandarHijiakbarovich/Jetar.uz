using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jetar.Application.Contracts;
using Jetar.Domain.Entities;
using Jetar.Domain.Enums;
using Jetar.Application.Interfaces;
using Jetar.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jetar.Infrastructure.External;

/// <summary>
/// Barcha provayderlar uchun umumiy asos. Sandbox rejimida tarmoqqa chiqmaydi —
/// to'lov ichki identifikator bilan yaratiladi va <c>/api/payments/sandbox/confirm</c> orqali tasdiqlanadi.
/// </summary>
public abstract class PaymentGatewayBase : IPaymentGateway
{
    protected readonly PaymentOptions Options;
    protected readonly ILogger Log;

    protected PaymentGatewayBase(IOptions<PaymentOptions> options, ILogger log)
    {
        Options = options.Value;
        Log = log;
    }

    public abstract PaymentMethod Method { get; }

    protected abstract PaymentOptions.ProviderCredentials Credentials { get; }

    /// <summary>Haqiqiy rejimda provayderning to'lov sahifasi manzilini quradi.</summary>
    protected abstract string BuildLiveCheckoutUrl(Payment payment, string providerPaymentId);

    public Task<PaymentInitResult> InitiateAsync(Payment payment, CancellationToken ct = default)
    {
        if (!Credentials.Enabled)
            return Task.FromResult(new PaymentInitResult(false, "", null, $"{Method} hozircha o'chirilgan."));

        var providerPaymentId = $"{Method.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

        // Provayder sahifasiga yo'naltirish: sandbox bo'lsa ham checkout Click/Payme/Uzum
        // sahifasiga o'tadi. Merchant ID qo'yilgach shu yo'l bilan jonli to'lov ishlaydi.
        if (Options.ProviderRedirect)
        {
            var providerUrl = BuildLiveCheckoutUrl(payment, providerPaymentId);
            Log.LogInformation("{Method} provayder sahifasiga yo'naltirildi {Id} summa={Amount}",
                Method, providerPaymentId, payment.Amount);
            return Task.FromResult(new PaymentInitResult(true, providerPaymentId, providerUrl, null));
        }

        if (Options.SandboxMode)
        {
            var url = $"{Options.ReturnUrl}?provider={Method}&paymentId={providerPaymentId}&amount={payment.Amount:0}&sandbox=1";
            Log.LogInformation("[SANDBOX] {Method} to'lovi yaratildi {Id} summa={Amount}", Method, providerPaymentId, payment.Amount);
            return Task.FromResult(new PaymentInitResult(true, providerPaymentId, url, null));
        }

        if (string.IsNullOrWhiteSpace(Credentials.MerchantId))
            return Task.FromResult(new PaymentInitResult(false, "", null,
                $"{Method} uchun MerchantId sozlanmagan. .env faylini tekshiring."));

        return Task.FromResult(new PaymentInitResult(true, providerPaymentId,
            BuildLiveCheckoutUrl(payment, providerPaymentId), null));
    }

    public virtual Task<PaymentCallbackResult> HandleCallbackAsync(
        string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        // Umumiy shakl: { "paymentId": "...", "status": "paid" | "cancelled" }
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;

            var id = TryGetString(root, "paymentId") ?? TryGetString(root, "payment_id") ?? "";
            var status = (TryGetString(root, "status") ?? "").ToLowerInvariant();

            return Task.FromResult(new PaymentCallbackResult(
                IsPaid: status is "paid" or "success" or "1",
                IsCancelled: status is "cancelled" or "canceled" or "failed",
                ProviderPaymentId: id,
                Error: string.IsNullOrEmpty(id) ? "paymentId topilmadi." : null));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new PaymentCallbackResult(false, false, "", $"Callback JSON xato: {ex.Message}"));
        }
    }

    public virtual Task<bool> RefundAsync(Payment payment, CancellationToken ct = default)
    {
        if (Options.SandboxMode)
        {
            Log.LogInformation("[SANDBOX] {Method} to'lovi qaytarildi {Id}", Method, payment.ProviderPaymentId);
            return Task.FromResult(true);
        }

        // Haqiqiy qaytarish provayder kabinetidan yoki reversal API orqali amalga oshiriladi.
        Log.LogWarning("{Method} uchun avtomatik refund hali ulanmagan — qo'lda bajarilsin ({Id})",
            Method, payment.ProviderPaymentId);
        return Task.FromResult(false);
    }

    protected static string? TryGetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) ? el.ToString() : null;

    protected static string Md5(string input)
        => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
}

/// <summary>Click — my.click.uz to'lov sahifasi.</summary>
public class ClickGateway : PaymentGatewayBase
{
    public ClickGateway(IOptions<PaymentOptions> o, ILogger<ClickGateway> log) : base(o, log) { }

    public override PaymentMethod Method => PaymentMethod.Click;
    protected override PaymentOptions.ProviderCredentials Credentials => Options.Click;

    protected override string BuildLiveCheckoutUrl(Payment payment, string providerPaymentId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(Credentials.BaseUrl)
            ? "https://my.click.uz/services/pay"
            : Credentials.BaseUrl;

        return $"{baseUrl}?service_id={Credentials.MerchantId}" +
               $"&merchant_id={Credentials.MerchantId}" +
               $"&amount={payment.Amount:0}" +
               $"&transaction_param={providerPaymentId}" +
               $"&return_url={Uri.EscapeDataString(Options.ReturnUrl)}";
    }

    public override Task<PaymentCallbackResult> HandleCallbackAsync(
        string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        if (Options.SandboxMode) return base.HandleCallbackAsync(rawPayload, headers, ct);

        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var r = doc.RootElement;

            var clickTransId = TryGetString(r, "click_trans_id") ?? "";
            var serviceId = TryGetString(r, "service_id") ?? "";
            var merchantTransId = TryGetString(r, "merchant_trans_id") ?? "";
            var amount = TryGetString(r, "amount") ?? "";
            var action = TryGetString(r, "action") ?? "";
            var signTime = TryGetString(r, "sign_time") ?? "";
            var signString = TryGetString(r, "sign_string") ?? "";
            var error = TryGetString(r, "error") ?? "0";

            // Click imzo formulasi: md5(click_trans_id + service_id + SECRET_KEY +
            //                          merchant_trans_id + amount + action + sign_time)
            var expected = Md5($"{clickTransId}{serviceId}{Credentials.SecretKey}{merchantTransId}{amount}{action}{signTime}");

            if (!string.Equals(expected, signString, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new PaymentCallbackResult(false, false, merchantTransId, "Imzo mos kelmadi."));

            var failed = error != "0";
            // action=1 — "complete" bosqichi, ya'ni pul yechildi.
            var paid = !failed && action == "1";

            return Task.FromResult(new PaymentCallbackResult(paid, failed, merchantTransId, failed ? $"Click xato: {error}" : null));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new PaymentCallbackResult(false, false, "", $"Callback JSON xato: {ex.Message}"));
        }
    }
}

/// <summary>Payme — checkout.paycom.uz (base64 kodlangan parametrlar).</summary>
public class PaymeGateway : PaymentGatewayBase
{
    public PaymeGateway(IOptions<PaymentOptions> o, ILogger<PaymeGateway> log) : base(o, log) { }

    public override PaymentMethod Method => PaymentMethod.Payme;
    protected override PaymentOptions.ProviderCredentials Credentials => Options.Payme;

    protected override string BuildLiveCheckoutUrl(Payment payment, string providerPaymentId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(Credentials.BaseUrl)
            ? "https://checkout.paycom.uz"
            : Credentials.BaseUrl;

        // Payme summani tiyinda kutadi.
        var tiyin = (long)(payment.Amount * 100);
        var raw = $"m={Credentials.MerchantId};ac.order_id={providerPaymentId};a={tiyin};c={Options.ReturnUrl}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));

        return $"{baseUrl}/{encoded}";
    }

    public override Task<PaymentCallbackResult> HandleCallbackAsync(
        string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        if (Options.SandboxMode) return base.HandleCallbackAsync(rawPayload, headers, ct);

        // Payme Merchant API — JSON-RPC. Avtorizatsiya: Basic base64("Paycom:SECRET_KEY").
        headers.TryGetValue("Authorization", out var auth);
        var expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"Paycom:{Credentials.SecretKey}"));

        if (!string.Equals(auth, expected, StringComparison.Ordinal))
            return Task.FromResult(new PaymentCallbackResult(false, false, "", "Avtorizatsiya xato."));

        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;

            var method = TryGetString(root, "method") ?? "";
            var orderId = root.TryGetProperty("params", out var p) && p.TryGetProperty("account", out var acc)
                ? TryGetString(acc, "order_id") ?? ""
                : "";

            return Task.FromResult(new PaymentCallbackResult(
                IsPaid: method == "PerformTransaction",
                IsCancelled: method == "CancelTransaction",
                ProviderPaymentId: orderId,
                Error: string.IsNullOrEmpty(orderId) ? "order_id topilmadi." : null));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(new PaymentCallbackResult(false, false, "", $"Callback JSON xato: {ex.Message}"));
        }
    }
}

/// <summary>Uzum Bank — hozircha sandbox; jonli integratsiya shartnomadan keyin ulanadi.</summary>
public class UzumGateway : PaymentGatewayBase
{
    public UzumGateway(IOptions<PaymentOptions> o, ILogger<UzumGateway> log) : base(o, log) { }

    public override PaymentMethod Method => PaymentMethod.Uzum;
    protected override PaymentOptions.ProviderCredentials Credentials => Options.Uzum;

    protected override string BuildLiveCheckoutUrl(Payment payment, string providerPaymentId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(Credentials.BaseUrl)
            ? "https://checkout.uzumbank.uz/pay"
            : Credentials.BaseUrl;

        return $"{baseUrl}?merchant={Credentials.MerchantId}&order={providerPaymentId}&amount={payment.Amount:0}";
    }
}

/// <summary>Apelsin hamyoni — hozircha sandbox.</summary>
public class ApelsinGateway : PaymentGatewayBase
{
    public ApelsinGateway(IOptions<PaymentOptions> o, ILogger<ApelsinGateway> log) : base(o, log) { }

    public override PaymentMethod Method => PaymentMethod.Apelsin;
    protected override PaymentOptions.ProviderCredentials Credentials => Options.Apelsin;

    protected override string BuildLiveCheckoutUrl(Payment payment, string providerPaymentId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(Credentials.BaseUrl)
            ? "https://apelsin.uz/pay"
            : Credentials.BaseUrl;

        return $"{baseUrl}?merchant={Credentials.MerchantId}&order={providerPaymentId}&amount={payment.Amount:0}";
    }
}
