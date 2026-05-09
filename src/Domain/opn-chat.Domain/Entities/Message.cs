using System;

namespace opn_chat.Domain.Entities
{
    public enum MessageType { Normal = 0, Action = 1 }

    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RoomId { get; set; }
        public Room Room { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Normal;
        public Guid? ReplyToId { get; set; }
        public Message? ReplyTo { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
    }
}
