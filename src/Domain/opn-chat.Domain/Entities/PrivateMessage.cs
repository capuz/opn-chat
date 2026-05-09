using System;

namespace opn_chat.Domain.Entities
{
    public class PrivateMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SenderId { get; set; }
        public User Sender { get; set; } = null!;
        public Guid ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public bool IsDeletedBySender { get; set; } = false;
        public bool IsDeletedByReceiver { get; set; } = false;
        public bool IsDeletedForEveryone { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
