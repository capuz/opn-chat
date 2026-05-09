using opn_chat.Domain.Entities;

namespace opn_chat.Application.Interfaces
{
    public interface IChatService
    {
        Task<Message> SaveMessageAsync(Guid roomId, Guid userId, string content, Guid? replyToId, MessageType type = MessageType.Normal);
    }
}
