namespace opn_chat.Application.Interfaces
{
    public record BoostState(string RoomId, string UserId, DateTime ExpiresAt);

    public interface IBoostService
    {
        BoostState? GetActiveBoost();
        bool TryActivateBoost(string roomId, string userId, bool isAdmin, TimeSpan duration, out string? error);
    }
}
