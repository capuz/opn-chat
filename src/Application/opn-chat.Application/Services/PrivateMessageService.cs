using opn_chat.Application.DTOs;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;

namespace opn_chat.Application.Services
{
    public class PrivateMessageService : IPrivateMessageService
    {
        private readonly IPrivateMessageRepository _privateMessageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PrivateMessageService(
            IPrivateMessageRepository privateMessageRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _privateMessageRepository = privateMessageRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PrivateMessage?> SendMessageAsync(Guid senderId, SendPrivateMessageDto dto)
        {
            var message = new PrivateMessage
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            await _privateMessageRepository.AddAsync(message);
            await _unitOfWork.CommitAsync();

            // TODO: Send real-time notification via NotificationHub
            return message;
        }

        public async Task<IEnumerable<PrivateMessage>> GetConversationAsync(Guid userId1, Guid userId2, Guid requesterId, int skip = 0, int take = 50)
        {
            return await _privateMessageRepository.GetConversationAsync(userId1, userId2, requesterId, skip, take);
        }

        public async Task<(bool success, string? error, PrivateMessage? message)> DeleteMessageAsync(Guid messageId, Guid requesterId, bool forEveryone)
        {
            var message = await _privateMessageRepository.GetByIdAsync(messageId);
            if (message == null) return (false, "Message not found", null);

            var isSender = message.SenderId == requesterId;
            var isReceiver = message.ReceiverId == requesterId;
            if (!isSender && !isReceiver) return (false, "Unauthorized", null);

            if (forEveryone)
            {
                if (!isSender) return (false, "Only the sender can delete for everyone", null);
                if (message.IsDeletedForEveryone) return (false, "Already deleted", null);
                if (DateTime.UtcNow - message.Timestamp > TimeSpan.FromMinutes(15))
                    return (false, "Time window expired (15 min)", null);

                message.IsDeletedForEveryone = true;
                message.DeletedAt = DateTime.UtcNow;
            }
            else
            {
                if (isSender) message.IsDeletedBySender = true;
                else message.IsDeletedByReceiver = true;
                message.DeletedAt ??= DateTime.UtcNow;
            }

            await _privateMessageRepository.UpdateAsync(message);
            await _unitOfWork.CommitAsync();
            return (true, null, message);
        }

        public async Task<IEnumerable<ConversationDto>> GetRecentConversationsAsync(Guid userId)
        {
            var messages = await _privateMessageRepository.GetAllAsync();

            var conversations = messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new ConversationDto
                {
                    UserId = g.Key,
                    Nickname = g.First().SenderId == userId
                        ? g.First().Receiver?.Nickname ?? "Unknown"
                        : g.First().Sender?.Nickname ?? "Unknown",
                    AvatarUrl = g.First().SenderId == userId
                        ? g.First().Receiver?.AvatarUrl
                        : g.First().Sender?.AvatarUrl,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).First().Content,
                    LastMessageTime = g.OrderByDescending(m => m.Timestamp).First().Timestamp,
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            return conversations;
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _privateMessageRepository.GetUnreadCountAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(Guid messageId, Guid userId)
        {
            var message = await _privateMessageRepository.GetByIdAsync(messageId);
            if (message == null || message.ReceiverId != userId) return false;

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _privateMessageRepository.UpdateAsync(message);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task MarkConversationAsReadAsync(Guid partnerId, Guid currentUserId)
        {
            await _privateMessageRepository.MarkConversationAsReadAsync(partnerId, currentUserId);
            await _unitOfWork.CommitAsync();
        }
    }
}
