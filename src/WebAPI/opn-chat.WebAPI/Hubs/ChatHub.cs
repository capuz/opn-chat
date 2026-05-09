using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using opn_chat.Application.Interfaces;
using opn_chat.Domain.Entities;

namespace opn_chat.WebAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IPresenceTracker _presenceTracker;

        public ChatHub(IChatService chatService, IPresenceTracker presenceTracker)
        {
            _chatService = chatService;
            _presenceTracker = presenceTracker;
        }

        public async Task SendMessage(string roomId, string content, string? replyToId = null, string? messageType = null)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            var type = messageType?.ToLowerInvariant() == "action"
                ? MessageType.Action
                : MessageType.Normal;

            var message = await _chatService.SaveMessageAsync(
                Guid.Parse(roomId),
                Guid.Parse(userId),
                content,
                replyToId != null ? Guid.Parse(replyToId) : null,
                type
            );

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new
            {
                Id = message.Id,
                UserId = userId,
                UserName = userName,
                Content = content,
                Type = type == MessageType.Action ? "action" : "normal",
                Timestamp = message.Timestamp,
                ReplyToId = replyToId,
                Badge = _presenceTracker.GetBadge(userId)
            });
        }

        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await Clients.Group(roomId).SendAsync("UserJoined", userId);
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await Clients.Group(roomId).SendAsync("UserLeft", userId);
        }

        public async Task TypingIndicator(string roomId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await Clients.Group(roomId).SendAsync("UserTyping", userId);
        }

        public override Task OnConnectedAsync() => base.OnConnectedAsync();
        public override Task OnDisconnectedAsync(Exception? exception) => base.OnDisconnectedAsync(exception);
    }
}
