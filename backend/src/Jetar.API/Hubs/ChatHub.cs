using Jetar.Core.Contracts;
using Jetar.Core.Interfaces;
using Jetar.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jetar.API.Hubs;

/// <summary>
/// Bitim chati. Klient <c>JoinTransaction(transactionId)</c> chaqiradi va
/// <c>ReceiveMessage</c> hodisasiga obuna bo'ladi.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chat;
    private readonly ICurrentUser _user;

    public ChatHub(IChatService chat, ICurrentUser user)
    {
        _chat = chat;
        _user = user;
    }

    public static string GroupName(Guid transactionId) => $"tx:{transactionId}";

    public async Task JoinTransaction(Guid transactionId)
    {
        // Kirish huquqini tekshiradi — huquq bo'lmasa AppException tashlanadi.
        await _chat.GetThreadAsync(transactionId, _user.RequireId(), _user.IsModerator);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(transactionId));
    }

    public Task LeaveTransaction(Guid transactionId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(transactionId));

    public async Task<MessageDto> SendMessage(Guid transactionId, string text)
        => await _chat.SendAsync(_user.RequireId(), new SendMessageRequest(transactionId, text, null));

    public Task MarkRead(Guid transactionId)
        => _chat.MarkReadAsync(transactionId, _user.RequireId());
}

/// <summary>Infrastructure qatlamidagi chatni SignalR hubiga ulaydi.</summary>
public class SignalRChatBroadcaster : IChatBroadcaster
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRChatBroadcaster(IHubContext<ChatHub> hub) => _hub = hub;

    public Task BroadcastAsync(Guid transactionId, MessageDto message, CancellationToken ct = default)
        => _hub.Clients.Group(ChatHub.GroupName(transactionId)).SendAsync("ReceiveMessage", message, ct);
}
