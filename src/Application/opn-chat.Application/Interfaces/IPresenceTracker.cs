namespace opn_chat.Application.Interfaces
{
    public record OnlineUser(string Id, string Nickname, string? CountryCode, bool ShowFlag, string? AwayMessage, string? Badge);

    public interface IPresenceTracker
    {
        bool AddConnection(string userId, string connectionId, string nickname, string? countryCode, bool showFlag, string? badge);
        bool RemoveConnection(string userId, string connectionId);
        bool IsUserOnline(string userId);
        IReadOnlyList<OnlineUser> GetOnlineUsers();
        string? GetNickname(string userId);
        string? GetBadge(string userId);
        void UpdateFlag(string userId, string? countryCode, bool showFlag);
        void SetAway(string userId, string awayMessage);
        void ClearAway(string userId);
    }
}
