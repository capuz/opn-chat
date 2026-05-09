using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using opn_chat.Application.Interfaces;
using opn_chat.Domain.Interfaces;

namespace opn_chat.WebAPI.Hubs
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly IPresenceTracker _presenceTracker;
        private readonly IUserRepository _userRepository;

        public PresenceHub(IPresenceTracker presenceTracker, IUserRepository userRepository)
        {
            _presenceTracker = presenceTracker;
            _userRepository = userRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
                var nickname = user?.Nickname ?? "User";
                var countryCode = user?.ShowFlag == true ? user?.CountryCode : null;
                var showFlag = user?.ShowFlag ?? false;
                var badge = user?.GlobalBadge;

                var isFirstConnection = _presenceTracker.AddConnection(userId, Context.ConnectionId, nickname, countryCode, showFlag, badge);
                if (isFirstConnection)
                    await Clients.Others.SendAsync("UserOnline", new { id = userId, nickname, countryCode, showFlag, badge });
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var isLastConnection = _presenceTracker.RemoveConnection(userId, Context.ConnectionId);
                if (isLastConnection)
                    await Clients.Others.SendAsync("UserOffline", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinPresenceRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"presence-room-{roomId}");
            await Clients.Caller.SendAsync("OnlineUsersList", _presenceTracker.GetOnlineUsers());
        }

        public async Task LeavePresenceRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"presence-room-{roomId}");
        }

        public async Task SetAway(string message)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            _presenceTracker.SetAway(userId, message);
            await Clients.All.SendAsync("UserAwayUpdated", new { userId, awayMessage = message });
        }

        public async Task ClearAway()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            _presenceTracker.ClearAway(userId);
            await Clients.All.SendAsync("UserAwayUpdated", new { userId, awayMessage = (string?)null });
        }
    }
}
