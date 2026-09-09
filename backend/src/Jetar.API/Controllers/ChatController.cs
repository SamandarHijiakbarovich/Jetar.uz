using Jetar.API.Infrastructure;
using Jetar.Application.Contracts;
using Jetar.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
[EscrowGate] // Escrow o'chirilgan — bitim chati 410 qaytaradi (kod saqlanadi).
public class ChatController : ControllerBase
{
    private readonly IChatService _chat;
    private readonly ICurrentUser _user;

    public ChatController(IChatService chat, ICurrentUser user)
    {
        _chat = chat;
        _user = user;
    }

    /// <summary>Bitim bo'yicha yozishma tarixi.</summary>
    [HttpGet("{transactionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> Thread(Guid transactionId, CancellationToken ct)
        => Ok(await _chat.GetThreadAsync(transactionId, _user.RequireId(), _user.IsModerator, ct));

    /// <summary>Xabar yuborish (SignalR ulanmagan klientlar uchun).</summary>
    [HttpPost("send")]
    public async Task<ActionResult<MessageDto>> Send(SendMessageRequest request, CancellationToken ct)
        => Ok(await _chat.SendAsync(_user.RequireId(), request, ct));

    [HttpPost("{transactionId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid transactionId, CancellationToken ct)
    {
        await _chat.MarkReadAsync(transactionId, _user.RequireId(), ct);
        return NoContent();
    }
}
