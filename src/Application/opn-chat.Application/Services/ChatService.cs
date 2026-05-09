using opn_chat.Application.Interfaces;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;

namespace opn_chat.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IMessageRepository messageRepository, IUnitOfWork unitOfWork)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Message> SaveMessageAsync(Guid roomId, Guid userId, string content, Guid? replyToId, MessageType type = MessageType.Normal)
        {
            var message = new Message
            {
                RoomId = roomId,
                UserId = userId,
                Content = content,
                Type = type,
                ReplyToId = replyToId,
                Timestamp = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(message);
            await _unitOfWork.CommitAsync();
            return message;
        }
    }
}
