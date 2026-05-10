using opn_chat.Domain.Entities;

namespace opn_chat.Domain.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }

    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByGoogleIdAsync(string googleId);
        Task<bool> NicknameExistsAsync(string nickname);
    }

    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
    }

    public interface IRoomRepository : IRepository<Room>
    {
        Task<IEnumerable<Room>> GetPublicRoomsAsync();
        Task<bool> IsUserMemberAsync(Guid roomId, Guid userId);
        Task<Room?> GetByNameAsync(string name);
        Task<int> CountCreatedTodayByUserAsync(Guid userId);
        Task<int> CountActiveByUserAsync(Guid userId);
        Task<IEnumerable<Room>> GetInactiveForArchivalAsync(DateTime cutoffDate);
    }

    public interface IRoomMemberRepository : IRepository<RoomMember>
    {
        Task<RoomMember?> GetUserRoomMemberAsync(Guid userId, Guid roomId);
        Task<IEnumerable<RoomMember>> GetRoomMembersAsync(Guid roomId);
        Task<bool> IsUserMemberAsync(Guid userId, Guid roomId);
    }

    public interface IMessageRepository : IRepository<Message>
    {
        Task<IEnumerable<Message>> GetRoomMessagesAsync(Guid roomId, int skip, int take);
    }

    public interface IPrivateMessageRepository : IRepository<PrivateMessage>
    {
        Task<IEnumerable<PrivateMessage>> GetConversationAsync(Guid user1Id, Guid user2Id, Guid requesterId, int skip, int take);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkConversationAsReadAsync(Guid senderId, Guid receiverId);
    }

    public interface ISystemSettingRepository
    {
        Task<string?> GetValueAsync(string key);
    }

    public interface ICommandPermissionRepository
    {
        Task<IEnumerable<CommandPermission>> GetAllAsync();
        Task<CommandPermission?> GetByNameAsync(string commandName);
        Task UpsertAsync(CommandPermission permission);
        Task UpsertManyAsync(IEnumerable<CommandPermission> permissions);
    }

    public interface IUnitOfWork
    {
        Task<int> CommitAsync();
    }
}
