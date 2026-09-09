using Jetar.Domain.Common;
using Jetar.Domain.Enums;
using Jetar.API.Infrastructure;
using Jetar.Application.Contracts;
using Jetar.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Moderator,Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IEscrowService _escrow;
    private readonly IBoostService _boosts;
    private readonly ICurrentUser _user;

    public AdminController(IAdminService admin, IEscrowService escrow, IBoostService boosts, ICurrentUser user)
    {
        _admin = admin;
        _escrow = escrow;
        _boosts = boosts;
        _user = user;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> Stats(CancellationToken ct)
        => Ok(await _admin.GetStatsAsync(ct));

    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<AdminTransactionRowDto>>> Transactions(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _admin.GetTransactionsAsync(search, page, pageSize, ct));

    [HttpGet("disputes")]
    public async Task<ActionResult<IReadOnlyList<AdminDisputeRowDto>>> Disputes(
        [FromQuery] bool openOnly = true, CancellationToken ct = default)
        => Ok(await _admin.GetDisputesAsync(openOnly, ct));

    /// <summary>11 — nizo bo'yicha qaror. favourBuyer=true bo'lsa pul xaridorga qaytariladi.</summary>
    [HttpPost("disputes/{id:guid}/resolve")]
    [EscrowGate] // Pul harakati — escrow o'chirilgan bo'lsa 410.
    public async Task<ActionResult<object>> ResolveDispute(Guid id, ResolveDisputeRequest request, CancellationToken ct)
    {
        var tx = await _escrow.ResolveDisputeAsync(id, _user.RequireId(), request, ct);
        return Ok(new { transactionId = tx.Id, status = tx.Status.ToString() });
    }

    [HttpGet("settings")]
    public async Task<ActionResult<PlatformSettingsDto>> Settings(CancellationToken ct)
        => Ok(await _admin.GetSettingsAsync(ct));

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserRowDto>>> Users(
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] bool? blocked,
        [FromQuery] bool? verified,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _admin.GetUsersAsync(search, role, blocked, verified, page, pageSize, ct));

    [HttpPost("users/{id:guid}/block")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BlockUser(Guid id, [FromQuery] bool blocked = true, CancellationToken ct = default)
    {
        await _admin.SetUserBlockedAsync(id, blocked, ct);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/verify")]
    public async Task<IActionResult> VerifyUser(Guid id, [FromQuery] bool verified = true, CancellationToken ct = default)
    {
        await _admin.SetUserVerifiedAsync(id, verified, ct);
        return NoContent();
    }

    /// <summary>Rol tayinlash (moderator qilish) — faqat admin.</summary>
    [HttpPost("users/{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetUserRole(Guid id, [FromQuery] UserRole role, CancellationToken ct = default)
    {
        await _admin.SetUserRoleAsync(id, role, ct);
        return NoContent();
    }

    [HttpPost("listings/{id:guid}/verify")]
    public async Task<IActionResult> VerifyListing(Guid id, [FromQuery] bool verified = true, CancellationToken ct = default)
    {
        await _admin.SetListingVerifiedAsync(id, verified, ct);
        return NoContent();
    }

    // ── Pullik ko'tarish (TOP/VIP) ────────────────────────────────────────────

    /// <summary>Ko'rib chiqilishi kutilayotgan boost so'rovlari.</summary>
    [HttpGet("boosts")]
    public async Task<ActionResult<IReadOnlyList<BoostRequestDto>>> Boosts(CancellationToken ct)
        => Ok(await _boosts.GetPendingAsync(ct));

    /// <summary>To'lov tasdiqlandi — e'lonni ko'taradi.</summary>
    [HttpPost("boosts/{id:guid}/approve")]
    public async Task<ActionResult<BoostRequestDto>> ApproveBoost(Guid id, CancellationToken ct)
        => Ok(await _boosts.ApproveAsync(id, _user.RequireId(), ct));

    /// <summary>Boost so'rovini rad etadi.</summary>
    [HttpPost("boosts/{id:guid}/reject")]
    public async Task<ActionResult<BoostRequestDto>> RejectBoost(Guid id, RejectBoostRequest request, CancellationToken ct)
        => Ok(await _boosts.RejectAsync(id, _user.RequireId(), request.Note, ct));
}
