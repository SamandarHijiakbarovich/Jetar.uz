using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Interfaces;
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
    private readonly ICurrentUser _user;

    public AdminController(IAdminService admin, IEscrowService escrow, ICurrentUser user)
    {
        _admin = admin;
        _escrow = escrow;
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
    public async Task<ActionResult<object>> ResolveDispute(Guid id, ResolveDisputeRequest request, CancellationToken ct)
    {
        var tx = await _escrow.ResolveDisputeAsync(id, _user.RequireId(), request, ct);
        return Ok(new { transactionId = tx.Id, status = tx.Status.ToString() });
    }

    [HttpGet("settings")]
    public async Task<ActionResult<PlatformSettingsDto>> Settings(CancellationToken ct)
        => Ok(await _admin.GetSettingsAsync(ct));

    [HttpPost("users/{id:guid}/block")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BlockUser(Guid id, [FromQuery] bool blocked = true, CancellationToken ct = default)
    {
        await _admin.SetUserBlockedAsync(id, blocked, ct);
        return NoContent();
    }

    [HttpPost("listings/{id:guid}/verify")]
    public async Task<IActionResult> VerifyListing(Guid id, [FromQuery] bool verified = true, CancellationToken ct = default)
    {
        await _admin.SetListingVerifiedAsync(id, verified, ct);
        return NoContent();
    }
}
