using opn_chat.Application.DTOs;

namespace opn_chat.Application.DTOs
{
    public class PrivateMessageDto
    {
        public Guid Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public bool IsDeletedForEveryone { get; set; }
    }

    public class SendPrivateMessageDto
    {
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class ConversationDto
    {
        public Guid UserId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}
