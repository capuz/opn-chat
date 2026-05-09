using opn_chat.Application.DTOs;
using opn_chat.Domain.Entities;

namespace opn_chat.Application.Services
{
    public interface IPrivateMessageService
    {
        Task<PrivateMessage?> SendMessageAsync(Guid senderId, SendPrivateMessageDto dto);
        Task<IEnumerable<PrivateMessage>> GetConversationAsync(Guid userId1, Guid userId2, Guid requesterId, int skip = 0, int take = 50);
        Task<IEnumerable<ConversationDto>> GetRecentConversationsAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid messageId, Guid userId);
        Task<(bool success, string? error, PrivateMessage? message)> DeleteMessageAsync(Guid messageId, Guid requesterId, bool forEveryone);
        Task MarkConversationAsReadAsync(Guid partnerId, Guid currentUserId);
    }
}
