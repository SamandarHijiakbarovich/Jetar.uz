using Jetar.Domain.Common;
using Jetar.API.Infrastructure;
using Jetar.Application.Contracts;
using Jetar.Application.Interfaces;
using Jetar.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
[EscrowGate] // Escrow o'chirilgan — butun controller 410 qaytaradi (kod saqlanadi).
public class TransactionsController : ControllerBase
{
    private readonly IEscrowService _escrow;
    private readonly ITransactionService _transactions;
    private readonly ICurrentUser _user;

    public TransactionsController(
        IEscrowService escrow,
        ITransactionService transactions,
        ICurrentUser user)
    {
        _escrow = escrow;
        _transactions = transactions;
        _user = user;
    }

    /// <summary>Mening bitimlarim. role = buyer | seller | (bo'sh — hammasi).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionDto>>> List(
        [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _transactions.ListForUserAsync(_user.RequireId(), role, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> Get(Guid id, CancellationToken ct)
        => Ok(await _transactions.GetAsync(id, _user.RequireId(), _user.IsModerator, ct));

    /// <summary>02 — escrow bitimini ochish.</summary>
    [HttpPost("initiate")]
    public async Task<ActionResult<TransactionDto>> Initiate(InitiateTransactionRequest request, CancellationToken ct)
    {
        var tx = await _escrow.InitiateAsync(request.ListingId, _user.RequireId(), ct);
        return Ok(await _transactions.GetAsync(tx.Id, _user.RequireId(), false, ct));
    }

    /// <summary>05 — sotuvchi akkaunt ma'lumotlarini yuborganini belgilaydi.</summary>
    [HttpPost("{id:guid}/credentials-sent")]
    public async Task<ActionResult<TransactionDto>> CredentialsSent(Guid id, CancellationToken ct)
    {
        await _escrow.MarkCredentialsSentAsync(id, _user.RequireId(), ct);
        return Ok(await _transactions.GetAsync(id, _user.RequireId(), false, ct));
    }

    /// <summary>07–08 — xaridor tasdiqlaydi, pul sotuvchiga chiqariladi.</summary>
    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<TransactionDto>> Release(Guid id, CancellationToken ct)
    {
        await _escrow.ReleaseAsync(id, _user.RequireId(), ct);
        return Ok(await _transactions.GetAsync(id, _user.RequireId(), false, ct));
    }

    /// <summary>09 — nizo ochish.</summary>
    [HttpPost("{id:guid}/dispute")]
    public async Task<ActionResult<DisputeDto>> Dispute(Guid id, OpenDisputeRequest request, CancellationToken ct)
    {
        var dispute = await _escrow.OpenDisputeAsync(id, _user.RequireId(), request, ct);

        return Ok(new DisputeDto(
            dispute.Id, dispute.TransactionId, 0, dispute.Reason, dispute.EvidenceUrls,
            dispute.Status, _user.Username ?? "—", dispute.ResolutionNote, dispute.CreatedAt, dispute.ResolvedAt));
    }

    /// <summary>To'lovgacha bitimni bekor qilish.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<TransactionDto>> Cancel(Guid id, CancelTransactionRequest request, CancellationToken ct)
    {
        await _escrow.CancelAsync(id, _user.RequireId(), request.Reason, ct);
        return Ok(await _transactions.GetAsync(id, _user.RequireId(), false, ct));
    }
}
