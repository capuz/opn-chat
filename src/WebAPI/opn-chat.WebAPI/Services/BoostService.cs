using Microsoft.AspNetCore.SignalR;
using opn_chat.Application.Interfaces;
using opn_chat.WebAPI.Hubs;

namespace opn_chat.WebAPI.Services
{
    public class BoostService : IBoostService
    {
        private readonly IHubContext<ChatHub> _hub;
        private BoostState? _active;
        private Timer? _timer;
        private readonly object _lock = new();

        public BoostService(IHubContext<ChatHub> hub)
        {
            _hub = hub;
        }

        public BoostState? GetActiveBoost()
        {
            lock (_lock) return _active;
        }

        public bool TryActivateBoost(string roomId, string userId, bool isAdmin, TimeSpan duration, out string? error)
        {
            lock (_lock)
            {
                if (_active != null && !isAdmin)
                {
                    error = "boost_active";
                    return false;
                }
                _timer?.Dispose();
                _active = new BoostState(roomId, userId, DateTime.UtcNow.Add(duration));
                _timer = new Timer(OnBoostExpired, roomId, duration, Timeout.InfiniteTimeSpan);
                error = null;
                return true;
            }
        }

        private void OnBoostExpired(object? state)
        {
            string roomId;
            lock (_lock)
            {
                roomId = (string)(state ?? _active?.RoomId ?? "");
                _active = null;
                _timer?.Dispose();
                _timer = null;
            }
            if (!string.IsNullOrEmpty(roomId))
                _ = _hub.Clients.All.SendAsync("RoomBoostExpired", new { roomId });
        }
    }
}
