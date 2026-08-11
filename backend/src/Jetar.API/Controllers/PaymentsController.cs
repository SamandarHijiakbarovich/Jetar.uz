using Jetar.Application.Contracts;
using Jetar.Domain.Enums;
using Jetar.Application.Interfaces;
using Jetar.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ICurrentUser _user;
    private readonly PaymentOptions _options;

    public PaymentsController(IPaymentService payments, ICurrentUser user, IOptions<PaymentOptions> options)
    {
        _payments = payments;
        _user = user;
        _options = options.Value;
    }

    /// <summary>03 — to'lovni boshlash. Javobda provayder checkout URL'i qaytadi.</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> Create(CreatePaymentRequest request, CancellationToken ct)
        => Ok(await _payments.CreateAsync(request.TransactionId, _user.RequireId(), request.Method, ct));

    /// <summary>Mavjud to'lov usullari — o'chirilganlari ro'yxatga tushmaydi.</summary>
    [HttpGet("methods")]
    public ActionResult<object> Methods()
    {
        var methods = new[]
        {
            new { key = nameof(PaymentMethod.Click),   name = "Click",      brand = "#00A3E0", note = "Bir zumda, komissiyasiz", enabled = _options.Click.Enabled },
            new { key = nameof(PaymentMethod.Payme),   name = "Payme",      brand = "#33CCCC", note = "Karta yoki hisob orqali", enabled = _options.Payme.Enabled },
            new { key = nameof(PaymentMethod.Uzum),    name = "Uzum Bank",  brand = "#7000FF", note = "Uzum ilovasi orqali",     enabled = _options.Uzum.Enabled },
            new { key = nameof(PaymentMethod.Apelsin), name = "Apelsin",    brand = "#FF6B35", note = "Apelsin hamyoni",         enabled = _options.Apelsin.Enabled }
        };

        return Ok(new
        {
            sandbox = _options.SandboxMode,
            providerRedirect = _options.ProviderRedirect,
            methods = methods.Where(m => m.enabled)
        });
    }

    /// <summary>Click callback.</summary>
    [HttpPost("click")]
    [AllowAnonymous]
    public Task<IActionResult> ClickCallback(CancellationToken ct) => HandleCallback(PaymentMethod.Click, ct);

    /// <summary>Payme merchant API callback.</summary>
    [HttpPost("payme")]
    [AllowAnonymous]
    public Task<IActionResult> PaymeCallback(CancellationToken ct) => HandleCallback(PaymentMethod.Payme, ct);

    [HttpPost("uzum")]
    [AllowAnonymous]
    public Task<IActionResult> UzumCallback(CancellationToken ct) => HandleCallback(PaymentMethod.Uzum, ct);

    [HttpPost("apelsin")]
    [AllowAnonymous]
    public Task<IActionResult> ApelsinCallback(CancellationToken ct) => HandleCallback(PaymentMethod.Apelsin, ct);

    /// <summary>
    /// Sandbox rejimida to'lovni qo'lda tasdiqlash. Haqiqiy provayder ulanganda bu endpoint o'chadi.
    /// </summary>
    [HttpPost("sandbox/confirm")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> SandboxConfirm(SandboxConfirmRequest request, CancellationToken ct)
    {
        if (!_options.SandboxMode)
            return NotFound(new { code = "not_found", message = "Sandbox rejimi o'chirilgan." });

        _user.RequireId();
        return Ok(await _payments.SandboxConfirmAsync(request.ProviderPaymentId, request.Success, ct));
    }

    private async Task<IActionResult> HandleCallback(PaymentMethod method, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var ok = await _payments.HandleCallbackAsync(method, body, headers, ct);

        // Provayderlar 200 kutadi; xatolik javob tanasida qaytariladi.
        return Ok(new { error = ok ? 0 : -1, error_note = ok ? "OK" : "Rejected" });
    }
}
