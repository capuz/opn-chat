using System.Collections.Concurrent;
using opn_chat.Application.Interfaces;

namespace opn_chat.Application.Services
{
    public class PresenceTracker : IPresenceTracker
    {
        private sealed record PresenceEntry(HashSet<string> Connections, string Nickname, string? CountryCode, bool ShowFlag, string? AwayMessage, string? Badge);

        private readonly ConcurrentDictionary<string, PresenceEntry> _users = new();

        public bool AddConnection(string userId, string connectionId, string nickname, string? countryCode, bool showFlag, string? badge)
        {
            var entry = _users.GetOrAdd(userId, _ => new PresenceEntry(new HashSet<string>(), nickname, countryCode, showFlag, null, badge));
            lock (entry.Connections)
            {
                entry.Connections.Add(connectionId);
                return entry.Connections.Count == 1;
            }
        }

        public bool RemoveConnection(string userId, string connectionId)
        {
            if (!_users.TryGetValue(userId, out var entry)) return false;

            lock (entry.Connections)
            {
                entry.Connections.Remove(connectionId);
                if (entry.Connections.Count > 0) return false;
            }

            _users.TryRemove(userId, out _);
            return true;
        }

        public bool IsUserOnline(string userId) =>
            _users.TryGetValue(userId, out var e) && e.Connections.Count > 0;

        public IReadOnlyList<OnlineUser> GetOnlineUsers() =>
            _users.Select(kv => new OnlineUser(kv.Key, kv.Value.Nickname, kv.Value.CountryCode, kv.Value.ShowFlag, kv.Value.AwayMessage, kv.Value.Badge)).ToList();

        public string? GetNickname(string userId) =>
            _users.TryGetValue(userId, out var e) ? e.Nickname : null;

        public string? GetBadge(string userId) =>
            _users.TryGetValue(userId, out var e) ? e.Badge : null;

        public void UpdateFlag(string userId, string? countryCode, bool showFlag)
        {
            _users.AddOrUpdate(
                userId,
                _ => new PresenceEntry(new HashSet<string>(), string.Empty, countryCode, showFlag, null, null),
                (_, existing) => existing with { CountryCode = countryCode, ShowFlag = showFlag }
            );
        }

        public void SetAway(string userId, string awayMessage)
        {
            _users.AddOrUpdate(
                userId,
                _ => new PresenceEntry(new HashSet<string>(), string.Empty, null, false, awayMessage, null),
                (_, existing) => existing with { AwayMessage = awayMessage }
            );
        }

        public void ClearAway(string userId)
        {
            _users.AddOrUpdate(
                userId,
                _ => new PresenceEntry(new HashSet<string>(), string.Empty, null, false, null, null),
                (_, existing) => existing with { AwayMessage = null }
            );
        }
    }
}
